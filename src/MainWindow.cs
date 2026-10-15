using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;

namespace Reswin {
    internal sealed class MainWindow : Window {
        readonly Settings settings;
        readonly Action changed;
        readonly StackPanel exclusions=new StackPanel();
        readonly CheckBox enabled=new CheckBox();
        readonly CheckBox startup=new CheckBox();
        bool updatingStartup;
        internal bool AllowClose;
        internal MainWindow(Settings settings,Action changed,bool preview=false) {
            this.settings=settings; this.changed=changed;
            SetResourceReference(StyleProperty,typeof(Window));
            Title="Reswin 1.0.0"; Width=420; Height=Math.Min(680,SystemParameters.WorkArea.Height);
            MinWidth=420; MinHeight=Math.Min(540,SystemParameters.WorkArea.Height);
            WindowStyle=WindowStyle.SingleBorderWindow; ResizeMode=ResizeMode.CanMinimize; WindowStartupLocation=WindowStartupLocation.CenterScreen;
            var body=new Border(); body.SetResourceReference(Border.BackgroundProperty,"BackgroundBrush"); Content=body;
            var design=new Grid { Width=468,Height=720 };
            body.Child=new Viewbox { Child=design,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,VerticalAlignment=VerticalAlignment.Top };
            var content=new Grid { Margin=new Thickness(16,12,16,10) }; design.Children.Add(content);
            content.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height=new GridLength(1,GridUnitType.Star) });

            var header=new Grid { Height=52,Margin=new Thickness(8,0,6,10) };
            header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
            var heading=new StackPanel { VerticalAlignment=VerticalAlignment.Center };
            var title=Theme.Text("Reswin",30,false); title.FontWeight=FontWeights.Bold; heading.Children.Add(title);
            header.Children.Add(heading);
            Interaction.AnimateSwitch(enabled); enabled.Style=(Style)Application.Current.FindResource("Switch"); enabled.IsChecked=settings.Enabled; enabled.FontSize=15;
            enabled.VerticalAlignment=VerticalAlignment.Center; enabled.Content=settings.Enabled?L.T("Включено"):L.T("Пауза");
            Theme.Accessible(enabled,L.T("Включить управление окнами")); Grid.SetColumn(enabled,1); header.Children.Add(enabled);
            enabled.Checked+=delegate { Toggle(true); }; enabled.Unchecked+=delegate { Toggle(false); }; content.Children.Add(header);

            var settingsRows=new StackPanel(); var settingsCard=Card(settingsRows,new Thickness(12,8,12,8)); Grid.SetRow(settingsCard,1); content.Children.Add(settingsCard);
            var modifier=new ComboBox { ItemsSource=new[]{L.T("Левый Ctrl"),L.T("Левый Alt")},SelectedIndex=settings.Modifier==0xA4?1:0 };
            settingsRows.Children.Add(SettingRow(L.T("Зажать клавишу"),Icons.Mouse("keyboard_black 1.svg"),modifier));
            Interaction.AnimateDropDown(modifier); Theme.Accessible(modifier,L.T("Клавиша-модификатор")); modifier.SelectionChanged+=delegate { settings.Modifier=modifier.SelectedIndex==1?0xA4:0xA2; changed(); };
            settingsRows.Children.Add(ActionRow(L.T("Левая кнопка"),"mouse_black 1.svg",0));
            settingsRows.Children.Add(ActionRow(L.T("Правая кнопка"),"mouse_black 2.svg",1));
            settingsRows.Children.Add(ActionRow(L.T("Средняя кнопка"),"mouse_black 3.svg",2));
            settingsRows.Children.Add(ActionRow(L.T("Боковая назад"),"mouse_back.svg",3));
            settingsRows.Children.Add(ActionRow(L.T("Боковая вперёд"),"mouse_forward.svg",4));
            var divider=new Border { Height=1,Margin=new Thickness(0,7,0,5) }; divider.SetResourceReference(Border.BackgroundProperty,"LineBrush"); settingsRows.Children.Add(divider);
            var startupRow=new Grid { Height=48 }; startupRow.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(48) }); startupRow.ColumnDefinitions.Add(new ColumnDefinition()); startupRow.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
            var windowsIcon=Icons.Symbol("windows",26); windowsIcon.HorizontalAlignment=HorizontalAlignment.Left; windowsIcon.Margin=new Thickness(7,0,0,0); startupRow.Children.Add(windowsIcon);
            var startupLabel=Theme.Text(L.T("Запускать вместе с Windows"),15,false); Grid.SetColumn(startupLabel,1); startupRow.Children.Add(startupLabel);
            Interaction.AnimateSwitch(startup); startup.Style=(Style)Application.Current.FindResource("Switch"); startup.VerticalAlignment=VerticalAlignment.Center; startup.HorizontalAlignment=HorizontalAlignment.Right; Grid.SetColumn(startup,2); startupRow.Children.Add(startup);
            Theme.Accessible(startup,L.T("Запускать Reswin вместе с Windows")); startup.ToolTip=L.T("При входе в Windows запускать Reswin в трее.");
            string startupWarning=null;
            try { startup.IsChecked=!preview && Startup.Enabled; } catch(Exception e) { startup.IsEnabled=false; startupWarning=L.T("Не удалось прочитать автозапуск: ")+e.Message; }
            startup.IsEnabled=startupWarning==null;
            if(!preview) { startup.Checked+=delegate { ChangeStartup(true); }; startup.Unchecked+=delegate { ChangeStartup(false); }; } settingsRows.Children.Add(startupRow);

            var appearance=new ComboBox { ItemsSource=new[]{L.T("Как в Windows"),L.T("Светлая"),L.T("Тёмная")},SelectedIndex=(int)settings.Appearance };
            Interaction.AnimateDropDown(appearance); Theme.Accessible(appearance,L.T("Тема оформления"));
            settingsRows.Children.Add(SettingRow(L.T("Тема"),Icons.Symbol("theme",26),appearance));
            appearance.SelectionChanged+=delegate { settings.Appearance=(ThemeMode)appearance.SelectedIndex; Theme.Preference=settings.Appearance; Theme.Refresh(Application.Current); changed(); };

            var language=new ComboBox { ItemsSource=new[]{"Русский","English"},SelectedIndex=(int)settings.Language };
            Theme.Accessible(language,L.T("Язык интерфейса")); Interaction.AnimateDropDown(language);
            settingsRows.Children.Add(SettingRow(L.T("Язык"),Icons.Symbol("language",26),language));
            language.SelectionChanged+=delegate { settings.Language=(AppLanguage)language.SelectedIndex; changed(); };

            var blacklist=new DockPanel(); var blacklistCard=Card(blacklist,new Thickness(16,12,16,14)); blacklistCard.Margin=new Thickness(0,14,0,0); Grid.SetRow(blacklistCard,2); content.Children.Add(blacklistCard);
            var blacklistHeader=new DockPanel { Height=34,Margin=new Thickness(0,0,0,6) };
            var add=Interaction.IconButton("plus",23,L.T("Добавить открытую программу"));
            Theme.Accessible(add,L.T("Добавить программу в чёрный список")); DockPanel.SetDock(add,Dock.Right); blacklistHeader.Children.Add(add);
            var blacklistIcon=Icons.Symbol("ban",25); blacklistIcon.Margin=new Thickness(4,0,20,0); DockPanel.SetDock(blacklistIcon,Dock.Left); blacklistHeader.Children.Add(blacklistIcon);
            var label=Theme.Text(L.T("Чёрный список"),17,false); label.FontWeight=FontWeights.SemiBold; blacklistHeader.Children.Add(label); DockPanel.SetDock(blacklistHeader,Dock.Top); blacklist.Children.Add(blacklistHeader);
            add.Click+=delegate { var picker=new ProgramPicker(settings) { Owner=this }; if(picker.ShowDialog()==true) { changed(); RenderExclusions(); } };
            blacklist.Children.Add(new SmoothScrollViewer { Content=exclusions,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Focusable=false });
            RenderExclusions(); if(startupWarning!=null) SetStatus(startupWarning);
            Closing+=delegate(object sender,System.ComponentModel.CancelEventArgs e) { if(!AllowClose) { e.Cancel=true; Hide(); } };
            StateChanged+=delegate { if(WindowState==WindowState.Minimized) { Hide(); WindowState=WindowState.Normal; } };
        }
        static Border Card(UIElement child,Thickness padding) {
            var border=new Border { Child=child,CornerRadius=new CornerRadius(16),BorderThickness=new Thickness(1),Padding=padding };
            border.SetResourceReference(Border.BackgroundProperty,"SurfaceBrush"); border.SetResourceReference(Border.BorderBrushProperty,"LineBrush"); return border;
        }
        FrameworkElement SettingRow(string label,FrameworkElement icon,FrameworkElement control) {
            var row=new Grid { Height=46 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(48) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(150) }); row.ColumnDefinitions.Add(new ColumnDefinition());
            icon.HorizontalAlignment=HorizontalAlignment.Left; icon.VerticalAlignment=VerticalAlignment.Center; icon.Margin=new Thickness(4,0,0,0); row.Children.Add(icon);
            var text=Theme.Text(label,15,false); Grid.SetColumn(text,1); row.Children.Add(text);
            control.VerticalAlignment=VerticalAlignment.Center; control.Margin=new Thickness(0,4,0,4); Grid.SetColumn(control,2); row.Children.Add(control); return row;
        }
        FrameworkElement ActionRow(string name,string icon,int button) {
            var combo=new ComboBox { ItemsSource=new[]{L.T("Перемещение"),L.T("Изменение размера"),L.T("Центрирование"),L.T("Ничего"),L.T("Свернуть"),L.T("Развернуть")},SelectedIndex=(int)settings.Action(button) };
            Interaction.AnimateDropDown(combo); Theme.Accessible(combo,name+L.T(": действие")); combo.ToolTip=L.T("Развернуть: повторное нажатие возвращает прежний размер и положение окна.");
            combo.SelectionChanged+=delegate { var selected=(WindowAction)combo.SelectedIndex; if(button==0) settings.Left=selected; else if(button==1) settings.Right=selected; else if(button==2) settings.Middle=selected; else if(button==3) settings.Back=selected; else settings.Forward=selected; changed(); };
            return SettingRow(name,Icons.Mouse(icon),combo);
        }
        void ChangeStartup(bool value) {
            if(updatingStartup) return;
            try { Startup.SetEnabled(value); }
            catch(Exception e) { updatingStartup=true; startup.IsChecked=!value; updatingStartup=false; SetStatus(L.T("Не удалось изменить автозапуск: ")+e.Message); }
        }
        void Toggle(bool value) { settings.Enabled=value; enabled.Content=value?L.T("Включено"):L.T("Пауза"); changed(); }
        internal void SyncEnabled() { enabled.IsChecked=settings.Enabled; enabled.Content=settings.Enabled?L.T("Включено"):L.T("Пауза"); }
        internal void SetStatus(string message) {
            // Routine status text is no longer shown. Failures still need actionable feedback.
            if(message!=null && message.StartsWith(L.T("Не удалось"),StringComparison.Ordinal))
                MessageBox.Show(message,"Reswin",MessageBoxButton.OK,MessageBoxImage.Warning);
        }
        async void LoadExclusionIcon(ContentControl host,string path) {
            var image=await Task.Run(()=>Icons.ApplicationIcon(path)).ConfigureAwait(false);
            if(image!=null && !host.Dispatcher.HasShutdownStarted)
                await host.Dispatcher.InvokeAsync(new Action(delegate { host.Content=new Image { Source=image,Width=24,Height=24 }; })).Task.ConfigureAwait(false);
        }
        internal void RenderExclusions() {
            exclusions.Children.Clear();
            if(settings.Exclusions.Count==0) {
                var empty=new Grid { Height=80 };
                var outline=new Rectangle { RadiusX=13,RadiusY=13,StrokeThickness=1,StrokeDashArray=new DoubleCollection { 4,3 },Margin=new Thickness(1),IsHitTestVisible=false };
                outline.SetResourceReference(Shape.StrokeProperty,"ControlLineBrush"); empty.Children.Add(outline);
                var center=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center };
                var folder=Icons.Symbol("folder",28,"MutedBrush"); folder.Margin=new Thickness(0,0,25,0); center.Children.Add(folder);
                var text=Theme.Text(L.T("Здесь приложения, которым\nне нужно мешать.\nДобавьте через +."),14,true); text.LineHeight=21; center.Children.Add(text); empty.Children.Add(center); exclusions.Children.Add(empty); return;
            }
            foreach(var item in settings.Exclusions.ToArray()) {
                var row=new DockPanel { Margin=new Thickness(1,2,1,5),MinHeight=36 };
                var remove=Interaction.IconButton("close",16,L.T("Убрать из чёрного списка"),true);
                Theme.Accessible(remove,L.T("Убрать ")+item.Name+L.T(" из чёрного списка")); DockPanel.SetDock(remove,Dock.Right); row.Children.Add(remove);
                var icon=new ContentControl { Content=Icons.Symbol("folder",22,"MutedBrush"),Width=24,Height=24,Margin=new Thickness(5,0,14,0),VerticalAlignment=VerticalAlignment.Center,Focusable=false };
                DockPanel.SetDock(icon,Dock.Left); row.Children.Add(icon); LoadExclusionIcon(icon,item.Path);
                var text=Theme.Text(item.Name,14,false); text.TextTrimming=TextTrimming.CharacterEllipsis; text.ToolTip=item.Path; row.Children.Add(text);
                remove.Click+=delegate { settings.Exclusions.Remove(item); changed(); RenderExclusions(); }; exclusions.Children.Add(row);
            }
        }
    }
}
