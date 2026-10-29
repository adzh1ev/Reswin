using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Reswin {
    internal static class Interaction {
        // The same timing curves drive control feedback and page navigation.
        // Native KeySpline evaluates the exact cubic-bezier values without a timer loop.
        internal static readonly IEasingFunction EaseOut=CreateEase(.23,1,.32,1);
        internal static readonly IEasingFunction EaseInOut=CreateEase(.77,0,.175,1);
        static IEasingFunction CreateEase(double x1,double y1,double x2,double y2) {
            var easing=new SplineEase(new Point(x1,y1),new Point(x2,y2)); easing.Freeze(); return easing;
        }
        sealed class SplineEase : EasingFunctionBase {
            readonly Point first,second;
            readonly KeySpline spline;
            internal SplineEase(Point first,Point second) {
                this.first=first; this.second=second;
                spline=new KeySpline(first,second); spline.Freeze();
                EasingMode=EasingMode.EaseIn;
            }
            protected override double EaseInCore(double progress) { return spline.GetSplineProgress(progress); }
            protected override Freezable CreateInstanceCore() { return new SplineEase(first,second); }
        }
        internal static DoubleAnimation Transition(double from,double to,int milliseconds,bool moving=false) {
            return new DoubleAnimation(from,to,TimeSpan.FromMilliseconds(milliseconds)) {EasingFunction=moving?EaseInOut:EaseOut};
        }
        internal static void AnimateDropDown(ComboBox control) {
            bool opening=false; int revision=0;
            control.DropDownOpened+=delegate {
                control.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(delegate {
                    if(!control.IsDropDownOpen || control.Template==null) return;
                    var popup=control.Template.FindName("PART_Popup",control) as Popup;
                    var panel=popup==null?null:popup.Child as FrameworkElement;
                    if(panel==null) return;
                    var slide=panel.RenderTransform as TranslateTransform;
                    if(slide==null) {slide=new TranslateTransform(); panel.RenderTransform=slide;}
                    else if(slide.IsFrozen) {slide=slide.CloneCurrentValue(); panel.RenderTransform=slide;}
                    int token=++revision;
                    double offset=opening?slide.Y:-6,opacity=opening?panel.Opacity:0;
                    opening=true;
                    slide.BeginAnimation(TranslateTransform.YProperty,Transition(offset,0,180));
                    var fade=Transition(opacity,1,180);
                    fade.Completed+=delegate {if(token==revision) opening=false;};
                    panel.BeginAnimation(UIElement.OpacityProperty,fade);
                }));
            };
        }
        internal static Button IconButton(string symbol,double size,string tooltip,bool destructive=false) {
            var glyph=Icons.Symbol(symbol,size,"ButtonForeground");
            var scale=new ScaleTransform(1,1); glyph.RenderTransform=scale; glyph.RenderTransformOrigin=new Point(.5,.5);
            var button=new Button {Content=glyph,Width=34,Height=34,Style=(Style)Application.Current.FindResource("IconButton"),ToolTip=tooltip};
            Theme.Accessible(button,tooltip);
            int revision=0;
            Action update=delegate {
                int token=++revision;
                bool active=button.IsEnabled && (button.IsMouseOver || button.IsKeyboardFocused);
                double target=active?1.12:1;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty,Transition(scale.ScaleX,target,150));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty,Transition(scale.ScaleY,target,150));
                var normal=(SolidColorBrush)Application.Current.FindResource("TextBrush");
                var accent=(SolidColorBrush)Application.Current.FindResource("FocusBrush");
                Color color=active?accent.Color:normal.Color;
                var current=button.Foreground as SolidColorBrush;
                var animated=new SolidColorBrush(current==null?normal.Color:current.Color); button.Foreground=animated;
                var animation=new ColorAnimation(color,TimeSpan.FromMilliseconds(150)) {EasingFunction=EaseOut};
                animation.Completed+=delegate {if(token==revision && !active) button.SetResourceReference(Control.ForegroundProperty,"TextBrush");};
                animated.BeginAnimation(SolidColorBrush.ColorProperty,animation);
            };
            button.MouseEnter+=delegate {update();}; button.MouseLeave+=delegate {update();};
            button.GotKeyboardFocus+=delegate {update();}; button.LostKeyboardFocus+=delegate {update();};
            button.IsEnabledChanged+=delegate {update();};
            return button;
        }
        internal static void AnimateSwitch(CheckBox control) {
            Action<bool> update=delegate(bool animate) {
                control.ApplyTemplate(); if(control.Template==null) return;
                var thumb=control.Template.FindName("Thumb",control) as FrameworkElement; if(thumb==null) return;
                var transform=thumb.RenderTransform as TranslateTransform;
                if(transform==null) {transform=new TranslateTransform(); thumb.RenderTransform=transform;}
                else if(transform.IsFrozen) {transform=transform.CloneCurrentValue(); thumb.RenderTransform=transform;}
                //48px track,22px thumb and3px inset need20px of travel on both sides.
                double target=control.IsChecked==true?20:0;
                var track=control.Template.FindName("ActiveTrack",control) as UIElement;
                if(track!=null) {
                    double opacity=control.IsChecked==true?1:0;
                    if(!animate) {track.BeginAnimation(UIElement.OpacityProperty,null); track.Opacity=opacity;}
                    else track.BeginAnimation(UIElement.OpacityProperty,Transition(track.Opacity,opacity,220));
                }
                if(!animate) {transform.BeginAnimation(TranslateTransform.XProperty,null); transform.X=target;}
                else transform.BeginAnimation(TranslateTransform.XProperty,Transition(transform.X,target,220,true));
            };
            control.Loaded+=delegate {update(false);};
            control.Checked+=delegate {if(control.IsLoaded) update(true);};
            control.Unchecked+=delegate {if(control.IsLoaded) update(true);};
        }
    }
}
