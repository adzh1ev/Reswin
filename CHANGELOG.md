# Изменения Reswin / Reswin changelog

## 1.1.5

# Русский

## Что нового в Reswin 1.1.5

- **Плавная смена темы.** Светлая и тёмная темы раскрываются из центра окна. Быстрые повторные переключения объединяются по кадрам.
- **Смена языка без пересоздания окна.** Русский и английский переключаются с плавным переходом. Выбранная вкладка, прокрутка списков и настройки остаются на месте.
- **Семь акцентных цветов.** К коралловому добавлены голубой, зелёный, оливковый, персиковый, серо-голубой и жёлтый. Выбор сохраняется; подписи адаптируются к светлым акцентам.
- **Настройка «Сворачивать в трей».** Если включена, крестик скрывает окно, а Reswin продолжает работать в трее. Если выключена, крестик полностью завершает приложение.
- **Исправлена иконка чёрного списка.** Круг и диагональ теперь рисуются в одинаковых координатах, без выступающей линии.
- **Меньше повторной работы при быстрых действиях.** Сохранение настроек перенесено в фон, поиск фильтрует готовые строки, а список исключений обновляет только изменённые элементы. Геометрия иконок переиспользуется, кэш иконок приложений ограничен, временные изображения переходов освобождаются после анимации.

## Как обновиться

Выйдите из старого Reswin через меню в трее, распакуйте `Reswin-portable.zip` в постоянную папку и запустите `Reswin.exe`. Настройки и чёрный список сохраняются. Шрифт встроен в приложение.

Требования: Windows 10/11 x64 и .NET Framework 4.8.

---

# English

## What's new in Reswin 1.1.5

- **Smooth theme changes.** Light and dark themes reveal from the center of the window. Rapid changes are coalesced per frame.
- **Language changes without rebuilding the window.** Russian and English switch with a smooth transition. The selected tab, list scroll positions and settings stay in place.
- **Seven accent colors.** Coral is joined by sky blue, green, olive, peach, blue gray and yellow. Your choice is saved, and labels adapt to light accent colors.
- **Close to tray setting.** When enabled, the close button hides the window while Reswin keeps running in the tray. When disabled, it exits the application completely.
- **Fixed the exclusions icon.** The circle and diagonal now use matching coordinates, without a protruding line.
- **Less repeated work during rapid interaction.** Settings save in the background, search filters existing rows, and the exclusions list updates only changed items. Icon geometry is reused, the application icon cache is bounded, and temporary transition images are released after the animation.

## How to update

Exit the old Reswin instance using the tray menu, extract `Reswin-portable.zip` to a permanent folder, and launch `Reswin.exe`. Your settings and exclusions are preserved. The font is bundled with the application.

Requirements: Windows 10/11 x64 and .NET Framework 4.8.

## 1.1.4

# Русский

## Что нового в Reswin 1.1.4

- **Привязка без предложений Windows.** При перетаскивании к левому или правому краю окно занимает соответствующую половину рабочей области. Панель Snap Assist с выбором второго приложения больше не открывается. Верхний край по-прежнему разворачивает окно.
- **Более широкие боковые зоны.** Зона срабатывания увеличена до 120 логических пикселей. После активации привязка сохраняется до 152 пикселей от края, чтобы небольшое обратное движение курсора не сбрасывало её. Размер зон учитывает масштаб экрана.
- **Восстановление размера при откреплении.** Когда вы снова начинаете перемещать закреплённое окно, оно возвращается к прежнему размеру и остаётся под курсором.
- **Соединённые вкладки.** «Управление», «Чёрный список» и «Настройки» объединены плавными перемычками. Коралловое выделение мягко перемещается между ними и остаётся внутри общего контура при переключении и перетаскивании.

## Как обновиться

Выйдите из работающего Reswin через меню в трее, распакуйте новый `Reswin-portable.zip` в постоянную папку и запустите `Reswin.exe`. Настройки и чёрный список сохраняются. Установка приложения или шрифта не требуется.

Требования: Windows 10/11 x64 и .NET Framework 4.8. Размещение учитывает рабочую область монитора; приложения с ограничениями размера окна могут занимать больше половины экрана.

---

# English

## What's new in Reswin 1.1.4

- **Side placement without Windows suggestions.** Dragging a window to the left or right edge places it on that half of the work area. The Snap Assist panel for choosing a second application no longer opens. The top edge still maximizes the window.
- **Wider side activation zones.** The activation zone now extends up to 120 logical pixels from each side. Once activated, it remains active up to 152 pixels from the edge, so a small movement back does not cancel it. Zone sizes follow display scaling.
- **Restore the original size when detaching.** Moving a side-placed window again restores its previous size and keeps it under the pointer.
- **Connected tabs.** Controls, Exclusions and Settings are joined by smooth connectors. The coral selection moves smoothly between them and stays within the shared outline during both switching and dragging.

## How to update

Exit the running instance of Reswin using the system tray menu, extract the new `Reswin-portable.zip` to a permanent folder, and launch `Reswin.exe`. Your settings and exclusions are preserved. No application or font installation is required.

Requirements: Windows 10/11 x64 and .NET Framework 4.8. Placement respects the monitor work area; applications with minimum window size limits may occupy more than half the screen.


## 1.1.3

# Русский

## Что нового в Reswin 1.1.3

- **Новый интерфейс с тремя вкладками:** «Управление», «Чёрный список» и «Настройки». Коралловый акцент `#D03E36`, встроенный Manrope и ровные иконки Lucide в светлой и тёмной темах.
- **Плавная навигация.** Нажимайте вкладки или удерживайте левую кнопку мыши и перетаскивайте выделение между ними. Страницы переключаются с направленным слайдом; выделение, переключатели и бегунок чувствительности анимированы.
- **Компактное окно.** Высота подстраивается под содержимое без лишнего места снизу. Чёрный список размещён на отдельной вкладке и прокручивается внутри своей области.
- **Привязка к трём краям экрана.** Удерживайте выбранную клавишу Ctrl/Alt и кнопку мыши с действием «Перемещение». Доведите курсор до края и отпустите мышь: сверху окно развернётся, слева или справа — займёт соответствующую половину экрана. Для боковой привязки используется системная функция Snap Windows.
- **Двойное нажатие для разворачивания.** Удерживайте выбранную клавишу и дважды нажмите кнопку мыши с действием «Перемещение». Повторное двойное нажатие восстанавливает окно. Привязка к краям и двойное нажатие отключаются независимо.
- Сохранены автоматический выбор клавиши прокрутки, настройка чувствительности 25–200%, русский и английский языки, автозапуск и исключения приложений.
- Исправлена ошибка запуска интерфейса с сообщением `x is an undeclared prefix`. Проверка разметки добавлена в процесс сборки.

## Как обновиться

Эта сборка заменяет ранее опубликованную 1.1.3. Выйдите из работающего Reswin через трей, распакуйте новый `Reswin-portable.zip` в постоянную папку и запустите `Reswin.exe`. Настройки и чёрный список сохраняются. Отдельная установка шрифта не требуется.

Системная привязка зависит от настроек Snap Windows и поддержки окна приложением. Требования: Windows 10/11 x64 и .NET Framework 4.8. Лицензии шрифта и иконок включены в `ThirdPartyNotices.txt`.

---

# English

## What's new in Reswin 1.1.3

- **A new interface with three tabs:** Controls, Exclusions and Settings. Coral accent `#D03E36`, bundled Manrope and consistent Lucide icons in both light and dark themes.
- **Smooth navigation.** Click a tab or hold the left mouse button and drag the selection across the tab strip. Pages switch with a directional slide; selection, switches and the sensitivity thumb animate smoothly.
- **A compact window.** Height fits the content without extra space below it. Exclusions now has a dedicated tab with scrolling inside its own list area.
- **Snap to three screen edges.** Hold your selected Ctrl/Alt key and the mouse button assigned to Move. Drag the pointer to an edge and release the mouse button: the top maximizes the window; the left or right edge snaps it to that half of the screen. Side snapping uses the native Windows Snap feature.
- **Double-click to maximize.** Hold the selected modifier and double-click the mouse button assigned to Move. Double-click again to restore the window. Edge snapping and double-click behavior can be disabled independently.
- Preserved automatic scroll-key selection, 25–200% sensitivity adjustment, Russian and English, startup and application exclusions.
- Fixed the interface startup error reporting `x is an undeclared prefix`. Markup validation is now part of the build process.

## How to update

This build replaces the previously published 1.1.3. Exit the running instance of Reswin using the system tray, extract the new `Reswin-portable.zip` to a permanent folder, and launch `Reswin.exe`. Your settings and exclusions are preserved. No separate font installation is needed.

Native snapping depends on Windows Snap settings and the application's window support. Requirements: Windows 10/11 x64 and .NET Framework 4.8. Font and icon licenses are included in `ThirdPartyNotices.txt`.


## 1.1.2

# Русский

## Улучшения и исправления

- Интерфейс переведён на бесплатный Manrope. Шрифт встроен в приложение и меню трея, поэтому отдельная установка не требуется.
- Убран крупный заголовок Reswin и надпись «Включено». Переключатель «Включить приложение» с иконкой Power от Lucide перенесён в блок рядом с автозапуском.
- Исправлены перемещение и центрирование окна настроек самого Reswin через выбранную клавишу Ctrl или Alt и соответствующую кнопку мыши.
- Под настройкой прокрутки добавлен ползунок чувствительности от 25% до 200% с плавным увеличением бегунка при нажатии. Выбранное значение сохраняется между запусками; 100% соответствует стандартной чувствительности этой версии.
- Повышена чувствительность прокрутки: скорость сильнее увеличивается по мере удаления курсора от начальной точки.
- При чувствительности 100% предел максимальной скорости прокрутки поднят в 2,5 раза относительно версии 1.1.1. Ползунок позволяет дополнительно менять скорость. Устранено внутреннее ограничение шага, которое могло сдерживать скорость при более редкой обработке команд.
- Сохранены плавный разгон и торможение, обычный курсор и остановка режима при отпускании клавиши.

## Как обновиться

Выйдите из работающего Reswin через меню в трее, распакуйте `Reswin-portable.zip` в постоянную папку и запустите новый `Reswin.exe`. Настройки и чёрный список сохраняются. Отдельная установка приложения или шрифта не требуется.

Требования: Windows 10/11 x64 и .NET Framework 4.8. Лицензии встроенного шрифта и иконок находятся в `ThirdPartyNotices.txt` внутри архива.

---

# English

## Improvements and fixes

- Switched the interface to the free Manrope font. The font is bundled for both the application and the tray menu, so no separate installation is required.
- Removed the large Reswin heading and the Enabled label. The Enable application switch with Lucide's Power icon now sits in the same group as Start with Windows.
- Fixed moving and centering Reswin's own settings window using the selected Ctrl or Alt modifier and the corresponding mouse button.
- Added a scroll sensitivity slider below the scrolling setting, ranging from 25% to 200%, with a smoothly expanding thumb when pressed. The chosen value is saved between launches; 100% is this version's standard sensitivity.
- Increased scroll sensitivity: speed grows more strongly as the pointer moves further from the starting point.
- At 100% sensitivity, the maximum scroll speed limit is 2.5 times higher than in version 1.1.1. The slider lets you adjust it further. Removed an internal per-update limit that could restrict speed when commands are processed less frequently.
- Preserved smooth acceleration and braking, the normal cursor, and ending scroll mode when the key is released.

## How to update

Exit the running instance of Reswin using the system tray menu, extract `Reswin-portable.zip` to a permanent folder, and launch the new `Reswin.exe`. Your settings and exclusions are preserved. No separate application or font installation is required.

Requirements: Windows 10/11 x64 and .NET Framework 4.8. The bundled font and icon licenses are included in `ThirdPartyNotices.txt` inside the archive.


## 1.1.1

# Русский

## Улучшения и исправления

- **Исправлен конфликт с масштабированием.** При прокрутке удержанием Ctrl браузер больше не должен воспринимать её как Ctrl + колёсико. Клавиша перехватывается на время режима прокрутки.
- **Переработана плавность прокрутки.** Отдельный цикл с высокоточным таймером обеспечивает более равномерную отправку небольших шагов прокрутки. Скорость и ускорение сглаживаются при разгоне, торможении и смене направления.
- **Убраны скачки после задержек.** Пропущенные обновления не накапливаются в очередь, которую приложение затем должно резко догонять.
- **Обычный курсор.** Прокрутка работает без кружка или замены указателя. Отпускание клавиши завершает режим.
- **Сохранены привычные сочетания.** При вводе Ctrl+C, Ctrl+V, Alt+Tab и других сочетаний прокрутка прекращается, а сочетание передаётся приложению.

## Как обновиться

Выйдите из работающего Reswin через меню в трее, распакуйте `Reswin-portable.zip` и запустите новый `Reswin.exe`. Настройки и чёрный список сохраняются. Установка не требуется.

Требования: Windows 10/11 x64 и .NET Framework 4.8. Визуальная плавность также зависит от приложения: некоторые программы округляют прокрутку до целых строк.

---

# English

## Improvements and fixes

- **Fixed the zoom conflict.** Hold-to-scroll with Ctrl should no longer be interpreted by the browser as Ctrl + mouse wheel. The key is intercepted while scroll mode is active.
- **Reworked scroll smoothing.** A dedicated loop with a high-resolution timer delivers small scroll increments more evenly. Speed and acceleration are smoothed when accelerating, braking, and changing direction.
- **Removed catch-up jumps.** Missed updates do not accumulate into a queue that the application then has to catch up with in a sudden burst.
- **Normal cursor.** Scrolling works without a circle or a replacement pointer. Releasing the key ends scroll mode.
- **Preserved familiar shortcuts.** Pressing Ctrl+C, Ctrl+V, Alt+Tab, or another key combination stops scrolling and passes the shortcut to the application.

## How to update

Exit the running instance of Reswin using the system tray menu, extract `Reswin-portable.zip`, and launch the new `Reswin.exe`. Your settings and exclusions are preserved. No installation is required.

Requirements: Windows 10/11 x64 and .NET Framework 4.8. Visual smoothness also depends on the receiving application: some programs round scrolling to whole lines.


## 1.1.0

## Что нового

- **Прокрутка удержанием клавиши.** Удерживайте клавишу и двигайте мышь вверх или вниз, чтобы прокручивать содержимое. Отпустите клавишу — прокрутка остановится.
- **Автоматический выбор клавиши прокрутки.** Если управление окнами назначено на левый Ctrl, прокрутка работает через левый Alt, и наоборот. В настройках отображается конкретная клавиша; прокрутку можно отключить через «— Без настройки —».
- **Обновлённый интерфейс.** Компактное окно настроек с мягкими округлениями, переработанным расположением элементов и плавными переходами в светлой и тёмной темах.
- **Новое меню в трее.** Минималистичное оформление с закруглёнными краями и понятными действиями для паузы и возобновления работы.

## Улучшения и исправления

- Переработана обработка перемещения и изменения размера окон: медленно отвечающие приложения больше не создают очередь устаревших положений курсора.
- Подсказки соответствуют выбранному действию и обновляются при смене функции или основной клавиши Ctrl/Alt.
- Исправлена читаемость подсказок в тёмной теме.
- Пункт «— Без настройки —» расположен первым в списках действий.
- Сохранены привычные иконки кнопок мыши; обновлён значок «Зажать клавишу».

## Как обновиться

Выйдите из работающего Reswin через меню в трее, затем запустите новый `Reswin.exe`. Настройки и чёрный список сохраняются.

Скачайте `Reswin-portable.zip` и распакуйте его в постоянную папку. Установка не требуется. Требования: Windows 10/11 x64 и .NET Framework 4.8.
