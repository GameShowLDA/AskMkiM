# Кнопки без всплывающих подсказок

Обновлено 30 октября 2026 после добавления выбранных подсказок. Учтены разметка, локальные стили, стили родителя/стрелок разделов и найденные назначения из C#. Строки означают объявления, а не число одновременно видимых кнопок. Наличие старого компонента в исходниках не доказывает его использование в текущем интерфейсе.

## Кнопки и кнопки-переключатели

### [Ask.UI/Components/DeviceSelectorPanel/ChoiceDevice.xaml](../../Ask.UI/Components/DeviceSelectorPanel/ChoiceDevice.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 11 | toggleButton | {Binding SelectedDisplayText} |

### [Ask.UI/Controls/ErrorList/ErrorListControl.xaml](../../Ask.UI/Controls/ErrorList/ErrorListControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 129 | WarningButton | 0 из 0 предупреждений |
| 136 | ErrorsButton | 0 из 0 ошибок |
| 156 | PrevPageButton | &lt; Назад |
| 171 | — | {Binding DisplayText} |
| 180 | NextPageButton | Далее &gt; |

### [Ask.UI/Controls/ProtocolNew/ProtocolInfoWindow.xaml](../../Ask.UI/Controls/ProtocolNew/ProtocolInfoWindow.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 95 | — | Сохранить |
| 102 | — | Отмена |

### [Ask.UI/Features/Archive/Views/ArchiveNameInputControl.xaml](../../Ask.UI/Features/Archive/Views/ArchiveNameInputControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 59 | CreateButton | Создать |
| 64 | CancelButton | Отмена |

### [Ask.UI/Features/Archive/Views/ArchiveSaveTargetPickerControl.xaml](../../Ask.UI/Features/Archive/Views/ArchiveSaveTargetPickerControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 70 | CreateArchiveButton | Создать архив |
| 74 | SaveButton | Сохранить |
| 79 | CancelButton | Отмена |

### [Ask.UI/Features/BuildDiagnostics/Views/BuildHistoryWindow.xaml](../../Ask.UI/Features/BuildDiagnostics/Views/BuildHistoryWindow.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 74 | — | × |
| 170 | — | Копировать сведения |
| 178 | — | Закрыть |

### [Ask.UI/Features/FileComparison/Views/FileCompareSelectionControl.xaml](../../Ask.UI/Features/FileComparison/Views/FileCompareSelectionControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 388 | — | Обновить список |
| 399 | — | Запустить сравнение |

### [Ask.UI/Features/RoleManagement/Views/RolePasswordManagementControl.xaml](../../Ask.UI/Features/RoleManagement/Views/RolePasswordManagementControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 205 | — | Сохранить пароль |

### [Ask.UI/Features/ServiceTools/Chassis/ChassisControl.xaml](../../Ask.UI/Features/ServiceTools/Chassis/ChassisControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 82 | — | Инициализировать |
| 86 | — | Полный сброс |
| 90 | — | Обновить |
| 111 | — | Включить питание |
| 115 | — | Выключить питание |
| 119 | — | Проверить питание |

### [Ask.UI/Features/ServiceTools/Gpt/Modes/AcwMode.xaml](../../Ask.UI/Features/ServiceTools/Gpt/Modes/AcwMode.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 68 | ConnectButton | Включить |
| 188 | — | Сохранить параметры |
| 224 | — | Считать с устройства |
| 245 | — | Запустить тест |

### [Ask.UI/Features/ServiceTools/Gpt/Modes/DcwMode.xaml](../../Ask.UI/Features/ServiceTools/Gpt/Modes/DcwMode.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 49 | ConnectButton | Включить |
| 122 | — | Сохранить |
| 140 | — | Считать с устройства |
| 148 | — | Запустить тест |

### [Ask.UI/Features/ServiceTools/Gpt/Modes/IrMode.xaml](../../Ask.UI/Features/ServiceTools/Gpt/Modes/IrMode.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 48 | ConnectButton | Включить |
| 113 | — | Сохранить |
| 130 | — | Считать с устройства |
| 140 | — | Запустить тест |

### [Ask.UI/Features/ServiceTools/Multimeter/MultimeterControl.xaml](../../Ask.UI/Features/ServiceTools/Multimeter/MultimeterControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 78 | — | Обновить |
| 96 | — | Подключить |
| 97 | — | Отключить |
| 98 | — | Инициализировать |
| 100 | — | Сбросить |
| 118 | — | Установить режим |
| 120 | — | Установить диапазон |
| 155 | — | Измерить |

### [Ask.UI/Features/ServiceTools/RelaySwitchModule/RelaySwitchModuleControl.xaml](../../Ask.UI/Features/ServiceTools/RelaySwitchModule/RelaySwitchModuleControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 92 | — | Обновить |
| 127 | — | Подключить |
| 128 | — | Отключить |
| 129 | — | Подключить с проверкой |
| 131 | — | Отключить с проверкой |
| 133 | — | Перевести на эту шину |
| 159 | — | Запустить самоконтроль |
| 197 | — | Подключить диапазон |
| 199 | — | Отключить диапазон |
| 209 | — | Измеритель: вкл. |
| 210 | — | Измеритель: выкл. |
| 211 | — | Отключить все точки |
| 213 | — | Отключить шину A |
| 214 | — | Отключить шину B |
| 230 | — | Подключить шину |
| 231 | — | Отключить шину |

### [Ask.UI/Features/ServiceTools/SwitchingDevice/SwitchingDeviceControl.xaml](../../Ask.UI/Features/ServiceTools/SwitchingDevice/SwitchingDeviceControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 105 | — | Обновить |
| 136 | — | Подключить |
| 139 | — | Отключить |
| 146 | — | ППУ подключить |
| 149 | — | ППУ отключить |
| 152 | — | ППУ + мультиметр |
| 155 | — | Отключить оба |
| 162 | — | Все шины: вкл. |
| 165 | — | Все шины: выкл. |
| 168 | — | Делитель: вкл. |
| 171 | — | Делитель: выкл. |
| 204 | — | Замкнуть |
| 208 | — | Разомкнуть |
| 214 | — | Все реле: вкл. |
| 217 | — | Все реле: выкл. |
| 233 | — | Подключить |
| 236 | — | Отключить |
| 247 | — | Подключить |
| 250 | — | Отключить |
| 292 | — | Подключить цепь |
| 295 | — | Отключить цепь |

### [Ask.UI/Windows/VoltageValue.xaml](../../Ask.UI/Windows/VoltageValue.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 42 | — | Сохранить |

### [MainWindow/RoleLoginWindow.xaml](../../MainWindow/RoleLoginWindow.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 536 | LoginButton | Войти |

### [MainWindow/Windows/ApkToApkwConversionWindow.xaml](../../MainWindow/Windows/ApkToApkwConversionWindow.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 95 | — | Выбрать файл |
| 119 | — | Отмена |
| 124 | ConvertButton | Конвертировать |

### [MainWindow/Windows/ApkwToApkConversionWindow.xaml](../../MainWindow/Windows/ApkwToApkConversionWindow.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 96 | — | Выбрать файл |
| 124 | — | Выбрать папку |
| 147 | — | Отмена |
| 152 | ConvertButton | Конвертировать |

### [MainWindow/Windows/OpkToPkConversionWindow.xaml](../../MainWindow/Windows/OpkToPkConversionWindow.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 104 | — | Выбрать папку |
| 137 | — | Выбрать файлы |
| 140 | — | Очистить список |
| 167 | — | Отмена |
| 171 | ConvertButton | Конвертировать |

### [MainWindow/Windows/RunAskMkiWindow.xaml](../../MainWindow/Windows/RunAskMkiWindow.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 161 | — | × |
| 214 | — | Выбрать |
| 252 | — | Выбрать |
| 273 | — | Отмена |
| 279 | RunButton | Запустить |

### [Message/MessageBoxCustom.xaml](../../Message/MessageBoxCustom.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 175 | OkButton | Ок [Enter] |
| 179 | CancelButton | Отмена [Esc] |
| 183 | YesButton | Да [Y] |
| 187 | NoButton | Нет [N] |

### [UI/Components/MultiWindowControl.xaml](../../UI/Components/MultiWindowControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 99 | minimizeButton | Иконка: WindowMinimizeIcon |
| 111 | exitButton | Иконка: WindowCloseIcon |

### [UI/Components/SearchControls/CaseToggleButton.xaml](../../UI/Components/SearchControls/CaseToggleButton.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 11 | ToggleButton | Aa |

### [UI/Components/SearchControls/WholeWordToggleButton.xaml](../../UI/Components/SearchControls/WholeWordToggleButton.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 10 | ToggleButton | Abl |

### [UI/Controls/Calendar/CalendarControl.xaml](../../UI/Controls/Calendar/CalendarControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 342 | TodayButton | Сегодня |
| 419 | — | {Binding DayNumber} |
| 440 | AddNoteButton | + Добавить |
| 444 | ReloadNotesButton | Повторить загрузку |

### [UI/Controls/DailyReportDateWindow.xaml](../../UI/Controls/DailyReportDateWindow.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 40 | — | × |
| 86 | — | Отмена |
| 88 | — | Сохранить ZIP |

### [UI/Controls/DeviceHealthView/DeviceHealthViewControl.xaml](../../UI/Controls/DeviceHealthView/DeviceHealthViewControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 24 | — | {Binding NameNumber} |

### [UI/Controls/DevicesStatus.xaml](../../UI/Controls/DevicesStatus.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 18 | UpdateState | Обновить состояние |

### [UI/Controls/ErrorList/ErrorListControl.xaml](../../UI/Controls/ErrorList/ErrorListControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 60 | WarningButton | 0 из 0 предупреждений |
| 67 | ErrorsButton | 0 из 0 ошибок |

### [UI/Controls/Keysight/KeysightPunchControl.xaml](../../UI/Controls/Keysight/KeysightPunchControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 16 | — | Отправить |

### [UI/Controls/Settings/AskMkiConfig/AskMkiGroupEditorDrawerControl.xaml](../../UI/Controls/Settings/AskMkiConfig/AskMkiGroupEditorDrawerControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 270 | — | Отменить |
| 278 | — | Сохранить |

### [UI/Controls/Settings/DeviceConfig/Base/BaseSettingsConfig/DeviceSettingsControl.xaml](../../UI/Controls/Settings/DeviceConfig/Base/BaseSettingsConfig/DeviceSettingsControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 445 | CancelButton | Отменить |
| 454 | SaveButton | Сохранить |

### [UI/Controls/Settings/DeviceConfig/ChassisManager/ChassisManagerControl.xaml](../../UI/Controls/Settings/DeviceConfig/ChassisManager/ChassisManagerControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 165 | — | {Binding Name} / {Binding Number, StringFormat='Номер: {0}'} |
| 247 | — | {Binding Name} / {Binding Number, StringFormat='Номер: {0}'} |

### [UI/Windows/WpfDocking/Themes/Generic/WindowSwitcher.xaml](../../UI/Windows/WpfDocking/Themes/Generic/WindowSwitcher.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 116 | — | {Binding TabText} |

## Собственные кнопки, кликабельные карточки и иконки

### [UI/Components/Invoke/OpenFileButton.xaml](../../UI/Components/Invoke/OpenFileButton.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 118 | CloseButton | Без текстовой подписи |

### [UI/Components/SearchControls/ReplaseAllWordsButton.xaml](../../UI/Components/SearchControls/ReplaseAllWordsButton.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 9 | replaceAllWords | ab / AC |

### [UI/Components/SearchControls/ReplaseWordButton.xaml](../../UI/Components/SearchControls/ReplaseWordButton.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 9 | replaceWord | b / C |

### [UI/Controls/DailyReportDateWindow.xaml](../../UI/Controls/DailyReportDateWindow.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 47 | DateField | Иконка: CalendarIcon |

### [UI/Controls/EmptyWorkspace/EmptyWorkspaceView.xaml](../../UI/Controls/EmptyWorkspace/EmptyWorkspaceView.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 201 | — | {Binding AppVersion, RelativeSource={RelativeSource AncestorType=UserControl}} |

### [UI/Controls/Settings/DeviceConfig/ChassisManager/ChassisManagerControl.xaml](../../UI/Controls/Settings/DeviceConfig/ChassisManager/ChassisManagerControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 126 | addChassisButton | Добавить тестер АСКМ |
| 213 | addRackButton | Добавить стойку СКМ |

### [UI/Controls/Settings/DeviceInfo/DeviceDisplaySettingsControl.xaml](../../UI/Controls/Settings/DeviceInfo/DeviceDisplaySettingsControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 29 | MachineAddressesCard | Машинные адреса |
| 37 | ConnectionInfo | Подключение точек и шин |
| 44 | DeviceExecutionParameters | Параметры устройств |
| 51 | MeasurementResults | Результаты измерений |
| 58 | IntermediateMeasurementResults | Промежуточные результаты измерений |
| 65 | DelayMessages | Сообщения о задержках |

### [UI/Controls/Settings/Execution/ExecutionControl.xaml](../../UI/Controls/Settings/Execution/ExecutionControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 109 | StopInError | Остановка по ошибке |
| 118 | RepeatMeasurement | Повтор измерения |
| 126 | IdleMode | Холостой режим выполнения |
| 135 | CompatibilityModeCheckBox | Выполнение в режиме совместимости со старой системой |
| 148 | DisablePowerCheck | Отключить проверку питания |

### [UI/Controls/Settings/Protocol/ProtocolControl.xaml](../../UI/Controls/Settings/Protocol/ProtocolControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 102 | CommandHeadersCheckBox | Заголовки команд |
| 110 | OperationTime | Вывод времени операции |
| 118 | Header | Отображение заголовков действий |
| 126 | TestStepChecker | Сообщения шагов проверки |
| 134 | AutoPrint | Печать протокола |
| 142 | ProtocolFromPO | Формирование протокола программы контроля |

### [UI/Controls/Settings/UserInterface/UiSettingsControl.xaml](../../UI/Controls/Settings/UserInterface/UiSettingsControl.xaml)

| Строка | Элемент | Подпись / обозначение |
| --- | --- | --- |
| 123 | CommandAutoCollapsing | Автоскрытие блоков в протоколе |
| 131 | TopMenuIcons | Иконки в верхнем меню |
| 250 | SyntaxHighlighting | Подсветка синтаксиса |
| 267 | CommandBodyBackgroundHighlighting | Фоновое выделение команд |
| 275 | ChainPointBodyBackgroundHighlighting | Фоновое выделение цепей и точек |

## Кнопки HTML-справки без title

### [Ask.Support/AppHelp/ru/Content/AboutProgramPart.html](../../Ask.Support/AppHelp/ru/Content/AboutProgramPart.html)

| Строка | Элемент | Подпись |
| --- | --- | --- |
| 62 | — | Лицензионное соглашение |
| 63 | — | Поддержка |
| 69 | — | Закрыть |
| 140 | — | Закрыть |

### [Ask.Support/AppHelp/ru/FastMenuCommand.html](../../Ask.Support/AppHelp/ru/FastMenuCommand.html)

| Строка | Элемент | Подпись |
| --- | --- | --- |
| 120 | theme-toggle | Включить темную тему |
| 144 | ОК | Команда ОК — Объект контроля |
| 147 | СП | Команда СП — Схема подключения |
| 150 | РМ | Команда РМ — Рабочее место |
| 153 | ЦУ | Команда ЦУ — Сообщение оператору |
| 156 | УП | Команда УП — Условный переход |
| 159 | КЦ | Команда КЦ — Конец программы контроля |
| 162 | ПР | Команда ПР — Проверка релейная |
| 165 | ЭТ | Команда ЭТ — Электронная 4-х-точка |
| 168 | СИ | Команда СИ — Сопротивление изоляции |
| 171 | ПИ | Команда ПИ — Прочность изоляции |
| 174 | КС | Команда КС — Контроль сопротивлений |
| 177 | НЭ | Команда НЭ — Нелинейные элементы |
| 180 | ИЕ | Команда ИЕ — Измерение ёмкости |
| 183 | ВШ | Команда ВШ — Включение шин коммутатора |
| 186 | ПТ | Команда ПТ — Подключение точек к шинам |
| 189 | ОТ | Команда ОТ — Отключение точек от шин |
| 192 | СК | Команда СК — Сброс коммутатора |
| 195 | ОС | Команда ОС — Общий сброс |

### [Ask.Support/AppHelp/ru/index.html](../../Ask.Support/AppHelp/ru/index.html)

| Строка | Элемент | Подпись |
| --- | --- | --- |
| 44 | clear-btn | Очистить поиск |
| 49 | menu | Разделы |
| 50 | bookmarks | Закладки |
| 2667 | image-lightbox-close | Закрыть просмотр изображения |

`aria-label` и `data-title` не создают всплывающую подсказку браузера. Элементы с динамически назначаемым title из scripts.js исключены.
