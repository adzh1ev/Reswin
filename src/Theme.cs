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
        internal static string AccentColor=Settings.AccentColors[0];
        internal static string[] AccentColors {get {return Settings.AccentColors;}}
        const string StateKey="Reswin.Theme.State";
        sealed class PaletteState {
            internal bool StylesInstalled,Initialized,Dark,HighContrast;
            internal string Accent;
            internal Color Window,WindowText,Highlight,HighlightText;
        }
        static PaletteState State(Application app) {
            var state=app.Resources[StateKey] as PaletteState;
            if(state==null) {state=new PaletteState(); app.Resources[StateKey]=state;}
            return state;
        }
        internal static void Install(Application app) {
            var state=State(app);
            if(state.StylesInstalled) {Refresh(app); return;}
            app.Resources["UIFontFamily"]=Typography.Family;
            app.Resources["MotionEaseOut"]=Interaction.EaseOut;
            string markup=@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='Window'><Setter Property='UseLayoutRounding' Value='True'/><Setter Property='SnapsToDevicePixels' Value='True'/><Setter Property='Background' Value='{DynamicResource BackgroundBrush}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='FontFamily' Value='{DynamicResource UIFontFamily}'/><Setter Property='FontSize' Value='14'/></Style>
<Style TargetType='TextBlock'><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='FontFamily' Value='{DynamicResource UIFontFamily}'/></Style>
<Style x:Key='SensitivitySlider' TargetType='Slider'>
 <Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Height' Value='32'/><Setter Property='MinWidth' Value='80'/><Setter Property='Focusable' Value='True'/><Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Slider'>
  <Grid Background='Transparent' SnapsToDevicePixels='True'>
   <Border Height='4' Margin='12,0,12,0' CornerRadius='2' Background='{DynamicResource LineBrush}'/>
   <Track x:Name='PART_Track' Orientation='Horizontal' IsDirectionReversed='{TemplateBinding IsDirectionReversed}' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{Binding Value, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}'>
    <Track.DecreaseRepeatButton><RepeatButton Focusable='False' IsTabStop='False' Command='{x:Static Slider.DecreaseLarge}'>
     <RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Height='4' Margin='12,0,-12,0' CornerRadius='2' Background='{DynamicResource AccentBrush}' BorderBrush='{DynamicResource AccentOutlineBrush}' BorderThickness='{DynamicResource AccentOutlineThickness}'/></ControlTemplate></RepeatButton.Template>
    </RepeatButton></Track.DecreaseRepeatButton>
    <Track.Thumb><Thumb Width='24' Height='28' Focusable='False' IsTabStop='False'>
     <Thumb.Template><ControlTemplate TargetType='Thumb'><Grid Background='Transparent'>
      <Ellipse x:Name='ThumbCore' Width='14' Height='14' Fill='{DynamicResource AccentBrush}' Stroke='{DynamicResource AccentOutlineBrush}' StrokeThickness='{DynamicResource AccentOutlineWidth}' RenderTransformOrigin='0.5,0.5'>
       <Ellipse.RenderTransform><ScaleTransform/></Ellipse.RenderTransform>
      </Ellipse>
     </Grid></ControlTemplate></Thumb.Template>
    </Thumb></Track.Thumb>
    <Track.IncreaseRepeatButton><RepeatButton Focusable='False' IsTabStop='False' Command='{x:Static Slider.IncreaseLarge}'>
     <RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template>
    </RepeatButton></Track.IncreaseRepeatButton>
   </Track>
  </Grid>
  <ControlTemplate.Triggers><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='ToolTip'>
 <Setter Property='Background' Value='{DynamicResource PopupBrush}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/>
 <Setter Property='BorderBrush' Value='{DynamicResource LineBrush}'/><Setter Property='BorderThickness' Value='1'/><Setter Property='Padding' Value='12,9'/>
 <Setter Property='FontFamily' Value='{DynamicResource UIFontFamily}'/><Setter Property='FontSize' Value='13'/><Setter Property='MaxWidth' Value='340'/>
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
  <Grid x:Name='Body' RenderTransformOrigin='0.5,0.5'><Grid.RenderTransform><ScaleTransform/></Grid.RenderTransform>
   <Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='10'/>
   <Border x:Name='Hover' Background='{DynamicResource AccentSoftBrush}' CornerRadius='10' Opacity='0'/>
   <ContentPresenter Margin='{TemplateBinding Padding}' HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center'/>
  </Grid>
  <ControlTemplate.Triggers>
   <Trigger Property='IsMouseOver' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger>
   <Trigger Property='IsPressed' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard>
    <DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='0.96' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/>
    <DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='0.96' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/>
   </Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard>
    <DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/>
    <DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/>
   </Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger>
   <Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger>
  </ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style x:Key='PrimaryButton' TargetType='Button' BasedOn='{StaticResource {x:Type Button}}'>
 <Setter Property='Background' Value='{DynamicResource AccentBrush}'/><Setter Property='BorderBrush' Value='{DynamicResource AccentBrush}'/><Setter Property='Foreground' Value='{DynamicResource AccentForegroundBrush}'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'>
  <Grid x:Name='Body' RenderTransformOrigin='0.5,0.5'><Grid.RenderTransform><ScaleTransform/></Grid.RenderTransform>
   <Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='10'/>
   <Border x:Name='Hover' Background='{DynamicResource AccentHoverBrush}' CornerRadius='10' Opacity='0'/>
   <ContentPresenter TextElement.Foreground='{TemplateBinding Foreground}' Margin='{TemplateBinding Padding}' HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center'><ContentPresenter.Resources><Style TargetType='TextBlock'><Setter Property='Foreground' Value='{DynamicResource AccentForegroundBrush}'/></Style></ContentPresenter.Resources></ContentPresenter>
  </Grid>
  <ControlTemplate.Triggers>
   <Trigger Property='IsMouseOver' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger>
   <Trigger Property='IsPressed' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard>
    <DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='0.96' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/>
    <DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='0.96' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/>
   </Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard>
    <DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/>
    <DoubleAnimation Storyboard.TargetName='Body' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.15' EasingFunction='{StaticResource MotionEaseOut}'/>
   </Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger>
   <Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger>
  </ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style x:Key='IconButton' TargetType='Button'>
 <Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border Background='Transparent'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='ComboBox'>
 <Setter Property='Padding' Value='12,6,30,6'/>
 <Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='MinHeight' Value='34'/><Setter Property='FontSize' Value='14'/><Setter Property='HorizontalContentAlignment' Value='Left'/><Setter Property='ScrollViewer.CanContentScroll' Value='True'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBox'><Grid>
  <ToggleButton Focusable='False' FocusVisualStyle='{x:Null}' IsChecked='{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'>
   <ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Grid x:Name='Body'>
    <Border Background='{DynamicResource ControlBrush}' BorderBrush='{DynamicResource ControlLineBrush}' BorderThickness='1' CornerRadius='10'/><Border x:Name='Hover' Background='{DynamicResource HoverBrush}' CornerRadius='10' Opacity='0'/>
    <Path x:Name='Arrow' Data='M0,0 L4,4 L8,0' Stroke='{DynamicResource TextBrush}' StrokeThickness='1.3' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round' Width='8' Height='4' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,15,0' RenderTransformOrigin='0.5,0.5'><Path.RenderTransform><RotateTransform/></Path.RenderTransform></Path>
   </Grid><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.15'><DoubleAnimation.EasingFunction><StaticResource ResourceKey='MotionEaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.15'><DoubleAnimation.EasingFunction><StaticResource ResourceKey='MotionEaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsChecked' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Arrow' Storyboard.TargetProperty='(UIElement.RenderTransform).(RotateTransform.Angle)' To='180' Duration='0:0:0.15'><DoubleAnimation.EasingFunction><StaticResource ResourceKey='MotionEaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Arrow' Storyboard.TargetProperty='(UIElement.RenderTransform).(RotateTransform.Angle)' To='0' Duration='0:0:0.15'><DoubleAnimation.EasingFunction><StaticResource ResourceKey='MotionEaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template>
  </ToggleButton>
  <ContentPresenter Margin='{TemplateBinding Padding}' VerticalAlignment='Center' IsHitTestVisible='False' Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'/>
  <Popup x:Name='PART_Popup' IsOpen='{TemplateBinding IsDropDownOpen}' Placement='Bottom' AllowsTransparency='True' Focusable='False' PopupAnimation='None'>
   <Border Background='{DynamicResource PopupBrush}' BorderBrush='{DynamicResource ControlLineBrush}' BorderThickness='1' CornerRadius='10' MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}' Margin='0,5,0,0' Padding='5'><ScrollViewer MaxHeight='300' CanContentScroll='True' HorizontalScrollBarVisibility='Disabled' VerticalScrollBarVisibility='Auto'><ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/></ScrollViewer></Border>
  </Popup>
 </Grid></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='ComboBoxItem'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Padding' Value='10,8'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBoxItem'><Grid Background='Transparent'><Border x:Name='Hover' Background='{DynamicResource HoverBrush}' CornerRadius='7' Opacity='0'/><ContentPresenter Margin='{TemplateBinding Padding}'/></Grid><ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.15'><DoubleAnimation.EasingFunction><StaticResource ResourceKey='MotionEaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Hover' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.15'><DoubleAnimation.EasingFunction><StaticResource ResourceKey='MotionEaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='CheckBox' x:Key='Switch'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='Cursor' Value='Hand'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='CheckBox'><Grid Width='52' Height='32' Background='Transparent'>
  <Border Width='48' Height='28' CornerRadius='14' VerticalAlignment='Center' HorizontalAlignment='Center' Background='{DynamicResource SwitchOffBrush}'/>
  <Border x:Name='ActiveTrack' Width='48' Height='28' CornerRadius='14' VerticalAlignment='Center' HorizontalAlignment='Center' Background='{DynamicResource AccentBrush}' BorderBrush='{DynamicResource AccentOutlineBrush}' BorderThickness='{DynamicResource AccentOutlineThickness}' Opacity='0'/>
  <Ellipse x:Name='Thumb' Fill='{DynamicResource SwitchThumbBrush}' Width='22' Height='22' VerticalAlignment='Center' HorizontalAlignment='Left' Margin='5,0,0,0'/>
 </Grid><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Thumb' Property='Fill' Value='{DynamicResource AccentForegroundBrush}'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='TextBox'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Foreground' Value='{DynamicResource TextBrush}'/><Setter Property='Background' Value='{DynamicResource ControlBrush}'/><Setter Property='BorderBrush' Value='{DynamicResource ControlLineBrush}'/><Setter Property='Padding' Value='12,10'/><Setter Property='CaretBrush' Value='{DynamicResource TextBrush}'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='TextBox'><Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1' CornerRadius='10' Padding='{TemplateBinding Padding}'><ScrollViewer x:Name='PART_ContentHost' Focusable='False'/></Border></ControlTemplate></Setter.Value></Setter>
</Style>
<Style x:Key='ScrollPage' TargetType='RepeatButton'><Setter Property='Focusable' Value='False'/><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate></Setter.Value></Setter></Style>
<Style x:Key='ScrollThumb' TargetType='Thumb'><Setter Property='FocusVisualStyle' Value='{x:Null}'/><Setter Property='MinHeight' Value='24'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Thumb'><Grid Background='Transparent'><Border x:Name='Handle' Width='4' Margin='2,0' CornerRadius='2' Background='{DynamicResource MutedBrush}' Opacity='0.38'/></Grid><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Trigger.EnterActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Handle' Storyboard.TargetProperty='Opacity' To='0.8' Duration='0:0:0.15'><DoubleAnimation.EasingFunction><StaticResource ResourceKey='MotionEaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.EnterActions><Trigger.ExitActions><BeginStoryboard><Storyboard><DoubleAnimation Storyboard.TargetName='Handle' Storyboard.TargetProperty='Opacity' To='0.38' Duration='0:0:0.15'><DoubleAnimation.EasingFunction><StaticResource ResourceKey='MotionEaseOut'/></DoubleAnimation.EasingFunction></DoubleAnimation></Storyboard></BeginStoryboard></Trigger.ExitActions></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
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
            app.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse(markup));
            state.StylesInstalled=true;
            Refresh(app);
        }
        internal static void Refresh(Application app,bool? darkOverride=null) {
            var state=State(app);
            bool dark=darkOverride.HasValue?darkOverride.Value:Preference==ThemeMode.System?SystemDark(state.Initialized && state.Dark):Preference==ThemeMode.Dark;
            bool highContrast=SystemParameters.HighContrast;
            string selected=Settings.NormalizeAccentColor(AccentColor);
            if(state.Initialized && state.Dark==dark && state.HighContrast==highContrast && state.Accent==selected &&
                (!highContrast || (state.Window==SystemColors.WindowColor && state.WindowText==SystemColors.WindowTextColor &&
                    state.Highlight==SystemColors.HighlightColor && state.HighlightText==SystemColors.HighlightTextColor))) return;
            Color accent=ColorOf(selected);
            Color surface=ColorOf(dark?"#23272D":"#FFFFFF");
            Color foreground=Contrast(accent,Colors.White)>=Contrast(accent,ColorOf("#18212A"))?Colors.White:ColorOf("#18212A");
            Color ink=Readable(accent,ColorOf(dark?"#23272D":"#F1F3F5"));
            Set(app,"BackgroundBrush",dark?"#191C20":"#F1F3F5");
            Set(app,"SurfaceBrush",dark?"#23272D":"#FFFFFF");
            Set(app,"ControlBrush",dark?"#2E343C":"#F1F3F5");
            Set(app,"ChromeBrush",dark?"#191C20":"#F1F3F5");
            Set(app,"AccentBrush",accent);
            Set(app,"AccentForegroundBrush",foreground);
            Set(app,"AccentHoverBrush",Mix(accent,Colors.Black,0.12));
            Set(app,"AccentSoftBrush",Mix(surface,accent,dark?0.16:0.12));
            Set(app,"AccentInkBrush",ink);
            bool outline=Contrast(accent,surface)<3;
            Set(app,"AccentOutlineBrush",ink);
            Set(app,"NavigationBrush",dark?"#23272D":"#FFFFFF");
            Set(app,"NavigationHoverBrush",Mix(surface,accent,dark?0.09:0.05));
            Set(app,"PopupBrush",dark?"#292E35":"#FFFFFF");
            Set(app,"TextBrush",dark?"#F1F3F5":"#24292F");
            Set(app,"MutedBrush",dark?"#B1BAC5":"#616B77");
            Set(app,"LineBrush",dark?"#383F48":"#E4E8EC");
            Set(app,"ControlLineBrush",dark?"#2E343C":"#F1F3F5");
            Set(app,"HoverBrush",dark?"#3D4651":"#E3E9EE");
            Set(app,"SwitchOffBrush",dark?"#646C77":"#8D97A4");
            Set(app,"SwitchThumbBrush","#FFFFFF");
            Set(app,"FocusBrush",ink);
            Set(app,"RimBrush",dark?"#383F48":"#FFFFFF");
            if(highContrast) {
                foreach(string name in new[]{"BackgroundBrush","SurfaceBrush","ControlBrush","ChromeBrush","PopupBrush","HoverBrush","NavigationBrush","NavigationHoverBrush","AccentSoftBrush"}) Set(app,name,SystemColors.WindowColor);
                foreach(string name in new[]{"TextBrush","MutedBrush","LineBrush","ControlLineBrush","RimBrush","SwitchOffBrush"}) Set(app,name,SystemColors.WindowTextColor);
                Set(app,"AccentBrush",SystemColors.HighlightColor); Set(app,"FocusBrush",SystemColors.HighlightColor);
                Set(app,"AccentHoverBrush",SystemColors.HighlightColor); Set(app,"AccentInkBrush",SystemColors.HighlightColor);
                Set(app,"AccentForegroundBrush",SystemColors.HighlightTextColor); Set(app,"SwitchThumbBrush",SystemColors.WindowColor);
                Set(app,"AccentOutlineBrush",SystemColors.WindowTextColor); outline=true;
            }
            app.Resources["AccentOutlineWidth"]=outline?1.0:0.0;
            app.Resources["AccentOutlineThickness"]=new Thickness(outline?1:0);
            state.Dark=dark; state.HighContrast=highContrast; state.Accent=selected; state.Initialized=true;
            if(highContrast) {
                state.Window=SystemColors.WindowColor; state.WindowText=SystemColors.WindowTextColor;
                state.Highlight=SystemColors.HighlightColor; state.HighlightText=SystemColors.HighlightTextColor;
            }
        }
        static bool SystemDark(bool fallback) {
            try {
                using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return key==null?false:Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))==0;
            } catch(System.Security.SecurityException) {return fallback;}
            catch(UnauthorizedAccessException) {return fallback;}
            catch(System.IO.IOException) {return fallback;}
            catch(FormatException) {return fallback;}
            catch(InvalidCastException) {return fallback;}
            catch(OverflowException) {return fallback;}
        }
        static Color ColorOf(string value) {
            return Color.FromRgb((byte)(Hex(value[1])*16+Hex(value[2])),(byte)(Hex(value[3])*16+Hex(value[4])),(byte)(Hex(value[5])*16+Hex(value[6])));
        }
        static int Hex(char value) {return value<='9'?value-'0':char.ToUpperInvariant(value)-'A'+10;}
        static Color Mix(Color from,Color to,double amount) {
            return Color.FromRgb((byte)Math.Round(from.R+(to.R-from.R)*amount),(byte)Math.Round(from.G+(to.G-from.G)*amount),(byte)Math.Round(from.B+(to.B-from.B)*amount));
        }
        static double Channel(byte value) {double v=value/255.0; return v<=0.04045?v/12.92:Math.Pow((v+0.055)/1.055,2.4);}
        static double Luminance(Color value) {return 0.2126*Channel(value.R)+0.7152*Channel(value.G)+0.0722*Channel(value.B);}
        static double Contrast(Color a,Color b) {double first=Luminance(a),second=Luminance(b); return (Math.Max(first,second)+0.05)/(Math.Min(first,second)+0.05);}
        static Color Readable(Color accent,Color background) {
            Color end=Luminance(background)<0.2?Colors.White:Colors.Black;
            for(int step=0;step<=32;step++) {var candidate=Mix(accent,end,step/32.0); if(Contrast(candidate,background)>=4.5) return candidate;}
            return end;
        }
        static void Set(Application app,string name,string color) {
            Set(app,name,ColorOf(color));
        }
        static void Set(Application app,string name,Color color) {
            // Stable role brushes avoid invalidating the entire resource graph.
            // The transition snapshots the old palette; colors commit atomically.
            var brush=app.Resources[name] as SolidColorBrush;
            if(brush!=null && !brush.IsFrozen) {if(brush.Color!=color) brush.Color=color;}
            else app.Resources[name]=new SolidColorBrush(color);
        }
        internal static TextBlock Text(string text,double size,bool muted) {
            var block=new TextBlock { Text=text,FontSize=size,VerticalAlignment=VerticalAlignment.Center };
            block.SetResourceReference(TextBlock.ForegroundProperty,muted?"MutedBrush":"TextBrush"); return block;
        }
        internal static void Accessible(DependencyObject element,string name) { AutomationProperties.SetName(element,name); }
    }
}
