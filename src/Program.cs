using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms=System.Windows.Forms;

namespace Reswin {
    internal static class Program {
        [STAThread] static int Main(string[] args) {
            AppContext.SetSwitch("Switch.System.Windows.DoNotScaleForDpiChanges",false);
            if(args.Length>0 && args[0]=="--fixture") { TestSuite.Fixture(args[1]); return 0; }
            if(args.Length>0 && args[0]=="--self-test") return TestSuite.Run(args.Length>1?args[1]:"test-results.txt");
            if(args.Length>0 && args[0]=="--ui-checks") return UiChecks.Run(args.Length>1?args[1]:"ui-results.txt");
            if(args.Length>0 && args[0]=="--preview") { Preview(args[1],Array.IndexOf(args,"--dark")>=0,Array.IndexOf(args,"--paused")>=0,Array.IndexOf(args,"--english")>=0); return 0; }
            string identity=System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            bool first;
            using(var mutex=new Mutex(true,"Local\\Reswin.Instance."+identity,out first)) {
                using(var show=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\Reswin.Show."+identity)) {
                    if(!first) { show.Set(); return 0; }
                    try { Run(show,args.Length>0 && args[0]=="--tray"); }
                    catch(Exception e) { MessageBox.Show(e.Message,"Reswin",MessageBoxButton.OK,MessageBoxImage.Error); return 1; }
                    finally { mutex.ReleaseMutex(); }
                }
            }
            return 0;
        }
        static void Run(EventWaitHandle show,bool trayOnly) {
            var app=new Application { ShutdownMode=ShutdownMode.OnExplicitShutdown };
            string warning; var settings=Settings.Load(out warning);
            Theme.Preference=settings.Appearance; Theme.AccentColor=settings.AccentColor; L.Language=settings.Language;
            using(var engine=new WindowEngine(settings,app.Dispatcher))
            using(var tray=new Forms.NotifyIcon())
            using(var icon=MakeIcon()) {
                MainWindow window=null;
                Action reveal=null; bool themeInstalled=false,exiting=false;
                var menu=new TrayMenu();
                var open=new Forms.ToolStripMenuItem(L.T("Открыть настройки"));
                var pause=new Forms.ToolStripMenuItem(settings.Enabled?L.T("Приостановить"):L.T("Возобновить")) { Checked=settings.Enabled,CheckOnClick=false };
                var exit=new Forms.ToolStripMenuItem(L.T("Выйти"));
                string lastStatus=warning;
                using(var persistence=new SettingsPersistence(delegate(Exception error) {
                    if(app.Dispatcher.HasShutdownStarted) return;
                    try {app.Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(delegate {
                        lastStatus=L.T("Не удалось сохранить: ")+error.Message;
                        if(window!=null) window.SetStatus(lastStatus);
                    }));} catch(InvalidOperationException) { }
                })) {
                var appliedSettings=settings.Snapshot();
                Action save=delegate {
                    bool languageChanged=L.Language!=settings.Language;
                    var snapshot=settings.Snapshot();
                    bool behaviorChanged=!Settings.SameBehavior(appliedSettings,snapshot);
                    bool enabledChanged=appliedSettings.Enabled!=snapshot.Enabled;
                    appliedSettings=snapshot;
                    L.Language=settings.Language;
                    if(languageChanged) {
                        open.Text=L.T("Открыть настройки"); exit.Text=L.T("Выйти");
                        // Retain the window, pages, scroll positions and hooks.
                        // MainWindow replaces only translated text and animates it.
                        if(window!=null && !exiting) window.RefreshLanguage();
                    }
                    if(languageChanged || enabledChanged) {
                        pause.Text=settings.Enabled?L.T("Приостановить"):L.T("Возобновить");
                        pause.Checked=settings.Enabled;
                        tray.Text=settings.Enabled?L.T("Reswin · включено"):L.T("Reswin · пауза");
                    }
                    if(behaviorChanged && !exiting) engine.Cancel();
                    persistence.Schedule(snapshot);
                };
                Action exitApplication=delegate {
                    if(exiting) return;
                    exiting=true;
                    if(window!=null) window.AllowClose=true;
                    tray.Visible=false; engine.Dispose(); app.Shutdown();
                };
                reveal=delegate {
                    if(exiting) return;
                    if(window==null) {
                        if(!themeInstalled) { Theme.Install(app); themeInstalled=true; }
                        window=new MainWindow(settings,save);
                        window.ExitRequested+=exitApplication;
                        window.Icon=System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());
                        app.MainWindow=window;
                        if(lastStatus!=null) window.SetStatus(lastStatus);
                    }
                    window.Show(); window.WindowState=WindowState.Normal; window.Activate(); Native.SetForegroundWindow(new System.Windows.Interop.WindowInteropHelper(window).Handle);
                };
                open.Click+=delegate { reveal(); }; tray.DoubleClick+=delegate { reveal(); };
                pause.Click+=delegate { settings.Enabled=!settings.Enabled; if(window!=null) window.SyncEnabled(); else save(); };
                exit.Click+=delegate {exitApplication();};
                menu.Items.Add(open); menu.Items.Add(pause); menu.Items.Add(new Forms.ToolStripSeparator()); menu.Items.Add(exit);
                tray.Icon=icon; tray.ContextMenuStrip=menu; tray.Text=settings.Enabled?L.T("Reswin · включено"):L.T("Reswin · пауза"); tray.Visible=true;
                engine.Status+=delegate(string text) { lastStatus=text; if(window!=null) window.SetStatus(text); };
                engine.Start();
                var registration=ThreadPool.RegisterWaitForSingleObject(show,delegate {
                    if(app.Dispatcher.HasShutdownStarted) return;
                    try {app.Dispatcher.BeginInvoke(reveal);} catch(InvalidOperationException) { }
                },null,Timeout.Infinite,false);
                int themeRefreshPending=0;
                UserPreferenceChangedEventHandler themeChanged=delegate(object sender,UserPreferenceChangedEventArgs e) {
                    bool accessibility=e.Category==UserPreferenceCategory.Accessibility;
                    if(!accessibility && e.Category!=UserPreferenceCategory.General && e.Category!=UserPreferenceCategory.Color && e.Category!=UserPreferenceCategory.VisualStyle) return;
                    if(!accessibility && settings.Appearance!=ThemeMode.System) return;
                    if(app.Dispatcher.HasShutdownStarted || Interlocked.CompareExchange(ref themeRefreshPending,1,0)!=0) return;
                    try {app.Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(delegate {
                        Interlocked.Exchange(ref themeRefreshPending,0);
                        if(themeInstalled) Theme.Refresh(app);
                    }));} catch(InvalidOperationException) {Interlocked.Exchange(ref themeRefreshPending,0);}
                };
                SystemEvents.UserPreferenceChanged+=themeChanged;
                try { if(!trayOnly) reveal(); app.Run(); }
                finally { registration.Unregister(null); SystemEvents.UserPreferenceChanged-=themeChanged; tray.Visible=false; menu.Dispose(); }
                }
            }
        }
        internal static System.Drawing.Icon MakeIcon() {
            using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Reswin.AppIcon"))
            using(var icon=new System.Drawing.Icon(stream,32,32)) return (System.Drawing.Icon)icon.Clone();
        }
        static void Preview(string path,bool dark,bool paused,bool english) {
            L.Language=english?AppLanguage.English:AppLanguage.Russian;
            var app=new Application(); Theme.Install(app); Theme.Refresh(app,dark);
            var settings=new Settings {Enabled=!paused,Language=L.Language};
            var window=new MainWindow(settings,delegate {},true) { ShowInTaskbar=false,AllowClose=true };
            window.Loaded+=delegate { window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(delegate {
                var content=(FrameworkElement)window.Content;
                var bitmap=new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth),(int)Math.Ceiling(content.ActualHeight),96,96,PixelFormats.Pbgra32);
                bitmap.Render(content); var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using(var output=File.Create(path)) encoder.Save(output); window.Close();
            })); };
            app.Run(window);
        }
    }
}
