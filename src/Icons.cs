using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using System.Collections.Concurrent;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Reswin {
    internal static class Icons {
        static readonly ConcurrentDictionary<string,ImageSource> appIcons=new ConcurrentDictionary<string,ImageSource>(StringComparer.OrdinalIgnoreCase);
        internal static ImageSource ApplicationIcon(string path) {
            ImageSource cached; if(appIcons.TryGetValue(path,out cached)) return cached;
            try {
                using(var icon=System.Drawing.Icon.ExtractAssociatedIcon(path)) {
                    if(icon==null) return null;
                    var bitmap=Imaging.CreateBitmapSourceFromHIcon(icon.Handle,Int32Rect.Empty,BitmapSizeOptions.FromWidthAndHeight(32,32)); bitmap.Freeze();
                    appIcons[path]=bitmap; return bitmap;
                }
            } catch { return null; }
        }
        // The user's original SVG paths are embedded in the portable executable.
        internal static FrameworkElement Mouse(string file) {
            var canvas=new Canvas { Width=512,Height=512,IsHitTestVisible=false };
            var document=new XmlDocument();
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Reswin.Icons."+file)) document.Load(stream);
            foreach(XmlNode node in document.DocumentElement.ChildNodes) {
                var element=node as XmlElement; if(element==null) continue;
                Shape shape;
                if(element.LocalName=="path") shape=new Path { Data=System.Windows.Media.Geometry.Parse(element.GetAttribute("d")) };
                else if(element.LocalName=="rect") {
                    shape=new Rectangle { Width=Number(element,"width"),Height=Number(element,"height"),RadiusX=Number(element,"rx"),RadiusY=Number(element,"rx") };
                    Canvas.SetLeft(shape,Number(element,"x")); Canvas.SetTop(shape,Number(element,"y"));
                } else continue;
                if(element.GetAttribute("fill")=="black") shape.SetResourceReference(Shape.FillProperty,"TextBrush");
                if(element.GetAttribute("stroke")=="black") { shape.SetResourceReference(Shape.StrokeProperty,"TextBrush"); shape.StrokeThickness=Number(element,"stroke-width"); }
                shape.StrokeStartLineCap=shape.StrokeEndLineCap=PenLineCap.Round; shape.StrokeLineJoin=PenLineJoin.Round;
                canvas.Children.Add(shape);
            }
            return new Viewbox { Child=canvas,Width=32,Height=32,Stretch=Stretch.Uniform,IsHitTestVisible=false,Focusable=false };
        }
        static double Number(XmlElement element,string attribute) { string value=element.GetAttribute(attribute); return value.Length==0?0:double.Parse(value,CultureInfo.InvariantCulture); }
        internal static FrameworkElement Lucide(string name,double size=20,string brush="TextBrush") {
            var canvas=new Canvas {Width=24,Height=24}; var document=new XmlDocument();
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Reswin.Icons.lucide-"+name+".svg")) document.Load(stream);
            foreach(XmlNode node in document.DocumentElement.ChildNodes) {
                var el=node as XmlElement; if(el==null) continue; Shape shape;
                switch(el.LocalName) {
                    case "path": shape=new Path {Data=System.Windows.Media.Geometry.Parse(el.GetAttribute("d"))}; break;
                    case "rect": shape=new Rectangle {Width=Number(el,"width"),Height=Number(el,"height"),RadiusX=Number(el,"rx"),RadiusY=Number(el,"rx")}; Canvas.SetLeft(shape,Number(el,"x")); Canvas.SetTop(shape,Number(el,"y")); break;
                    case "circle": double radius=Number(el,"r"); shape=new Ellipse {Width=radius*2,Height=radius*2}; Canvas.SetLeft(shape,Number(el,"cx")-radius); Canvas.SetTop(shape,Number(el,"cy")-radius); break;
                    case "line": shape=new Line {X1=Number(el,"x1"),Y1=Number(el,"y1"),X2=Number(el,"x2"),Y2=Number(el,"y2")}; break;
                    case "polyline": shape=new Polyline {Points=PointCollection.Parse(el.GetAttribute("points"))}; break;
                    default: continue;
                }
                shape.StrokeThickness=1.8; shape.StrokeStartLineCap=shape.StrokeEndLineCap=PenLineCap.Round; shape.StrokeLineJoin=PenLineJoin.Round;
                if(brush=="ButtonForeground") shape.SetBinding(Shape.StrokeProperty,new Binding("Foreground") {RelativeSource=new RelativeSource(RelativeSourceMode.FindAncestor,typeof(Button),1)});
                else shape.SetResourceReference(Shape.StrokeProperty,brush);
                canvas.Children.Add(shape);
            }
            return new Viewbox {Child=canvas,Width=size,Height=size,IsHitTestVisible=false,Focusable=false};
        }
        internal static FrameworkElement Symbol(string name,double size=24,string brush="TextBrush") {
            if(name=="plus" || name=="folder" || name=="ban" || name=="close") return Lucide(name=="close"?"x":name,size,brush);
            string data;
            switch(name) {
                case "plus": data="M12,3 L12,21 M3,12 L21,12"; break;
                case "language": data="M12,2 A10,10 0 1 1 11.99,2 M2,12 L22,12 M12,2 C6,7 6,17 12,22 C18,17 18,7 12,2"; break;
                case "theme": data="M12,3 A9,9 0 1 1 11.99,3 M12,3 L12,21"; break;
                case "close": data="M5,5 L19,19 M19,5 L5,19"; break;
                case "minimize": data="M5,12 L19,12"; break;
                case "maximize": data="M5,5 L19,5 L19,19 L5,19 Z"; break;
                case "restore": data="M8,5 L19,5 L19,16 M5,8 L16,8 L16,19 L5,19 Z"; break;
                case "ban": data="M12,2 A10,10 0 1 1 11.99,2 M5,5 L19,19"; break;
                case "folder": data="M2,7 L2,5 Q2,3 4,3 L9,3 L12,6 L20,6 Q22,6 22,8 L22,19 Q22,21 20,21 L4,21 Q2,21 2,19 Z M2,8 L22,8"; break;
                case "windows": data="M2,3 L10,2 L10,11 L2,11 Z M12,2 L22,1 L22,11 L12,11 Z M2,13 L10,13 L10,22 L2,21 Z M12,13 L22,13 L22,23 L12,22 Z"; break;
                default: throw new ArgumentException("Unknown icon", "name");
            }
            var path=new Path { Data=System.Windows.Media.Geometry.Parse(data),StrokeThickness=name=="windows"?0:1.8,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round };
            var property=name=="windows"?Shape.FillProperty:Shape.StrokeProperty;
            if(brush=="ButtonForeground") path.SetBinding(property,new Binding("Foreground") { RelativeSource=new RelativeSource(RelativeSourceMode.FindAncestor,typeof(Button),1) });
            else path.SetResourceReference(property,brush);
            var canvas=new Canvas { Width=24,Height=24 }; canvas.Children.Add(path);
            return new Viewbox { Child=canvas,Width=size,Height=size,IsHitTestVisible=false,Focusable=false };
        }
        internal static FrameworkElement Logo() {
            var square=new Border { Width=24,Height=24,CornerRadius=new CornerRadius(5),Background=new SolidColorBrush(Color.FromRgb(22,65,70)),Padding=new Thickness(5) };
            square.Child=new Border { CornerRadius=new CornerRadius(1),BorderThickness=new Thickness(2),BorderBrush=new SolidColorBrush(Color.FromRgb(72,219,181)) };
            return square;
        }
    }
}
