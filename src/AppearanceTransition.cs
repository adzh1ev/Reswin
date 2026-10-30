using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Reswin {
    // Only the old pixels are temporary. The actual controls, focus, settings and
    // scroll positions survive both palette changes and language changes.
    internal static class AppearanceTransition {
        const int MaxSnapshotPixels=1500000;
        static readonly DependencyProperty StateProperty=DependencyProperty.RegisterAttached(
            "State",typeof(TransitionState),typeof(AppearanceTransition),new PropertyMetadata(null));

        internal static AdornerDecorator CreateHost(UIElement content) {
            return new AdornerDecorator {Child=content,ClipToBounds=true};
        }
        internal static void Run(Window window,Action apply,bool radial) {
            if(apply==null) throw new ArgumentNullException("apply");
            var host=window==null?null:window.Content as AdornerDecorator;
            if(host==null) {apply(); return;}
            host.VerifyAccess();
            var state=host.GetValue(StateProperty) as TransitionState;
            if(state==null) {state=new TransitionState(window,host); host.SetValue(StateProperty,state);}
            state.Enqueue(apply,radial);
        }
        static BitmapSource Capture(AdornerDecorator host) {
            double width=host.ActualWidth,height=host.ActualHeight;
            if(double.IsNaN(width) || double.IsInfinity(width) || double.IsNaN(height) || double.IsInfinity(height)) return null;
            var source=PresentationSource.FromVisual(host);
            if(source==null || source.CompositionTarget==null) return null;
            double dpi=Math.Max(source.CompositionTarget.TransformToDevice.M11,source.CompositionTarget.TransformToDevice.M22);
            // A single bounded bitmap is retained only during the transition.
            // Native DPI is kept at ordinary sizes; high DPI cannot allocate huge images.
            double scale=Math.Min(dpi,Math.Min(2048/Math.Max(width,height),Math.Sqrt(MaxSnapshotPixels/(width*height))));
            if(scale<=0 || double.IsNaN(scale) || double.IsInfinity(scale)) return null;
            try {
                var bitmap=new RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(width*scale)),Math.Max(1,(int)Math.Ceiling(height*scale)),96*scale,96*scale,PixelFormats.Pbgra32);
                bitmap.Render(host); bitmap.Freeze(); return bitmap;
            } catch(InvalidOperationException) {return null;}
            catch(ArgumentException) {return null;}
            catch(System.Runtime.InteropServices.COMException) {return null;}
        }

        sealed class TransitionState {
            readonly Window window;
            readonly AdornerDecorator host;
            SnapshotAdorner overlay;
            Action pendingTheme,pendingLanguage;
            long sequence,themeOrder,languageOrder;
            bool scheduled,flushing,invalidated;
            int generation;
            internal TransitionState(Window window,AdornerDecorator host) {
                this.window=window; this.host=host;
                host.Unloaded+=delegate {Invalidate();};
                host.IsVisibleChanged+=delegate {if(!host.IsVisible) Invalidate();};
                host.SizeChanged+=delegate {Invalidate();};
                window.StateChanged+=delegate {if(window.WindowState==WindowState.Minimized) Invalidate();};
            }
            bool CanAnimate {
                get {
                    if(!window.IsVisible || window.WindowState==WindowState.Minimized || !host.IsLoaded ||
                        host.ActualWidth<1 || host.ActualHeight<1 || host.Child==null) return false;
                    var source=PresentationSource.FromVisual(host);
                    return source!=null && source.CompositionTarget!=null;
                }
            }
            bool HasPending {get {return pendingTheme!=null || pendingLanguage!=null;}}
            internal void Enqueue(Action apply,bool radial) {
                // Independent slots prevent a theme request from erasing a
                // language request made before the same display frame.
                if(radial) {pendingTheme=apply; themeOrder=++sequence;}
                else {pendingLanguage=apply; languageOrder=++sequence;}
                if(flushing) return;
                if(!CanAnimate) Flush(false);
                else Schedule();
            }
            void Schedule() {
                if(scheduled) return;
                scheduled=true; CompositionTarget.Rendering+=Render;
            }
            void Detach() {
                if(!scheduled) return;
                scheduled=false; CompositionTarget.Rendering-=Render;
            }
            void Render(object sender,EventArgs args) {Flush(true);}
            void Invalidate() {
                invalidated=true; Cancel();
                // Minimized/hidden windows do not receive render ticks. Apply
                // their latest values now rather than showing stale UI on reopen.
                if(!flushing) Flush(false);
            }
            void Flush(bool animate) {
                if(flushing) return;
                Detach();
                if(!HasPending) return;
                var theme=pendingTheme; var language=pendingLanguage;
                long first=themeOrder,second=languageOrder;
                pendingTheme=null; pendingLanguage=null;
                flushing=true; invalidated=false;
                try {
                    Size oldSize=host.RenderSize;
                    // Capture the current composite, including an in-flight
                    // overlay, only once for all requests in this render frame.
                    var bitmap=animate && CanAnimate?Capture(host):null;
                    Cancel();
                    if(theme!=null && language!=null) {
                        if(first<second) {theme(); language();} else {language(); theme();}
                    } else if(theme!=null) theme();
                    else language();
                    if(bitmap!=null && !invalidated && CanAnimate && host.RenderSize==oldSize)
                        Show(bitmap,theme!=null);
                } finally {
                    flushing=false;
                    if(HasPending) {
                        if(CanAnimate) Schedule(); else Flush(false);
                    }
                }
            }
            internal void Show(BitmapSource bitmap,bool radial) {
                var target=host.Child;
                if(target==null) return;
                var layer=host.AdornerLayer;
                if(layer==null) return;
                var current=new SnapshotAdorner(target,bitmap,host.RenderSize);
                overlay=current; int version=++generation;
                layer.Add(current);
                if(radial) {
                    var size=host.RenderSize;
                    double radius=Math.Sqrt(size.Width*size.Width+size.Height*size.Height)/2+2;
                    var hole=new EllipseGeometry(new Point(size.Width/2,size.Height/2),0,0);
                    current.Hole=hole;
                    current.Clip=new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(size)),hole);
                    var x=new DoubleAnimation(0,radius,TimeSpan.FromMilliseconds(300)) {EasingFunction=Interaction.EaseOut};
                    var y=x.Clone();
                    y.Completed+=delegate {if(version==generation) Cancel();};
                    hole.BeginAnimation(EllipseGeometry.RadiusXProperty,x);
                    hole.BeginAnimation(EllipseGeometry.RadiusYProperty,y);
                } else {
                    var fade=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(200)) {EasingFunction=Interaction.EaseOut};
                    fade.Completed+=delegate {if(version==generation) Cancel();};
                    current.BeginAnimation(UIElement.OpacityProperty,fade);
                }
            }
            internal void Cancel() {
                generation++;
                if(overlay==null) return;
                host.AdornerLayer.Remove(overlay);
                overlay.Release(); overlay=null;
            }
        }
        sealed class SnapshotAdorner : Adorner {
            BitmapSource bitmap;
            readonly Size size;
            internal EllipseGeometry Hole;
            internal SnapshotAdorner(UIElement target,BitmapSource bitmap,Size size) : base(target) {
                this.bitmap=bitmap; this.size=size;
                IsHitTestVisible=false; Focusable=false;
            }
            protected override Size MeasureOverride(Size constraint) {return size;}
            protected override void OnRender(DrawingContext drawing) {
                if(bitmap!=null) drawing.DrawImage(bitmap,new Rect(size));
            }
            internal void Release() {
                BeginAnimation(UIElement.OpacityProperty,null);
                if(Hole!=null) {
                    Hole.BeginAnimation(EllipseGeometry.RadiusXProperty,null);
                    Hole.BeginAnimation(EllipseGeometry.RadiusYProperty,null); Hole=null;
                }
                Clip=null; bitmap=null;
            }
        }
    }
}
