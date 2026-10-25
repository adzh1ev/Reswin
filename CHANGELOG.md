# Изменения Reswin / Reswin changelog

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
