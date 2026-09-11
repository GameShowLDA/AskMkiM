# ErrorOverviewBar

`Ask.UI.Controls.TextEditorControl.ErrorOverviewBar` — WPF `UserControl` с XAML-разметкой.
Внутренняя `ErrorOverviewSurface` рисует маркеры через `DrawingContext`, сохраняя компактность
при большом количестве строк. Рамка и карточка предпросмотра описаны в XAML.

## Пример самостоятельного элемента

```xml
<overview:ErrorOverviewBar
    xmlns:overview="clr-namespace:Ask.UI.Controls.TextEditorControl;assembly=Ask.UI"
    Width="28"
    Background="#20252D"
    BorderBrush="#526070"
    BorderThickness="1"
    CornerRadius="5"
    Padding="2"
    ErrorBrush="#FF5050"
    WarningBrush="#FFC040"
    CommandBrush="#40BFFF"
    MarkerHeight="4"
    MarkerGap="2"
    MarkerCornerRadius="2"
    ActiveMarkerBorderBrush="White"
    ActiveMarkerBorderThickness="2"
    HoverMarkerBrush="#FFFFFF"
    IsViewportVisible="False"
    IsTrackNavigationEnabled="False"
    AreToolTipsEnabled="False"/>
```

`Width`, `Height`, `MinWidth`, `MaxWidth`, `Margin`, `Padding`, `Background`, `BorderBrush`,
`BorderThickness`, `Opacity`, `Visibility`, `IsEnabled` и `IsHitTestVisible` — стандартные
WPF-свойства. `Padding` и внешняя рамка уменьшают область рисования; позиции и клики
вычисляются относительно внутренней поверхности. `Background` задаёт фон внутренней
полосы; при `null` используется `ScrollBarTrackBackground`, затем полупрозрачный серый.
`CornerRadius` задаёт внешнюю рамку, `TrackCornerRadius` — фон внутренней полосы.

Все дополнительные свойства — dependency properties: поддерживают XAML, `Binding`,
`Style`/триггеры и `DynamicResource`. Настройки не сохраняются автоматически в БД.
Кисти принимают любой `Brush`, включая градиентные кисти. Цвета `null`, указанные ниже,
означают fallback; для явного отсутствия заливки используйте `Transparent`.

## Встраивание в ProtocolListBoxUI

В текущей разметке протокола свойства хоста ниже не подключены привязками:
оформление задаётся прямо на `OverviewTrackHost` и `errorOverviewBar` в
`Ask.UI/Components/ProtocolListBox/ProtocolListBoxUI.xaml`.
У полосы установлен `Background="Transparent"`, фон хоста не задан, рамки отключены,
у полосы — `IsViewportVisible="False"`, поэтому видны только маркеры.
Полоса маркеров находится над правым `ProtocolVerticalScrollBar` в общей колонке.
`OverviewTrackHost` имеет `Panel.ZIndex="2"`, полоса — `IsMarkerHitTestOnly="True"`:
клики и подсказки работают над маркерами, свободная область пропускает мышь к скроллу.
Для такого наложения фон внешнего хоста должен оставаться `null`, а не `Transparent`,
иначе хост сам перехватит попадание мыши. Перетаскивание ползунка начинается вне маркеров.
Внутренний скролл списка скрыт; внешний синхронизируется с тем же ScrollViewer
через `RefreshVerticalScrollBar` и `ProtocolVerticalScrollBar_Scroll`.
Пример ниже применим при подключении соответствующих свойств хоста к элементам.

```xml
<protocol:ProtocolListBoxUI
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:protocol="clr-namespace:Ask.UI.Components.ProtocolListBox;assembly=Ask.UI"
    xmlns:overview="clr-namespace:Ask.UI.Controls.TextEditorControl;assembly=Ask.UI"
    ProtocolVerticalScrollBarVisibility="Hidden">
    <protocol:ProtocolListBoxUI.OverviewBarStyle>
        <Style TargetType="overview:ErrorOverviewBar">
            <Setter Property="ErrorBrush" Value="#FF5050"/>
            <Setter Property="CommandBrush" Value="#40BFFF"/>
            <Setter Property="Background" Value="#20252D"/>
            <Setter Property="AreToolTipsEnabled" Value="False"/>
            <Setter Property="IsViewportVisible" Value="False"/>
        </Style>
    </protocol:ProtocolListBoxUI.OverviewBarStyle>
    <protocol:ProtocolListBoxUI.OverviewHostStyle>
        <Style TargetType="Border">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Padding" Value="2"/>
            <Setter Property="MinWidth" Value="24"/>
            <Setter Property="BorderBrush" Value="#526070"/>
            <Setter Property="BorderThickness" Value="1"/>
        </Style>
    </protocol:ProtocolListBoxUI.OverviewHostStyle>
</protocol:ProtocolListBoxUI>
```

- `OverviewBarStyle` — стиль самого `ErrorOverviewBar`.
- `OverviewHostStyle` — стиль внешнего `Border`, включая фон, размеры, отступы и рамку.
  При `null` используется прежнее оформление хоста. Собственный стиль заменяет его целиком.
- `OverviewVisibility="Collapsed"` — полностью убирает обзорную полосу и её колонку.
- `ProtocolVerticalScrollBarVisibility="Hidden"` — убирает штатный вертикальный скролл,
  сохраняя возможность прокрутки. `Disabled` имеет стандартную семантику WPF и отключает
  вертикальную прокрутку; для простого скрытия используйте `Hidden`.
- `IsViewportVisible="False"` — скрывает только индикатор видимого диапазона на полосе.
- `IsTrackNavigationEnabled="False"` — отключает переход кликом по свободной области,
  сохраняя клики по маркерам. `IsNavigationEnabled="False"` отключает оба вида переходов.
- `AreToolTipsEnabled="False"` — закрывает карточку и исключает вызов фабрики предпросмотра.
  `IsPositionPreviewEnabled="False"` оставляет описание маркера без фрагмента протокола.

Кнопки перехода к ошибкам, счётчик и F8 принадлежат `ProtocolListBoxUI` и продолжают
работать независимо от видимости полосы и её локальной навигации.

## Свойства

| Свойство | Тип | Назначение |
| --- | --- | --- |
| `IsMarkerHitTestOnly` | `bool` | При `true` мышь обрабатывается только над маркерами; по умолчанию `false`. |
| `ErrorBrush` | `Brush?` | Кисть ошибок; null выбирает ресурс темы. |
| `WarningBrush` | `Brush?` | Кисть предупреждений; null выбирает ресурс темы. |
| `CommandBrush` | `Brush` | Кисть команд и информационных маркеров. |
| `ViewportBrush` | `Brush?` | Кисть видимой области; null использует цвет её рамки. |
| `ViewportBorderBrush` | `Brush?` | Кисть рамки видимой области; null выбирает ресурс темы. |
| `MarkerBorderBrush` | `Brush?` | Кисть рамки обычного маркера. |
| `ActiveMarkerBrush` | `Brush?` | Кисть активного маркера; null сохраняет цвет категории. |
| `HoverMarkerBrush` | `Brush?` | Кисть маркера под указателем; null сохраняет цвет категории. |
| `ActiveMarkerBorderBrush` | `Brush?` | Кисть рамки активного маркера; null использует рамку видимой области. |
| `HoverMarkerBorderBrush` | `Brush?` | Кисть рамки маркера под указателем. |
| `PreviewBackground` | `Brush` | Фон подсказки. |
| `PreviewForeground` | `Brush` | Цвет текста подсказки. |
| `PreviewBorderBrush` | `Brush` | Кисть рамки подсказки. |
| `PreviewAccentBrush` | `Brush` | Кисть акцента подсказки. |
| `MarkerHeight` | `double` | Высота обычного маркера. |
| `MarkerGap` | `double` | Расстояние между маркерами и порог группировки. |
| `ActiveMarkerHeight` | `double` | Высота активного маркера. |
| `HoverMarkerHeight` | `double` | Высота маркера под указателем. |
| `MarkerInset` | `double` | Горизонтальный отступ маркеров. |
| `MarkerHitPadding` | `double` | Допуск попадания указателя по вертикали. |
| `MarkerCornerRadius` | `double` | Радиус углов маркеров. |
| `TrackCornerRadius` | `double` | Радиус углов фона полосы. |
| `ViewportCornerRadius` | `double` | Радиус углов видимой области. |
| `MarkerBorderThickness` | `double` | Толщина рамки маркера. |
| `ActiveMarkerBorderThickness` | `double` | Толщина рамки активного маркера. |
| `HoverMarkerBorderThickness` | `double` | Толщина рамки маркера под указателем. |
| `ViewportBorderThickness` | `double` | Толщина рамки видимой области. |
| `ViewportOpacity` | `double` | Непрозрачность заливки видимой области от нуля до единицы. |
| `PreviewWidth` | `double` | Ширина подсказки. |
| `PreviewFontSize` | `double` | Размер шрифта подсказки. |
| `PreviewLineHeight` | `double` | Высота строки подсказки. |
| `PreviewHorizontalGap` | `double` | Расстояние от подсказки до полосы. |
| `PreviewVerticalOffset` | `double` | Смещение подсказки относительно указателя по вертикали. |
| `PreviewAccentWidth` | `double` | Ширина цветового акцента подсказки. |
| `IsViewportVisible` | `bool` | Видимость индикатора области прокрутки. |
| `IsNavigationEnabled` | `bool` | Разрешение переходов кликом по полосе. |
| `IsTrackNavigationEnabled` | `bool` | Разрешение прокрутки кликом по свободной области. |
| `AreToolTipsEnabled` | `bool` | Разрешение показа подсказок. |
| `IsPositionPreviewEnabled` | `bool` | Добавление фрагмента протокола в подсказку. |
| `AreCommandMarkersVisible` | `bool` | Видимость маркеров команд. |
| `AreWarningMarkersVisible` | `bool` | Видимость маркеров предупреждений. |
| `AreErrorMarkersVisible` | `bool` | Видимость маркеров ошибок. |
| `IsMarkerGroupingEnabled` | `bool` | Объединение близких маркеров в группы. |
| `CornerRadius` | `CornerRadius` | Радиусы углов внешней рамки. |
| `PreviewCornerRadius` | `CornerRadius` | Радиусы углов подсказки. |
| `PreviewPadding` | `Thickness` | Внутренние отступы подсказки. |
| `PreviewBorderThickness` | `Thickness` | Толщина рамки подсказки. |
| `PreviewAccentMargin` | `Thickness` | Отступы цветового акцента. |
| `PreviewFontFamily` | `FontFamily` | Шрифт подсказки. |
| `PreviewTextWrapping` | `TextWrapping` | Перенос строк подсказки. |
| `PreviewContentTemplate` | `DataTemplate?` | Шаблон содержимого подсказки; контекст — строка предпросмотра. |
| `PreviewEffect` | `Effect` | Эффект подсказки; null отключает эффект. |

Значения по умолчанию сохраняют прежнюю палитру: команды `DodgerBlue`, ошибки и
предупреждения — `ErrorListErrorIconBrush`/`ErrorListWarningIconBrush` (fallback Red/Orange),
видимая область — `TextEditorLineNumberBrush` (fallback SlateGray) с непрозрачностью 0.14.
Маркер имеет высоту 3, активный/под указателем — 5, интервал — 1; ширина подсказки 680.
Полный перечень defaults находится рядом с регистрациями свойств в
`Ask.UI/Controls/TextEditorControl/ErrorOverviewBar.Properties.cs`.
Отрицательные/бесконечные размеры и NaN отклоняются WPF при установке; непрозрачность — 0…1.
`PreviewVerticalOffset` допускает отрицательное конечное значение.

## Собственный шаблон подсказки

`PreviewContentTemplate` получает строку описания и/или предпросмотра как `DataContext`.
Шаблон заменяет внутреннее содержимое; внешняя рамка, акцент и эффект остаются
настраиваемыми отдельными свойствами. `PreviewAccentWidth="0"` и
`PreviewAccentMargin="0"` убирают акцент; `PreviewEffect="{x:Null}"` убирает тень.
Через шаблон доступны произвольные шрифты, выравнивание и собственная разметка.

```xml
<overview:ErrorOverviewBar xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                           xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                           xmlns:overview="clr-namespace:Ask.UI.Controls.TextEditorControl;assembly=Ask.UI"
                           PreviewBackground="#FAFAFA"
                           PreviewForeground="#202020"
                           PreviewTextWrapping="Wrap"
                           PreviewWidth="360"
                           PreviewEffect="{x:Null}">
    <overview:ErrorOverviewBar.PreviewContentTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding}" TextWrapping="Wrap"
                       FontFamily="Segoe UI" FontSize="14" FontWeight="SemiBold"/>
        </DataTemplate>
    </overview:ErrorOverviewBar.PreviewContentTemplate>
</overview:ErrorOverviewBar>
```

## Данные и взаимодействие

Существующие методы сохранены: `SetEditor`, `SetIssues`, `AddIssue`, `ClearIssues`,
`SetDiagnostics`, `SetLineDiagnostics`, `SetViewport`, `SetActiveLine`.
Они подают данные; appearance properties не меняют формат диагностик.
`Information` отображается через `CommandBrush`, потому что протокол использует этот уровень
для команд. Фильтры категорий применяются до объединения строк и кластеров, поэтому
скрытая ошибка не поглощает видимую команду той же строки.

`ProtocolListBoxUI.RefreshOverviewPositions → SetLinePositions` подаёт нормированные
позиции. `SetPositionPreviewFactory` задаёт построение текстового фрагмента.
Клик по группе перебирает её строки; Shift+клик — в обратном направлении.
`IsMarkerGroupingEnabled` управляет группировкой близких строк; несколько диагностик
одной строки всегда остаются одним маркером. Изменение геометрии и фильтров перестраивает
маркеры, изменение кистей перерисовывает поверхность. Подсказка закрывается при выгрузке
или скрытии элемента.

## Проверки

`Ask.UI.UnitTests/Components/ProtocolListBox/ProtocolOverviewTests.cs` проверяет навигацию,
группировку, фильтрацию, XAML Styles/DynamicResource, привязки карточки, цвета пикселей
через `RenderTargetBitmap`, запрет недопустимой геометрии и настройки хоста.
