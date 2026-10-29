using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Reswin {
    internal static class Native {
        static bool arrangedAvailable=true;
        internal delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
        internal delegate bool EnumProc(IntPtr window, IntPtr data);
        [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X,Y; public Point(int x,int y) { X=x; Y=y; } }
        [StructLayout(LayoutKind.Sequential)] internal struct Rect {
            public int Left,Top,Right,Bottom;
            public Rect(int l,int t,int r,int b) { Left=l;Top=t;Right=r;Bottom=b; }
            public int Width { get { return Right-Left; } } public int Height { get { return Bottom-Top; } }
        }
        [StructLayout(LayoutKind.Sequential)] internal struct MouseData { public Point Point; public uint Mouse,Flags,Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct KeyData { public uint Key,Scan,Flags,Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct MonitorInfo { public int Size; public Rect Monitor,Work; public uint Flags; }
        [StructLayout(LayoutKind.Sequential)] internal struct MinMax { public Point Reserved,MaxSize,MaxPosition,MinTrack,MaxTrack; }
        [StructLayout(LayoutKind.Sequential)] internal struct Placement { public int Length,Flags,Show; public Point Min,Max; public Rect Normal; }
        [StructLayout(LayoutKind.Sequential)] internal struct KeyboardInput { public ushort Key,Scan; public uint Flags,Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct MouseInput { public int X,Y; public uint Data,Flags,Time; public UIntPtr Extra; }
        [StructLayout(LayoutKind.Explicit,Size=40)] internal struct Input { [FieldOffset(0)] public uint Type; [FieldOffset(8)] public KeyboardInput Keyboard; [FieldOffset(8)] public MouseInput Mouse; }
        [DllImport("user32.dll",SetLastError=true)] internal static extern uint SendInput(uint count,Input[] inputs,int size);
        internal static uint Inject(params Input[] inputs) { return SendInput((uint)inputs.Length,inputs,Marshal.SizeOf(typeof(Input))); }
        [DllImport("user32.dll", SetLastError=true)] internal static extern IntPtr SetWindowsHookEx(int type,HookProc proc,IntPtr module,uint thread);
        [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll",EntryPoint="SendMessageTimeoutW",SetLastError=true)] internal static extern IntPtr SendMessageTimeoutValue(IntPtr window,uint msg,IntPtr w,IntPtr l,uint flags,uint timeout,out IntPtr result);
        [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] internal static extern uint GetDoubleClickTime();
        [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] static extern int GetSystemMetricsForDpi(int index,uint dpi);
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] internal static extern IntPtr WindowFromPoint(Point point);
        [DllImport("user32.dll")] internal static extern IntPtr GetAncestor(IntPtr window,uint flags);
        [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll")] internal static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
        [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool IsZoomed(IntPtr window);
        [DllImport("user32.dll",EntryPoint="IsWindowArranged")] static extern bool IsWindowArrangedCore(IntPtr window);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetWindowText(IntPtr window,StringBuilder text,int count);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetClassName(IntPtr window,StringBuilder text,int count);
        [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc proc,IntPtr data);
        [DllImport("user32.dll")] internal static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
        [DllImport("user32.dll")] internal static extern IntPtr MonitorFromPoint(Point point,uint flags);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
        [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
        [DllImport("user32.dll",SetLastError=true)] internal static extern IntPtr SendMessageTimeout(IntPtr window,uint msg,IntPtr w,ref MinMax l,uint flags,uint timeout,out IntPtr result);
        [DllImport("user32.dll")] internal static extern bool ShowWindowAsync(IntPtr window,int command);
        [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr window,int command);
        [DllImport("user32.dll")] internal static extern bool GetWindowPlacement(IntPtr window,ref Placement placement);
        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr OpenProcess(uint access,bool inherit,uint id);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] internal static extern bool QueryFullProcessImageName(IntPtr process,uint flags,StringBuilder path,ref int size);
        [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
        [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(IntPtr window,int attribute,out int value,int size);
        [DllImport("advapi32.dll",SetLastError=true)] internal static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
        [DllImport("advapi32.dll",SetLastError=true)] internal static extern bool GetTokenInformation(IntPtr token,int type,IntPtr information,int length,out int returned);
        [DllImport("advapi32.dll")] internal static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);
        [DllImport("advapi32.dll")] internal static extern IntPtr GetSidSubAuthority(IntPtr sid,uint index);
        [DllImport("user32.dll")] internal static extern bool DestroyIcon(IntPtr icon);
        internal static string Title(IntPtr window) { var b=new StringBuilder(512); GetWindowText(window,b,b.Capacity); return b.ToString(); }
        internal static string Class(IntPtr window) { var b=new StringBuilder(128); GetClassName(window,b,b.Capacity); return b.ToString(); }
        internal static string ProcessPath(uint id) {
            IntPtr process=OpenProcess(0x1000,false,id); if(process==IntPtr.Zero) return null;
            try { var b=new StringBuilder(32768); int size=b.Capacity; return QueryFullProcessImageName(process,0,b,ref size)?b.ToString():null; }
            finally { CloseHandle(process); }
        }
        internal static int Integrity(uint id) {
            IntPtr process=OpenProcess(0x1000,false,id),token=IntPtr.Zero,buffer=IntPtr.Zero;
            if(process==IntPtr.Zero) return int.MaxValue;
            try {
                if(!OpenProcessToken(process,8,out token)) return int.MaxValue;
                int length; GetTokenInformation(token,25,IntPtr.Zero,0,out length);
                buffer=Marshal.AllocHGlobal(length);
                if(!GetTokenInformation(token,25,buffer,length,out length)) return int.MaxValue;
                IntPtr sid=Marshal.ReadIntPtr(buffer); byte count=Marshal.ReadByte(GetSidSubAuthorityCount(sid));
                return Marshal.ReadInt32(GetSidSubAuthority(sid,(uint)(count-1)));
            } finally { if(buffer!=IntPtr.Zero) Marshal.FreeHGlobal(buffer); if(token!=IntPtr.Zero) CloseHandle(token); CloseHandle(process); }
        }
        internal static MonitorInfo Monitor(IntPtr window) {
            var info=new MonitorInfo(); info.Size=Marshal.SizeOf(info); GetMonitorInfo(MonitorFromWindow(window,2),ref info); return info;
        }
        internal static uint WindowDpi(IntPtr window) {
            try { uint dpi=GetDpiForWindow(window); return dpi==0?96:dpi; }
            catch(EntryPointNotFoundException) { return 96; }
        }
        internal static int PointerMetric(int index,uint dpi) {
            try { int value=GetSystemMetricsForDpi(index,dpi); if(value!=0) return Math.Max(1,Math.Abs(value)); }
            catch(EntryPointNotFoundException) { }
            return Math.Max(1,Math.Abs(GetSystemMetrics(index)));
        }
        internal static bool IsArranged(IntPtr window) {
            // Available from Windows10 1903. Older systems keep the original
            // maximized-window path without depending on a missing export.
            if(!arrangedAvailable) return false;
            try { return IsWindowArrangedCore(window); }
            catch(EntryPointNotFoundException) { arrangedAvailable=false; return false; }
        }
        static Input SnapKey(int key,bool up=false) {
            bool extended=key==0x5B || key==0x25 || key==0x27;
            return new Input {Type=1,Keyboard=new KeyboardInput {Key=(ushort)key,Flags=(extended?1u:0u)|(up?2u:0u)}};
        }
        internal static bool CanSendSnapKeys(int modifier) {
            if(modifier!=0xA2 && modifier!=0xA4) return false;
            // Never turn Ctrl+Win+Arrow into a desktop switch, or temporarily
            // release a user's extra Shift/Win/AltGr/other shortcut combination.
            for(int key=1;key<256;key++) {
                if(key==modifier || key==(modifier==0xA2?0x11:0x12)) continue;
                if((GetAsyncKeyState(key)&0x8000)!=0) return false;
            }
            return true;
        }
        internal static bool SnapToSide(IntPtr window,bool right,int modifier) {
            if(GetForegroundWindow()!=window || !CanSendSnapKeys(modifier)) return false;
            bool held=(GetAsyncKeyState(modifier)&0x8000)!=0;
            int arrow=right?0x27:0x25;
            var sequence=new System.Collections.Generic.List<Input>();
            // Focus may have moved from the previously active application.
            // Mask Alt in this target too before its temporary key-up.
            if(held && modifier==0xA4) { sequence.Add(SnapKey(0xE8)); sequence.Add(SnapKey(0xE8,true)); }
            if(held) sequence.Add(SnapKey(modifier,true));
            sequence.Add(SnapKey(0x5B)); sequence.Add(SnapKey(arrow));
            sequence.Add(SnapKey(arrow,true)); sequence.Add(SnapKey(0x5B,true));
            if(held) {
                sequence.Add(SnapKey(modifier));
                // A restored Alt must not become a bare menu key on its real up.
                if(modifier==0xA4) { sequence.Add(SnapKey(0xE8)); sequence.Add(SnapKey(0xE8,true)); }
            }
            // SendInput guarantees this complete array is not interleaved with
            // physical input. Existing key state is accounted for above.
            Input[] inputs=sequence.ToArray();
            uint sent=Inject(inputs);
            if(sent==inputs.Length) return true;
            if(sent==0) return false;
            // If an input filter accepts only a prefix, balance only our downs;
            // never replay a rejected arrow without its Windows modifier.
            bool winDown=false,arrowDown=false,modifierReleased=false,maskDown=false;
            bool modifierRestored=false,maskAfterRestore=false;
            for(int i=0;i<Math.Min((int)sent,inputs.Length);i++) {
                int key=inputs[i].Keyboard.Key; bool up=(inputs[i].Keyboard.Flags&2)!=0;
                if(key==0x5B) winDown=!up;
                else if(key==arrow) arrowDown=!up;
                else if(key==modifier) { modifierReleased=up; if(!up) modifierRestored=true; }
                else if(key==0xE8) { maskDown=!up; if(!up && modifierRestored) maskAfterRestore=true; }
            }
            var balance=new System.Collections.Generic.List<Input>();
            if(maskDown) balance.Add(SnapKey(0xE8,true));
            if(arrowDown) balance.Add(SnapKey(arrow,true));
            if(winDown) {
                balance.Add(SnapKey(0xE8)); balance.Add(SnapKey(0xE8,true));
                balance.Add(SnapKey(0x5B,true));
            }
            if(modifierReleased) {
                balance.Add(SnapKey(modifier));
                if(modifier==0xA4) { balance.Add(SnapKey(0xE8)); balance.Add(SnapKey(0xE8,true)); }
            } else if(modifier==0xA4 && modifierRestored && !maskAfterRestore) {
                balance.Add(SnapKey(0xE8)); balance.Add(SnapKey(0xE8,true));
            }
            if(balance.Count!=0) Inject(balance.ToArray());
            return false;
        }
        internal static void MaskAltMenu() {
            // An otherwise unused key prevents a consumed Alt+mouse gesture from
            // becoming a bare Alt menu activation. Alt down/up itself still passes
            // through, so applications never receive an unmatched modifier.
            var down=new Input {Type=1,Keyboard=new KeyboardInput {Key=0xE8}};
            var up=new Input {Type=1,Keyboard=new KeyboardInput {Key=0xE8,Flags=2}};
            SendInput(2,new[]{down,up},Marshal.SizeOf(typeof(Input)));
        }
    }
}
