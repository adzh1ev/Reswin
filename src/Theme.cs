using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace Reswin {
    internal static class Theme {
        internal static ThemeMode Preference=ThemeMode.System;
        internal static void Install(Application app) {
            string markup=@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='Window'><Setter Property='UseLayoutRounding' Value='True'/><Setter Property='SnapsToDevicePixels' Value='True'/><Setter Property='Background' Value='{DynamicResource BackgroundBrush}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='FontFamily' Value='Segoe UI'/><Setter Property='FontSize' Value='14'/></Style>
<Style TargetType='TextBlock'><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/></Style>
<Style TargetType='ToolTip'>
 <Setter Property='Background' Value='{DynamicResource PopupBrush}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/>
 <Setter Property='BorderBrush' Value='{DynamicResource LineBrush}'/><Setter Property='BorderThickness' Value='1'/><Setter Property='Padding' Value='12,9'/>
 <Setter Property='FontFamily' Value='Segoe UI'/><Setter Property='FontSize' Value='13'/><Setter Property='MaxWidth' Value='340'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ToolTip'>
  <Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='9' Padding='{TemplateBinding Padding}'>
   <ContentPresenter TextElement.Foreground='{TemplateBinding Foreground}'>
    <ContentPresenter.Resources><Style TargetType='TextBlock'><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='TextWrapping' Value='Wrap'/></Style></ContentPresenter.Resources>
   </ContentPresenter>
  </Border>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='Button'>
 <Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Background' Value='{DynamicResource ControlBrush}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='BorderBrush' Value='{DynamicResource ControlLineBrush}'/><Setter Property='BorderThickness' Value='1'/><Setter Property='Padding' Value='12,7'/><Setter Property='Cursor' Value='Hand'/><Setter Property='HorizontalContentAlignment' Value='Center'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'>
  <Grid x:Name='Body'><Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='10'/><Border x:Name='Hover' Background='{DynamicResource HoverBrush}' CornerRadius='10' Opacity='0'/><ContentPresenter Margin='{TemplateBinding Padding}' HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center'/></Grid>
  <ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsPressed' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='Opacity' To='0.72' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style x:Key='IconButton' TargetType='Button'>
 <Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border Background='Transparent'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='ComboBox'>
 <Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='MinHeight' Value='34'/><Setter Property='FontSize' Value='14'/><Setter Property='HorizontalContentAlignment' Value='Left'/><Setter Property='ScrollViewer.CanContentScroll' Value='True'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBox'><Grid>
  <ToggleButton Focusable='False' FocusVisualStyle='{x:Null}' IsChecked='{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'>
   <ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Grid x:Name='Body'>
    <Border Background='{DynamicResource ControlBrush}' BorderBrush='{DynamicResource ControlLineBrush}' BorderThickness='1' CornerRadius='10'/><Border x:Name='Hover' Background='{DynamicResource HoverBrush}' CornerRadius='10' Opacity='0'/>
    <Path x:Name='Arrow' Data='M0,0 L4,4 L8,0' Stroke='{DynamicResource TextBrush}' StrokeThickness='1.3' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round' Width='8' Height='4' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,15,0' RenderTransformOrigin='0.5,0.5'><Path.RenderTransform><RotateTransform/></Path.RenderTransform></Path>
   </Grid><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsChecked' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Arrow' Storyboard.TargetProperty='(UIElement.RenderTransform).(RotateTransform.Angle)' To='180' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Arrow' Storyboard.TargetProperty='(UIElement.RenderTransform).(RotateTransform.Angle)' To='0' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template>
  </ToggleButton>
  <ContentPresenter Margin='12,6,30,6' VerticalAlignment='Center' IsHitTestVisible='False' Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'/>
  <Popup x:Name='PART_Popup' IsOpen='{TemplateBinding IsDropDownOpen}' Placement='Bottom' AllowsTransparency='True' Focusable='False' PopupAnimation='None'>
   <Border Background='{DynamicResource PopupBrush}' BorderBrush='{DynamicResource ControlLineBrush}' BorderThickness='1' CornerRadius='10' MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}' Margin='0,5,0,0' Padding='5'><ScrollViewer MaxHeight='300' CanContentScroll='True' HorizontalScrollBarVisibility='Disabled' VerticalScrollBarVisibility='Auto'><ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/></ScrollViewer></Border>
  </Popup>
 </Grid></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='ComboBoxItem'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Padding' Value='10,8'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBoxItem'><Grid Background='Transparent'><Border x:Name='Hover' Background='{DynamicResource HoverBrush}' CornerRadius='7' Opacity='0'/><ContentPresenter Margin='{TemplateBinding Padding}'/></Grid><ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='CheckBox' x:Key='Switch'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='CheckBox'><StackPanel Orientation='Horizontal' Background='Transparent'><ContentPresenter VerticalAlignment='Center' Margin='0,0,12,0'/><Grid Width='52' Height='32'>
  <Border Width='48' Height='28' CornerRadius='16' VerticalAlignment='Center' HorizontalAlignment='Center' Background='{DynamicResource SwitchOffBrush}'/>
  <Border x:Name='ActiveTrack' Width='48' Height='28' CornerRadius='16' VerticalAlignment='Center' HorizontalAlignment='Center' Background='{DynamicResource AccentBrush}' Opacity='0'/>
  <Ellipse x:Name='Thumb' Fill='White' Width='22' Height='22' VerticalAlignment='Center' HorizontalAlignment='Left' Margin='5,0,0,0'/>
 </Grid></StackPanel></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='TextBox'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='Background' Value='{DynamicResource ControlBrush}'/><Setter Property='BorderBrush' Value='{DynamicResource ControlLineBrush}'/><Setter Property='Padding' Value='12,10'/><Setter Property='CaretBrush' Value='{DynamicResource TextBrush}'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='TextBox'><Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1' CornerRadius='10' Padding='{TemplateBinding Padding}'><ScrollViewer x:Name='PART_ContentHost' Focusable='False'/></Border></ControlTemplate></Setter.Value></Setter>
</Style>
<Style x:Key='ScrollPage' TargetType='RepeatButton'><Setter Property='Focusable' Value='False'/><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate></Setter.Value></Setter></Style>
<Style x:Key='ScrollThumb' TargetType='Thumb'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='MinHeight' Value='24'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Thumb'><Grid Background='Transparent'><Border x:Name='Handle' Width='4' Margin='2,0' CornerRadius='2' Background='{DynamicResource MutedBrush}' Opacity='0.38'/></Grid><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Handle' Storyboard.TargetProperty='Opacity' To='0.8' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Handle' Storyboard.TargetProperty='Opacity' To='0.38' Duration='0:0:0.20'><DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='ScrollBar'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Width' Value='8'/><Setter Property='Background' Value='Transparent'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollBar'>
 <Grid Background='Transparent'><Track x:Name='PART_Track' Orientation='Vertical' IsDirectionReversed='True' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{Binding Value, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}' ViewportSize='{TemplateBinding ViewportSize}'>
  <Track.DecreaseRepeatButton><RepeatButton Style='{StaticResource ScrollPage}' Command='{x:Static ScrollBar.PageUpCommand}'/></Track.DecreaseRepeatButton>
  <Track.Thumb><Thumb Style='{StaticResource ScrollThumb}'/></Track.Thumb>
  <Track.IncreaseRepeatButton><RepeatButton Style='{StaticResource ScrollPage}' Command='{x:Static ScrollBar.PageDownCommand}'/></Track.IncreaseRepeatButton>
 </Track></Grid>
 </ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='ScrollViewer'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollViewer'>
 <Grid><Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width='8'/></Grid.ColumnDefinitions>
  <ScrollContentPresenter x:Name='PART_ScrollContentPresenter' Content='{TemplateBinding Content}' ContentTemplate='{TemplateBinding ContentTemplate}' CanContentScroll='{TemplateBinding CanContentScroll}' Margin='{TemplateBinding Padding}'/>
  <ScrollBar x:Name='PART_VerticalScrollBar' Grid.Column='1' Orientation='Vertical' Maximum='{TemplateBinding ScrollableHeight}' ViewportSize='{TemplateBinding ViewportHeight}' Value='{Binding VerticalOffset, RelativeSource={RelativeSource TemplatedParent}, Mode=OneWay}' Visibility='{TemplateBinding ComputedVerticalScrollBarVisibility}'/>
 </Grid>
 </ControlTemplate></Setter.Value></Setter></Style>
</ResourceDictionary>";
            if(!SystemParameters.ClientAreaAnimation) markup=markup.Replace("0:0:0.20","0:0:0").Replace("PopupAnimation='None'","PopupAnimation='None'");
            app.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse(markup));
            Refresh(app);
        }
        internal static void Refresh(Application app,bool? darkOverride=null) {
            bool dark=false;
            using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) { if(key!=null) dark=Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))==0; }
            if(darkOverride.HasValue) dark=darkOverride.Value;
            else if(Preference!=ThemeMode.System) dark=Preference==ThemeMode.Dark;
            Set(app,"BackgroundBrush",dark?"#191C20":"#F1F3F5");
            Set(app,"SurfaceBrush",dark?"#23272D":"#FFFFFF");
            Set(app,"ControlBrush",dark?"#2E343C":"#F1F3F5");
            Set(app,"ChromeBrush",dark?"#191C20":"#F1F3F5");
            Set(app,"AccentBrush",dark?"#40AE98":"#278A76");
            Set(app,"PopupBrush",dark?"#292E35":"#FFFFFF");
            Set(app,"TextBrush",dark?"#F1F3F5":"#24292F");
            Set(app,"MutedBrush",dark?"#B1BAC5":"#616B77");
            Set(app,"LineBrush",dark?"#383F48":"#E4E8EC");
            Set(app,"ControlLineBrush",dark?"#2E343C":"#F1F3F5");
            Set(app,"HoverBrush",dark?"#3D4651":"#E3E9EE");
            Set(app,"SwitchOffBrush",dark?"#67727F":"#8995A2");
            Set(app,"FocusBrush",dark?"#68D4BB":"#156E5C");
            Set(app,"RimBrush",dark?"#383F48":"#FFFFFF");
            if(SystemParameters.HighContrast) {
                foreach(string name in new[]{"BackgroundBrush","SurfaceBrush","ControlBrush","ChromeBrush","PopupBrush","HoverBrush"}) app.Resources[name]=SystemColors.WindowBrush;
                foreach(string name in new[]{"TextBrush","MutedBrush","LineBrush","ControlLineBrush","RimBrush","SwitchOffBrush"}) app.Resources[name]=SystemColors.WindowTextBrush;
                app.Resources["AccentBrush"]=SystemColors.HighlightBrush; app.Resources["FocusBrush"]=SystemColors.HighlightBrush;
            }
        }
        static Color ColorOf(string value) { return (Color)ColorConverter.ConvertFromString(value); }
        static ColorAnimation Transition(Color from,Color to) { return new ColorAnimation(from,to,TimeSpan.FromMilliseconds(220)) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut },FillBehavior=FillBehavior.Stop }; }
        static void Set(Application app,string name,string color) {
            var previous=app.Resources[name] as SolidColorBrush; var brush=new SolidColorBrush(ColorOf(color));
            if(previous!=null && SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast) brush.BeginAnimation(SolidColorBrush.ColorProperty,Transition(previous.Color,brush.Color));
            app.Resources[name]=brush;
        }
        static void Gradient(Application app,string name,string start,string end) {
            var previous=app.Resources[name] as LinearGradientBrush; var brush=new LinearGradientBrush(ColorOf(start),ColorOf(end),35);
            if(previous!=null && previous.GradientStops.Count==2 && SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast) {
                for(int i=0;i<2;i++) brush.GradientStops[i].BeginAnimation(GradientStop.ColorProperty,Transition(previous.GradientStops[i].Color,brush.GradientStops[i].Color));
            }
            app.Resources[name]=brush;
        }
        internal static TextBlock Text(string text,double size,bool muted) {
            var block=new TextBlock { Text=text,FontSize=size,VerticalAlignment=VerticalAlignment.Center };
            block.SetResourceReference(TextBlock.ForegroundProperty,muted?"MutedBrush":"TextBrush"); return block;
        }
        internal static void Accessible(DependencyObject element,string name) { AutomationProperties.SetName(element,name); }
    }
}
