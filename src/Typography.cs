using System;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Runtime.InteropServices;

namespace Reswin {
    // The font travels in Reswin.g.resources. Neither WPF nor the GDI tray menu
    // depends on a font installation on the user's computer.
    internal static class Typography {
        static System.Windows.Media.FontFamily family;
        static readonly object menuLock=new object();
        static PrivateFontCollection menuFonts;
        static System.Drawing.FontFamily menuFamily;
        static IntPtr menuMemory,gdiFont;
        static bool menuInitialized;

        internal static System.Windows.Media.FontFamily Family {
            get {
                if(family==null)
                    family=new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/"),"./assets/fonts/#Manrope");
                return family;
            }
        }
        internal static Font CreateMenuFont(float points) {
            lock(menuLock) {
                if(!menuInitialized) {
                    menuInitialized=true;
                    try {
                        var resources=new ResourceManager("Reswin.g",typeof(Typography).Assembly);
                        byte[] data;
                        using(var source=resources.GetStream("assets/fonts/manrope-regular.ttf",CultureInfo.InvariantCulture))
                        using(var buffer=new MemoryStream()) {
                            if(source==null) throw new IOException("The bundled menu font is missing.");
                            source.CopyTo(buffer); data=buffer.ToArray();
                        }
                        menuMemory=Marshal.AllocHGlobal(data.Length);
                        Marshal.Copy(data,0,menuMemory,data.Length);
                        uint count;
                        // TextRenderer uses GDI, whereas System.Drawing.Font uses
                        // GDI+. Register the same private font in both systems.
                        gdiFont=AddFontMemResourceEx(menuMemory,(uint)data.Length,IntPtr.Zero,out count);
                        if(gdiFont==IntPtr.Zero || count==0) throw new InvalidOperationException("Unable to register the bundled menu font.");
                        menuFonts=new PrivateFontCollection();
                        menuFonts.AddMemoryFont(menuMemory,data.Length);
                        var families=menuFonts.Families;
                        if(families.Length==0) throw new InvalidOperationException("The bundled menu font has no family.");
                        menuFamily=families[0];
                        for(int i=1;i<families.Length;i++) families[i].Dispose();
                        AppDomain.CurrentDomain.ProcessExit+=delegate { ReleaseMenuFont(); };
                    } catch {
                        ReleaseMenuFont();
                        // Keep the tray operable if an OS font API rejects a font.
                        // The ordinary path always uses the embedded Manrope face.
                    }
                }
                return menuFamily==null
                    ?new Font(SystemFonts.MenuFont.FontFamily,points,FontStyle.Regular,GraphicsUnit.Point)
                    :new Font(menuFamily,points,FontStyle.Regular,GraphicsUnit.Point);
            }
        }
        static void ReleaseMenuFont() {
            lock(menuLock) {
                if(menuFamily!=null) { menuFamily.Dispose(); menuFamily=null; }
                if(menuFonts!=null) { menuFonts.Dispose(); menuFonts=null; }
                if(gdiFont!=IntPtr.Zero) { RemoveFontMemResourceEx(gdiFont); gdiFont=IntPtr.Zero; }
                if(menuMemory!=IntPtr.Zero) { Marshal.FreeHGlobal(menuMemory); menuMemory=IntPtr.Zero; }
            }
        }
        [DllImport("gdi32.dll",ExactSpelling=true)]
        static extern IntPtr AddFontMemResourceEx(IntPtr memory,uint length,IntPtr reserved,out uint count);
        [DllImport("gdi32.dll",ExactSpelling=true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        static extern bool RemoveFontMemResourceEx(IntPtr handle);
    }
}
