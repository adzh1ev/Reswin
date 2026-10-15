using System;
using System.Reflection;
using Microsoft.Win32;

namespace Reswin {
    internal static class Startup {
        const string KeyPath=@"Software\Microsoft\Windows\CurrentVersion\Run";
        static string Command { get { return "\""+Assembly.GetExecutingAssembly().Location+"\" --tray"; } }
        internal static bool Enabled {
            get { using(var key=Registry.CurrentUser.OpenSubKey(KeyPath)) return key!=null && string.Equals(key.GetValue("Reswin") as string,Command,StringComparison.OrdinalIgnoreCase); }
        }
        // Called only by an explicit user click. No startup changes during launch or preview.
        internal static void SetEnabled(bool enabled) {
            using(var key=Registry.CurrentUser.CreateSubKey(KeyPath)) {
                if(key==null) throw new InvalidOperationException(L.T("Не удалось открыть настройки автозапуска Windows."));
                if(enabled) key.SetValue("Reswin",Command,RegistryValueKind.String);
                else key.DeleteValue("Reswin",false);
            }
        }
    }
}
