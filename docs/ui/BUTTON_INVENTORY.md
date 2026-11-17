# Реестр кнопок и интерактивных элементов AskMkiM

Срез исходного кода: 30 октября 2026; commit `8917b229`. Это техническая инвентаризация для согласования ToolTip, а не реализация подсказок и не пользовательская справка.

## Как читать список

- Просмотрены все 281 XAML-файл production-проектов `MainWindow`, `UI`, `Ask.UI`, `Message`, `ConsoleUI`, `Ask.Support`; ошибки XML-разбора отсутствуют. Дополнительно проверены C#-создание кнопок, подписки обработчиков и HTML встроенной справки.
- Строка означает объявление в исходниках, а не уникальную кнопку на экране. DataTemplate может создать много экземпляров; разные UI-представления и версии компонентов перечислены отдельно. Шаблоны не прибавляются к числу пользовательских действий.
- Комментарии XAML/HTML не учитываются. Тестовые приложения/проекты и стенд расследования зависания не включены. Стандартные кнопки системных Open/Save/Print-диалогов Windows не принадлежат к исходникам приложения и отдельно не перечислены.
- Файл и строка однозначно указывают элемент. «Действие» содержит фактический обработчик или binding; если связь не задана прямо, указана необходимость проверки code-behind/шаблона. Имя метода не выдается за доказательство его внутренней семантики.
- «—» в ToolTip означает отсутствие найденного ToolTip в этом объявлении, его локальном стиле или стиле родителя; подсказка может назначаться стилем, кодом или родителем. Нельзя считать эту колонку окончательным списком отсутствующих подсказок. Динамические назначения описаны ниже.
- Visibility — исходное значение из XAML, не итоговая доступность. Полный аудит условий роли, состояния выполнения и Command.CanExecute в эту инвентаризацию не входит. Перечислены также объявления с Collapsed/Hidden и служебные экраны.
- Подписи из LocalizedString раскрыты по русскому Strings.resx, где ключ удалось разрешить. Binding без статического текста сохранён буквально. Для иконок предпочтено существующее текстовое описание ToolTip; непрозрачная иконка помечена именем типа.

## Сводка

| Категория | Объявлений |
| --- | ---: |
| Кнопки | 244 |
| Собственные компоненты | 36 |
| Кликабельные элементы | 31 |
| Шаблоны | 34 |
| Флажки и переключатели | 31 |
| Меню | 83 |
| HTML-кнопки и закрытие увеличенного изображения | 33 |

244 элемента первой категории — кнопки и ToggleButton/RepeatButton вне ControlTemplate. Кнопки из DataTemplate включены как объявления. Дополнительные категории не следует суммировать как число уникальных кнопок приложения: собственный компонент может содержать Button из первой категории.

Обновление: выбранные оконные кнопки, большая панель выполнения, сворачивание сообщений протокола, поиск и стрелки секций получили подсказки. Текст меняется по состоянию там, где действие переключается.

## Существующая система ToolTip

- [UI/Style.xaml](../../UI/Style.xaml), строка 54: общий неименованный Style для ToolTip; шаблон, отступы, перенос текста, стрелка, ограничение ширины, блок горячей клавиши.
- Тематические ресурсы `ToolTipBackgroundBrush`, `ToolTipForegroundBrush`, `ToolTipBorderBrush`, `ToolTipHotkeyBackgroundBrush`, `ToolTipHotkeyForegroundBrush`, `ToolTipShadowBrush` уже определены в `UI/Resources/Theme/{light,dark,light.custom,dark.custom}.xaml`.
- [MainWindow/HotkeyBindings/UniversalHotkeyBinder.cs](../../MainWindow/HotkeyBindings/UniversalHotkeyBinder.cs): обновляет ToolTip с учётом комбинации клавиш. При дальнейшей реализации нужно сохранить эту связь.
- Разметка HTML-справки использует title/aria-label и свою тему; WPF Style к ней не применяется.

## Кнопки

### [Ask.Support/HelpBrowserWindow.xaml](../../Ask.Support/HelpBrowserWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X001 / 56 | Button | Свернуть | Click → Minimize_Click | Свернуть | — |
| X002 / 60 | MaximizeButton / Button | Развернуть | Click → Maximize_Click | Развернуть | — |
| X003 / 64 | Button | Закрыть | Click → Close_Click | Закрыть | — |

### [Ask.UI/Components/DeviceSelectorPanel/ChoiceDevice.xaml](../../Ask.UI/Components/DeviceSelectorPanel/ChoiceDevice.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X004 / 11 | toggleButton / ToggleButton | {Binding SelectedDisplayText} | Checked → toggleButton_Checked; Unchecked → toggleButton_Unchecked | — | — |

### [Ask.UI/Components/ProtocolListBox/ProtocolListBoxUI.xaml](../../Ask.UI/Components/ProtocolListBox/ProtocolListBoxUI.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X005 / 111 | ServiceLogsToggle / ToggleButton | Показать служебные сообщения операции (стиль; текст зависит от состояния) | Click → ServiceLogsToggleButton_Click | Показать служебные сообщения операции (стиль; текст зависит от состояния) | Collapsed |
| X006 / 395 | ToggleButton | {Binding} | Click → ProtocolCommandHeaderToggleButton_Click | Развернуть сообщения команды (стиль; текст зависит от состояния) | — |

### [Ask.UI/Controls/ErrorList/ErrorListControl.xaml](../../Ask.UI/Controls/ErrorList/ErrorListControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X007 / 129 | WarningButton / ToggleButton | 0 из 0 предупреждений | PreviewMouseDown → WarningButton_PreviewMouseDown | — | — |
| X008 / 136 | ErrorsButton / ToggleButton | 0 из 0 ошибок | PreviewMouseDown → ErrorsButton_PreviewMouseDown | — | — |
| X009 / 156 | PrevPageButton / Button | &lt; Назад | Click → PrevPageButton_Click | — | — |
| X010 / 171 | Button | {Binding DisplayText} | Click → PageNumberButton_Click | — | — |
| X011 / 180 | NextPageButton / Button | Далее &gt; | Click → NextPageButton_Click | — | — |

### [Ask.UI/Controls/ProtocolNew/ProtocolInfoWindow.xaml](../../Ask.UI/Controls/ProtocolNew/ProtocolInfoWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X012 / 95 | Button | Сохранить | PreviewMouseDown → SaveButton_Click | — | — |
| X013 / 102 | Button | Отмена | PreviewMouseDown → CancelButton_Click | — | — |

### [Ask.UI/Controls/ProtocolNew/ProtocolUI.xaml](../../Ask.UI/Controls/ProtocolNew/ProtocolUI.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X014 / 144 | OpenFileButton / Button | Открыть файл в редакторе | Click → OpenFileButton_Click | Открыть файл в редакторе | — |
| X015 / 154 | OpenFolderButton / Button | Показать файл в папке | Click → OpenFolderButton_Click | Показать файл в папке | — |
| X016 / 165 | PrintButton / Button | Печать файла | Click → PrintButton_Click | Печать файла | — |
| X017 / 204 | Button | Открыть протокол проверки в редакторе | Click → OpenInspectionFileButton_Click | Открыть протокол проверки в редакторе | — |
| X018 / 213 | Button | Показать протокол проверки в папке | Click → OpenInspectionFolderButton_Click | Показать протокол проверки в папке | — |
| X019 / 223 | Button | Печать протокола проверки | Click → PrintInspectionButton_Click | Печать протокола проверки | — |
| X020 / 270 | ContinueButtonTop / Button | Продолжить (P/F5) | SetupEventHandlers → NextButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Продолжить (P/F5) | — |
| X021 / 274 | StartButtonTop / Button | Запустить выполнение | SetupEventHandlers → StartMeasureResistanceButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Запустить (Enter/F5) | — |
| X022 / 288 | PauseButtonTop / Button | Пауза (P/F5) | SetupEventHandlers → StopButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Пауза (P/F5) | — |
| X023 / 292 | StopButtonTop / Button | Остановить (Esc) | SetupEventHandlers → ExitButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Остановить (Esc) | — |
| X024 / 296 | SkipCommandButtonTop / Button | Завершить текущую команду и перейти к следующей | Click → SkipCommandButtonTop_Click | Завершить текущую команду и перейти к следующей | {Binding ElementName=ContinueButtonTop, Path=Visibility} |
| X025 / 305 | RepeatButtonTop / Button | Повторить (R) | SetupEventHandlers → ReturnMeasureResistanceButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Повторить (R) | — |
| X026 / 309 | StepOverTop / Button | Шаг с обходом (F10) | SetupEventHandlers → TopLayer_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Шаг с обходом (F10) | — |
| X027 / 313 | StepIntoTop / Button | Шаг с заходом (F11) | SetupEventHandlers → BottomLayer_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Шаг с заходом (F11) | — |
| X028 / 317 | CommandJumpButtonTop / Button | Перейти к другой команде (F4) | Click → CommandJumpButtonTop_Click | Перейти к другой команде (F4) | {Binding ElementName=ContinueButtonTop, Path=Visibility} |
| X029 / 326 | ExecutionSettingsButtonTop / Button | Настройки выполнения | Click → ExecutionSettingsButtonTop_Click | Настройки выполнения | Collapsed |
| X030 / 446 | StartButton / Button | ЗАПУСТИТЬ(F5) | SetupEventHandlers → StartMeasureResistanceButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Начать выполнение проверки (F5) | — |
| X031 / 451 | RepeatButton / Button | ПОВТОРИТЬ(R) | SetupEventHandlers → ReturnMeasureResistanceButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Запустить повторное выполнение проверки (R) | — |
| X032 / 458 | PauseButton / Button | ОСТАНОВИТЬ(P) | SetupEventHandlers → StopButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Приостановить выполнение проверки (P/F5) | — |
| X033 / 471 | StepOver / Button | Поверх(F10) | SetupEventHandlers → TopLayer_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Выполнить следующий шаг без углубления в проверку (F10) | — |
| X034 / 479 | StepInto / Button | Вглубь(F11) | SetupEventHandlers → BottomLayer_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Выполнить следующий шаг с входом в проверку (F11) | — |
| X035 / 490 | ContinueButton / Button | ПРОДОЛЖИТЬ(P) | SetupEventHandlers → NextButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Возобновить выполнение проверки (P/F5) | — |
| X036 / 499 | StopButton / Button | ЗАВЕРШИТЬ(Esc) | SetupEventHandlers → ExitButton_PreviewMouseDown (ProtocolUI.ButtonManager.cs) | Остановить выполнение и завершить проверку (Esc) | — |

### [Ask.UI/Controls/ProtocolNew/SavedProtocolPairUI.xaml](../../Ask.UI/Controls/ProtocolNew/SavedProtocolPairUI.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X037 / 37 | Button | Печать протокола | Click → PrintButton_Click | Печать протокола | — |
| X038 / 41 | Button | Экспортировать протокол в PDF | Click → ExportPdfButton_Click | Экспортировать протокол в PDF | — |

### [Ask.UI/Features/Archive/Views/ArchiveControl.xaml](../../Ask.UI/Features/Archive/Views/ArchiveControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X039 / 646 | RecheckReviewArchiveButton / Button | Проверить все файлы ещё раз | Command → {Binding RecheckSelectedReviewArchiveCommand} | Проверить все файлы ещё раз | {Binding RecheckReviewArchiveVisibility} |
| X040 / 653 | SaveArchiveButton / Button | Сохранить архив на диск | Command → {Binding SaveSelectedArchiveCommand} | Сохранить архив на диск | {Binding SaveArchiveVisibility} |
| X041 / 660 | ConvertArchiveToApkButton / Button | Конвертировать APKW в APK для старой программы | Command → {Binding ConvertSelectedArchiveToApkCommand} | Конвертировать APKW в APK для старой программы | {Binding ConvertArchiveToApkVisibility} |
| X042 / 668 | PrintArchiveCatalogButton / Button | Распечатать каталог архива | Command → {Binding PrintContextArchiveCatalogCommand} | Распечатать каталог архива | {Binding PrintArchiveCatalogVisibility} |
| X043 / 675 | AddFileToArchiveButton / Button | Добавить файл в архив | Command → {Binding AddFileToSelectedArchiveCommand} | Добавить файл в архив | {Binding AddFileToArchiveVisibility} |
| X044 / 683 | PasteIntoArchiveButton / Button | Вставить файл в архив | Command → {Binding PasteIntoSelectedArchiveCommand} | Вставить файл в архив | {Binding PasteIntoArchiveVisibility} |
| X045 / 690 | DeleteArchiveButton / Button | Удалить архив | Command → {Binding DeleteSelectedArchiveCommand} | Удалить архив | {Binding DeleteArchiveVisibility} |
| X046 / 862 | RecheckReviewFileButton / Button | Проверить файл ещё раз | Command → {Binding RecheckSelectedReviewFileCommand} | Проверить файл ещё раз | {Binding RecheckReviewFileVisibility} |
| X047 / 869 | OpenInEditorButton / Button | Открыть файл в текстовом редакторе | Command → {Binding OpenSelectedFileInEditorCommand} | Открыть файл в текстовом редакторе | {Binding OpenInEditorVisibility} |
| X048 / 876 | CopyArchiveFileButton / Button | Копировать файл | Command → {Binding CopySelectedFileCommand} | Копировать файл | {Binding CopyArchiveFileVisibility} |
| X049 / 884 | CutArchiveFileButton / Button | Вырезать файл | Command → {Binding CutSelectedFileCommand} | Вырезать файл | {Binding CutArchiveFileVisibility} |
| X050 / 892 | PasteArchiveFileButton / Button | Вставить файл | Command → {Binding PasteIntoSelectedFileArchiveCommand} | Вставить файл | {Binding PasteArchiveFileVisibility} |
| X051 / 899 | ConvertToPkwButton / Button | Конвертировать в pkw | Command → {Binding ConvertSelectedFileToPkwCommand} | Конвертировать в pkw | {Binding ConvertToPkwVisibility} |
| X052 / 907 | RunInExecutorButton / Button | Запустить в исполнителе | Command → {Binding RunSelectedFileInExecutorCommand} | Запустить в исполнителе | {Binding RunInExecutorVisibility} |
| X053 / 914 | DeleteArchiveFileButton / Button | Удалить файл | Command → {Binding DeleteSelectedFileCommand} | Удалить файл | {Binding DeleteArchiveFileVisibility} |

### [Ask.UI/Features/Archive/Views/ArchiveNameInputControl.xaml](../../Ask.UI/Features/Archive/Views/ArchiveNameInputControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X054 / 59 | CreateButton / Button | Создать | Click → CreateButton_OnClick | — | — |
| X055 / 64 | CancelButton / Button | Отмена | Закрытие диалога (IsCancel=True) | — | — |

### [Ask.UI/Features/Archive/Views/ArchiveSaveTargetPickerControl.xaml](../../Ask.UI/Features/Archive/Views/ArchiveSaveTargetPickerControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X056 / 70 | CreateArchiveButton / Button | Создать архив | Click → CreateArchiveButton_OnClick | — | — |
| X057 / 74 | SaveButton / Button | Сохранить | Click → SaveButton_OnClick | — | — |
| X058 / 79 | CancelButton / Button | Отмена | Закрытие диалога (IsCancel=True) | — | — |

### [Ask.UI/Features/BuildDiagnostics/Views/BuildHistoryWindow.xaml](../../Ask.UI/Features/BuildDiagnostics/Views/BuildHistoryWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X059 / 74 | Button | × | Click → CloseButton_Click | — | — |
| X060 / 170 | Button | Копировать сведения | Click → CopyButton_Click | — | — |
| X061 / 178 | Button | Закрыть | Закрытие диалога (IsCancel=True) | — | — |

### [Ask.UI/Features/FileComparison/Views/FileCompareSelectionControl.xaml](../../Ask.UI/Features/FileComparison/Views/FileCompareSelectionControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X062 / 388 | Button | Обновить список | Click → RefreshButton_Click | — | — |
| X063 / 399 | Button | Запустить сравнение | Click → StartCompareButton_Click | — | — |

### [Ask.UI/Features/ReleaseNotes/Views/ReleaseNotesPrompt.xaml](../../Ask.UI/Features/ReleaseNotes/Views/ReleaseNotesPrompt.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X064 / 5 | OpenButton / Button | Открыть историю изменений в справке | Click → OpenButton_Click | Открыть историю изменений в справке | — |

### [Ask.UI/Features/RoleManagement/Views/RolePasswordManagementControl.xaml](../../Ask.UI/Features/RoleManagement/Views/RolePasswordManagementControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X065 / 205 | Button | Сохранить пароль | Command → {Binding SavePasswordCommand} | — | — |

### [Ask.UI/Features/ServiceTools/Chassis/ChassisControl.xaml](../../Ask.UI/Features/ServiceTools/Chassis/ChassisControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X066 / 82 | Button | Инициализировать | Click → InitializeButton_Click | — | — |
| X067 / 86 | Button | Полный сброс | Click → ResetButton_Click | — | — |
| X068 / 90 | Button | Обновить | Click → RefreshButton_Click | — | — |
| X069 / 111 | Button | Включить питание | Click → StartPowerButton_Click | — | — |
| X070 / 115 | Button | Выключить питание | Click → StopPowerButton_Click | — | — |
| X071 / 119 | Button | Проверить питание | Click → VerifyPowerButton_Click | — | — |

### [Ask.UI/Features/ServiceTools/Gpt/Modes/AcwMode.xaml](../../Ask.UI/Features/ServiceTools/Gpt/Modes/AcwMode.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X072 / 68 | ConnectButton / Button | Включить | PreviewMouseDown → Button_PreviewMouseDown | — | — |
| X073 / 188 | Button | Сохранить параметры | PreviewMouseDown → Button_PreviewMouseDown_1 | — | — |
| X074 / 224 | Button | Считать с устройства | Click → ReadConfigurationButton_Click | — | — |
| X075 / 245 | Button | Запустить тест | Click → StartTestButton_Click | — | — |

### [Ask.UI/Features/ServiceTools/Gpt/Modes/DcwMode.xaml](../../Ask.UI/Features/ServiceTools/Gpt/Modes/DcwMode.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X076 / 49 | ConnectButton / Button | Включить | PreviewMouseDown → Button_PreviewMouseDown | — | — |
| X077 / 122 | Button | Сохранить | PreviewMouseDown → Button_PreviewMouseDown_1 | — | — |
| X078 / 140 | Button | Считать с устройства | Click → ReadConfigurationButton_Click | — | — |
| X079 / 148 | Button | Запустить тест | Click → StartTestButton_Click | — | — |

### [Ask.UI/Features/ServiceTools/Gpt/Modes/IrMode.xaml](../../Ask.UI/Features/ServiceTools/Gpt/Modes/IrMode.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X080 / 48 | ConnectButton / Button | Включить | PreviewMouseDown → Button_PreviewMouseDown | — | — |
| X081 / 113 | Button | Сохранить | PreviewMouseDown → Button_PreviewMouseDown_1 | — | — |
| X082 / 130 | Button | Считать с устройства | Click → ReadConfigurationButton_Click | — | — |
| X083 / 140 | Button | Запустить тест | Click → StartTestButton_Click | — | — |

### [Ask.UI/Features/ServiceTools/Multimeter/MultimeterControl.xaml](../../Ask.UI/Features/ServiceTools/Multimeter/MultimeterControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X084 / 78 | Button | Обновить | Click → RefreshButton_Click | — | — |
| X085 / 96 | Button | Подключить | Click → ConnectButton_Click | — | — |
| X086 / 97 | Button | Отключить | Click → DisconnectButton_Click | — | — |
| X087 / 98 | Button | Инициализировать | Click → InitializeButton_Click | — | — |
| X088 / 100 | Button | Сбросить | Click → ResetButton_Click | — | — |
| X089 / 118 | Button | Установить режим | Click → SetModeButton_Click | — | — |
| X090 / 120 | Button | Установить диапазон | Click → SetRangeButton_Click | — | — |
| X091 / 155 | Button | Измерить | Click → MeasureButton_Click | — | — |

### [Ask.UI/Features/ServiceTools/RelaySwitchModule/RelaySwitchModuleControl.xaml](../../Ask.UI/Features/ServiceTools/RelaySwitchModule/RelaySwitchModuleControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X092 / 92 | Button | Обновить | Click → RefreshButton_Click | — | — |
| X093 / 127 | Button | Подключить | Click → ConnectPointButton_Click | — | — |
| X094 / 128 | Button | Отключить | Click → DisconnectPointButton_Click | — | — |
| X095 / 129 | Button | Подключить с проверкой | Click → ConnectPointVerifiedButton_Click | — | — |
| X096 / 131 | Button | Отключить с проверкой | Click → DisconnectPointVerifiedButton_Click | — | — |
| X097 / 133 | Button | Перевести на эту шину | Click → MovePointButton_Click | — | — |
| X098 / 159 | Button | Запустить самоконтроль | Click → CheckPointButton_Click | — | — |
| X099 / 197 | Button | Подключить диапазон | Click → ConnectGroupButton_Click | — | — |
| X100 / 199 | Button | Отключить диапазон | Click → DisconnectGroupButton_Click | — | — |
| X101 / 209 | Button | Измеритель: вкл. | Click → ConnectMeterButton_Click | — | — |
| X102 / 210 | Button | Измеритель: выкл. | Click → DisconnectMeterButton_Click | — | — |
| X103 / 211 | Button | Отключить все точки | Click → DisconnectAllPointsButton_Click | — | — |
| X104 / 213 | Button | Отключить шину A | Click → DisconnectBusAPointsButton_Click | — | — |
| X105 / 214 | Button | Отключить шину B | Click → DisconnectBusBPointsButton_Click | — | — |
| X106 / 230 | Button | Подключить шину | Click → ConnectBusButton_Click | — | — |
| X107 / 231 | Button | Отключить шину | Click → DisconnectBusButton_Click | — | — |

### [Ask.UI/Features/ServiceTools/SwitchingDevice/SwitchingDeviceControl.xaml](../../Ask.UI/Features/ServiceTools/SwitchingDevice/SwitchingDeviceControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X108 / 105 | Button | Обновить | Click → RefreshButton_Click | — | — |
| X109 / 136 | Button | Подключить | Click → ConnectMultimeterButton_Click | — | — |
| X110 / 139 | Button | Отключить | Click → DisconnectMultimeterButton_Click | — | — |
| X111 / 146 | Button | ППУ подключить | Click → ConnectBreakdownTesterButton_Click | — | — |
| X112 / 149 | Button | ППУ отключить | Click → DisconnectBreakdownTesterButton_Click | — | — |
| X113 / 152 | Button | ППУ + мультиметр | Click → ConnectBreakdownAndMeterButton_Click | — | — |
| X114 / 155 | Button | Отключить оба | Click → DisconnectBreakdownAndMeterButton_Click | — | — |
| X115 / 162 | Button | Все шины: вкл. | Click → ConnectAllBusesButton_Click | — | — |
| X116 / 165 | Button | Все шины: выкл. | Click → DisconnectAllBusesButton_Click | — | — |
| X117 / 168 | Button | Делитель: вкл. | Click → EnableDividerButton_Click | — | — |
| X118 / 171 | Button | Делитель: выкл. | Click → DisableDividerButton_Click | — | — |
| X119 / 204 | Button | Замкнуть | Click → ConnectRelayButton_Click | — | — |
| X120 / 208 | Button | Разомкнуть | Click → DisconnectRelayButton_Click | — | — |
| X121 / 214 | Button | Все реле: вкл. | Click → EnableRelayButton_Click | — | — |
| X122 / 217 | Button | Все реле: выкл. | Click → DisableRelayButton_Click | — | — |
| X123 / 233 | Button | Подключить | Click → ConnectResistorButton_Click | — | — |
| X124 / 236 | Button | Отключить | Click → DisconnectResistorButton_Click | — | — |
| X125 / 247 | Button | Подключить | Click → ConnectCapacitorButton_Click | — | — |
| X126 / 250 | Button | Отключить | Click → DisconnectCapacitorButton_Click | — | — |
| X127 / 292 | Button | Подключить цепь | Click → ConnectSelfTestCircuitButton_Click | — | — |
| X128 / 295 | Button | Отключить цепь | Click → DisconnectSelfTestCircuitButton_Click | — | — |

### [Ask.UI/Features/TextSearch/Views/SearchPanelWindow.xaml](../../Ask.UI/Features/TextSearch/Views/SearchPanelWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X129 / 311 | PreviousButton / Button | Предыдущее совпадение | Click → Previous_Click | Предыдущее совпадение | — |
| X130 / 314 | NextButton / Button | Следующее совпадение | Click → Next_Click | Следующее совпадение | — |
| X131 / 319 | FindAllButton / Button | Найти все | Click → FindAll_Click | Найти все | — |
| X132 / 327 | SettingsButton / Button | Настройки поиска | Click → Settings_Click | Настройки поиска | — |
| X133 / 330 | Button | Закрыть поиск | Click → Close_Click | Закрыть (Esc) | — |
| X134 / 349 | ReplaceButton / Button | Заменить | Click → Replace_Click | Заменить текущее совпадение указанным текстом | — |
| X135 / 350 | ReplaceAllButton / Button | Заменить всё | Click → ReplaceAll_Click | Заменить все совпадения в выбранной области поиска | — |
| X136 / 354 | MatchCaseOption / ToggleButton | Aa Регистр | Click → ClearSearchResults; опция используется поиском (SearchPanelWindow.Search.cs) | Различать прописные и строчные буквы при поиске | — |
| X137 / 355 | WholeWordOption / ToggleButton | ab Целое слово | Click → ClearSearchResults; опция используется поиском (SearchPanelWindow.Search.cs) | Искать только целые слова, исключая части слов | — |
| X138 / 356 | RegexOption / ToggleButton | .* Регулярное выражение | Click → ClearSearchResults; опция используется поиском (SearchPanelWindow.Search.cs) | Регулярные выражения .NET. В замене: $1, ${имя}, $$ | — |
| X139 / 419 | ResetSettingsButton / Button | Сбросить | Click → ResetSettings_Click | Очистить текст замены и вернуть исходные параметры поиска | — |
| X140 / 462 | PreviousResultsPage / Button | Предыдущая страница | Click → PreviousResultsPage_Click | Предыдущая страница | — |
| X141 / 466 | NextResultsPage / Button | Следующая страница | Click → NextResultsPage_Click | Следующая страница | — |

### [Ask.UI/Shared/Components/Icons/ArrowButton.xaml](../../Ask.UI/Shared/Components/Icons/ArrowButton.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X142 / 21 | PART_Button / Button | Скрыть содержимое раздела (стиль; текст зависит от состояния) (на родителе) | Click → Button_Click; Command → {Binding RelativeSource={RelativeSource AncestorType=local:ArrowButton}, Path=Command} | Скрыть содержимое раздела (стиль; текст зависит от состояния) (на родителе) | — |

### [Ask.UI/Windows/VoltageValue.xaml](../../Ask.UI/Windows/VoltageValue.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X143 / 42 | Button | Сохранить | PreviewMouseDown → SaveButton_Click | — | — |

### [ConsoleUI/ConsoleUI/ConsoleOverlay.xaml](../../ConsoleUI/ConsoleUI/ConsoleOverlay.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X144 / 284 | Button | Close | Click → CloseButton_Click | Close | — |
| X145 / 288 | Button | Minimize | Click → MinimizeButton_Click | Minimize | — |
| X146 / 292 | Button | Maximize | Click → MaximizeButton_Click | Maximize | — |

### [MainWindow/MainWindow.xaml](../../MainWindow/MainWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X147 / 278 | TodayTasksButton / Button | Дела на сегодня | Click → TodayTasksButton_Click | Дела на сегодня | — |
| X148 / 316 | DailyReportButton / Button | Собрать отчёт за день | Click → DailyReportButton_Click | Собрать отчёт за выбранный день в ZIP | — |
| X149 / 324 | CurrentUserButton / Button |  / Пользователь | Click → CurrentUserButton_Click | Сменить пользователя | — |
| X150 / 361 | DarkThemeButton / Button | Тёмная тема | Click → ThemeToggleButton_Click | Тёмная тема | Collapsed |
| X151 / 373 | LightThemeButton / Button | Светлая тема | Click → ThemeToggleButton_Click | Светлая тема | Collapsed |
| X152 / 385 | minimizeButton / Button | Свернуть окно на панель задач | Command → {Binding Window.MinimizeCommand} | Свернуть окно на панель задач | — |
| X153 / 396 | maximizeButton / Button | Развернуть окно на весь экран (стиль; текст зависит от состояния) | Command → {Binding Window.MaximizeCommand} | Развернуть окно на весь экран (стиль; текст зависит от состояния) | — |
| X154 / 442 | exitButton / Button | Завершить работу приложения | Command → {Binding Window.CloseCommand} | Завершить работу приложения | — |
| X155 / 478 | TerminalButton / Button | Открыть терминал | Click → TerminalButton_Click | Терминал (Ctrl + ~) | — |

### [MainWindow/RoleLoginWindow.xaml](../../MainWindow/RoleLoginWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X156 / 418 | CancelButton / Button | Закрыть окно входа | Click → CancelButton_Click | Закрыть | — |
| X157 / 478 | TogglePasswordVisibilityButton / Button | Показать пароль | Click → TogglePasswordVisibilityButton_Click | — | — |
| X158 / 530 | KeyboardLayoutButton / Button | Переключить раскладку клавиатуры | Click → KeyboardLayoutButton_Click | — | — |
| X159 / 536 | LoginButton / Button | Войти | Click → LoginButton_Click | — | — |

### [MainWindow/Windows/ApkToApkwConversionWindow.xaml](../../MainWindow/Windows/ApkToApkwConversionWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X160 / 51 | Button | Закрыть окно | PreviewMouseDown → CloseButton_PreviewMouseDown | Закрыть окно | — |
| X161 / 95 | Button | Выбрать файл | Click → SelectInputFileButton_Click | — | — |
| X162 / 119 | Button | Отмена | Закрытие диалога (IsCancel=True) | — | — |
| X163 / 124 | ConvertButton / Button | Конвертировать | Click → ConvertButton_Click | — | — |

### [MainWindow/Windows/ApkwToApkConversionWindow.xaml](../../MainWindow/Windows/ApkwToApkConversionWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X164 / 51 | Button | Закрыть окно | PreviewMouseDown → CloseButton_PreviewMouseDown | Закрыть окно | — |
| X165 / 96 | Button | Выбрать файл | Click → SelectInputFileButton_Click | — | — |
| X166 / 124 | Button | Выбрать папку | Click → SelectOutputDirectoryButton_Click | — | — |
| X167 / 147 | Button | Отмена | Закрытие диалога (IsCancel=True) | — | — |
| X168 / 152 | ConvertButton / Button | Конвертировать | Click → ConvertButton_Click | — | — |

### [MainWindow/Windows/OpkToPkConversionWindow.xaml](../../MainWindow/Windows/OpkToPkConversionWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X169 / 58 | Button | Закрыть окно | PreviewMouseDown → CloseButton_PreviewMouseDown | Закрыть окно | — |
| X170 / 104 | Button | Выбрать папку | Click → SelectOutputDirectoryButton_Click | — | — |
| X171 / 137 | Button | Выбрать файлы | Click → SelectFilesButton_Click | — | — |
| X172 / 140 | Button | Очистить список | Click → ClearFilesButton_Click | — | — |
| X173 / 167 | Button | Отмена | Закрытие диалога (IsCancel=True) | — | — |
| X174 / 171 | ConvertButton / Button | Конвертировать | Click → ConvertButton_Click | — | — |

### [MainWindow/Windows/RunAskMkiWindow.xaml](../../MainWindow/Windows/RunAskMkiWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X175 / 161 | Button | × | Click → CancelButton_Click | — | — |
| X176 / 214 | Button | Выбрать | Click → SelectMkiButton_Click | — | — |
| X177 / 252 | Button | Выбрать | Click → SelectProgramButton_Click | — | — |
| X178 / 273 | Button | Отмена | Click → CancelButton_Click | — | — |
| X179 / 279 | RunButton / Button | Запустить | Click → RunButton_Click | — | — |

### [Message/MessageBoxCustom.xaml](../../Message/MessageBoxCustom.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X180 / 175 | OkButton / Button | Ок [Enter] | PreviewMouseDown → OkButton_PreviewMouseDown | — | — |
| X181 / 179 | CancelButton / Button | Отмена [Esc] | PreviewMouseDown → CancelButton_PreviewMouseDown | — | — |
| X182 / 183 | YesButton / Button | Да [Y] | PreviewMouseDown → YesButton_PreviewMouseDown | — | — |
| X183 / 187 | NoButton / Button | Нет [N] | PreviewMouseDown → NoButton_PreviewMouseDown | — | — |

### [UI/Components/KeyboardLayoutComponent.xaml](../../UI/Components/KeyboardLayoutComponent.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X184 / 14 | LayoutButton / Button | Переключить раскладку клавиатуры | Click → LayoutButton_Click | Раскладка клавиатуры. Нажмите, чтобы переключить язык. | — |

### [UI/Components/MultiWindowControl.xaml](../../UI/Components/MultiWindowControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X185 / 99 | minimizeButton / Button | Иконка: WindowMinimizeIcon | PreviewMouseDown → minimizeButton_PreviewMouseDown | — | — |
| X186 / 111 | exitButton / Button | Иконка: WindowCloseIcon | PreviewMouseDown → exitButton_PreviewMouseDown | — | — |

### [UI/Components/PowerButton.xaml](../../UI/Components/PowerButton.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X187 / 6 | PowerActionButton / Button | Управление питанием системы | Click → OnPowerActionButtonClick | Подключить систему | — |

### [UI/Components/SearchControls/ArrowButton.xaml](../../UI/Components/SearchControls/ArrowButton.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X188 / 15 | ArrowButtonControl / Button | Без текстовой подписи | Click → Button_Click | — | — |

### [UI/Components/SearchControls/CaseToggleButton.xaml](../../UI/Components/SearchControls/CaseToggleButton.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X189 / 11 | ToggleButton / Button | Aa | PreviewMouseDown → ToggleButton_PreviewMouseDown | — | — |

### [UI/Components/SearchControls/WholeWordToggleButton.xaml](../../UI/Components/SearchControls/WholeWordToggleButton.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X190 / 10 | ToggleButton / Button | Abl | PreviewMouseDown → ToggleButton_PreviewMouseDown | — | — |

### [UI/Controls/AdminPanel/DatabaseTableView.xaml](../../UI/Controls/AdminPanel/DatabaseTableView.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X191 / 93 | SaveButton / Button | Сохранить изменения | Click → SaveButton_Click | Сохранить изменения | Collapsed |
| X192 / 105 | ResetButton / Button | Сбросить изменения | Click → ResetButton_Click | Сбросить изменения | Collapsed |

### [UI/Controls/Calendar/CalendarControl.xaml](../../UI/Controls/Calendar/CalendarControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X193 / 342 | TodayButton / Button | Сегодня | Command → {Binding TodayCommand} | — | — |
| X194 / 359 | PreviousMonthButton / Button | Предыдущий месяц | Command → {Binding PrevMonthCommand} | Предыдущий месяц | — |
| X195 / 381 | NextMonthButton / Button | Следующий месяц | Command → {Binding NextMonthCommand} | Следующий месяц | — |
| X196 / 419 | Button | {Binding DayNumber} | Command → {Binding DataContext.SelectDateCommand, RelativeSource={RelativeSource AncestorType=UserControl}} | — | — |
| X197 / 440 | AddNoteButton / Button | + Добавить | Click → AddNote_Click | — | — |
| X198 / 441 | CloseNotesButton / Button | × | Click → CloseNotes_Click | Закрыть заметки | — |
| X199 / 444 | ReloadNotesButton / Button | Повторить загрузку | Click → ReloadNotes_Click | — | Collapsed |
| X200 / 476 | Button | Редактировать заметку | Click → EditListNote_Click | Редактировать заметку | — |
| X201 / 479 | Button | Удалить заметку | Click → DeleteListNote_Click | Удалить заметку | — |
| X202 / 484 | Button | Подтвердить удаление | Click → ConfirmDelete_Click | Подтвердить удаление | — |
| X203 / 487 | Button | Отменить удаление | Click → CancelDelete_Click | Отменить удаление | — |
| X204 / 533 | SaveNoteButton / Button | Сохранить заметку | Click → SaveNote_Click | Сохранить заметку | — |
| X205 / 536 | CancelEditButton / Button | Отменить редактирование | Click → CancelEdit_Click | Отменить редактирование | — |
| X206 / 545 | UndoDeleteButton / Button | Отменить | Click → UndoDelete_Click | Восстановить последнюю удалённую заметку | Collapsed |

### [UI/Controls/DailyReportDateWindow.xaml](../../UI/Controls/DailyReportDateWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X207 / 40 | Button | × | Закрытие диалога (IsCancel=True) | — | — |
| X208 / 86 | Button | Отмена | Закрытие диалога (IsCancel=True) | — | — |
| X209 / 88 | Button | Сохранить ZIP | Click → Save_Click | — | — |

### [UI/Controls/DateTimeControl.xaml](../../UI/Controls/DateTimeControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X210 / 8 | DateTimeButton / Button | Время и дата. Открыть календарь | Click → DateTimeButton_Click | Открыть календарь | — |

### [UI/Controls/DeviceHealthView/DeviceHealthViewControl.xaml](../../UI/Controls/DeviceHealthView/DeviceHealthViewControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X211 / 24 | Button | {Binding NameNumber} | PreviewMouseDown → DevicePreviewMouseDown | — | — |

### [UI/Controls/DevicesStatus.xaml](../../UI/Controls/DevicesStatus.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X212 / 18 | UpdateState / Button | Обновить состояние | PreviewMouseDown → ExecutionEventAdapter.RaiseDeviceStatusUpdate (DevicesStatus.xaml.cs) | — | — |

### [UI/Controls/EmptyWorkspace/EmptyWorkspaceView.xaml](../../UI/Controls/EmptyWorkspace/EmptyWorkspaceView.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X213 / 103 | OpenFileButton / Button | Открыть файл | Command → {Binding File.OpenFileCommand} | Открыть файл (Ctrl+O) | — |
| X214 / 162 | Button | {Binding Name} / {Binding FullName} | Command → {Binding DataContext.File.OpenRecentFileCommand, RelativeSource={RelativeSource AncestorType=UserControl}} | {Binding FullName} | — |

### [UI/Controls/ErrorList/ErrorListControl.xaml](../../UI/Controls/ErrorList/ErrorListControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X215 / 60 | WarningButton / ToggleButton | 0 из 0 предупреждений | PreviewMouseDown → WarningButton_PreviewMouseDown | — | — |
| X216 / 67 | ErrorsButton / ToggleButton | 0 из 0 ошибок | PreviewMouseDown → ErrorsButton_PreviewMouseDown | — | — |

### [UI/Controls/Keysight/KeysightPunchControl.xaml](../../UI/Controls/Keysight/KeysightPunchControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X217 / 16 | Button | Отправить | PreviewMouseDown → Button_PreviewMouseDown | — | — |

### [UI/Controls/Runner/RunControl.xaml](../../UI/Controls/Runner/RunControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X218 / 92 | Button | Открыть файл в редакторе | Click → OpenActiveDocumentButton_Click | Открыть файл в редакторе | — |
| X219 / 101 | Button | Показать файл в папке | Click → OpenActiveDocumentFolderButton_Click | Показать файл в папке | — |
| X220 / 111 | ExportPdfButton / Button | Экспортировать протокол в PDF | Click → ExportActiveDocumentPdfButton_Click | Экспортировать протокол в PDF | — |
| X221 / 120 | Button | Печать файла | Click → PrintActiveDocumentButton_Click | Печать файла | — |

### [UI/Controls/Settings/AskMkiConfig/AskMkiConfigControl.xaml](../../UI/Controls/Settings/AskMkiConfig/AskMkiConfigControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X222 / 921 | Button | Редактировать параметры | Click → EditGroupButton_Click | Редактировать параметры | — |

### [UI/Controls/Settings/AskMkiConfig/AskMkiGroupEditorDrawerControl.xaml](../../UI/Controls/Settings/AskMkiConfig/AskMkiGroupEditorDrawerControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X223 / 270 | Button | Отменить | Click → CancelButton_Click | — | — |
| X224 / 278 | Button | Сохранить | Click → SaveButton_Click | — | — |

### [UI/Controls/Settings/DeviceConfig/Base/BaseSettingsConfig/DeviceSettingsControl.xaml](../../UI/Controls/Settings/DeviceConfig/Base/BaseSettingsConfig/DeviceSettingsControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X225 / 445 | CancelButton / Button | Отменить | PreviewMouseDown → CancelButton_PreviewMouseDown | — | — |
| X226 / 454 | SaveButton / Button | Сохранить | PreviewMouseDown → SaveButton_PreviewMouseDown | — | — |

### [UI/Controls/Settings/DeviceConfig/ChassisManager/ChassisManagerControl.xaml](../../UI/Controls/Settings/DeviceConfig/ChassisManager/ChassisManagerControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X227 / 165 | Button | {Binding Name} / {Binding Number, StringFormat='Номер: {0}'} | Click → OnSystemSelected | — | — |
| X228 / 171 | Button | Редактировать тестер АСКМ | Click → EditSystemButton_Click | Редактировать тестер АСКМ | {Binding RelativeSource={RelativeSource AncestorType=UserControl}, Path=IsEditingEnabled, Converter={StaticResource BoolToVisibilityConverter}} |
| X229 / 180 | Button | Удалить тестер АСКМ | Click → RemoveSystemButton_Click | Удалить тестер АСКМ | {Binding RelativeSource={RelativeSource AncestorType=UserControl}, Path=IsEditingEnabled, Converter={StaticResource BoolToVisibilityConverter}} |
| X230 / 247 | Button | {Binding Name} / {Binding Number, StringFormat='Номер: {0}'} | Click → OnRackSelected | — | — |

### [UI/Controls/Settings/DeviceConfig/Controls/DeviceListControl.xaml](../../UI/Controls/Settings/DeviceConfig/Controls/DeviceListControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X231 / 140 | AddButtonContainer / Button | Добавить устройство | PreviewMouseDown → PlusPreviewMouseDown | Добавить устройство | {Binding IsEditingEnabled, RelativeSource={RelativeSource AncestorType=local:DeviceListControl}, Converter={StaticResource BoolToVisibilityConverter}} |
| X232 / 169 | Button | Редактировать устройство | Click → EditDeviceButton_Click | Редактировать устройство | {Binding IsEditingEnabled, RelativeSource={RelativeSource AncestorType=local:DeviceListControl}, Converter={StaticResource BoolToVisibilityConverter}} |
| X233 / 182 | Button | Удалить устройство | Click → RemoveDeviceButton_Click | Удалить устройство | {Binding IsEditingEnabled, RelativeSource={RelativeSource AncestorType=local:DeviceListControl}, Converter={StaticResource BoolToVisibilityConverter}} |

### [UI/Controls/Settings/SettingsProgrammControl.xaml](../../UI/Controls/Settings/SettingsProgrammControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X234 / 128 | Button | Печать активной конфигурации | PreviewMouseDown → PrintConfig | Печать активной конфигурации | — |
| X235 / 139 | ExportConfigButton / Button | Экспорт конфигурации в JSON | PreviewMouseDown → ExportConfig | Экспорт конфигурации в JSON | — |
| X236 / 151 | ImportConfigButton / Button | Импорт конфигурации из JSON | PreviewMouseDown → ImportConfig | Импорт конфигурации из JSON | — |

### [UI/Controls/StatusBarControl.xaml](../../UI/Controls/StatusBarControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X237 / 148 | EncodingButton / Button | {Binding EncodingName} | Command → {Binding ToggleEncodingCommand} | Кодировка файла. ЛКМ: UTF-8 / DOS. ПКМ: выбрать из списка. | — |

### [UI/Controls/TranslatorEditor.xaml](../../UI/Controls/TranslatorEditor.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X238 / 67 | SaveButton / Button | Сохранить странслированный файл в архив | Click → SaveButton_Click | Сохранить странслированный файл в архив | — |
| X239 / 78 | SaveToDiskButton / Button | Иконка: SaveIcon | Click → SaveToDiskButton_Click | — | Collapsed |

### [UI/Controls/TranslatorTextEditor.xaml](../../UI/Controls/TranslatorTextEditor.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X240 / 32 | SaveButton / Button | Сохранить файл | Click → SaveButton_Click | Сохранить файл | — |
| X241 / 42 | OpenFolderButton / Button | Показать файл в папке | Click → OpenFolderButton_Click | Показать файл в папке | — |
| X242 / 53 | PrintButton / Button | Печать файла | Click → PrintButton_Click | Печать файла | — |

### [UI/Icon/ArrowButton.xaml](../../UI/Icon/ArrowButton.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X243 / 21 | PART_Button / Button | Скрыть содержимое раздела (стиль; текст зависит от состояния) (на родителе) | Click → Button_Click; Command → {Binding RelativeSource={RelativeSource AncestorType=local:ArrowButton}, Path=Command} | Скрыть содержимое раздела (стиль; текст зависит от состояния) (на родителе) | — |

### [UI/Windows/WpfDocking/Themes/Generic/WindowSwitcher.xaml](../../UI/Windows/WpfDocking/Themes/Generic/WindowSwitcher.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X244 / 116 | Button | {Binding TabText} | Command → docking:DockCommands.Show | — | — |

## Собственные компоненты

### [Ask.UI/Controls/ProtocolNew/ProtocolUI.xaml](../../Ask.UI/Controls/ProtocolNew/ProtocolUI.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X245 / 405 | ArrowButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | Click → ArrowButton_Click | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |

### [Ask.UI/Features/RoleManagement/Views/RolePasswordManagementControl.xaml](../../Ask.UI/Features/RoleManagement/Views/RolePasswordManagementControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X246 / 92 | TogglePasswordSettingsButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | В code-behind / шаблоне; см. примечания | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |

### [Ask.UI/Features/TextSearch/Views/SearchPanelWindow.xaml](../../Ask.UI/Features/TextSearch/Views/SearchPanelWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X247 / 429 | CollapseResultsButton / ArrowButton | Свернуть результаты | Click → CollapseResults_Click | Свернуть результаты | — |

### [MainWindow/MainWindow.xaml](../../MainWindow/MainWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X248 / 575 | PowerButton / PowerButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [UI/Components/Invoke/OpenFileButton.xaml](../../UI/Components/Invoke/OpenFileButton.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X249 / 118 | CloseButton / CloseButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [UI/Controls/Settings/Delays/DelaySettingsControl.xaml](../../UI/Controls/Settings/Delays/DelaySettingsControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X250 / 76 | ToggleSettingsButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | В code-behind / шаблоне; см. примечания | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |

### [UI/Controls/Settings/DeviceInfo/DeviceDisplaySettingsControl.xaml](../../UI/Controls/Settings/DeviceInfo/DeviceDisplaySettingsControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X251 / 29 | MachineAddressesCard / SettingsCard | Машинные адреса | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X252 / 37 | ConnectionInfo / SettingsCard | Подключение точек и шин | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X253 / 44 | DeviceExecutionParameters / SettingsCard | Параметры устройств | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X254 / 51 | MeasurementResults / SettingsCard | Результаты измерений | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X255 / 58 | IntermediateMeasurementResults / SettingsCard | Промежуточные результаты измерений | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X256 / 65 | DelayMessages / SettingsCard | Сообщения о задержках | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |

### [UI/Controls/Settings/Execution/ExecutionControl.xaml](../../UI/Controls/Settings/Execution/ExecutionControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X257 / 83 | ToggleSettingsButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | В code-behind / шаблоне; см. примечания | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |
| X258 / 109 | StopInError / SettingsCard | Остановка по ошибке | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X259 / 118 | RepeatMeasurement / SettingsCard | Повтор измерения | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X260 / 126 | IdleMode / SettingsCard | Холостой режим выполнения | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X261 / 135 | CompatibilityModeCheckBox / SettingsCard | Выполнение в режиме совместимости со старой системой | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X262 / 148 | DisablePowerCheck / SettingsCard | Отключить проверку питания | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |

### [UI/Controls/Settings/Protocol/ProtocolControl.xaml](../../UI/Controls/Settings/Protocol/ProtocolControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X263 / 68 | ToggleSettingsButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | В code-behind / шаблоне; см. примечания | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |
| X264 / 102 | CommandHeadersCheckBox / SettingsCard | Заголовки команд | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X265 / 110 | OperationTime / SettingsCard | Вывод времени операции | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X266 / 118 | Header / SettingsCard | Отображение заголовков действий | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X267 / 126 | TestStepChecker / SettingsCard | Сообщения шагов проверки | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X268 / 134 | AutoPrint / SettingsCard | Печать протокола | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X269 / 142 | ProtocolFromPO / SettingsCard | Формирование протокола программы контроля | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X270 / 213 | ToggleSettingsTextProtocolButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | В code-behind / шаблоне; см. примечания | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |
| X271 / 255 | ToggleSettingsTextProtocolErrorsButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | В code-behind / шаблоне; см. примечания | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |

### [UI/Controls/Settings/SettingsProgrammControl.xaml](../../UI/Controls/Settings/SettingsProgrammControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X272 / 171 | ToggleSettingsButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | В code-behind / шаблоне; см. примечания | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |

### [UI/Controls/Settings/UserInterface/UiSettingsControl.xaml](../../UI/Controls/Settings/UserInterface/UiSettingsControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X273 / 72 | ToggleSettingsButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | В code-behind / шаблоне; см. примечания | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |
| X274 / 123 | CommandAutoCollapsing / SettingsCard | Автоскрытие блоков в протоколе | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X275 / 131 | TopMenuIcons / SettingsCard | Иконки в верхнем меню | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X276 / 220 | ToggleHighlightsSettingsButton / ArrowButton | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | В code-behind / шаблоне; см. примечания | Скрыть содержимое раздела (стиль компонента; при скрытом разделе — Показать содержимое раздела) | — |
| X277 / 250 | SyntaxHighlighting / SettingsCard | Подсветка синтаксиса | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X278 / 267 | CommandBodyBackgroundHighlighting / SettingsCard | Фоновое выделение команд | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |
| X279 / 275 | ChainPointBodyBackgroundHighlighting / SettingsCard | Фоновое выделение цепей и точек | SettingsCard.CardBorder_MouseLeftButtonUp → IsChecked; В code-behind / шаблоне; см. примечания | — | — |

### [UI/Controls/TranslatorEditor.xaml](../../UI/Controls/TranslatorEditor.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X280 / 37 | BackButton / ArrowButton | Вернуться к исходному файлу | Click → BackButton_Click | Вернуться к исходному файлу | — |

## Кликабельные элементы

### [Ask.UI/Controls/ErrorList/IssueTableControl.xaml](../../Ask.UI/Controls/ErrorList/IssueTableControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X281 / 111 | Border | {Binding ., Converter={StaticResource DiagnosticCodeToTagConverter}} / {Binding ., Converter={StaticResource DiagnosticsCodeLinePairConverter}} / {Binding Command} / {Binding MeasureResult} / {Binding Description} / {Binding ., Converter={StaticResource IssueDebugInfoConverter}} | MouseLeftButtonDown → IssueRow_MouseLeftButtonDown | — | — |

### [Ask.UI/Features/BuildDiagnostics/Views/BuildHistoryWindow.xaml](../../Ask.UI/Features/BuildDiagnostics/Views/BuildHistoryWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X282 / 37 | Border | &lt;/&gt; / Сведения о сборке / Диагностическая информация текущего экземпляра приложения | PreviewMouseLeftButtonDown → TitleBar_MouseLeftButtonDown | — | — |

### [Ask.UI/Features/Notifications/Views/WindowNotificationManagerControl.xaml](../../Ask.UI/Features/Notifications/Views/WindowNotificationManagerControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X283 / 54 | NotificationCard / Border | {Binding Title} / {Binding Message} | MouseLeftButtonDown → OnNotificationCardMouseLeftButtonDown | — | — |

### [ConsoleUI/ConsoleUI/ConsoleOverlay.xaml](../../ConsoleUI/ConsoleUI/ConsoleOverlay.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X284 / 255 | RootBorder / Border | Console / LIVE | MouseLeftButtonDown → Border_MouseLeftButtonDown | — | — |

### [MainWindow/MainWindow.xaml](../../MainWindow/MainWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X285 / 100 | TopPanel / Grid | Дела на сегодня / На сегодня дел нет / {Binding Text} /  / Пользователь | PreviewMouseLeftButtonDown → TopPanel_PreviewMouseLeftButtonDown | — | — |

### [MainWindow/RoleLoginWindow.xaml](../../MainWindow/RoleLoginWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X286 / 417 | Border | Без текстовой подписи | MouseLeftButtonDown → TitleBar_MouseLeftButtonDown | — | — |

### [MainWindow/Windows/ApkToApkwConversionWindow.xaml](../../MainWindow/Windows/ApkToApkwConversionWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X287 / 37 | Border | {Binding RelativeSource={RelativeSource AncestorType=Window}, Path=Title} | PreviewMouseLeftButtonDown → HeaderBorder_MouseLeftButtonDown | — | — |

### [MainWindow/Windows/ApkwToApkConversionWindow.xaml](../../MainWindow/Windows/ApkwToApkConversionWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X288 / 37 | Border | {Binding RelativeSource={RelativeSource AncestorType=Window}, Path=Title} | PreviewMouseLeftButtonDown → HeaderBorder_MouseLeftButtonDown | — | — |

### [MainWindow/Windows/OpkToPkConversionWindow.xaml](../../MainWindow/Windows/OpkToPkConversionWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X289 / 44 | Border | {Binding RelativeSource={RelativeSource AncestorType=Window}, Path=Title} | PreviewMouseLeftButtonDown → HeaderBorder_MouseLeftButtonDown | — | — |

### [MainWindow/Windows/RunAskMkiWindow.xaml](../../MainWindow/Windows/RunAskMkiWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X290 / 146 | Border | Выполнение АСК-МКИ OLD | MouseLeftButtonDown → TitleBar_MouseLeftButtonDown | — | — |

### [UI/Components/SearchControls/ReplaseAllWordsButton.xaml](../../UI/Components/SearchControls/ReplaseAllWordsButton.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X291 / 9 | replaceAllWords / Border | ab / AC | PreviewMouseDown → replaceAllWords_PreviewMouseDown | — | — |

### [UI/Components/SearchControls/ReplaseWordButton.xaml](../../UI/Components/SearchControls/ReplaseWordButton.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X292 / 9 | replaceWord / Border | b / C | PreviewMouseDown → replaceWord_PreviewMouseDown | — | — |

### [UI/Components/SearchControls/SearchArrows.xaml](../../UI/Components/SearchControls/SearchArrows.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X293 / 59 | PART_ContentPresenter / ContentPresenter | {TemplateBinding SelectedItem} | PreviewMouseDown → PART_ContentPresenter_PreviewMouseDown | — | — |

### [UI/Controls/DailyReportDateWindow.xaml](../../UI/Controls/DailyReportDateWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X294 / 21 | Border | Отчёт за день / Диагностический архив приложения | PreviewMouseLeftButtonDown → TitleBar_MouseLeftButtonDown | — | — |
| X295 / 47 | DateField / Border | Иконка: CalendarIcon | MouseLeftButtonUp → CalendarButton_Click | — | — |

### [UI/Controls/EmptyWorkspace/EmptyWorkspaceView.xaml](../../UI/Controls/EmptyWorkspace/EmptyWorkspaceView.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X296 / 201 | TextBlock | {Binding AppVersion, RelativeSource={RelativeSource AncestorType=UserControl}} | MouseLeftButtonDown → VersionText_MouseLeftButtonDown | — | — |

### [UI/Controls/Settings/AskMkiConfig/AskMkiConfigControl.xaml](../../UI/Controls/Settings/AskMkiConfig/AskMkiConfigControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X297 / 1048 | SuccessAskMkiConfig / SuccessIconControlV2 | Без текстовой подписи | PreviewMouseDown → SaveConfigIcon_PreviewMouseDown | {StaticResource AskMki.Text.SaveTooltip} | Collapsed |
| X298 / 1062 | ErrorAskMkiConfig / ErrorIconControlV2 | Без текстовой подписи | PreviewMouseDown → CancelConfigIcon_PreviewMouseDown | {StaticResource AskMki.Text.CancelTooltip} | Collapsed |

### [UI/Controls/Settings/Delays/DelaySettingsControl.xaml](../../UI/Controls/Settings/Delays/DelaySettingsControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X299 / 86 | SaveIcon / SuccessIconControlV2 | Сохранить | PreviewMouseDown → SaveIcon_PreviewMouseDown | Сохранить | Collapsed |
| X300 / 97 | CancelIcon / ErrorIconControlV2 | Отменить изменения | PreviewMouseDown → CancelIcon_PreviewMouseDown | Отменить изменения | Collapsed |

### [UI/Controls/Settings/DeviceConfig/ChassisManager/ChassisManagerControl.xaml](../../UI/Controls/Settings/DeviceConfig/ChassisManager/ChassisManagerControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X301 / 126 | addChassisButton / Border | Добавить тестер АСКМ | PreviewMouseDown → addChassisButton_PreviewMouseDown | — | — |
| X302 / 213 | addRackButton / Border | Добавить стойку СКМ | PreviewMouseDown → addRackButton_PreviewMouseDown | — | — |

### [UI/Controls/Settings/Protocol/ProtocolControl.xaml](../../UI/Controls/Settings/Protocol/ProtocolControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X454 / 43 | Success / SuccessIconControlV2 | Сохранить настройки протокола | PreviewMouseDown → Success_PreviewMouseDown → SaveData (ProtocolControl.xaml.cs) | Сохранить изменения настроек протокола | Collapsed |
| X455 / 55 | Error / ErrorIconControlV2 | Отменить изменения настроек протокола | PreviewMouseDown → Error_PreviewMouseDown (ProtocolControl.xaml.cs) | Отменить изменения и восстановить сохранённые настройки протокола | Collapsed |
| X303 / 224 | RestartClearProtocol / RepeatIcon | Восстановить сохранённый текст протокола | PreviewMouseDown → RepeatIcon_PreviewMouseDown | Восстановить сохранённый текст протокола | — |
| X304 / 266 | RestartClearProtocolErrors / RepeatIcon | Восстановить сохранённый текст протокола ошибок | PreviewMouseDown → RepeatErrorIcon_PreviewMouseDown | Восстановить сохранённый текст протокола ошибок | — |

### [UI/Icon/PreviousArrow.xaml](../../UI/Icon/PreviousArrow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X305 / 1 | root / UserControl | Без текстовой подписи | PreviewMouseDown → root_PreviewMouseDown | — | — |

### [UI/Controls/Settings/Execution/ExecutionControl.xaml](../../UI/Controls/Settings/Execution/ExecutionControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X456 / 61 | Success / SuccessIconControlV2 | Сохранить настройки выполнения | PreviewMouseDown → Success_PreviewMouseDown → SaveData (ExecutionControl.xaml.cs) | Сохранить изменения настроек выполнения | — |
| X457 / 72 | Error / ErrorIconControlV2 | Отменить изменения настроек выполнения | PreviewMouseDown → Error_PreviewMouseDown (ExecutionControl.xaml.cs) | Отменить изменения и восстановить сохранённые настройки выполнения | — |

### [UI/Controls/Settings/UserInterface/UiSettingsControl.xaml](../../UI/Controls/Settings/UserInterface/UiSettingsControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X458 / 49 | Success / SuccessIconControlV2 | Сохранить настройки интерфейса | PreviewMouseDown → Success_PreviewMouseDown → SaveData (UiSettingsControl.xaml.cs) | Сохранить изменения настроек интерфейса | — |
| X459 / 60 | Error / ErrorIconControlV2 | Отменить изменения настроек интерфейса | PreviewMouseDown → Error_PreviewMouseDown (UiSettingsControl.xaml.cs) | Отменить изменения и восстановить сохранённые настройки интерфейса | — |

## Шаблоны

### [Ask.UI/Features/Archive/Views/ArchiveControl.xaml](../../Ask.UI/Features/Archive/Views/ArchiveControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X306 / 382 | Expander / ToggleButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |
| X307 / 574 | RepeatButton | Без текстовой подписи | Command → ScrollBar.PageLeftCommand | — | — |
| X308 / 583 | RepeatButton | Без текстовой подписи | Command → ScrollBar.PageRightCommand | — | — |

### [Ask.UI/Features/FileComparison/Views/FileCompareSelectionControl.xaml](../../Ask.UI/Features/FileComparison/Views/FileCompareSelectionControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X309 / 155 | DecreasePage / RepeatButton | Без текстовой подписи | Command → ScrollBar.PageUpCommand | — | — |
| X310 / 162 | IncreasePage / RepeatButton | Без текстовой подписи | Command → ScrollBar.PageDownCommand | — | — |

### [Ask.UI/Features/TextSearch/Views/SearchPanelWindow.xaml](../../Ask.UI/Features/TextSearch/Views/SearchPanelWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X311 / 377 | ToggleButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [Ask.UI/Resources/Styles/ComboBoxStyle.xaml](../../Ask.UI/Resources/Styles/ComboBoxStyle.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X312 / 87 | ToggleButton / ToggleButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [Ask.UI/Resources/Styles/DataGridStyle.xaml](../../Ask.UI/Resources/Styles/DataGridStyle.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X313 / 360 | ToggleButton / ToggleButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |
| X314 / 457 | RepeatButton | Без текстовой подписи | Command → ScrollBar.PageUpCommand | — | — |
| X315 / 463 | RepeatButton | Без текстовой подписи | Command → ScrollBar.PageDownCommand | — | — |

### [MainWindow/RoleLoginWindow.xaml](../../MainWindow/RoleLoginWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X316 / 291 | ToggleButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [UI/Components/SearchControls/SearchArrows.xaml](../../UI/Components/SearchControls/SearchArrows.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X317 / 63 | ToggleButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [UI/Components/SettingsCardSelect.xaml](../../UI/Components/SettingsCardSelect.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X318 / 45 | PART_DropDownButton / ToggleButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [UI/Controls/Calendar/CalendarControl.xaml](../../UI/Controls/Calendar/CalendarControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X319 / 513 | PART_Button / Button | Выбрать дату | В code-behind / шаблоне; см. примечания | Выбрать дату | — |

### [UI/Controls/Settings/AskMkiConfig/AskMkiConfigControl.xaml](../../UI/Controls/Settings/AskMkiConfig/AskMkiConfigControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X320 / 659 | HeaderSite / ToggleButton | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [UI/Resources/StyleApp/DataGridStyle.xaml](../../UI/Resources/StyleApp/DataGridStyle.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X321 / 172 | RepeatButton | Без текстовой подписи | Command → ScrollBar.PageUpCommand | — | — |
| X322 / 178 | RepeatButton | Без текстовой подписи | Command → ScrollBar.PageDownCommand | — | — |

### [UI/Style.xaml](../../UI/Style.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X323 / 413 | RepeatButton | Без текстовой подписи | Command → ScrollBar.LineUpCommand | — | — |
| X324 / 423 | RepeatButton | Без текстовой подписи | Command → ScrollBar.LineDownCommand | — | — |
| X325 / 474 | RepeatButton | Без текстовой подписи | Command → ScrollBar.LineUpCommand | — | — |
| X326 / 484 | RepeatButton | Без текстовой подписи | Command → ScrollBar.LineDownCommand | — | — |

### [UI/Windows/WpfDocking/Themes/Generic/DocumentTab.xaml](../../UI/Windows/WpfDocking/Themes/Generic/DocumentTab.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X327 / 37 | closeButton / Button | Без текстовой подписи | Command → {x:Static docking:DockCommands.PerformClose} | — | {Binding CloseButtonVisibility, RelativeSource={RelativeSource TemplatedParent}} |

### [UI/Windows/WpfDocking/Themes/Generic/DocumentWindow.xaml](../../UI/Windows/WpfDocking/Themes/Generic/DocumentWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X328 / 109 | dropDownButton / InertButton | Active Files | В code-behind / шаблоне; см. примечания | Active Files | — |
| X329 / 118 | performCloseButton / InertButton | Close | Command → docking:DockCommands.PerformClose | Close | — |

### [UI/Windows/WpfDocking/Themes/Generic/NativeFloatingWindow.xaml](../../UI/Windows/WpfDocking/Themes/Generic/NativeFloatingWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X330 / 38 | minimizeButton / Button | Иконка: WindowMinimizeIcon | Command → {x:Static docking:DockCommands.MinimizeWindow} | — | — |
| X331 / 49 | maximizeButton / Button | Иконка: WindowMaximizeIcon, WindowRestoreIcon | Command → {x:Static docking:DockCommands.ToggleWindowState} | — | — |
| X332 / 87 | exitButton / Button | Иконка: WindowCloseIcon | Command → {x:Static docking:DockCommands.PerformClose} | — | — |

### [UI/Windows/WpfDocking/Themes/Generic/ToolWindowCaption.xaml](../../UI/Windows/WpfDocking/Themes/Generic/ToolWindowCaption.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X333 / 49 | dropDownButton / InertButton | Option | В code-behind / шаблоне; см. примечания | Option | — |
| X334 / 53 | autoHideButton / InertButton | Auto Hide | Command → docking:DockCommands.ToggleAutoHide | Auto Hide | — |
| X335 / 56 | performCloseButton / InertButton | Close | Command → docking:DockCommands.PerformClose | Close | — |

### [UI/Windows/WpfDocking/Themes/Generic/ToolWindowTab.xaml](../../UI/Windows/WpfDocking/Themes/Generic/ToolWindowTab.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X336 / 48 | Button | Без текстовой подписи | Command → {x:Static docking:DockCommands.PerformClose} | — | — |

### [UI/Windows/WpfDocking/Themes/Generic/WindowSwitcher.xaml](../../UI/Windows/WpfDocking/Themes/Generic/WindowSwitcher.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X337 / 78 | topButton / RepeatButton | Без текстовой подписи | Command → ScrollBar.LineUpCommand | — | — |
| X338 / 93 | bottomButton / RepeatButton | Без текстовой подписи | Command → ScrollBar.LineDownCommand | — | — |

### [UI/Windows/WpfDocking/Themes/Generic/WpfFloatingWindow.xaml](../../UI/Windows/WpfDocking/Themes/Generic/WpfFloatingWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X339 / 49 | Button | Без текстовой подписи | Command → docking:DockCommands.PerformClose | — | — |

## Флажки и переключатели

### [Ask.UI/Components/InputField/Controls/BusInput.xaml](../../Ask.UI/Components/InputField/Controls/BusInput.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X340 / 61 | BusACheckBox / CheckBox | Без текстовой подписи | Checked → Bus_Checked; Unchecked → Bus_Unchecked | — | — |
| X341 / 77 | BusBCheckBox / CheckBox | Без текстовой подписи | Checked → Bus_Checked; Unchecked → Bus_Unchecked | — | — |
| X342 / 108 | BusAB1CheckBox / CheckBox | Без текстовой подписи | Checked → BusGroup_Checked; Unchecked → BusGroup_Unchecked | — | — |
| X343 / 124 | BusAB2CheckBox / CheckBox | Без текстовой подписи | Checked → BusGroup_Checked; Unchecked → BusGroup_Unchecked | — | — |
| X344 / 141 | BusAB3CheckBox / CheckBox | Без текстовой подписи | Checked → BusGroup_Checked; Unchecked → BusGroup_Unchecked | — | — |
| X345 / 158 | BusAB4CheckBox / CheckBox | Без текстовой подписи | Checked → BusGroup_Checked; Unchecked → BusGroup_Unchecked | — | — |

### [Ask.UI/Controls/ErrorList/ErrorListControl.xaml](../../Ask.UI/Controls/ErrorList/ErrorListControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X346 / 261 | CheckBox | Без текстовой подписи | Click → BreakpointEnabled_Click | — | — |

### [Ask.UI/Controls/ProtocolNew/ProtocolUI.xaml](../../Ask.UI/Controls/ProtocolNew/ProtocolUI.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X347 / 353 | RepeatMeasurementToggle / CheckBox | Повтор измерения | Checked → ExecutionSetting_Changed; Unchecked → ExecutionSetting_Changed | — | — |
| X348 / 362 | StopOnErrorToggle / CheckBox | Остановка при ошибке | Checked → ExecutionSetting_Changed; Unchecked → ExecutionSetting_Changed | — | — |

### [Ask.UI/Features/FileComparison/Views/FileCompareSelectionControl.xaml](../../Ask.UI/Features/FileComparison/Views/FileCompareSelectionControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X349 / 458 | CheckBox | {Binding File.DisplayName} / {Binding File.FilePath} | В code-behind / шаблоне; см. примечания | — | — |

### [Ask.UI/Features/ServiceTools/Gpt/GPTController.xaml](../../Ask.UI/Features/ServiceTools/Gpt/GPTController.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X350 / 98 | AcwModeButton / RadioButton | ACW | Checked → Mode_Checked | — | — |
| X351 / 104 | DcwModeButton / RadioButton | DCW | Checked → Mode_Checked | — | — |
| X352 / 110 | IrModeButton / RadioButton | IR | Checked → Mode_Checked | — | — |
| X353 / 116 | SettingsModeButton / RadioButton | Общие настройки | Checked → Mode_Checked | — | — |

### [Ask.UI/Features/TextSearch/Views/SearchPanelWindow.xaml](../../Ask.UI/Features/TextSearch/Views/SearchPanelWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X354 / 404 | SelectionOnlyOption / CheckBox | Искать в выделении | В code-behind / шаблоне; см. примечания | Выделите текст перед открытием поиска | — |

### [Ask.UI/Shared/Components/SettingsCard.xaml](../../Ask.UI/Shared/Components/SettingsCard.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X355 / 23 | CheckBox | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [MainWindow/MainWindow.xaml](../../MainWindow/MainWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X356 / 299 | TodayCompletionBox / CheckBox | Отметить выполнение дела | Click → TodayTaskCompleted_Click | Отметить выполнение дела | — |

### [UI/Controls/AdminPanel/ServiceUtilitiesControl.xaml](../../UI/Controls/AdminPanel/ServiceUtilitiesControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X357 / 86 | SetCommandTab / RadioButton | SetCommand | Checked → SetCommandTab_Checked | — | — |
| X358 / 91 | RadioButton | GPT | Checked → GptTab_Checked | — | — |
| X359 / 95 | RadioButton | Шасси | Checked → ChassisTab_Checked | — | — |
| X360 / 99 | RadioButton | УКШ | Checked → SwitchingDeviceTab_Checked | — | — |
| X361 / 103 | RadioButton | МКР | Checked → RelaySwitchModuleTab_Checked | — | — |
| X362 / 107 | RadioButton | Мультиметры | Checked → MultimeterTab_Checked | — | — |

### [UI/Controls/Calendar/CalendarControl.xaml](../../UI/Controls/Calendar/CalendarControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X363 / 466 | TaskCompletionBox / CheckBox | Отметить выполнение дела | Click → Completion_Click | Отметить выполнение дела | — |

### [UI/Controls/ErrorList/ErrorListControl.xaml](../../UI/Controls/ErrorList/ErrorListControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X364 / 283 | CheckBox | Без текстовой подписи | Click → BreakpointEnabled_Click | — | — |

### [UI/Controls/Settings/AskMkiConfig/AskMkiConfigControl.xaml](../../UI/Controls/Settings/AskMkiConfig/AskMkiConfigControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X365 / 758 | InlineToggleCheckBox / CheckBox | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |
| X366 / 850 | CheckBox | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |
| X367 / 1155 | InlineToggleCheckBox / CheckBox | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |
| X368 / 1306 | CheckBox | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

### [UI/Controls/Settings/AskMkiConfig/AskMkiGroupEditorDrawerControl.xaml](../../UI/Controls/Settings/AskMkiConfig/AskMkiGroupEditorDrawerControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X369 / 84 | ToggleEditorHost / CheckBox | {Binding Label} | В code-behind / шаблоне; см. примечания | — | Collapsed |
| X370 / 144 | PresentCheckBox / CheckBox | Без текстовой подписи | В code-behind / шаблоне; см. примечания | — | — |

## Меню

### [Ask.UI/Features/Archive/Views/ArchiveControl.xaml](../../Ask.UI/Features/Archive/Views/ArchiveControl.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X371 / 138 | CreateArchiveMenuItem / MenuItem | Создать архив | Command → {Binding CreateArchiveCommand} | — | {Binding ContextMenu.CreateArchiveVisibility} |
| X372 / 141 | UploadArchiveMenuItem / MenuItem | Импортировать архив | Command → {Binding UploadArchiveCommand} | — | {Binding ContextMenu.UploadArchiveVisibility} |
| X373 / 144 | DownloadArchivesMenuItem / MenuItem | Экспортировать архивы | Command → {Binding DownloadArchivesCommand} | — | {Binding ContextMenu.DownloadArchivesVisibility} |
| X374 / 147 | OpenArchiveMenuItem / MenuItem | Открыть | Command → {Binding OpenContextArchiveCommand} | — | {Binding ContextMenu.OpenArchiveVisibility} |
| X375 / 150 | SaveArchiveMenuItem / MenuItem | Сохранить архив на диск | Command → {Binding SaveContextArchiveCommand} | — | {Binding ContextMenu.SaveArchiveVisibility} |
| X376 / 153 | PrintArchiveCatalogMenuItem / MenuItem | Распечатать каталог архива | Command → {Binding PrintContextArchiveCatalogCommand} | — | {Binding ContextMenu.PrintArchiveCatalogVisibility} |
| X377 / 156 | DeleteArchiveMenuItem / MenuItem | Удалить | Command → {Binding DeleteContextArchiveCommand} | — | {Binding ContextMenu.DeleteArchiveVisibility} |
| X378 / 159 | AddFileToArchiveMenuItem / MenuItem | Добавить файл | Command → {Binding AddFileToContextArchiveCommand} | — | {Binding ContextMenu.AddFileToArchiveVisibility} |
| X379 / 162 | OpenArchiveFileMenuItem / MenuItem | Открыть | Command → {Binding OpenContextFileCommand} | — | {Binding ContextMenu.OpenArchiveFileVisibility} |
| X380 / 165 | OpenInTextEditorMenuItem / MenuItem | Открыть в редакторе | Command → {Binding OpenContextFileInEditorCommand} | — | {Binding ContextMenu.OpenInTextEditorVisibility} |
| X381 / 168 | CopyArchiveFileMenuItem / MenuItem | Копировать | Command → {Binding CopyContextFileCommand} | — | {Binding ContextMenu.CopyArchiveFileVisibility} |
| X382 / 171 | CutArchiveFileMenuItem / MenuItem | Вырезать | Command → {Binding CutContextFileCommand} | — | {Binding ContextMenu.CutArchiveFileVisibility} |
| X383 / 174 | PasteArchiveFileMenuItem / MenuItem | Вставить | Command → {Binding PasteContextFileCommand} | — | {Binding ContextMenu.PasteArchiveFileVisibility} |
| X384 / 177 | DeleteArchiveFileMenuItem / MenuItem | Удалить | Command → {Binding DeleteContextFileCommand} | — | {Binding ContextMenu.DeleteArchiveFileVisibility} |

### [MainWindow/MainWindow.xaml](../../MainWindow/MainWindow.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X385 / 120 | File / MenuItem | Файл | В code-behind / шаблоне; см. примечания | — | — |
| X386 / 123 | saveMenuItem / MenuItem | Сохранить | Command → {Binding File.SaveFileCommand} | — | Collapsed |
| X387 / 124 | saveAsMenuItem / MenuItem | Сохранить как | Command → {Binding File.SaveFileAsCommand} | — | Collapsed |
| X388 / 125 | openFolderMenuItem / MenuItem | Открыть папку в проводнике | Command → {Binding File.OpenFolderCommand} | — | Collapsed |
| X389 / 126 | printMenuItem / MenuItem | Печать | Command → {Binding File.PrintFileCommand} | — | Collapsed |
| X390 / 127 | MenuItem | Поиск и замена | Command → {Binding File.SearchFileCommand} | — | — |
| X391 / 128 | compareMenuItem / MenuItem | Сравнить | Command → {Binding File.CompareFileCommand} | — | Collapsed |
| X392 / 131 | MenuItem | Открыть | Command → {Binding File.OpenFileCommand} | — | — |
| X393 / 132 | MenuItem | Создать | Command → {Binding File.CreateNewFileCommand} | — | — |
| X394 / 134 | MenuItem | Конвертация | В code-behind / шаблоне; см. примечания | — | — |
| X395 / 135 | MenuItem | OPK в PK | Command → {Binding File.ConvertOpkToPkCommand} | — | — |
| X396 / 137 | MenuItem | APK в APKW | Command → {Binding File.ConvertApkToApkwCommand} | — | — |
| X397 / 138 | MenuItem | APKW в APK | Command → {Binding File.ConvertApkwToApkCommand} | — | — |
| X398 / 142 | MenuItem | Выход | Command → {Binding File.ExitApplicationCommand} | — | — |
| X399 / 146 | ArchiveMenu / MenuItem | Архив | В code-behind / шаблоне; см. примечания | — | — |
| X400 / 149 | openArchiveMenuItem / MenuItem | Открыть архив | Command → {Binding File.OpenArchiveCommand} | — | — |
| X401 / 150 | createArchiveMenuItem / MenuItem | Создать архив | Command → {Binding File.CreateArchiveCommand} | — | Visible |
| X402 / 151 | downloadArchivesMenuItem / MenuItem | Экспортировать архивы | Command → {Binding File.DownloadArchivesCommand} | — | Visible |
| X403 / 152 | uploadArchiveMenuItem / MenuItem | Импортировать архив | Command → {Binding File.UploadArchiveCommand} | — | Visible |
| X404 / 155 | TestMenu / MenuItem | Испытания | В code-behind / шаблоне; см. примечания | — | — |
| X405 / 157 | Metrology / MenuItem | Метрология | В code-behind / шаблоне; см. примечания | — | — |
| X406 / 159 | ModeKSMenuItem / MenuItem | Режим КС | Command → {Binding Metrology.KCCommand} | — | — |
| X407 / 160 | ModeIEMenuItem / MenuItem | Режим ИЕ | Command → {Binding Metrology.IECommand} | — | — |
| X408 / 161 | ModeCIMenuItem / MenuItem | Режим СИ | Command → {Binding Metrology.CICommand} | — | — |
| X409 / 162 | ModePrMenuItem / MenuItem | Режим ПР | Command → {Binding Metrology.PRCommand} | — | — |
| X410 / 163 | ModeEhtMenuItem / MenuItem | Режим ЭТ | Command → {Binding Metrology.EHTCommand} | — | — |
| X411 / 165 | ModePIDCWMenuItem / MenuItem | Режим ПИ (постоянный) | Command → {Binding Metrology.PIDCWCommand} | — | — |
| X412 / 166 | ModePIACWMenuItem / MenuItem | Режим ПИ (переменный) | Command → {Binding Metrology.PIACWCommand} | — | — |
| X413 / 167 | ModeKNDCWMenuItem / MenuItem | Режим КН (постоянный) | Command → {Binding Metrology.KNDCWCommand} | — | — |
| X414 / 168 | ModeKNACWMenuItem / MenuItem | Режим КН (переменный) | Command → {Binding Metrology.KNACWCommand} | — | — |
| X415 / 172 | SelfControl / MenuItem | Самоконтроль | В code-behind / шаблоне; см. примечания | — | — |
| X416 / 174 | SelfControlMenuItem / MenuItem | Модуль | Command → {Binding SelfTest.SelfTestModuleCommand} | — | — |
| X417 / 175 | SelfControlAllMenuItem / MenuItem | Система | Command → {Binding SelfTest.SelfTestSystemCommand} | — | — |
| X418 / 179 | TestControl / MenuItem | Тесты | В code-behind / шаблоне; см. примечания | — | — |
| X419 / 181 | MenuItem | Тест СИ | В code-behind / шаблоне; см. примечания | — | — |
| X420 / 183 | NodeMethodMenuItemCI / MenuItem | Метод узла | Command → {Binding Test.CiNodeMethodCommand} | — | — |
| X421 / 184 | MethodExecutorMenuItemCI / MenuItem | Групповой метод | Command → {Binding Test.CiMethodExecutorCommand} | — | — |
| X422 / 188 | MenuItem | Тест ПИ на постоянном токе | В code-behind / шаблоне; см. примечания | — | — |
| X423 / 190 | NodeMethodMenuItemPiDCW / MenuItem | Метод узла | Command → {Binding Test.PiDCWNodeMethodCommand} | — | — |
| X424 / 191 | MenuItem | Групповой метод | Command → {Binding Test.PiDCWMethodExecutorCommand} | — | — |
| X425 / 195 | MenuItem | Тест ПИ на переменном токе | В code-behind / шаблоне; см. примечания | — | — |
| X426 / 197 | NodeMethodMenuItemPiACW / MenuItem | Метод узла | Command → {Binding Test.PiACWNodeMethodCommand} | — | — |
| X427 / 198 | MethodExecutorMenuItemPiACW / MenuItem | Групповой метод | Command → {Binding Test.PiACWMethodExecutorCommand} | — | — |
| X428 / 202 | CrossTestMkrMenuItem / MenuItem | Перекрёстный тест (МКР) | Command → {Binding Test.CrossTestMkrExecutorCommand} | — | — |
| X429 / 204 | RelayContactResistMenuItem / MenuItem | Сопротивление коммутатора | Command → {Binding Test.RelayContactResistExecutorCommand} | — | — |
| X430 / 210 | Translation / MenuItem | Выполнение | В code-behind / шаблоне; см. примечания | — | — |
| X431 / 212 | Build / MenuItem | Трансляции | Command → {Binding Translation.BuildCommand} | — | — |
| X432 / 213 | Run / MenuItem | Запуск | Command → {Binding Translation.RunCommand} | — | — |
| X433 / 214 | MenuItem | Без текстовой подписи | Command → {Binding Translation.RunStepByStepModeCommand} | — | Collapsed |
| X434 / 215 | MenuItem | Без текстовой подписи | Command → {Binding Translation.RunStepIntoModeCommand} | — | Collapsed |
| X435 / 216 | runAskMkiMenuItem / MenuItem | Выполнение АСК-МКИ(OLD) | Command → {Binding File.RunAskMkiActiveFileCommand} | — | — |
| X436 / 219 | Settings / MenuItem | Настройки | Command → {Binding Settings.SettingsCommand} | — | — |
| X437 / 221 | HelpText / MenuItem | Справка | В code-behind / шаблоне; см. примечания | — | — |
| X438 / 222 | HelpReleaseNotes / MenuItem | Что нового | Command → {Binding Settings.HelpOpenReleaseNotesCommand} | — | — |
| X439 / 223 | HelpTextGeneralInformation / MenuItem | Общие сведения | Command → {Binding Settings.HelpOpenGeneralInformationCommand} | — | — |
| X440 / 224 | HelpTextMainMenu / MenuItem | Основное меню | Command → {Binding Settings.HelpOpenMainMenuCommand} | — | — |
| X441 / 225 | HelpTextLanguageControlPrograms / MenuItem | Язык АСК | Command → {Binding Settings.HelpOpenLanguageControlProgramsCommand} | — | — |
| X442 / 226 | HelpTextTranslator / MenuItem | Транслятор | Command → {Binding Settings.HelpOpenTranslatorCommand} | — | — |
| X443 / 227 | HelpTextTextEditor / MenuItem | Текстовый редактор | Command → {Binding Settings.HelpOpenTextEditorCommand} | — | — |
| X444 / 228 | HelpTextHotKeys / MenuItem | Горячие клавиши | Command → {Binding Settings.HelpOpenHotKeysCommand} | — | — |
| X445 / 229 | HelpTextAboutProgram / MenuItem | О программе | Command → {Binding Settings.HelpOpenAboutProgramCommand} | — | — |
| X446 / 231 | FastMenuCommand / MenuItem | Список команд | Command → {Binding Settings.HelpOpenFastMenuCommand} | — | — |
| X447 / 234 | Admin / MenuItem | Администратор | В code-behind / шаблоне; см. примечания | — | — |
| X448 / 235 | MenuItem | Сервисные утилиты | Command → {Binding Admin.ServiceUtilitiesCommand} | — | — |
| X449 / 238 | MenuItem | База данных | Command → {Binding Admin.DatabaseCommand} | — | — |
| X450 / 241 | MenuItem | Сбросить просмотр «Что нового» для root | Command → {Binding Admin.ResetReleaseNotesViewCommand} | — | — |

### [UI/Windows/WpfDocking/Themes/Generic/DockItem.TabContextMenu.xaml](../../UI/Windows/WpfDocking/Themes/Generic/DockItem.TabContextMenu.xaml)

| ID / строка | Имя / тип | Кнопка или подпись | Действие / привязка | ToolTip (разметка / стиль) | Visibility |
| --- | --- | --- | --- | --- | --- |
| X451 / 15 | MenuItem | Floating | Command → docking:DockCommands.ToggleFloating | — | — |
| X452 / 19 | MenuItem | Auto Hide | Command → docking:DockCommands.ToggleAutoHide | — | — |
| X453 / 23 | MenuItem | Без текстовой подписи | Command → docking:DockCommands.PerformClose | — | — |

## Программно создаваемые элементы и семейства кнопок

| Элемент / семейство | Создание и обработка | Подсказка |
| --- | --- | --- |
| Номера страниц результатов поиска | [SearchPanelWindow.Results.cs](../../Ask.UI/Features/TextSearch/Views/SearchPanelWindow.Results.cs), строки 94–101: new Button → смена _resultsPage → ShowResultsPage; до четырёх номеров одновременно | «Страница N» |
| Список кодировок | [TextEditorStatusViewModel.cs](../../MainWindow/ViewModels/TextEditorStatusViewModel.cs), строки 136–137: MenuItem для каждой кодировки; обработчик меняет Encoding | Прямо при создании не задана |
| Подтвердить замену фрагмента | [ReplacementReviewMenu.cs](../../Ask.UI/Shared/Search/ReplacementReviewMenu.cs), строки 126–131: CreateCompactItem → review.Resolve(false) → Close | Прямо при создании не задана |
| Отменить замену фрагмента | [ReplacementReviewMenu.cs](../../Ask.UI/Shared/Search/ReplacementReviewMenu.cs), строки 127–132: CreateCompactItem → review.Resolve(true) → Close | При недоступности: фрагмент изменён / только чтение |
| Кнопки-вкладки рабочего пространства | [ControlManager.cs](../../UI/Components/MultiEditorMethods/ControlManager.cs), CreateTabButton/ConfigureTabEvents: new OpenFileButton; нажатие открывает соответствующий control, вложенный CloseButton закрывает его | Проверять экземпляр/код |
| Кнопки-вкладки верхнего многооконного контейнера | [MultiWindowControl.xaml.cs](../../UI/Components/MultiWindowControl.xaml.cs), CreateTabButton и подписки около строк 519–521: выбор/закрытие вкладки | Проверять экземпляр/код |
| Создание вкладки контейнера через сервис | [ControlManagerService.cs](../../UI/Services/ControlManagerService.cs), CreateTabButton: new OpenFileButton, заголовок из EditorType | Проверять экземпляр/код |

## Определения собственных компонентов

Список определений помогает не пропустить компоненты без прямого Button и старые компоненты, для которых текущие runtime-вызовы не доказаны. Наличие определения не означает наличие отдельной видимой кнопки в текущем shell.

| Компонент | Файл | Примечание |
| --- | --- | --- |
| Ask.UI.Shared.Components.Icons.ArrowButton | [Ask.UI/Shared/Components/Icons/ArrowButton.xaml](../../Ask.UI/Shared/Components/Icons/ArrowButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| Ask.UI.Shared.Components.SettingsCard | [Ask.UI/Shared/Components/SettingsCard.xaml](../../Ask.UI/Shared/Components/SettingsCard.xaml) | Нажатие карточки переключает IsChecked; не считать отдельно от её checkbox |
| UI.Components.BackButton | [UI/Components/BackButton.xaml](../../UI/Components/BackButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Components.CloseButton | [UI/Components/CloseButton.xaml](../../UI/Components/CloseButton.xaml) | Вложенное закрытие вкладки; обработчик назначается контейнером |
| UI.Components.Invoke.OpenFileButton | [UI/Components/Invoke/OpenFileButton.xaml](../../UI/Components/Invoke/OpenFileButton.xaml) | Кнопка-вкладка; экземпляры создаются программно |
| UI.Components.MenuButton | [UI/Components/MenuButton.xaml](../../UI/Components/MenuButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Components.PlusButtonControl | [UI/Components/PlusButtonControl.xaml](../../UI/Components/PlusButtonControl.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Components.PowerButton | [UI/Components/PowerButton.xaml](../../UI/Components/PowerButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Components.SearchControls.ArrowButton | [UI/Components/SearchControls/ArrowButton.xaml](../../UI/Components/SearchControls/ArrowButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Components.SearchControls.CaseToggleButton | [UI/Components/SearchControls/CaseToggleButton.xaml](../../UI/Components/SearchControls/CaseToggleButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Components.SearchControls.ExitButton | [UI/Components/SearchControls/ExitButton.xaml](../../UI/Components/SearchControls/ExitButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Components.SearchControls.ReplaseAllWordsButton | [UI/Components/SearchControls/ReplaseAllWordsButton.xaml](../../UI/Components/SearchControls/ReplaseAllWordsButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Components.SearchControls.ReplaseWordButton | [UI/Components/SearchControls/ReplaseWordButton.xaml](../../UI/Components/SearchControls/ReplaseWordButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Components.SearchControls.WholeWordToggleButton | [UI/Components/SearchControls/WholeWordToggleButton.xaml](../../UI/Components/SearchControls/WholeWordToggleButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |
| UI.Icon.ArrowButton | [UI/Icon/ArrowButton.xaml](../../UI/Icon/ArrowButton.xaml) | Определение; конкретные использования ищутся по полному имени типа |

## Встроенная HTML-справка

### [Ask.Support/AppHelp/ru/Content/AboutProgramPart.html](../../Ask.Support/AppHelp/ru/Content/AboutProgramPart.html)

| Строка | Имя | Кнопка | Действие | title в HTML |
| --- | --- | --- | --- | --- |
| 62 | — | Лицензионное соглашение | About.open('license') | — |
| 63 | — | Поддержка | About.open('support') | — |
| 69 | — | Закрыть | About.close() | — |
| 140 | — | Закрыть | About.close() | — |

### [Ask.Support/AppHelp/ru/FastMenuCommand.html](../../Ask.Support/AppHelp/ru/FastMenuCommand.html)

| Строка | Имя | Кнопка | Действие | title в HTML |
| --- | --- | --- | --- | --- |
| 120 | theme-toggle | Включить темную тему | Переключить тему (toggleTheme) | — |
| 131 | back-btn | Вернуться к списку команд | Вернуться к списку команд | Вернуться к списку команд |
| 144 | ОК | Команда ОК — Объект контроля | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/CommandsOrganizingControlProcessChapter/CommandOK_Part.html | — |
| 147 | СП | Команда СП — Схема подключения | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/CommandsOrganizingControlProcessChapter/CommandSP_Part.html | — |
| 150 | РМ | Команда РМ — Рабочее место | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/CommandsOrganizingControlProcessChapter/CommandRM_Part.html | — |
| 153 | ЦУ | Команда ЦУ — Сообщение оператору | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/CommandsOrganizingControlProcessChapter/CommandTsU_Part.html | — |
| 156 | УП | Команда УП — Условный переход | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/CommandsOrganizingControlProcessChapter/CommandUP_Part.html | — |
| 159 | КЦ | Команда КЦ — Конец программы контроля | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/CommandsOrganizingControlProcessChapter/CommandKTs_Part.html | — |
| 162 | ПР | Команда ПР — Проверка релейная | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/MeasurementCommandsChapter/CommandPR_Part.html | — |
| 165 | ЭТ | Команда ЭТ — Электронная 4-х-точка | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/MeasurementCommandsChapter/CommandET_Part.html | — |
| 168 | СИ | Команда СИ — Сопротивление изоляции | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/MeasurementCommandsChapter/CommandSI_Part.html | — |
| 171 | ПИ | Команда ПИ — Прочность изоляции | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/MeasurementCommandsChapter/CommandPI_Part.html | — |
| 174 | КС | Команда КС — Контроль сопротивлений | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/MeasurementCommandsChapter/CommandKC_Part.html | — |
| 177 | НЭ | Команда НЭ — Нелинейные элементы | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/MeasurementCommandsChapter/CommandNE_Part.html | — |
| 180 | ИЕ | Команда ИЕ — Измерение ёмкости | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/MeasurementCommandsChapter/CommandIE_Part.html | — |
| 183 | ВШ | Команда ВШ — Включение шин коммутатора | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/ASCDeviceManagementCommandsChapter/CommandVSh_Part.html | — |
| 186 | ПТ | Команда ПТ — Подключение точек к шинам | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/ASCDeviceManagementCommandsChapter/CommandPT_Part.html | — |
| 189 | ОТ | Команда ОТ — Отключение точек от шин | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/ASCDeviceManagementCommandsChapter/CommandOT_Part.html | — |
| 192 | СК | Команда СК — Сброс коммутатора | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/ASCDeviceManagementCommandsChapter/CommandSK_Part.html | — |
| 195 | ОС | Команда ОС — Общий сброс | Открыть статью: Content/LanguageControlProgramsChapter/ASK_MKIControlProgramLanguageChapter/ASCDeviceManagementCommandsChapter/CommandOS_Part.html | — |

### [Ask.Support/AppHelp/ru/index.html](../../Ask.Support/AppHelp/ru/index.html)

| Строка | Имя | Кнопка | Действие | title в HTML |
| --- | --- | --- | --- | --- |
| 15 | sidebar-toggle | Свернуть панель навигации | Показать/скрыть меню справки (toggleSidebar) | Свернуть меню |
| 28 | theme-toggle | Включить темную тему | Переключить тему (toggleTheme) | — |
| 44 | clear-btn | Очистить поиск | Очистить поиск | — |
| 49 | menu | Разделы | Показать дерево разделов (showTab) | — |
| 50 | bookmarks | Закладки | Показать закладки (showTab) | — |
| 2645 | bookmark-btn | Добавить в закладки | Добавить/удалить текущий раздел из закладок | Добавить в закладки |
| 2648 | prev-btn | Предыдущий | Открыть предыдущий раздел | — |
| 2651 | next-btn | Следующий | Открыть следующий раздел | — |
| 2667 | image-lightbox-close | Закрыть просмотр изображения | Закрыть увеличенное изображение | — |

Обработчики главного окна справки: [scripts.js](../../Ask.Support/AppHelp/scripts.js), строки 892–996. Названия и title темы, сворачивания меню, закладок и предыдущего/следующего раздела меняются динамически. Карточки команд FastMenuCommand открывают data-src соответствующей команды. Элементы дерева разделов, результатов поиска, закладок и ссылки внутри статей являются навигацией; каждый пункт/ссылка не пересчитан как отдельная кнопка.

## Назначение подсказок и действий из кода

- ProtocolUI: [ProtocolUI.ButtonManager.cs](../../Ask.UI/Controls/ProtocolNew/ProtocolUI.ButtonManager.cs), SetupButtons → SetupEventHandlers → SetEventControls; элементы верхней/нижней панели выбираются текущим представлением, затем получают PreviewMouseDown. [ProtocolUI.cs](../../Ask.UI/Controls/ProtocolNew/ProtocolUI.cs) связывает события со StartAsync, паузой, Resume, StopAsync и повтором.
- Кнопки с IsCancel=True закрывают диалог через штатный механизм WPF, даже когда Click не указан.
- Параметры поиска подписываются на Click в [SearchPanelWindow.Search.cs](../../Ask.UI/Features/TextSearch/Views/SearchPanelWindow.Search.cs); подпись стрелки сворачивания результатов меняется в SearchPanelWindow.Results.cs.
- Подсказка TodayTasksButton зависит от наличия дел: [MainWindow.TodayTasks.cs](../../MainWindow/MainWindow.TodayTasks.cs).
- Подсказки видимости пароля и раскладки меняются в [RoleLoginWindow.xaml.cs](../../MainWindow/RoleLoginWindow.xaml.cs).
- Подсказка PowerActionButton зависит от состояния подключения/отмены: [PowerButton.xaml.cs](../../UI/Components/PowerButton.xaml.cs), SetButtonToolTip; проверять реальные тексты при изменении.
- Сохранение на диск переводчика имеет присваивание ToolTip в [TranslatorEditor.xaml.cs](../../UI/Controls/TranslatorEditor.xaml.cs).
- Подсказка максимизации окна справки меняется в [HelpBrowserWindow.xaml.cs](../../Ask.Support/HelpBrowserWindow.xaml.cs).
- Общая кнопка «Что нового» получает текущий Caption из [ReleaseNotesPrompt.xaml.cs](../../Ask.UI/Features/ReleaseNotes/Views/ReleaseNotesPrompt.xaml.cs); в XAML текст не задан.

## Проверка охвата и ограничения

- XML-проход охватывает Button, ToggleButton, RepeatButton, InertButton, DropDownButton, InvokeToggleButton; дополнительно MenuItem, CheckBox, RadioButton, ArrowButton, PowerButton, CloseButton, SettingsCard и элементы с обработчиками нажатия мыши.
- Второй поиск выполнен по C#-конструкторам, фабрикам, подпискам Click/MouseDown/MouseUp и присваиваниям ToolTip. Кнопки, создаваемые платформой внутри стандартных ComboBox/DatePicker/ScrollViewer, представлены только там, где собственный шаблон явно объявляет соответствующую кнопку.
- Служебные заголовки окон и области перетаскивания оставлены в разделе кликабельных элементов, чтобы явно отличать их от кнопок. Не всем из них нужен ToolTip.
- Перед массовым добавлением подсказок требуется точечная проверка обработчика, актуальности компонента и условий видимости. Для динамических binding-подписей окончательный текст зависит от текущих данных.
- Production-код и пользовательская справка не изменены; сборка и тесты для этой инвентаризации не запускались.
