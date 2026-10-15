using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace Reswin {
    internal static class TestSuite {
        static int count;
        static readonly StringBuilder log=new StringBuilder();
        static void Check(bool condition,string name) { if(!condition) throw new Exception("FAIL: "+name); count++; log.AppendLine("PASS: "+name); }
        static Native.Rect Read(IntPtr window) { Native.Rect r; Native.GetWindowRect(window,out r); return r; }
        static void Set(IntPtr window,Native.Rect r) { Native.SetWindowPos(window,IntPtr.Zero,r.Left,r.Top,r.Width,r.Height,0x4|0x10|0x4000); WaitForPosition(window,r.Left,r.Top); }
        static void WaitForPosition(IntPtr window,int left,int top) {
            var clock=Stopwatch.StartNew();
            while(clock.ElapsedMilliseconds<1500) { var actual=Read(window); if(actual.Left==left && actual.Top==top) return; Pump(); }
        }
        static void Pump() { var frame=new DispatcherFrame(); Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(delegate { frame.Continue=false; })); Dispatcher.PushFrame(frame); Thread.Sleep(100); }
        internal static int Run(string output) {
            try {
                var rect=new Native.Rect(100,100,700,700);
                Check(Geometry.Edges(rect,new Native.Point(110,110))==5,"top-left corner");
                Check(Geometry.Edges(rect,new Native.Point(690,110))==6,"top-right corner");
                Check(Geometry.Edges(rect,new Native.Point(110,690))==9,"bottom-left corner");
                Check(Geometry.Edges(rect,new Native.Point(690,690))==10,"bottom-right corner");
                Check(Geometry.Edges(rect,new Native.Point(110,400))==1,"left edge");
                Check(Geometry.Edges(rect,new Native.Point(690,400))==2,"right edge");
                Check(Geometry.Edges(rect,new Native.Point(400,110))==4,"top edge");
                Check(Geometry.Edges(rect,new Native.Point(400,690))==8,"bottom edge");
                Check(Geometry.Edges(rect,new Native.Point(320,400))==1,"central cell nearest edge");
                var resized=Geometry.Resize(rect,5,900,900,200,150,1000,1000);
                Check(resized.Right==700 && resized.Bottom==700 && resized.Width==200 && resized.Height==150,"minimum size keeps opposite corner anchored");
                resized=Geometry.Resize(rect,10,900,900,200,150,800,750);
                Check(resized.Left==100 && resized.Top==100 && resized.Width==800 && resized.Height==750,"maximum size keeps opposite corner anchored");
                var centered=Geometry.Center(new Native.Rect(0,0,400,300),new Native.Rect(-1920,0,0,1040));
                Check(centered.Left==-1160 && centered.Top==370 && centered.Width==400,"centering on negative-coordinate monitor");
                var settings=new Settings(); settings.Exclusions.Add(new Exclusion {Name="Test",Path=@"C:\Apps\Test.exe"});
                Check(settings.IsExcluded(@"c:\apps\TEST.EXE"),"case-insensitive full-path exclusion");
                Check(!settings.IsExcluded(@"D:\Apps\Test.exe"),"different executable path remains enabled");
                Check(settings.Left==WindowAction.Move && settings.Right==WindowAction.Resize && settings.Middle==WindowAction.Center && settings.Modifier==0xA2,"default bindings");
                Check(settings.Back==WindowAction.Minimize && settings.Forward==WindowAction.Maximize,"side button default bindings");
                var serializer=new System.Xml.Serialization.XmlSerializer(typeof(Settings));
                using(var stream=new MemoryStream()) { serializer.Serialize(stream,settings); stream.Position=0; var restored=(Settings)serializer.Deserialize(stream); Check(restored.IsExcluded(@"C:\Apps\Test.exe") && restored.Middle==WindowAction.Center,"settings serialization round trip"); }
                Integration(settings,output);
                log.AppendLine("SUCCESS: "+count+" checks"); File.WriteAllText(output,log.ToString()); return 0;
            } catch(Exception e) { log.AppendLine(e.ToString()); File.WriteAllText(output,log.ToString()); return 1; }
        }
        static void Integration(Settings settings,string output) {
            string handleFile=Path.GetFullPath(output)+".handle";
            if(File.Exists(handleFile)) File.Delete(handleFile);
            using(var child=Process.Start(new ProcessStartInfo { FileName=Process.GetCurrentProcess().MainModule.FileName,Arguments="--fixture \""+handleFile+"\"",UseShellExecute=false })) {
                try {
                    for(int n=0;n<100 && !File.Exists(handleFile);n++) Thread.Sleep(50);
                    Check(File.Exists(handleFile),"real test window created in separate process");
                    IntPtr window=new IntPtr(long.Parse(File.ReadAllText(handleFile))); Thread.Sleep(100);
                    using(var engine=new WindowEngine(settings,Dispatcher.CurrentDispatcher)) {
                        Native.Rect original;
                        Check(engine.Eligible(window,WindowAction.Move,out original),"normal foreign window eligible");
                        string path=Native.ProcessPath((uint)child.Id); settings.Exclusions.Add(new Exclusion {Name="Fixture",Path=path});
                        Check(!engine.Eligible(window,WindowAction.Move,out original) && !engine.Eligible(window,WindowAction.Resize,out original) && !engine.Eligible(window,WindowAction.Center,out original),"blacklist blocks all three actions on real window");
                        settings.Exclusions.RemoveAt(settings.Exclusions.Count-1);
                        original=Read(window);
                        var gesture=new WindowEngine.Gesture { Window=window,Action=WindowAction.Move,Origin=original,Start=new Native.Point(0,0),Latest=new Native.Point(71,43) };
                        engine.Apply(gesture); Thread.Sleep(100); var moved=Read(window);
                        Check(moved.Left==original.Left+71 && moved.Top==original.Top+43 && moved.Width==original.Width,"move via Windows API preserves size and offset");
                        Set(window,original);
                        gesture=new WindowEngine.Gesture {Window=window,Action=WindowAction.Resize,Origin=original,Start=new Native.Point(0,0),Latest=new Native.Point(-80,-60),Edges=5};
                        engine.Apply(gesture); Thread.Sleep(100); var large=Read(window);
                        Check(large.Left==original.Left-80 && large.Top==original.Top-60 && large.Right==original.Right && large.Bottom==original.Bottom,"real diagonal resize anchors bottom-right");
                        Set(window,original);
                        gesture=new WindowEngine.Gesture {Window=window,Action=WindowAction.Resize,Origin=original,Start=new Native.Point(0,0),Latest=new Native.Point(900,900),Edges=5};
                        engine.Apply(gesture); Thread.Sleep(100); var minimum=Read(window);
                        Check(minimum.Width>=200 && minimum.Height>=150 && minimum.Right==original.Right && minimum.Bottom==original.Bottom,"real app minimum dimensions respected");
                        Set(window,original);
                        engine.Apply(new WindowEngine.Gesture {Window=window,Action=WindowAction.Center,Origin=original}); Thread.Sleep(100);
                        var actual=Read(window); var expected=Geometry.Center(original,Native.Monitor(window).Work);
                        Check(actual.Left==expected.Left && actual.Top==expected.Top && actual.Width==original.Width && actual.Height==original.Height,"real centering in monitor work area");
                        var programs=ProgramPicker.Enumerate(); Check(programs.Exists(x=>string.Equals(x.Path,path,StringComparison.OrdinalIgnoreCase)),"open-program picker finds real target process");
                        Native.ShowWindowAsync(window,3); Thread.Sleep(150);
                        Check(!engine.Eligible(window,WindowAction.Resize,out original),"maximized window resize passes through");
                        original=Read(window);
                        engine.Apply(new WindowEngine.Gesture { Window=window,Action=WindowAction.Move,Origin=original,Start=new Native.Point(original.Left+200,original.Top+100),Latest=new Native.Point(original.Left+230,original.Top+130) }); Thread.Sleep(150);
                        Check(!Native.IsZoomed(window),"moving maximized window restores it");
                        Native.SetForegroundWindow(window); Thread.Sleep(100);
                        var before=Read(window); var point=new Native.Point(before.Left+before.Width/2,before.Top+before.Height/2);
                        Check(Native.GetAncestor(Native.WindowFromPoint(point),2)==window,"event target is the fixture window");
                        Check(!engine.HandleMouse(0x201,point,false),"plain mouse press passes through");
                        Check(engine.HandleMouse(0x201,point,true),"modifier plus left press is consumed");
                        var end=new Native.Point(point.X+35,point.Y+24);
                        Check(!engine.HandleMouse(0x200,end,true),"active movement passes through so the system cursor can move");
                        Check(engine.HandleMouse(0x202,end,true),"matching release consumed"); Pump();
                        WaitForPosition(window,before.Left+35,before.Top+24);
                        var after=Read(window); Check(after.Left==before.Left+35 && after.Top==before.Top+24,"event sequence moves real window, retaining latest coalesced position (actual "+after.Left+","+after.Top+"; expected "+(before.Left+35)+","+(before.Top+24)+")");
                        point=new Native.Point(after.Left+after.Width/2,after.Top+after.Height/2);
                        Check(!engine.HandleMouse(0x202,point,false),"ordinary release after gesture passes through");
                        Check(engine.HandleMouse(0x201,point,true),"second drag starts"); Pump();
                        engine.Cancel(); Pump();
                        Check(!engine.HandleMouse(0x200,new Native.Point(point.X+100,point.Y+100),false),"modifier release stops movement immediately");
                        Check(engine.HandleMouse(0x202,point,false),"mouse release still consumed after modifier release");
                        Check(Read(window).Left==after.Left,"cancelled gesture does not resume");
                        settings.Enabled=false; Check(!engine.HandleMouse(0x201,point,true),"pause passes mouse input through"); settings.Enabled=true;
                        settings.Left=WindowAction.None; Check(!engine.HandleMouse(0x201,point,true),"unassigned mouse action passes through"); settings.Left=WindowAction.Move;
                        Check(engine.HandleMouse(0x207,point,true),"middle press centers"); Pump();
                        Check(engine.HandleMouse(0x208,point,true),"middle release consumed");
                        after=Read(window); expected=Geometry.Center(after,Native.Monitor(window).Work);
                        Check(after.Left==expected.Left && after.Top==expected.Top,"middle button event actually centers window");
                        point=new Native.Point(after.Left+after.Width/2,after.Top+after.Height/2);
                        Check(engine.HandleMouse(0x20B,point,true,2u<<16),"forward side button press consumed"); Pump();
                        Check(engine.HandleMouse(0x20C,point,true,2u<<16),"forward side button release consumed");
                        Check(Native.IsZoomed(window),"forward side button maximizes the window");
                        var maximized=Read(window); point=new Native.Point(maximized.Left+maximized.Width/2,maximized.Top+maximized.Height/2);
                        Check(engine.HandleMouse(0x20B,point,true,2u<<16),"second forward press consumed"); Pump();
                        Check(engine.HandleMouse(0x20C,point,true,2u<<16),"second forward release consumed");
                        Check(!Native.IsZoomed(window),"second forward press restores the window");
                        var restored=Read(window);
                        Check(restored.Left==after.Left && restored.Top==after.Top && restored.Width==after.Width && restored.Height==after.Height,"restore preserves previous size and position");
                        point=new Native.Point(restored.Left+restored.Width/2,restored.Top+restored.Height/2);
                        Check(engine.HandleMouse(0x20B,point,true,1u<<16),"back side button press consumed"); Pump();
                        Check(engine.HandleMouse(0x20C,point,false,1u<<16),"back release consumed after modifier release");
                        Check(Native.IsIconic(window),"back side button minimizes the window");
                        engine.Start(); engine.Dispose(); Check(true,"both global hooks install and uninstall");
                    }
                } finally { if(!child.HasExited) { child.CloseMainWindow(); if(!child.WaitForExit(2000)) child.Kill(); } if(File.Exists(handleFile)) File.Delete(handleFile); }
            }
        }
        internal static void Fixture(string handleFile) {
            Forms.Application.EnableVisualStyles();
            using(var window=new Forms.Form {Text="Reswin integration fixture",TopMost=true,Width=420,Height=300,MinimumSize=new System.Drawing.Size(200,150),StartPosition=Forms.FormStartPosition.CenterScreen}) {
                window.Shown+=delegate { File.WriteAllText(handleFile,window.Handle.ToInt64().ToString()); };
                Forms.Application.Run(window);
            }
        }
    }
}
