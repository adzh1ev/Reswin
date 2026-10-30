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
        readonly List<ProgramRow> programRows=new List<ProgramRow>();
        bool closed,loading;
        int generation;
        sealed class ProgramRow {
            internal RunningProgram Program;
            internal Button Button;
            internal ContentControl Icon;
            internal string SearchText;
        }
        internal ProgramPicker(Settings settings) {
            this.settings=settings;
            SetResourceReference(StyleProperty,typeof(Window));
            Title=L.T("Добавить исключение"); Width=500; Height=500; MinWidth=360; MinHeight=360; WindowStartupLocation=WindowStartupLocation.CenterOwner;
            refresh.Style=(Style)Application.Current.FindResource("PrimaryButton");
            var root=new DockPanel { Margin=new Thickness(24) }; Content=root;
            var top=new StackPanel(); DockPanel.SetDock(top,Dock.Top); root.Children.Add(top);
            var title=Theme.Text(L.T("Добавить исключение"),23,false); title.FontWeight=FontWeights.SemiBold; title.Margin=new Thickness(0,0,0,7); top.Children.Add(title);
            var subtitle=Theme.Text(L.T("Выберите программу из открытых."),12,true); subtitle.Margin=new Thickness(0,0,0,14); top.Children.Add(subtitle);
            search.Margin=new Thickness(0,0,0,12); search.ToolTip=L.T("Поиск по программе или заголовку окна"); Theme.Accessible(search,L.T("Поиск открытых программ")); var searchLabel=Theme.Text(L.T("Поиск приложения"),12,true); searchLabel.Margin=new Thickness(2,0,0,5); top.Children.Add(searchLabel); top.Children.Add(search); search.TextChanged+=delegate { Render(); };
            var bottom=new DockPanel { Margin=new Thickness(0,12,0,0) }; DockPanel.SetDock(bottom,Dock.Bottom); root.Children.Add(bottom); DockPanel.SetDock(refresh,Dock.Right); bottom.Children.Add(refresh); hint.TextWrapping=TextWrapping.Wrap; bottom.Children.Add(hint);
            root.Children.Add(new SmoothScrollViewer { Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled });
            Loaded+=delegate { LoadPrograms(); search.Focus(); };
            Closed+=delegate {closed=true; generation++; rows.Children.Clear(); programRows.Clear(); programs.Clear();};
            refresh.Click+=delegate { LoadPrograms(); };
        }
        async void LoadPrograms() {
            if(loading || closed) return;
            loading=true; refresh.IsEnabled=false; hint.Text=L.T("Загружаю открытые программы…");
            try {
                // Show the list as soon as metadata is ready. Shell icon work is
                // shared, bounded and applied afterwards without delaying the UI.
                var found=await Task.Run(()=>Enumerate(false));
                if(closed) return;
                programs=found; generation++; loading=false; BuildRows(); Render();
            }
            catch(Exception e) { if(!closed) hint.Text=L.T("Не удалось получить список: ")+e.Message; }
            finally {loading=false; if(!closed) refresh.IsEnabled=true;}
        }
        internal static List<RunningProgram> Enumerate(bool loadIcons=true) {
            var found=new Dictionary<string,RunningProgram>(StringComparer.OrdinalIgnoreCase); uint own=(uint)Process.GetCurrentProcess().Id;
            var processPaths=new Dictionary<uint,string>();
            Native.EnumWindows(delegate(IntPtr window,IntPtr unused) {
                if(!Native.IsWindowVisible(window)) return true;
                uint id; Native.GetWindowThreadProcessId(window,out id); if(id==own) return true;
                string title=Native.Title(window); if(string.IsNullOrWhiteSpace(title)) return true;
                string cls=Native.Class(window); if(cls=="Progman" || cls=="WorkerW" || cls=="Shell_TrayWnd" || cls=="Shell_SecondaryTrayWnd") return true;
                int cloaked; if(Native.DwmGetWindowAttribute(window,14,out cloaked,4)==0 && cloaked!=0) return true;
                string path;
                if(!processPaths.TryGetValue(id,out path)) {path=Native.ProcessPath(id); processPaths.Add(id,path);}
                if(path==null || found.ContainsKey(path)) return true;
                string name=Path.GetFileNameWithoutExtension(path);
                try { string description=FileVersionInfo.GetVersionInfo(path).FileDescription; if(!string.IsNullOrWhiteSpace(description)) name=description; } catch { }
                ImageSource icon=loadIcons?Icons.ApplicationIcon(path):null;
                found[path]=new RunningProgram { Name=name,Title=title,Path=path,Icon=icon }; return true;
            },IntPtr.Zero);
            return found.Values.OrderBy(x=>x.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        void BuildRows() {
            rows.Children.Clear(); programRows.Clear(); int version=generation;
            foreach(var program in programs) {
                var content=new DockPanel();
                var iconHost=new ContentControl {Width=24,Height=24,Margin=new Thickness(0,0,10,0),Focusable=false,Content=Icons.Lucide("folder",24,"MutedBrush")};
                DockPanel.SetDock(iconHost,Dock.Left); content.Children.Add(iconHost);
                var labels=new StackPanel(); var name=Theme.Text(program.Name,14,false); name.TextTrimming=TextTrimming.CharacterEllipsis; name.FontWeight=FontWeights.SemiBold; name.Margin=new Thickness(0,0,0,3); labels.Children.Add(name); var title=Theme.Text(program.Title,12,true); title.TextTrimming=TextTrimming.CharacterEllipsis; labels.Children.Add(title); content.Children.Add(labels);
                var button=new Button { Content=content,HorizontalContentAlignment=HorizontalAlignment.Stretch,Margin=new Thickness(0,0,0,6),Padding=new Thickness(12),BorderThickness=new Thickness(0),ToolTip=program.Path };
                Theme.Accessible(button,L.T("Исключить ")+program.Name);
                button.Click+=delegate { if(!settings.IsExcluded(program.Path)) settings.Exclusions.Add(new Exclusion { Name=program.Name,Path=program.Path }); DialogResult=true; };
                rows.Children.Add(button);
                var row=new ProgramRow {Program=program,Button=button,Icon=iconHost,SearchText=program.Name+" "+program.Title};
                programRows.Add(row); FillIcon(row,version);
            }
        }
        async void FillIcon(ProgramRow row,int version) {
            try {
                var source=row.Program.Icon ?? await Icons.ApplicationIconAsync(row.Program.Path);
                if(closed || version!=generation || source==null) return;
                row.Program.Icon=source;
                row.Icon.Content=new Image {Source=source,Width=24,Height=24};
            } catch { }
        }
        void Render() {
            if(closed) return;
            string query=search.Text.Trim(); int visible=0;
            // Filtering keeps the existing controls and images. Typing no longer
            // rebuilds an entire tree of buttons, labels and bindings per key.
            foreach(var row in programRows) {
                bool show=!settings.IsExcluded(row.Program.Path) && (query.Length==0 || row.SearchText.IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0);
                var visibility=show?Visibility.Visible:Visibility.Collapsed;
                if(row.Button.Visibility!=visibility) row.Button.Visibility=visibility;
                if(show) visible++;
            }
            if(!loading) hint.Text=visible==0?L.T("Подходящих программ нет."):L.T("Нажмите, чтобы добавить.");
        }
    }
}
