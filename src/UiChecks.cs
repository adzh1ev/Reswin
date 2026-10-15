using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace Reswin {
    internal static class UiChecks {
        static IEnumerable<T> Find<T>(DependencyObject parent) where T:DependencyObject {
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++) {
                var child=VisualTreeHelper.GetChild(parent,i); if(child is T) yield return (T)child;
                foreach(var item in Find<T>(child)) yield return item;
            }
        }
        internal static int Run(string report) {
            var log=new StringBuilder(); int count=0; MainWindow window=null;
            Action<bool,string> check=delegate(bool value,string name) { if(!value) throw new Exception("FAIL: "+name); log.AppendLine("PASS: "+name); count++; };
            try {
                var app=new Application { ShutdownMode=ShutdownMode.OnExplicitShutdown }; Theme.Install(app);
                var settings=new Settings(); string executable=System.Reflection.Assembly.GetExecutingAssembly().Location;
                settings.Exclusions.Add(new Exclusion { Name="First",Path=executable });
                window=new MainWindow(settings,delegate {},true) { AllowClose=true,ShowInTaskbar=false };
                window.Show(); window.UpdateLayout();
                var scroll=Find<SmoothScrollViewer>(window).Single();
                var first=Find<Button>(window).First(b=>(string)b.ToolTip==L.T("Убрать из чёрного списка"));
                double before=first.TransformToAncestor(window).Transform(new Point(0,0)).X;
                check(scroll.ScrollableHeight==0,"short exclusion list fits without scrolling");
                for(int i=0;i<20;i++) settings.Exclusions.Add(new Exclusion { Name="Application "+i,Path=executable });
                window.RenderExclusions(); window.UpdateLayout();
                first=Find<Button>(window).First(b=>(string)b.ToolTip==L.T("Убрать из чёрного списка"));
                double after=first.TransformToAncestor(window).Transform(new Point(0,0)).X;
                check(scroll.ScrollableHeight>0,"long exclusion list scrolls inside its card");
                check(Math.Abs(before-after)<0.1,"remove buttons keep their horizontal position when scrollbar appears");
                check(Find<SmoothScrollViewer>(window).Count()==1,"main window has only the exclusion scroller");
                var combo=Find<ComboBox>(window).First(); combo.IsDropDownOpen=true;
                var frame=new DispatcherFrame(); app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(delegate { frame.Continue=false; })); Dispatcher.PushFrame(frame);
                var popup=combo.Template.FindName("PART_Popup",combo) as Popup;
                check(popup!=null && popup.IsOpen,"dropdown opens successfully");
                check(popup.Child.RenderTransform is TranslateTransform,"dropdown uses slide animation");
                combo.IsDropDownOpen=false;
                var toggle=Find<CheckBox>(window).First(); toggle.IsChecked=!toggle.IsChecked;
                var thumb=(FrameworkElement)toggle.Template.FindName("Thumb",toggle);
                check(thumb.RenderTransform is TranslateTransform && ((TranslateTransform)thumb.RenderTransform).HasAnimatedProperties,"switch uses animated movement");
                window.Close(); window=null;
                L.Language=AppLanguage.English; settings.Language=AppLanguage.English;
                window=new MainWindow(settings,delegate {},true) { AllowClose=true,ShowInTaskbar=false }; window.Show(); window.UpdateLayout();
                check(Find<TextBlock>(window).Any(t=>t.Text=="Exclusions") && Find<TextBlock>(window).Any(t=>t.Text=="Language"),"English labels appear in settings");
                var picker=new ProgramPicker(settings); picker.UpdateLayout();
                check(picker.Title=="Add an exclusion","program picker title is localized"); picker.Close();
                using(var icon=Program.MakeIcon()) check(icon.Width==32 && icon.Height==32,"embedded application icon loads at tray size");
                log.AppendLine("SUCCESS: "+count+" UI checks"); File.WriteAllText(report,log.ToString()); return 0;
            } catch(Exception e) { log.AppendLine(e.ToString()); File.WriteAllText(report,log.ToString()); return 1; }
            finally { if(window!=null) window.Close(); L.Language=AppLanguage.Russian; }
        }
    }
}
