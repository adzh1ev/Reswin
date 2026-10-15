using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Reswin {
    internal sealed class SmoothScrollViewer : ScrollViewer {
        static readonly DependencyProperty AnimatedOffsetProperty=DependencyProperty.Register("AnimatedOffset",typeof(double),typeof(SmoothScrollViewer),new PropertyMetadata(0.0,OffsetChanged));
        double destination;
        bool animating;
        int revision;
        internal SmoothScrollViewer() {
            SetResourceReference(StyleProperty,typeof(ScrollViewer));
            CanContentScroll=false;
            AddHandler(Thumb.DragStartedEvent,new DragStartedEventHandler(delegate { CancelMotion(); }),true);
            PreviewMouseLeftButtonDown+=delegate { CancelMotion(); };
            PreviewKeyDown+=delegate { CancelMotion(); };
            Unloaded+=delegate { CancelMotion(); };
        }
        static void OffsetChanged(DependencyObject owner,DependencyPropertyChangedEventArgs e) { ((SmoothScrollViewer)owner).ScrollToVerticalOffset((double)e.NewValue); }
        void CancelMotion() {
            revision++; animating=false;
            double current=VerticalOffset; BeginAnimation(AnimatedOffsetProperty,null); SetValue(AnimatedOffsetProperty,current); destination=current;
        }
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e) {
            if(e.Handled || ScrollableHeight<=0) { base.OnPreviewMouseWheel(e); return; }
            int lines=SystemParameters.WheelScrollLines;
            if(lines==0) { e.Handled=true; return; }
            double step=lines<0?ViewportHeight:lines*18;
            destination=Math.Max(0,Math.Min(ScrollableHeight,(animating?destination:VerticalOffset)-e.Delta/120.0*step));
            e.Handled=true;
            if(!SystemParameters.ClientAreaAnimation) { CancelMotion(); ScrollToVerticalOffset(Math.Max(0,Math.Min(ScrollableHeight,VerticalOffset-e.Delta/120.0*step))); return; }
            int token=++revision; double from=VerticalOffset;
            BeginAnimation(AnimatedOffsetProperty,null); SetValue(AnimatedOffsetProperty,from); animating=true;
            double target=destination;
            var motion=new DoubleAnimation(from,target,TimeSpan.FromMilliseconds(220)) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut } };
            motion.Completed+=delegate { if(token==revision) { animating=false; BeginAnimation(AnimatedOffsetProperty,null); SetValue(AnimatedOffsetProperty,target); } };
            BeginAnimation(AnimatedOffsetProperty,motion,HandoffBehavior.SnapshotAndReplace);
        }
    }
}
