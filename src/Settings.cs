using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Reswin {
    public enum WindowAction { Move, Resize, Center, None, Minimize, Maximize }
    // Legacy values remain readable so older XML settings can migrate without losing bindings.
    public enum ScrollBinding { None, OtherModifier, CtrlAlt, Space, F8 }
    public enum ThemeMode { System, Light, Dark }
    public class Exclusion { public string Name {get;set;} public string Path {get;set;} }
    public class Settings {
        public const double MinScrollSensitivity=0.25;
        public const double MaxScrollSensitivity=2.0;
        internal static readonly string[] AccentColors={"#D03E36","#85D9F9","#BEF5AC","#B3B27C","#FEC28F","#9FB7CC","#FDEDB1"};
        static readonly Lazy<XmlSerializer> serializer=new Lazy<XmlSerializer>(delegate {return new XmlSerializer(typeof(Settings));});
        static readonly object persistenceGate=new object();
        public bool Enabled {get;set;}
        public ThemeMode Appearance {get;set;}
        public AppLanguage Language {get;set;}
        public string AccentColor {get;set;}
        public bool MinimizeToTray {get;set;}
        public int Modifier {get;set;}
        public ScrollBinding ScrollKey {get;set;}
        public double ScrollSensitivity {get;set;}
        public bool MaximizeOnTopEdge {get;set;}
        public bool MaximizeOnDoubleClick {get;set;}
        public WindowAction Left {get;set;}
        public WindowAction Right {get;set;}
        public WindowAction Middle {get;set;}
        public WindowAction Back {get;set;}
        public WindowAction Forward {get;set;}
        public List<Exclusion> Exclusions {get;set;}
        public Settings() { Enabled=true; Modifier=0xA2; ScrollKey=ScrollBinding.OtherModifier; ScrollSensitivity=1.0; MaximizeOnTopEdge=true; MaximizeOnDoubleClick=true; AccentColor="#D03E36"; MinimizeToTray=true; Left=WindowAction.Move; Right=WindowAction.Resize; Middle=WindowAction.Center; Back=WindowAction.Minimize; Forward=WindowAction.Maximize; Exclusions=new List<Exclusion>(); }
        internal static string NormalizeAccentColor(string value) {
            if(value!=null) {
                value=value.Trim();
                foreach(string color in AccentColors) if(string.Equals(value,color,StringComparison.OrdinalIgnoreCase)) return color;
            }
            return "#D03E36";
        }
        internal static double NormalizeScrollSensitivity(double value) {
            if(double.IsNaN(value) || double.IsInfinity(value)) return 1.0;
            return Math.Max(MinScrollSensitivity,Math.Min(MaxScrollSensitivity,value));
        }
        public bool IsExcluded(string path) { return Exclusions.Exists(x=>string.Equals(x.Path,path,StringComparison.OrdinalIgnoreCase)); }
        public WindowAction Action(int button) { return button==0?Left:button==1?Right:button==2?Middle:button==3?Back:button==4?Forward:WindowAction.None; }
        internal static string Location { get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Reswin","settings.xml"); } }
        internal static Settings Load(out string warning) {
            warning=null;
            try {
                if(!File.Exists(Location)) return new Settings();
                using(var stream=File.OpenRead(Location)) {
                    Settings value;
                    lock(persistenceGate) value=(Settings)serializer.Value.Deserialize(stream);
                    if(value.ScrollKey!=ScrollBinding.None) value.ScrollKey=ScrollBinding.OtherModifier;
                    value.ScrollSensitivity=NormalizeScrollSensitivity(value.ScrollSensitivity);
                    if(value.Modifier!=0xA2 && value.Modifier!=0xA4) value.Modifier=0xA2;
                    if(!Enum.IsDefined(typeof(ThemeMode),value.Appearance)) value.Appearance=ThemeMode.System;
                    if(!Enum.IsDefined(typeof(AppLanguage),value.Language)) value.Language=AppLanguage.Russian;
                    value.AccentColor=NormalizeAccentColor(value.AccentColor);
                    if(value.Exclusions==null) value.Exclusions=new List<Exclusion>();
                    value.Exclusions.RemoveAll(x=>x==null || string.IsNullOrWhiteSpace(x.Path));
                    if(!Enum.IsDefined(typeof(WindowAction),value.Left)) value.Left=WindowAction.Move;
                    if(!Enum.IsDefined(typeof(WindowAction),value.Right)) value.Right=WindowAction.Resize;
                    if(!Enum.IsDefined(typeof(WindowAction),value.Middle)) value.Middle=WindowAction.Center;
                    if(!Enum.IsDefined(typeof(WindowAction),value.Back)) value.Back=WindowAction.Minimize;
                    if(!Enum.IsDefined(typeof(WindowAction),value.Forward)) value.Forward=WindowAction.Maximize;
                    return value;
                }
            } catch(Exception e) { warning=L.T("Не удалось прочитать настройки: ")+e.Message; return new Settings(); }
        }
        internal void Save() {
            lock(persistenceGate) {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Location));
                string temp=Location+".tmp";
                try {
                    using(var stream=File.Create(temp)) serializer.Value.Serialize(stream,this);
                    if(File.Exists(Location)) File.Replace(temp,Location,null); else File.Move(temp,Location);
                } catch {
                    try { if(File.Exists(temp)) File.Delete(temp); } catch { }
                    throw;
                }
            }
        }
        // Capture on the UI thread. The background writer owns this independent
        // object and never enumerates the live exclusions while they are edited.
        internal Settings Snapshot() {
            var snapshot=(Settings)MemberwiseClone();
            snapshot.AccentColor=NormalizeAccentColor(AccentColor);
            snapshot.Exclusions=new List<Exclusion>(Exclusions.Count);
            foreach(var exclusion in Exclusions) snapshot.Exclusions.Add(new Exclusion {Name=exclusion.Name,Path=exclusion.Path});
            return snapshot;
        }
        internal static bool SameBehavior(Settings a,Settings b) {
            if(a.Enabled!=b.Enabled || a.Modifier!=b.Modifier || a.ScrollKey!=b.ScrollKey || a.ScrollSensitivity!=b.ScrollSensitivity ||
                a.MaximizeOnTopEdge!=b.MaximizeOnTopEdge || a.MaximizeOnDoubleClick!=b.MaximizeOnDoubleClick ||
                a.Left!=b.Left || a.Right!=b.Right || a.Middle!=b.Middle || a.Back!=b.Back || a.Forward!=b.Forward ||
                a.Exclusions.Count!=b.Exclusions.Count) return false;
            for(int i=0;i<a.Exclusions.Count;i++) if(!string.Equals(a.Exclusions[i].Path,b.Exclusions[i].Path,StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
    }
    internal static class Geometry {
        // Corner thirds choose two edges. The middle cell chooses its nearest edge.
        internal static int Edges(Native.Rect r,Native.Point p) {
            double x=(p.X-r.Left)/(double)Math.Max(1,r.Width), y=(p.Y-r.Top)/(double)Math.Max(1,r.Height);
            int edges=(x<1.0/3?1:x>2.0/3?2:0)|(y<1.0/3?4:y>2.0/3?8:0);
            if(edges!=0) return edges;
            int left=p.X-r.Left,right=r.Right-p.X,top=p.Y-r.Top,bottom=r.Bottom-p.Y;
            int min=Math.Min(Math.Min(left,right),Math.Min(top,bottom));
            return min==left?1:min==right?2:min==top?4:8;
        }
        internal static Native.Rect Resize(Native.Rect r,int edges,int dx,int dy,int minW,int minH,int maxW,int maxH) {
            if((edges&1)!=0) r.Left=r.Right-Clamp(r.Width-dx,minW,maxW);
            else if((edges&2)!=0) r.Right=r.Left+Clamp(r.Width+dx,minW,maxW);
            if((edges&4)!=0) r.Top=r.Bottom-Clamp(r.Height-dy,minH,maxH);
            else if((edges&8)!=0) r.Bottom=r.Top+Clamp(r.Height+dy,minH,maxH);
            return r;
        }
        internal static int Clamp(int n,int min,int max) { return Math.Max(min,Math.Min(Math.Max(min,max),n)); }
        internal static Native.Rect Center(Native.Rect r,Native.Rect work) {
            int x=work.Left+(work.Width-r.Width)/2,y=work.Top+(work.Height-r.Height)/2;
            return new Native.Rect(x,y,x+r.Width,y+r.Height);
        }
    }
}
