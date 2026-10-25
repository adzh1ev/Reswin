using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Reswin {
    // Native Slider semantics and keyboard controls, with direct click-and-drag
    // across the whole rail. Only the thumb's visual scale is animated.
    internal sealed class SensitivitySlider : Slider {
        Track track;
        FrameworkElement thumbCore;
        bool dragging;
        double grabOffset;
        internal bool IsAdjusting { get { return dragging; } }
        internal event Action AdjustmentCompleted;
        internal SensitivitySlider() {
            Style=(Style)Application.Current.FindResource("SensitivitySlider");
            SmallChange=1; LargeChange=10; TickFrequency=1; IsSnapToTickEnabled=true;
            IsEnabledChanged+=delegate {
                if(!IsEnabled) FinishDrag();
                Feedback();
            };
            Unloaded+=delegate { FinishDrag(); };
        }
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            track=Template.FindName("PART_Track",this) as Track;
            if(track!=null && track.Thumb!=null) {
                track.Thumb.ApplyTemplate();
                thumbCore=track.Thumb.Template.FindName("ThumbCore",track.Thumb) as FrameworkElement;
            }
            Feedback();
        }
        void Feedback() {
            if(thumbCore==null) return;
            var scale=thumbCore.RenderTransform as ScaleTransform;
            if(scale==null) return;
            // Freezable values from a ControlTemplate can be shared/frozen.
            // Direct animation needs a mutable transform owned by this instance.
            if(scale.IsFrozen) { scale=scale.Clone(); thumbCore.RenderTransform=scale; }
            double target=!IsEnabled?1:dragging?1.38:IsMouseOver || IsKeyboardFocused?1.1:1;
            var duration=TimeSpan.FromMilliseconds(dragging?160:190);
            var easing=new CubicEase {EasingMode=EasingMode.EaseOut};
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(scale.ScaleX,target,duration) {EasingFunction=easing});
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(scale.ScaleY,target,duration) {EasingFunction=easing});
        }
        void SetFromPointer(MouseEventArgs e) {
            if(track==null || track.ActualWidth<=0) return;
            Point point=e.GetPosition(track); point.X-=grabOffset;
            double value=track.ValueFromPoint(point);
            if(double.IsNaN(value) || double.IsInfinity(value)) return;
            value=Math.Max(Minimum,Math.Min(Maximum,Math.Round(value)));
            SetCurrentValue(ValueProperty,value);
        }
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
            if(IsEnabled) {
                Focus(); ApplyTemplate();
                if(track!=null && CaptureMouse()) {
                    dragging=true; grabOffset=0;
                    var thumb=track.Thumb;
                    if(thumb!=null) {
                        Point position=e.GetPosition(thumb);
                        if(position.X>=0 && position.X<=thumb.ActualWidth && position.Y>=0 && position.Y<=thumb.ActualHeight)
                            grabOffset=position.X-thumb.ActualWidth/2;
                    }
                    Feedback(); SetFromPointer(e); e.Handled=true;
                }
            }
            base.OnPreviewMouseLeftButtonDown(e);
        }
        protected override void OnPreviewMouseMove(MouseEventArgs e) {
            if(dragging) {
                if(e.LeftButton==MouseButtonState.Pressed) { SetFromPointer(e); e.Handled=true; }
                else FinishDrag();
            }
            base.OnPreviewMouseMove(e);
        }
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e) {
            if(dragging) { SetFromPointer(e); FinishDrag(); e.Handled=true; }
            base.OnPreviewMouseLeftButtonUp(e);
        }
        void FinishDrag() {
            if(!dragging) return;
            dragging=false;
            if(IsMouseCaptured) ReleaseMouseCapture();
            Feedback();
            if(AdjustmentCompleted!=null) AdjustmentCompleted();
        }
        protected override void OnLostMouseCapture(MouseEventArgs e) { FinishDrag(); base.OnLostMouseCapture(e); }
        protected override void OnMouseEnter(MouseEventArgs e) { base.OnMouseEnter(e); Feedback(); }
        protected override void OnMouseLeave(MouseEventArgs e) { base.OnMouseLeave(e); Feedback(); }
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) { base.OnGotKeyboardFocus(e); Feedback(); }
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) { base.OnLostKeyboardFocus(e); FinishDrag(); Feedback(); }
    }
}
