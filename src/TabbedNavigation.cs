using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Reswin {
    // Native tab semantics with an independent, retained page host. Only render
    // transforms and opacity animate; page layout and control values stay intact.
    internal sealed class TabbedNavigation : Grid {
        const double HeaderHeight=40;
        static readonly DependencyProperty ProgressProperty=DependencyProperty.Register("Progress",typeof(double),typeof(TabbedNavigation),new PropertyMetadata(0.0,MotionChanged));
        static readonly DependencyProperty DragOffsetProperty=DependencyProperty.Register("DragOffset",typeof(double),typeof(TabbedNavigation),new PropertyMetadata(0.0,MotionChanged));
        readonly TabControl tabs;
        readonly Grid header=new Grid {Height=HeaderHeight,Background=Brushes.Transparent,Cursor=Cursors.Hand,Clip=System.Windows.Media.Geometry.Empty};
        readonly Grid viewport=new Grid {ClipToBounds=true};
        readonly Border indicator=new Border {CornerRadius=new CornerRadius(9),HorizontalAlignment=HorizontalAlignment.Left,IsHitTestVisible=false};
        readonly TranslateTransform indicatorPosition=new TranslateTransform();
        readonly RectangleGeometry indicatorClip=new RectangleGeometry {RadiusX=9,RadiusY=9};
        readonly Grid activeHeaders=new DecorativeHeaderLayer {IsHitTestVisible=false};
        readonly Page[] pages;
        int selected,generation;
        bool ready,pointerPressed,dragging;
        Point pressedPoint;
        int pressedTab;
        double grabOffset;
        System.Windows.Media.Geometry stripShape;
        internal event Action<int> SelectionChanged;

        sealed class Page {
            internal Grid Host;
            internal TranslateTransform Position=new TranslateTransform();
        }
        // The clipped white copy is visual decoration. TabItem peers provide
        // the actual selection and names; do not announce its duplicate labels.
        sealed class DecorativeHeaderLayer : Grid {
            protected override AutomationPeer OnCreateAutomationPeer() {return new DecorativeHeaderPeer(this);}
        }
        sealed class DecorativeHeaderPeer : FrameworkElementAutomationPeer {
            internal DecorativeHeaderPeer(FrameworkElement owner) : base(owner) { }
            protected override bool IsControlElementCore() {return false;}
            protected override bool IsContentElementCore() {return false;}
            protected override List<AutomationPeer> GetChildrenCore() {return null;}
        }

        internal TabbedNavigation(string[] labels,string[] icons,FrameworkElement[] content,int selectedIndex=0) {
            if(labels==null || icons==null || content==null || labels.Length==0 || labels.Length!=icons.Length || labels.Length!=content.Length)
                throw new ArgumentException("Tab labels, icons and pages must have the same nonzero length.");
            selected=Math.Max(0,Math.Min(content.Length-1,selectedIndex));
            pages=new Page[content.Length];
            RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
            RowDefinitions.Add(new RowDefinition {Height=new GridLength(1,GridUnitType.Star)});
            var shell=new Border {Margin=new Thickness(0,0,0,14),Child=header}; Children.Add(shell);
            header.SetResourceReference(Panel.BackgroundProperty,"NavigationBrush");
            Grid.SetRow(viewport,1); Children.Add(viewport);
            KeyboardNavigation.SetTabNavigation(this,KeyboardNavigationMode.Continue);

            tabs=new TabControl {Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0),FocusVisualStyle=null};
            tabs.SetResourceReference(Control.FontFamilyProperty,"UIFontFamily");
            var panel=new FrameworkElementFactory(typeof(UniformGrid)); panel.SetValue(UniformGrid.ColumnsProperty,content.Length);
            tabs.ItemsPanel=new ItemsPanelTemplate(panel);
            tabs.Template=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='TabControl'><ItemsPresenter/></ControlTemplate>");
            var itemTemplate=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='TabItem'>
 <Grid Margin='3,0' Background='Transparent'>
  <Border x:Name='Hover' CornerRadius='9' Background='{DynamicResource NavigationHoverBrush}' Opacity='0'/>
  <ContentPresenter ContentSource='Header' HorizontalAlignment='Center' VerticalAlignment='Center' Margin='8,0'/>
 </Grid>
 <ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'>
  <Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.12'/></Storyboard></BeginStoryboard></Trigger.EnterActions>
  <Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.12'/></Storyboard></BeginStoryboard></Trigger.ExitActions>
 </Trigger></ControlTemplate.Triggers>
</ControlTemplate>");
            for(int i=0;i<content.Length;i++) {
                var item=new TabItem {Header=Header(labels[i],icons[i],"TextBrush"),Template=itemTemplate,FocusVisualStyle=null,Cursor=Cursors.Hand,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch};
                Theme.Accessible(item,labels[i]); tabs.Items.Add(item);
                activeHeaders.ColumnDefinitions.Add(new ColumnDefinition());
                var copy=Header(labels[i],icons[i],"AccentForegroundBrush"); copy.Margin=new Thickness(10,0,10,0); Grid.SetColumn(copy,i); activeHeaders.Children.Add(copy);
                var page=new Page {Host=new Grid()};
                page.Host.RenderTransform=page.Position; page.Host.Children.Add(content[i]);
                pages[i]=page; viewport.Children.Add(page.Host);
            }
            tabs.SelectedIndex=selected;
            header.Children.Add(tabs);
            indicator.SetResourceReference(Border.BackgroundProperty,"AccentBrush");
            indicator.RenderTransform=indicatorPosition;
            header.Children.Add(indicator);
            indicatorClip.Transform=indicatorPosition; activeHeaders.Clip=indicatorClip;
            header.Children.Add(activeHeaders);
            tabs.SelectionChanged+=delegate(object sender,SelectionChangedEventArgs e) {
                if(e.OriginalSource==tabs && tabs.SelectedIndex>=0 && tabs.SelectedIndex!=selected) {
                    CancelPointer(false); Select(tabs.SelectedIndex);
                }
            };
            header.PreviewMouseLeftButtonDown+=BeginPointer;
            header.PreviewMouseMove+=MovePointer;
            header.PreviewMouseLeftButtonUp+=EndPointer;
            header.LostMouseCapture+=delegate {if(pointerPressed) CancelPointer(true);};
            header.SizeChanged+=delegate {CancelPointer(false); UpdateStripGeometry(); SnapPages(); UpdateIndicator();};
            viewport.SizeChanged+=delegate {CancelPointer(false); SnapPages();};
            IsEnabledChanged+=delegate {if(!IsEnabled) CancelPointer(true);};
            IsVisibleChanged+=delegate {if(!IsVisible) {CancelPointer(false); SnapPages();}};
            Loaded+=delegate {ready=true; UpdateStripGeometry(); SnapPages(); UpdateIndicator();};
            Unloaded+=delegate {CancelPointer(false); ready=false; SnapPages();};
            PreviewKeyDown+=NavigateByKeyboard;
            SnapPages();
        }

        internal int SelectedIndex {
            get {return selected;}
            set {if(value<0 || value>=pages.Length) throw new ArgumentOutOfRangeException("value"); tabs.SelectedIndex=value;}
        }
        internal void SetLabels(string[] labels) {
            if(labels==null || labels.Length!=pages.Length) throw new ArgumentException("The tab label count must match the page count.");
            for(int i=0;i<labels.Length;i++) {
                var item=(TabItem)tabs.Items[i];
                var normal=(StackPanel)item.Header;
                ((TextBlock)normal.Children[1]).Text=labels[i];
                var active=(StackPanel)activeHeaders.Children[i];
                ((TextBlock)active.Children[1]).Text=labels[i];
                Theme.Accessible(item,labels[i]);
            }
        }
        static FrameworkElement Header(string label,string icon,string foreground) {
            var row=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            var glyph=Icons.Lucide(icon,18,foreground); glyph.Margin=new Thickness(0,0,7,0); row.Children.Add(glyph);
            var text=Theme.Text(label,12,false); text.SetResourceReference(TextBlock.ForegroundProperty,foreground); text.FontWeight=FontWeights.Medium;
            row.Children.Add(text); return row;
        }
        void NavigateByKeyboard(object sender,KeyEventArgs e) {
            if(pointerPressed && e.Key==Key.Escape) {CancelPointer(true); e.Handled=true; return;}
            if(e.Key!=Key.Tab || (Keyboard.Modifiers&ModifierKeys.Control)==0) return;
            CancelPointer(false);
            int direction=(Keyboard.Modifiers&ModifierKeys.Shift)!=0?-1:1;
            SelectedIndex=(selected+direction+pages.Length)%pages.Length;
            ((TabItem)tabs.Items[selected]).Focus(); e.Handled=true;
        }
        int TabAt(Point point) {
            if(header.ActualWidth<=0 || point.X<0 || point.X>=header.ActualWidth || point.Y<0 || point.Y>header.ActualHeight) return -1;
            if(stripShape!=null && !stripShape.FillContains(point)) return -1;
            int index=Math.Min(pages.Length-1,(int)(point.X/(header.ActualWidth/pages.Length)));
            return ((TabItem)tabs.Items[index]).IsEnabled?index:-1;
        }
        double VisualProgress {
            get {return Math.Max(0,Math.Min(pages.Length-1,(double)GetValue(ProgressProperty)+(double)GetValue(DragOffsetProperty)));}
        }
        static void MotionChanged(DependencyObject sender,DependencyPropertyChangedEventArgs e) {
            ((TabbedNavigation)sender).UpdateMotion();
        }
        void UpdateMotion() {
            double progress=VisualProgress;
            double slot=header.ActualWidth/pages.Length,width=viewport.ActualWidth;
            indicatorPosition.X=slot*progress;
            // All retained pages move in one coordinate system with the header.
            // These render transforms do not rebuild or remeasure content on input.
            for(int i=0;i<pages.Length;i++) pages[i].Position.X=(i-progress)*width;
        }
        void BeginPointer(object sender,MouseButtonEventArgs e) {
            if(!ready || !IsEnabled || pointerPressed || e.ChangedButton!=MouseButton.Left) return;
            Point point=e.GetPosition(header); int index=TabAt(point);
            if(index<0 || !header.CaptureMouse()) return;
            pointerPressed=true; dragging=false; pressedPoint=point; pressedTab=index;
            grabOffset=point.X-index*(header.ActualWidth/pages.Length);
            // Focusing an unselected native TabItem selects it immediately.
            // Keep keyboard focus on the committed header throughout preview.
            ReleasePage(pages[selected].Host,selected);
            PreparePages(); CollapseOffset();
            // Preview both the indicator and pages immediately. Only release
            // commits selection; cancelling therefore does not leave the page.
            Animate(ProgressProperty,index,150,null);
            e.Handled=true;
        }
        void MovePointer(object sender,MouseEventArgs e) {
            if(!pointerPressed) return;
            if(e.LeftButton!=MouseButtonState.Pressed) {CancelPointer(true); return;}
            Point point=e.GetPosition(header);
            if(!dragging && Math.Abs(point.X-pressedPoint.X)>SystemParameters.MinimumHorizontalDragDistance) {
                dragging=true;
                double visual=VisualProgress,target=DragProgress(point.X);
                Set(ProgressProperty,target); Set(DragOffsetProperty,visual-target);
                // Carry any unfinished press animation into the direct drag.
                // The same correction moves the pages and the indicator together.
                Animate(DragOffsetProperty,0,150,null);
            } else if(dragging) Set(ProgressProperty,DragProgress(point.X));
            e.Handled=true;
        }
        double DragProgress(double x) {
            double slot=header.ActualWidth/pages.Length;
            return slot<=0?selected:Math.Max(0,Math.Min(pages.Length-1,(x-grabOffset)/slot));
        }
        void EndPointer(object sender,MouseButtonEventArgs e) {
            if(!pointerPressed || e.ChangedButton!=MouseButton.Left) return;
            Point point=e.GetPosition(header); int index=TabAt(point);
            if(index>=0) index=dragging?(int)Math.Floor(DragProgress(point.X)+0.5):pressedTab;
            CancelPointer(false);
            if(index<0 || !((TabItem)tabs.Items[index]).IsEnabled) Settle(true);
            else {
                if(index!=selected) tabs.SelectedIndex=index;
                else Settle(true);
                ((TabItem)tabs.Items[index]).Focus();
            }
            e.Handled=true;
        }
        void CancelPointer(bool animateReturn) {
            if(!pointerPressed) return;
            pointerPressed=false; dragging=false;
            CollapseOffset();
            if(header.IsMouseCaptured) header.ReleaseMouseCapture();
            if(animateReturn) Settle(ready && IsVisible);
        }
        void CollapseOffset() {
            double visual=VisualProgress;
            Set(DragOffsetProperty,0); Set(ProgressProperty,visual);
        }
        void Select(int index) {
            int previous=selected;
            ReleasePage(pages[previous].Host,index);
            selected=index;
            Settle(ready && viewport.ActualWidth>0 && IsVisible);
            if(SelectionChanged!=null) SelectionChanged(selected);
        }
        void PreparePages() {
            generation++;
            foreach(var page in pages) {
                page.Host.Visibility=Visibility.Visible;
                page.Host.Opacity=1;
                page.Host.IsHitTestVisible=false;
                KeyboardNavigation.SetTabNavigation(page.Host,KeyboardNavigationMode.None);
            }
        }
        void Settle(bool animate) {
            CollapseOffset();
            if(!animate) {SnapPages(); return;}
            PreparePages(); int version=generation;
            // Only the committed page can receive input after release. During
            // preview every page is inert, including its keyboard descendants.
            pages[selected].Host.IsHitTestVisible=true;
            KeyboardNavigation.SetTabNavigation(pages[selected].Host,KeyboardNavigationMode.Continue);
            Animate(ProgressProperty,selected,220,delegate {if(version==generation && !pointerPressed) SnapPages();});
        }
        void ReleasePage(Grid page,int nextIndex) {
            CloseDropDowns(page);
            var captured=Mouse.Captured as DependencyObject;
            if(captured!=null && page.IsAncestorOf(captured)) Mouse.Capture(null);
            if(page.IsKeyboardFocusWithin) ((TabItem)tabs.Items[nextIndex]).Focus();
        }
        static void CloseDropDowns(DependencyObject node) {
            var combo=node as ComboBox; if(combo!=null) combo.IsDropDownOpen=false;
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++) CloseDropDowns(VisualTreeHelper.GetChild(node,i));
        }
        void UpdateIndicator() {
            double slot=header.ActualWidth/pages.Length;
            if(slot<=0) return;
            indicator.Width=slot; indicator.Height=HeaderHeight;
            indicatorClip.Rect=new Rect(0,0,slot,HeaderHeight);
            UpdateMotion();
        }
        void UpdateStripGeometry() {
            double width=header.ActualWidth,slot=width/pages.Length;
            if(width<=0) return;
            // One silhouette owns the background and ALL animated layers. The
            // coral selection cannot paint in a notch or beyond an outer cap.
            // Segments have 9-DIP corners, with 6-DIP seams bridged by a 16-DIP
            // central neck. Circular concave fillets keep each joint tangent.
            double inset=Math.Min(3,slot/8);
            double radius=Math.Min(9,(slot-2*inset)/2);
            double join=Math.Min(3,inset),depth=radius+join;
            double bottom=HeaderHeight-depth;
            var corner=new Size(radius,radius); var fillet=new Size(join,join);
            var geometry=new StreamGeometry();
            using(var draw=geometry.Open()) {
                draw.BeginFigure(new Point(inset+radius,0),true,true);
                for(int i=0;i<pages.Length;i++) {
                    double right=(i+1)*slot-inset;
                    draw.LineTo(new Point(right-radius,0),true,false);
                    draw.ArcTo(new Point(right,radius),corner,0,false,SweepDirection.Clockwise,true,false);
                    if(i+1<pages.Length) {
                        double next=(i+1)*slot+inset;
                        draw.LineTo(new Point(right,depth-join),true,false);
                        draw.ArcTo(new Point(right+join,depth),fillet,0,false,SweepDirection.Counterclockwise,true,false);
                        draw.LineTo(new Point(next-join,depth),true,false);
                        draw.ArcTo(new Point(next,depth-join),fillet,0,false,SweepDirection.Counterclockwise,true,false);
                        draw.LineTo(new Point(next,radius),true,false);
                        draw.ArcTo(new Point(next+radius,0),corner,0,false,SweepDirection.Clockwise,true,false);
                    }
                }
                double outerRight=width-inset;
                draw.LineTo(new Point(outerRight,HeaderHeight-radius),true,false);
                draw.ArcTo(new Point(outerRight-radius,HeaderHeight),corner,0,false,SweepDirection.Clockwise,true,false);
                for(int i=pages.Length-1;i>=0;i--) {
                    double left=i*slot+inset;
                    draw.LineTo(new Point(left+radius,HeaderHeight),true,false);
                    draw.ArcTo(new Point(left,HeaderHeight-radius),corner,0,false,SweepDirection.Clockwise,true,false);
                    if(i>0) {
                        double previous=i*slot-inset;
                        draw.LineTo(new Point(left,bottom+join),true,false);
                        draw.ArcTo(new Point(left-join,bottom),fillet,0,false,SweepDirection.Counterclockwise,true,false);
                        draw.LineTo(new Point(previous+join,bottom),true,false);
                        draw.ArcTo(new Point(previous,bottom+join),fillet,0,false,SweepDirection.Counterclockwise,true,false);
                        draw.LineTo(new Point(previous,HeaderHeight-radius),true,false);
                        draw.ArcTo(new Point(previous-radius,HeaderHeight),corner,0,false,SweepDirection.Clockwise,true,false);
                    }
                }
                draw.LineTo(new Point(inset,radius),true,false);
                draw.ArcTo(new Point(inset+radius,0),corner,0,false,SweepDirection.Clockwise,true,false);
            }
            geometry.Freeze(); stripShape=geometry;
            header.Clip=geometry;
        }
        void SnapPages() {
            generation++;
            Set(DragOffsetProperty,0); Set(ProgressProperty,selected);
            foreach(var page in pages) {
                bool active=page==pages[selected];
                page.Host.Opacity=1;
                page.Host.Visibility=active?Visibility.Visible:Visibility.Collapsed;
                page.Host.IsHitTestVisible=active;
                KeyboardNavigation.SetTabNavigation(page.Host,active?KeyboardNavigationMode.Continue:KeyboardNavigationMode.None);
            }
            UpdateMotion();
        }
        void Set(DependencyProperty property,double value) {BeginAnimation(property,null); SetValue(property,value);}
        void Animate(DependencyProperty property,double value,int milliseconds,Action completed) {
            double from=(double)GetValue(property); BeginAnimation(property,null); SetValue(property,value);
            var animation=new DoubleAnimation(from,value,TimeSpan.FromMilliseconds(milliseconds)) {EasingFunction=Interaction.EaseOut,FillBehavior=FillBehavior.Stop};
            if(completed!=null) animation.Completed+=delegate {completed();};
            BeginAnimation(property,animation,HandoffBehavior.SnapshotAndReplace);
        }
    }
}
