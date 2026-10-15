using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Reswin {
    internal sealed class RunningProgram {
        internal string Name,Path,Title;
        internal ImageSource Icon;
    }
    internal sealed class ProgramPicker : Window {
        readonly Settings settings;
        readonly StackPanel rows=new StackPanel();
        readonly TextBox search=new TextBox();
        readonly TextBlock hint=Theme.Text(L.T("Загружаю открытые программы…"),12,true);
        readonly Button refresh=new Button { Content=L.T("Обновить"),Padding=new Thickness(10,5,10,5) };
        List<RunningProgram> programs=new List<RunningProgram>();
        bool closed;
        internal ProgramPicker(Settings settings) {
            this.settings=settings;
            SetResourceReference(StyleProperty,typeof(Window));
            Title=L.T("Добавить исключение"); Width=420; Height=470; MinWidth=360; MinHeight=360; WindowStartupLocation=WindowStartupLocation.CenterOwner;
            var root=new DockPanel { Margin=new Thickness(20) }; Content=root;
            var top=new StackPanel(); DockPanel.SetDock(top,Dock.Top); root.Children.Add(top);
            var title=Theme.Text(L.T("Не трогать эти окна"),21,false); title.FontWeight=FontWeights.SemiBold; title.Margin=new Thickness(0,0,0,7); top.Children.Add(title);
            var subtitle=Theme.Text(L.T("Выберите программу из открытых."),12,true); subtitle.Margin=new Thickness(0,0,0,14); top.Children.Add(subtitle);
            search.Margin=new Thickness(0,0,0,12); search.ToolTip=L.T("Поиск по программе или заголовку окна"); Theme.Accessible(search,L.T("Поиск открытых программ")); top.Children.Add(search); search.TextChanged+=delegate { Render(); };
            var bottom=new DockPanel { Margin=new Thickness(0,12,0,0) }; DockPanel.SetDock(bottom,Dock.Bottom); root.Children.Add(bottom); DockPanel.SetDock(refresh,Dock.Right); bottom.Children.Add(refresh); hint.TextWrapping=TextWrapping.Wrap; bottom.Children.Add(hint);
            root.Children.Add(new SmoothScrollViewer { Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled });
            Loaded+=delegate { LoadPrograms(); search.Focus(); }; Closed+=delegate { closed=true; }; refresh.Click+=delegate { LoadPrograms(); };
        }
        async void LoadPrograms() {
            refresh.IsEnabled=false; hint.Text=L.T("Загружаю открытые программы…");
            try { programs=await Task.Run(()=>Enumerate()); if(!closed) Render(); }
            catch(Exception e) { if(!closed) hint.Text=L.T("Не удалось получить список: ")+e.Message; }
            finally { if(!closed) refresh.IsEnabled=true; }
        }
        internal static List<RunningProgram> Enumerate() {
            var found=new Dictionary<string,RunningProgram>(StringComparer.OrdinalIgnoreCase); uint own=(uint)Process.GetCurrentProcess().Id;
            Native.EnumWindows(delegate(IntPtr window,IntPtr unused) {
                if(!Native.IsWindowVisible(window)) return true;
                uint id; Native.GetWindowThreadProcessId(window,out id); if(id==own) return true;
                string title=Native.Title(window); if(string.IsNullOrWhiteSpace(title)) return true;
                string cls=Native.Class(window); if(cls=="Progman" || cls=="WorkerW" || cls=="Shell_TrayWnd" || cls=="Shell_SecondaryTrayWnd") return true;
                int cloaked; if(Native.DwmGetWindowAttribute(window,14,out cloaked,4)==0 && cloaked!=0) return true;
                string path=Native.ProcessPath(id); if(path==null || found.ContainsKey(path)) return true;
                string name=Path.GetFileNameWithoutExtension(path);
                try { string description=FileVersionInfo.GetVersionInfo(path).FileDescription; if(!string.IsNullOrWhiteSpace(description)) name=description; } catch { }
                ImageSource icon=Icons.ApplicationIcon(path);
                found[path]=new RunningProgram { Name=name,Title=title,Path=path,Icon=icon }; return true;
            },IntPtr.Zero);
            return found.Values.OrderBy(x=>x.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        void Render() {
            rows.Children.Clear(); string query=search.Text.Trim();
            foreach(var program in programs) {
                if(settings.IsExcluded(program.Path) || (query.Length>0 && (program.Name+" "+program.Title).IndexOf(query,StringComparison.CurrentCultureIgnoreCase)<0)) continue;
                var content=new DockPanel();
                if(program.Icon!=null) { var icon=new Image { Source=program.Icon,Width=24,Height=24,Margin=new Thickness(0,0,10,0) }; DockPanel.SetDock(icon,Dock.Left); content.Children.Add(icon); }
                var labels=new StackPanel(); var name=Theme.Text(program.Name,13,false); name.TextTrimming=TextTrimming.CharacterEllipsis; labels.Children.Add(name); var title=Theme.Text(program.Title,11,true); title.TextTrimming=TextTrimming.CharacterEllipsis; labels.Children.Add(title); content.Children.Add(labels);
                var button=new Button { Content=content,HorizontalContentAlignment=HorizontalAlignment.Stretch,Margin=new Thickness(0,0,0,6),Padding=new Thickness(10),ToolTip=program.Path };
                Theme.Accessible(button,L.T("Исключить ")+program.Name);
                button.Click+=delegate { if(!settings.IsExcluded(program.Path)) settings.Exclusions.Add(new Exclusion { Name=program.Name,Path=program.Path }); DialogResult=true; };
                rows.Children.Add(button);
            }
            hint.Text=rows.Children.Count==0?L.T("Подходящих программ нет."):L.T("Нажмите, чтобы добавить.");
        }
    }
}
