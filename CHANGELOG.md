# Изменения Reswin / Reswin changelog

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
