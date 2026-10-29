using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Automation;
using System.Windows.Threading;

namespace Reswin {
    internal sealed class MainWindow : Window {
        readonly Settings settings;
        readonly Action changed;
        readonly StackPanel exclusions=new StackPanel();
        readonly CheckBox enabled=new CheckBox();
        readonly CheckBox startup=new CheckBox();
        readonly CheckBox maximizeAtTop=new CheckBox();
        readonly CheckBox maximizeDoubleClick=new CheckBox();
        readonly ComboBox[] actionControls=new ComboBox[5];
        readonly bool compact=SystemParameters.WorkArea.Height<960;
        readonly bool tight=SystemParameters.WorkArea.Height<760;
        readonly bool veryTight=SystemParameters.WorkArea.Height<640;
        FrameworkElement topEdgeOption,doubleClickOption;
        readonly DispatcherTimer sensitivitySaveTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(220)};
        bool sensitivityDirty;
        bool updatingStartup;
        internal bool AllowClose;
        internal MainWindow(Settings settings,Action changed,bool preview=false) {
            this.settings=settings; this.changed=changed;
            SetResourceReference(StyleProperty,typeof(Window));
            Title="Reswin 1.1.3"; Width=460; Height=Math.Min(1000,SystemParameters.WorkArea.Height);
            MinWidth=460; MinHeight=Math.Min(540,SystemParameters.WorkArea.Height);
            WindowStyle=WindowStyle.SingleBorderWindow; ResizeMode=ResizeMode.CanMinimize; WindowStartupLocation=WindowStartupLocation.CenterScreen;
            var body=new Border(); body.SetResourceReference(Border.BackgroundProperty,"BackgroundBrush"); Content=body;
            var content=new Grid {Margin=tight?new Thickness(16,6,16,8):compact?new Thickness(16,8,16,12):new Thickness(20,16,20,20)}; body.Child=content;
            for(int i=0;i<2;i++) content.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
            content.RowDefinitions.Add(new RowDefinition {Height=new GridLength(1,GridUnitType.Star)});

            var bindings=new StackPanel(); var controls=Card(bindings,new Thickness(12,tight?4:compact?6:10,12,tight?4:compact?6:10)); Grid.SetRow(controls,0); content.Children.Add(controls);
            ComboBox scroll=null;
            var modifier=new ComboBox {ItemsSource=new[]{L.T("Левый Ctrl"),L.T("Левый Alt")},SelectedIndex=settings.Modifier==0xA4?1:0};
            bindings.Children.Add(SettingRow(L.T("Зажать клавишу"),Icons.Lucide("rectangle-ellipsis"),modifier));
            Interaction.AnimateDropDown(modifier); Theme.Accessible(modifier,L.T("Клавиша-модификатор"));
            modifier.SelectionChanged+=delegate {settings.Modifier=modifier.SelectedIndex==1?0xA4:0xA2; if(scroll!=null) RefreshScrollChoices(scroll); RefreshActionHints(); changed();};
            bindings.Children.Add(ActionRow(L.T("Левая кнопка"),"mouse_black 1.svg",0));
            bindings.Children.Add(ActionRow(L.T("Правая кнопка"),"mouse_black 2.svg",1));
            bindings.Children.Add(ActionRow(L.T("Средняя кнопка"),"mouse_black 3.svg",2));
            bindings.Children.Add(ActionRow(L.T("Боковая назад"),"mouse_back.svg",3));
            bindings.Children.Add(ActionRow(L.T("Боковая вперёд"),"mouse_forward.svg",4));
            bindings.Children.Add(BindingDivider());
            maximizeAtTop.IsChecked=settings.MaximizeOnTopEdge;
            maximizeDoubleClick.IsChecked=settings.MaximizeOnDoubleClick;
            Theme.Accessible(maximizeAtTop,L.T("Развернуть у верхнего края"));
            Theme.Accessible(maximizeDoubleClick,L.T("Развернуть двойным нажатием"));
            maximizeAtTop.Checked+=delegate {settings.MaximizeOnTopEdge=true; changed();};
            maximizeAtTop.Unchecked+=delegate {settings.MaximizeOnTopEdge=false; changed();};
            maximizeDoubleClick.Checked+=delegate {settings.MaximizeOnDoubleClick=true; changed();};
            maximizeDoubleClick.Unchecked+=delegate {settings.MaximizeOnDoubleClick=false; changed();};
            if(veryTight) {
                // Keep both gestures visible without using the exclusions viewport
                // for two extra rows on displays with little vertical space.
                var options=new Grid(); options.ColumnDefinitions.Add(new ColumnDefinition()); options.ColumnDefinitions.Add(new ColumnDefinition());
                topEdgeOption=CompactGestureOption(L.T("Верхний край"),maximizeAtTop);
                doubleClickOption=CompactGestureOption(L.T("Двойное нажатие"),maximizeDoubleClick);
                topEdgeOption.Margin=new Thickness(4,0,6,0); doubleClickOption.Margin=new Thickness(6,0,0,0);
                options.Children.Add(topEdgeOption); Grid.SetColumn(doubleClickOption,1); options.Children.Add(doubleClickOption); bindings.Children.Add(options);
            } else {
                topEdgeOption=TogglePreferenceRow(L.T("Развернуть у верхнего края"),"panels-top-left",maximizeAtTop);
                doubleClickOption=TogglePreferenceRow(L.T("Развернуть двойным нажатием"),"mouse-pointer-click",maximizeDoubleClick);
                bindings.Children.Add(topEdgeOption); bindings.Children.Add(doubleClickOption);
            }
            RefreshGestureHints();
            bindings.Children.Add(BindingDivider());
            scroll=new ComboBox {ItemsSource=new[]{L.T("— Без настройки —"),ScrollKeyLabel()},SelectedIndex=settings.ScrollKey==ScrollBinding.None?0:1,ToolTip=ScrollHint()};
            Interaction.AnimateDropDown(scroll); Theme.Accessible(scroll,L.T("Прокрутка удержанием"));
            sensitivitySaveTimer.Tick+=delegate { SaveSensitivity(); };
            var sensitivityRow=SensitivityRow(); sensitivityRow.IsEnabled=settings.ScrollKey!=ScrollBinding.None;
            scroll.SelectionChanged+=delegate {if(scroll.SelectedIndex<0) return; settings.ScrollKey=scroll.SelectedIndex==0?ScrollBinding.None:ScrollBinding.OtherModifier; sensitivityRow.IsEnabled=settings.ScrollKey!=ScrollBinding.None; changed();};
            bindings.Children.Add(SettingRow(L.T("Прокрутка"),Icons.Lucide("mouse"),scroll));
            bindings.Children.Add(sensitivityRow);

            var preferences=new StackPanel(); var preferenceCard=Card(preferences,new Thickness(14,tight?2:compact?4:8,14,tight?4:compact?8:12)); preferenceCard.Margin=new Thickness(0,compact?6:12,0,compact?6:12); Grid.SetRow(preferenceCard,1); content.Children.Add(preferenceCard);
            enabled.IsChecked=settings.Enabled;
            Theme.Accessible(enabled,L.T("Включить приложение"));
            enabled.ToolTip=L.T("При выключении управление окнами и прокрутка приостанавливаются.");
            enabled.Checked+=delegate {Toggle(true);}; enabled.Unchecked+=delegate {Toggle(false);};
            preferences.Children.Add(TogglePreferenceRow(L.T("Включить приложение"),"power",enabled));
            var startupRow=TogglePreferenceRow(L.T("Запускать вместе с Windows"),"monitor",startup);
            Theme.Accessible(startup,L.T("Запускать Reswin вместе с Windows")); startup.ToolTip=L.T("При входе в Windows запускать Reswin в трее.");
            string startupWarning=null;
            try {startup.IsChecked=!preview && Startup.Enabled;} catch(Exception e) {startupWarning=L.T("Не удалось прочитать автозапуск: ")+e.Message;}
            startup.IsEnabled=startupWarning==null;
            if(!preview) {startup.Checked+=delegate {ChangeStartup(true);}; startup.Unchecked+=delegate {ChangeStartup(false);};} preferences.Children.Add(startupRow);
            var appearance=new ComboBox {ItemsSource=new[]{L.T("Как в Windows"),L.T("Светлая"),L.T("Тёмная")},SelectedIndex=(int)settings.Appearance};
            Interaction.AnimateDropDown(appearance); Theme.Accessible(appearance,L.T("Тема оформления"));
            appearance.SelectionChanged+=delegate {settings.Appearance=(ThemeMode)appearance.SelectedIndex; Theme.Preference=settings.Appearance; Theme.Refresh(Application.Current); changed();};
            var language=new ComboBox {ItemsSource=new[]{"Русский","English"},SelectedIndex=(int)settings.Language};
            Interaction.AnimateDropDown(language); Theme.Accessible(language,L.T("Язык интерфейса"));
            language.SelectionChanged+=delegate {settings.Language=(AppLanguage)language.SelectedIndex; changed();};
            var appearanceRow=new Grid {Margin=new Thickness(0,tight?4:6,0,0)};
            appearanceRow.ColumnDefinitions.Add(new ColumnDefinition()); appearanceRow.ColumnDefinitions.Add(new ColumnDefinition());
            var themeField=PreferenceField(L.T("Тема"),appearance); themeField.Margin=new Thickness(0,0,6,0); appearanceRow.Children.Add(themeField);
            var languageField=PreferenceField(L.T("Язык"),language); languageField.Margin=new Thickness(6,0,0,0); Grid.SetColumn(languageField,1); appearanceRow.Children.Add(languageField); preferences.Children.Add(appearanceRow);

            var blacklist=new DockPanel(); var blacklistCard=Card(blacklist,new Thickness(14,tight?8:10,14,tight?8:12)); Grid.SetRow(blacklistCard,2); content.Children.Add(blacklistCard);
            var blacklistHeader=new DockPanel {Height=32,Margin=new Thickness(0,0,0,tight?4:8)};
            var add=Interaction.IconButton("plus",20,L.T("Добавить открытую программу"));
            Theme.Accessible(add,L.T("Добавить программу в чёрный список")); DockPanel.SetDock(add,Dock.Right); blacklistHeader.Children.Add(add);
            var label=Theme.Text(L.T("Чёрный список"),15,false); label.FontWeight=FontWeights.SemiBold; label.Margin=new Thickness(4,0,0,0); blacklistHeader.Children.Add(label); DockPanel.SetDock(blacklistHeader,Dock.Top); blacklist.Children.Add(blacklistHeader);
            add.Click+=delegate {var picker=new ProgramPicker(settings) {Owner=this}; if(picker.ShowDialog()==true) {changed(); RenderExclusions();}};
            blacklist.Children.Add(new SmoothScrollViewer {Content=exclusions,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Focusable=false});
            RenderExclusions(); if(startupWarning!=null) SetStatus(startupWarning);
            Closing+=delegate(object sender,System.ComponentModel.CancelEventArgs e) {if(!AllowClose) {e.Cancel=true; Hide();}};
            IsVisibleChanged+=delegate {if(!IsVisible) SaveSensitivity();};
            Closed+=delegate {SaveSensitivity(); sensitivitySaveTimer.Stop();};
            StateChanged+=delegate {if(WindowState==WindowState.Minimized) {Hide(); WindowState=WindowState.Normal;}};
        }
        void RefreshScrollChoices(ComboBox combo) {
            int selected=settings.ScrollKey==ScrollBinding.None?0:1;
            combo.ItemsSource=new[]{L.T("— Без настройки —"),ScrollKeyLabel()};
            combo.SelectedIndex=selected;
            combo.ToolTip=ScrollHint();
        }
        string ScrollHint() { return ScrollKeyLabel()+". "+L.T("Удерживайте клавишу и двигайте мышь вверх или вниз. Отпустите, чтобы остановить."); }
        FrameworkElement SensitivityRow() {
            settings.ScrollSensitivity=Settings.NormalizeScrollSensitivity(settings.ScrollSensitivity);
            var slider=new SensitivitySlider {Minimum=Settings.MinScrollSensitivity*100,Maximum=Settings.MaxScrollSensitivity*100,Value=settings.ScrollSensitivity*100,Height=tight?28:32};
            string hint=L.T("Меньше — медленнее и точнее, больше — быстрее. 100% — стандартная чувствительность.");
            Theme.Accessible(slider,L.T("Чувствительность прокрутки"));
            AutomationProperties.SetHelpText(slider,hint); slider.ToolTip=hint;
            var value=Theme.Text(slider.Value.ToString("0",CultureInfo.InvariantCulture)+"%",12,true);
            value.FontWeight=FontWeights.Medium;
            value.TextAlignment=TextAlignment.Right; value.Width=44; value.Margin=new Thickness(2,0,0,0);
            var control=new Grid(); control.ColumnDefinitions.Add(new ColumnDefinition()); control.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
            slider.Margin=new Thickness(0,0,4,0); control.Children.Add(slider); Grid.SetColumn(value,1); control.Children.Add(value);
            slider.ValueChanged+=delegate {
                settings.ScrollSensitivity=slider.Value/100;
                value.Text=slider.Value.ToString("0",CultureInfo.InvariantCulture)+"%";
                sensitivityDirty=true; sensitivitySaveTimer.Stop();
                if(!slider.IsAdjusting) sensitivitySaveTimer.Start();
            };
            slider.AdjustmentCompleted+=SaveSensitivity;
            return SettingRow(L.T("Чувствительность"),new Border {Width=22},control);
        }
        void SaveSensitivity() {
            sensitivitySaveTimer.Stop();
            if(!sensitivityDirty) return;
            sensitivityDirty=false; changed();
        }
        FrameworkElement TogglePreferenceRow(string title,string icon,CheckBox control) {
            var row=new Grid {Height=compact?32:40};
            row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(34)});
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
            row.Children.Add(Icons.Lucide(icon,20));
            var label=Theme.Text(title,compact?13:14,false); label.TextWrapping=TextWrapping.Wrap; label.Margin=new Thickness(0,0,8,0); Grid.SetColumn(label,1); row.Children.Add(label);
            Interaction.AnimateSwitch(control); control.Style=(Style)Application.Current.FindResource("Switch");
            control.VerticalAlignment=VerticalAlignment.Center; Grid.SetColumn(control,2); row.Children.Add(control);
            return row;
        }
        FrameworkElement CompactGestureOption(string title,CheckBox control) {
            var row=new Grid {Height=32}; row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
            var label=Theme.Text(title,12,false); label.TextWrapping=TextWrapping.Wrap; label.Margin=new Thickness(0,0,4,0); row.Children.Add(label);
            Interaction.AnimateSwitch(control); control.Style=(Style)Application.Current.FindResource("Switch");
            control.VerticalAlignment=VerticalAlignment.Center; Grid.SetColumn(control,1); row.Children.Add(control); return row;
        }
        FrameworkElement BindingDivider() {
            var line=new Border {Height=1,Margin=new Thickness(4,veryTight?2:tight?4:compact?5:8,4,veryTight?2:tight?4:compact?5:6)};
            line.SetResourceReference(Border.BackgroundProperty,"LineBrush"); return line;
        }
        FrameworkElement PreferenceField(string title,ComboBox control) {
            control.MinHeight=tight?26:compact?28:34; control.FontSize=compact?13:14;
            if(tight) control.Padding=new Thickness(12,4,30,4);
            var panel=new StackPanel(); var label=Theme.Text(title,12,true); label.Margin=new Thickness(4,0,0,tight?3:5); panel.Children.Add(label); panel.Children.Add(control); return panel;
        }
        static Border Card(UIElement child,Thickness padding) {
            var border=new Border { Child=child,CornerRadius=new CornerRadius(16),BorderThickness=new Thickness(0),Padding=padding };
            border.SetResourceReference(Border.BackgroundProperty,"SurfaceBrush"); border.SetResourceReference(Border.BorderBrushProperty,"LineBrush"); return border;
        }
        FrameworkElement SettingRow(string label,FrameworkElement icon,FrameworkElement control) {
            var row=new Grid { Height=veryTight?28:tight?30:compact?34:40 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(34) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(146) }); row.ColumnDefinitions.Add(new ColumnDefinition());
            icon.HorizontalAlignment=HorizontalAlignment.Left; icon.VerticalAlignment=VerticalAlignment.Center; icon.Margin=new Thickness(4,0,0,0); row.Children.Add(icon);
            var text=Theme.Text(label,compact?13:14,false); Grid.SetColumn(text,1); row.Children.Add(text);
            var input=control as Control; if(input!=null) {input.MinHeight=tight?26:compact?28:34; input.FontSize=compact?13:14;}
            var combo=control as ComboBox; if(tight && combo!=null) combo.Padding=new Thickness(12,4,30,4);
            control.VerticalAlignment=VerticalAlignment.Center; control.Margin=new Thickness(0,compact?1:3,0,compact?1:3); Grid.SetColumn(control,2); row.Children.Add(control); return row;
        }
        FrameworkElement ActionRow(string name,string icon,int button) {
            var actions=new[]{WindowAction.None,WindowAction.Move,WindowAction.Resize,WindowAction.Center,WindowAction.Minimize,WindowAction.Maximize};
            var combo=new ComboBox { ItemsSource=new[]{L.T("— Без настройки —"),L.T("Перемещение"),L.T("Изменение размера"),L.T("Центрирование"),L.T("Свернуть"),L.T("Развернуть")},SelectedIndex=Array.IndexOf(actions,settings.Action(button)) };
            Interaction.AnimateDropDown(combo); Theme.Accessible(combo,name+L.T(": действие")); actionControls[button]=combo; combo.ToolTip=ActionHint(settings.Action(button));
            combo.SelectionChanged+=delegate { if(combo.SelectedIndex<0) return; var selected=actions[combo.SelectedIndex]; if(button==0) settings.Left=selected; else if(button==1) settings.Right=selected; else if(button==2) settings.Middle=selected; else if(button==3) settings.Back=selected; else settings.Forward=selected; combo.ToolTip=ActionHint(selected); RefreshGestureHints(); changed(); };
            var glyph=button==3?Icons.Lucide("arrow-left"):button==4?Icons.Lucide("arrow-right"):Icons.Mouse(icon); glyph.Width=22; glyph.Height=22; return SettingRow(name,glyph,combo);
        }
        void RefreshActionHints() {
            for(int button=0;button<actionControls.Length;button++)
                if(actionControls[button]!=null) actionControls[button].ToolTip=ActionHint(settings.Action(button));
            RefreshGestureHints();
        }
        void RefreshGestureHints() {
            string modifier=settings.Modifier==0xA4?L.T("Левый Alt"):L.T("Левый Ctrl");
            var moveButtons=new System.Collections.Generic.List<string>();
            var names=new[]{"Левая кнопка","Правая кнопка","Средняя кнопка","Боковая назад","Боковая вперёд"};
            for(int button=0;button<names.Length;button++) if(settings.Action(button)==WindowAction.Move) moveButtons.Add(L.T(names[button]));
            string assignment=moveButtons.Count==0?L.T("Сначала назначьте действие «Перемещение» кнопке мыши."):string.Format(L.T("Для перемещения: {0}."),string.Join(" / ",moveButtons.ToArray()));
            string topHint=assignment+"\n\n"+string.Format(L.T("Удерживайте {0} и кнопку мыши с действием «Перемещение». Перетащите окно за любую его часть к верхнему краю экрана и отпустите кнопку мыши, чтобы развернуть окно."),modifier);
            string doubleHint=assignment+"\n\n"+string.Format(L.T("Удерживайте {0} и дважды нажмите кнопку мыши с действием «Перемещение» в любой части окна. Окно развернётся; повторное двойное нажатие вернёт прежний размер и положение."),modifier);
            maximizeAtTop.ToolTip=topHint; maximizeDoubleClick.ToolTip=doubleHint;
            AutomationProperties.SetHelpText(maximizeAtTop,topHint); AutomationProperties.SetHelpText(maximizeDoubleClick,doubleHint);
            if(topEdgeOption!=null) topEdgeOption.ToolTip=topHint;
            if(doubleClickOption!=null) doubleClickOption.ToolTip=doubleHint;
        }
        string ActionHint(WindowAction action) {
            if(action==WindowAction.None) return L.T("Действие Reswin не назначено. Кнопка мыши работает как обычно.");
            string instruction;
            switch(action) {
                case WindowAction.Move: instruction=L.T("Удерживайте {0} и эту кнопку мыши, затем двигайте мышь, чтобы переместить окно под указателем."); break;
                case WindowAction.Resize: instruction=L.T("Удерживайте {0} и эту кнопку мыши, затем двигайте мышь, чтобы изменить размер окна под указателем."); break;
                case WindowAction.Center: instruction=L.T("Удерживайте {0} и нажмите эту кнопку мыши, чтобы расположить окно под указателем по центру экрана."); break;
                case WindowAction.Minimize: instruction=L.T("Удерживайте {0} и нажмите эту кнопку мыши, чтобы свернуть окно под указателем."); break;
                case WindowAction.Maximize: instruction=L.T("Удерживайте {0} и нажмите эту кнопку мыши, чтобы развернуть окно под указателем. Повторное нажатие вернёт прежний размер и положение."); break;
                default: return null;
            }
            return string.Format(instruction,settings.Modifier==0xA4?L.T("Левый Alt"):L.T("Левый Ctrl"));
        }
        string ScrollKeyLabel() { return settings.Modifier==0xA2?L.T("Левый Alt (авто)"):L.T("Левый Ctrl (авто)"); }
        void ChangeStartup(bool value) {
            if(updatingStartup) return;
            try { Startup.SetEnabled(value); }
            catch(Exception e) { updatingStartup=true; startup.IsChecked=!value; updatingStartup=false; SetStatus(L.T("Не удалось изменить автозапуск: ")+e.Message); }
        }
        void Toggle(bool value) { settings.Enabled=value; changed(); }
        internal void SyncEnabled() { enabled.IsChecked=settings.Enabled; }
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
                var empty=new StackPanel {Margin=tight?new Thickness(4,2,4,2):new Thickness(12,18,12,12),HorizontalAlignment=HorizontalAlignment.Center};
                if(!tight) {var folder=Icons.Lucide("folder",26,"MutedBrush"); folder.HorizontalAlignment=HorizontalAlignment.Center; folder.Margin=new Thickness(0,0,0,10); empty.Children.Add(folder);}
                var text=Theme.Text(L.T("Добавьте приложения, которым\nReswin не должен мешать."),tight?12:13,true); text.TextAlignment=TextAlignment.Center; text.LineHeight=tight?16:20; empty.Children.Add(text); exclusions.Children.Add(empty); return;
            }
            foreach(var item in settings.Exclusions.ToArray()) {
                var row=new DockPanel { Margin=tight?new Thickness(1,1,1,2):new Thickness(1,2,1,5),MinHeight=tight?32:38 };
                var remove=Interaction.IconButton("close",16,L.T("Убрать из чёрного списка"),true);
                Theme.Accessible(remove,L.T("Убрать ")+item.Name+L.T(" из чёрного списка")); DockPanel.SetDock(remove,Dock.Right); row.Children.Add(remove);
                var icon=new ContentControl { Content=Icons.Symbol("folder",22,"MutedBrush"),Width=24,Height=24,Margin=new Thickness(5,0,14,0),VerticalAlignment=VerticalAlignment.Center,Focusable=false };
                DockPanel.SetDock(icon,Dock.Left); row.Children.Add(icon); LoadExclusionIcon(icon,item.Path);
                var text=Theme.Text(item.Name,13,false); text.TextTrimming=TextTrimming.CharacterEllipsis; text.ToolTip=item.Path; row.Children.Add(text);
                remove.Click+=delegate { settings.Exclusions.Remove(item); changed(); RenderExclusions(); }; exclusions.Children.Add(row);
            }
        }
    }
}
