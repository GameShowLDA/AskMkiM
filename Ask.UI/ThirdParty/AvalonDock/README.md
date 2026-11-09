# AvalonDock

Исходный код AvalonDock 5.0.1 зафиксирован из официального выпуска
`https://github.com/Dirkster99/AvalonDock/releases/tag/v5.0.1`.

Upstream commit `2bc82e18371998e6396fb5feb5d09fd8a6fe7ffa`.

Локальные отличия upstream ограничены сборкой WPF-компонентов для `net8.0-windows`
и отключением strong-name для локальных сборок. Подпись не требуется: все компоненты
поставляются вместе с AskMkiM; её отключение предотвращает ошибку ALINK при сборке
ресурсных сборок в Visual Studio. Исходная лицензия сохранена в `LICENSE`.

Каталог `Integration` содержит код AskMkiM: тематический адаптер и оболочку рабочей
области. Он не является частью исходного AvalonDock и отделён от `source`.

