using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Reswin {
    internal static class Icons {
        const int AppIconCacheLimit=128;
        static readonly object appIconLock=new object();
        static readonly Dictionary<string,IconEntry> appIcons=new Dictionary<string,IconEntry>(StringComparer.OrdinalIgnoreCase);
        static readonly LinkedList<string> appIconOrder=new LinkedList<string>();
        static readonly Dictionary<string,Task<ImageSource>> pendingIcons=new Dictionary<string,Task<ImageSource>>(StringComparer.OrdinalIgnoreCase);
        static readonly SemaphoreSlim iconWorkers=new SemaphoreSlim(2);
        static readonly ConcurrentDictionary<string,System.Windows.Media.Geometry> vectors=new ConcurrentDictionary<string,System.Windows.Media.Geometry>(StringComparer.Ordinal);
        sealed class IconEntry {
            internal ImageSource Source;
            internal LinkedListNode<string> Order;
            internal DateTime RetryAfter;
        }
        // Shared lookups keep rapid picker/list refreshes from extracting the same
        // native icon repeatedly. Only frozen bitmaps cross the worker boundary.
        internal static Task<ImageSource> ApplicationIconAsync(string path) {
            if(string.IsNullOrWhiteSpace(path)) return Task.FromResult<ImageSource>(null);
            TaskCompletionSource<ImageSource> completion;
            lock(appIconLock) {
                IconEntry cached;
                if(appIcons.TryGetValue(path,out cached)) {
                    if(cached.Source!=null || DateTime.UtcNow<cached.RetryAfter) {
                        appIconOrder.Remove(cached.Order); appIconOrder.AddLast(cached.Order);
                        return Task.FromResult(cached.Source);
                    }
                    appIconOrder.Remove(cached.Order); appIcons.Remove(path);
                }
                Task<ImageSource> pending;
                if(pendingIcons.TryGetValue(path,out pending)) return pending;
                completion=new TaskCompletionSource<ImageSource>(TaskCreationOptions.RunContinuationsAsynchronously);
                pendingIcons.Add(path,completion.Task);
            }
            ThreadPool.QueueUserWorkItem(async delegate {
                ImageSource source=null;
                try {
                    await iconWorkers.WaitAsync().ConfigureAwait(false);
                    try {source=ExtractApplicationIcon(path);}
                    finally {iconWorkers.Release();}
                } catch { }
                lock(appIconLock) {
                    // Negative results are cached briefly as well, with the same
                    // bounded budget, so missing/inaccessible files do not churn.
                    var order=appIconOrder.AddLast(path);
                    appIcons[path]=new IconEntry {Source=source,Order=order,RetryAfter=DateTime.UtcNow.AddSeconds(30)};
                    while(appIcons.Count>AppIconCacheLimit) {
                        string oldest=appIconOrder.First.Value;
                        appIconOrder.RemoveFirst(); appIcons.Remove(oldest);
                    }
                    pendingIcons.Remove(path);
                }
                completion.TrySetResult(source);
            });
            return completion.Task;
        }
        // Compatibility for existing background enumerators; UI callers use the
        // async entry point and never wait for shell extraction on the dispatcher.
        internal static ImageSource ApplicationIcon(string path) {return ApplicationIconAsync(path).GetAwaiter().GetResult();}
        static ImageSource ExtractApplicationIcon(string path) {
            try {
                using(var icon=System.Drawing.Icon.ExtractAssociatedIcon(path)) {
                    if(icon==null) return null;
                    var bitmap=Imaging.CreateBitmapSourceFromHIcon(icon.Handle,Int32Rect.Empty,BitmapSizeOptions.FromWidthAndHeight(32,32)); bitmap.Freeze();
                    return bitmap;
                }
            } catch { return null; }
        }
        // The user's original SVG paths are embedded in the portable executable.
        internal static FrameworkElement Mouse(string file) {
            var canvas=new Canvas { Width=512,Height=512,IsHitTestVisible=false };
            var document=new XmlDocument();
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Reswin.Icons."+file)) document.Load(stream);
            foreach(XmlNode node in document.DocumentElement.ChildNodes) {
                var element=node as XmlElement; if(element==null) continue;
                Shape shape;
                if(element.LocalName=="path") shape=new Path { Data=System.Windows.Media.Geometry.Parse(element.GetAttribute("d")) };
                else if(element.LocalName=="rect") {
                    shape=new Rectangle { Width=Number(element,"width"),Height=Number(element,"height"),RadiusX=Number(element,"rx"),RadiusY=Number(element,"rx") };
                    Canvas.SetLeft(shape,Number(element,"x")); Canvas.SetTop(shape,Number(element,"y"));
                } else continue;
                if(element.GetAttribute("fill")=="black") shape.SetResourceReference(Shape.FillProperty,"TextBrush");
                if(element.GetAttribute("stroke")=="black") { shape.SetResourceReference(Shape.StrokeProperty,"TextBrush"); shape.StrokeThickness=Number(element,"stroke-width"); }
                shape.StrokeStartLineCap=shape.StrokeEndLineCap=PenLineCap.Round; shape.StrokeLineJoin=PenLineJoin.Round;
                canvas.Children.Add(shape);
            }
            return new Viewbox { Child=canvas,Width=32,Height=32,Stretch=Stretch.Uniform,IsHitTestVisible=false,Focusable=false };
        }
        static double Number(XmlElement element,string attribute) { string value=element.GetAttribute(attribute); return value.Length==0?0:double.Parse(value,CultureInfo.InvariantCulture); }
        internal static FrameworkElement Lucide(string name,double size=20,string brush="TextBrush") {
            var glyph=new VectorGlyph(vectors.GetOrAdd(name,ReadLucide),size);
            if(brush=="ButtonForeground") glyph.SetBinding(VectorGlyph.InkProperty,new Binding("Foreground") {RelativeSource=new RelativeSource(RelativeSourceMode.FindAncestor,typeof(Button),1)});
            else glyph.SetResourceReference(VectorGlyph.InkProperty,brush);
            return glyph;
        }
        static System.Windows.Media.Geometry ReadLucide(string name) {
            var group=new GeometryGroup(); var document=new XmlDocument {XmlResolver=null};
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Reswin.Icons.lucide-"+name+".svg")) document.Load(stream);
            foreach(XmlNode node in document.DocumentElement.ChildNodes) {
                var el=node as XmlElement; if(el==null) continue; System.Windows.Media.Geometry geometry;
                switch(el.LocalName) {
                    case "path": geometry=System.Windows.Media.Geometry.Parse(el.GetAttribute("d")); break;
                    case "rect": geometry=new RectangleGeometry(new Rect(Number(el,"x"),Number(el,"y"),Number(el,"width"),Number(el,"height")),Number(el,"rx"),el.HasAttribute("ry")?Number(el,"ry"):Number(el,"rx")); break;
                    case "circle": double radius=Number(el,"r"); geometry=new EllipseGeometry(new Point(Number(el,"cx"),Number(el,"cy")),radius,radius); break;
                    case "ellipse": geometry=new EllipseGeometry(new Point(Number(el,"cx"),Number(el,"cy")),Number(el,"rx"),Number(el,"ry")); break;
                    case "line": geometry=new LineGeometry(new Point(Number(el,"x1"),Number(el,"y1")),new Point(Number(el,"x2"),Number(el,"y2"))); break;
                    case "polyline":
                    case "polygon":
                        var points=PointCollection.Parse(el.GetAttribute("points"));
                        if(points.Count==0) continue;
                        var line=new StreamGeometry();
                        using(var draw=line.Open()) {
                            draw.BeginFigure(points[0],false,el.LocalName=="polygon");
                            for(int i=1;i<points.Count;i++) draw.LineTo(points[i],true,false);
                        }
                        geometry=line; break;
                    default: continue;
                }
                group.Children.Add(geometry);
            }
            group.Freeze(); return group;
        }
        // One lightweight visual per icon, backed by shared immutable vectors.
        // SVG radii/rectangles describe stroke centerlines. WPF Ellipse/Rectangle
        // layout Shapes instead inset their stroke, making the ban slash escape
        // its undersized circle. Drawing geometries preserves the SVG dimensions.
        sealed class VectorGlyph : FrameworkElement {
            internal static readonly DependencyProperty InkProperty=DependencyProperty.Register("Ink",typeof(Brush),typeof(VectorGlyph),new FrameworkPropertyMetadata(Brushes.Black,FrameworkPropertyMetadataOptions.AffectsRender,InkChanged));
            readonly System.Windows.Media.Geometry geometry;
            readonly ScaleTransform scale=new ScaleTransform();
            Pen pen;
            internal VectorGlyph(System.Windows.Media.Geometry geometry,double size) {
                this.geometry=geometry; Width=size; Height=size;
                IsHitTestVisible=false; Focusable=false; ClipToBounds=true;
            }
            static void InkChanged(DependencyObject sender,DependencyPropertyChangedEventArgs e) {((VectorGlyph)sender).pen=null;}
            protected override void OnRender(DrawingContext drawing) {
                base.OnRender(drawing);
                var ink=(Brush)GetValue(InkProperty); if(ink==null || ActualWidth<=0 || ActualHeight<=0) return;
                if(pen==null) pen=new Pen(ink,1.8) {StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};
                scale.ScaleX=ActualWidth/24; scale.ScaleY=ActualHeight/24;
                drawing.PushTransform(scale); drawing.DrawGeometry(null,pen,geometry); drawing.Pop();
            }
        }
        internal static FrameworkElement Symbol(string name,double size=24,string brush="TextBrush") {
            if(name=="plus" || name=="folder" || name=="ban" || name=="close") return Lucide(name=="close"?"x":name,size,brush);
            string data;
            switch(name) {
                case "plus": data="M12,3 L12,21 M3,12 L21,12"; break;
                case "language": data="M12,2 A10,10 0 1 1 11.99,2 M2,12 L22,12 M12,2 C6,7 6,17 12,22 C18,17 18,7 12,2"; break;
                case "theme": data="M12,3 A9,9 0 1 1 11.99,3 M12,3 L12,21"; break;
                case "close": data="M5,5 L19,19 M19,5 L5,19"; break;
                case "minimize": data="M5,12 L19,12"; break;
                case "maximize": data="M5,5 L19,5 L19,19 L5,19 Z"; break;
                case "restore": data="M8,5 L19,5 L19,16 M5,8 L16,8 L16,19 L5,19 Z"; break;
                case "ban": data="M12,2 A10,10 0 1 1 11.99,2 M5,5 L19,19"; break;
                case "folder": data="M2,7 L2,5 Q2,3 4,3 L9,3 L12,6 L20,6 Q22,6 22,8 L22,19 Q22,21 20,21 L4,21 Q2,21 2,19 Z M2,8 L22,8"; break;
                case "windows": data="M2,3 L10,2 L10,11 L2,11 Z M12,2 L22,1 L22,11 L12,11 Z M2,13 L10,13 L10,22 L2,21 Z M12,13 L22,13 L22,23 L12,22 Z"; break;
                default: throw new ArgumentException("Unknown icon", "name");
            }
            var path=new Path { Data=System.Windows.Media.Geometry.Parse(data),StrokeThickness=name=="windows"?0:1.8,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round };
            var property=name=="windows"?Shape.FillProperty:Shape.StrokeProperty;
            if(brush=="ButtonForeground") path.SetBinding(property,new Binding("Foreground") { RelativeSource=new RelativeSource(RelativeSourceMode.FindAncestor,typeof(Button),1) });
            else path.SetResourceReference(property,brush);
            var canvas=new Canvas { Width=24,Height=24 }; canvas.Children.Add(path);
            return new Viewbox { Child=canvas,Width=size,Height=size,IsHitTestVisible=false,Focusable=false };
        }
        internal static FrameworkElement Logo() {
            var square=new Border { Width=24,Height=24,CornerRadius=new CornerRadius(5),Background=new SolidColorBrush(Color.FromRgb(22,65,70)),Padding=new Thickness(5) };
            square.Child=new Border { CornerRadius=new CornerRadius(1),BorderThickness=new Thickness(2),BorderBrush=new SolidColorBrush(Color.FromRgb(72,219,181)) };
            return square;
        }
    }
}
