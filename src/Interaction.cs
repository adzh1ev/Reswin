using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Reswin {
    internal static class Interaction {
        internal static void AnimateDropDown(ComboBox control) {
            control.DropDownOpened+=delegate {
                control.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(delegate {
                    if(!control.IsDropDownOpen) return;
                    var popup=control.Template.FindName("PART_Popup",control) as Popup;
                    var panel=popup==null?null:popup.Child as FrameworkElement;
                    if(panel==null) return;
                    var slide=new TranslateTransform(); panel.RenderTransform=slide;
                    var duration=TimeSpan.FromMilliseconds(280);
                    slide.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(-12,0,duration) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut } });
                    panel.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,duration) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut } });
                }));
            };
        }
        internal static Button IconButton(string symbol,double size,string tooltip,bool destructive=false) {
            var glyph=Icons.Symbol(symbol,size,"ButtonForeground");
            var scale=new ScaleTransform(1,1); glyph.RenderTransform=scale; glyph.RenderTransformOrigin=new Point(.5,.5);
            var button=new Button { Content=glyph,Width=34,Height=34,Style=(Style)Application.Current.FindResource("IconButton"),ToolTip=tooltip };
            Theme.Accessible(button,tooltip);
            int revision=0;
            Action update=delegate {
                int token=++revision;
                bool active=button.IsMouseOver || button.IsKeyboardFocused;
                double target=active?1.18:1;
                var duration=TimeSpan.FromMilliseconds(SystemParameters.ClientAreaAnimation?220:0);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(scale.ScaleX,target,duration) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut } });
                scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(scale.ScaleY,target,duration) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut } });
                var normal=(SolidColorBrush)Application.Current.FindResource("TextBrush");
                var accent=(SolidColorBrush)Application.Current.FindResource("FocusBrush");
                Color color=active?(destructive && !SystemParameters.HighContrast?Color.FromRgb(230,92,110):accent.Color):normal.Color;
                var current=button.Foreground as SolidColorBrush;
                var animated=new SolidColorBrush(current==null?normal.Color:current.Color); button.Foreground=animated;
                var animation=new ColorAnimation(color,duration) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut } };
                animation.Completed+=delegate { if(token==revision && !button.IsMouseOver && !button.IsKeyboardFocused) button.SetResourceReference(Control.ForegroundProperty,"TextBrush"); };
                animated.BeginAnimation(SolidColorBrush.ColorProperty,animation);
            };
            button.MouseEnter+=delegate { update(); }; button.MouseLeave+=delegate { update(); };
            button.GotKeyboardFocus+=delegate { update(); }; button.LostKeyboardFocus+=delegate { update(); };
            return button;
        }
        internal static void AnimateSwitch(CheckBox control) {
            Action<bool> update=delegate(bool animate) {
                control.ApplyTemplate(); var thumb=control.Template.FindName("Thumb",control) as FrameworkElement; if(thumb==null) return;
                var transform=thumb.RenderTransform as TranslateTransform;
                if(transform==null) { transform=new TranslateTransform(); thumb.RenderTransform=transform; }
                double target=control.IsChecked==true?24:0;
                var track=control.Template.FindName("ActiveTrack",control) as UIElement;
                if(track!=null) {
                    double opacity=control.IsChecked==true?1:0;
                    if(!animate) { track.BeginAnimation(UIElement.OpacityProperty,null); track.Opacity=opacity; }
                    else track.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(track.Opacity,opacity,TimeSpan.FromMilliseconds(280)) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseInOut } });
                }
                if(!animate) { transform.BeginAnimation(TranslateTransform.XProperty,null); transform.X=target; }
                else transform.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(transform.X,target,TimeSpan.FromMilliseconds(280)) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseInOut } });
            };
            control.Loaded+=delegate { update(false); };
            control.Checked+=delegate { if(control.IsLoaded) update(true); };
            control.Unchecked+=delegate { if(control.IsLoaded) update(true); };
        }
    }
}
