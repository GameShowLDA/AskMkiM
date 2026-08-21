using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  /// <summary>
  /// Содержит проверку обязательных параметров конфигурации устройства.
  /// </summary>
  public partial class DeviceSettingsControl
  {
    private readonly Dictionary<FrameworkElement, FrameworkElement> _validationSections = [];

    /// <summary>
    /// Подписывает поля формы на снятие подсветки при изменении значения.
    /// </summary>
    private void InitializeValidationTracking()
    {
      RegisterValidationSource(DeviceModelSelectionBox, DeviceModelContainer);
      RegisterValidationSource(DeviceNumberTextBox, DeviceNumberContainer);
      RegisterValidationSource(BusTypeSelectionBox, BusTypeContainer);
      RegisterValidationSource(RelayPointCountTextBox, RelayPointCountContainer);
      RegisterValidationSource(ResistanceTextBox, ResistanceContainer);
      RegisterValidationSource(CapacitanceTextBox, CapacitanceContainer);
      RegisterValidationSource(ConnectionTypeSelectionBox, ConnectionTypeContainer);
      RegisterValidationSource(IpPart1, IPAddressContainer);
      RegisterValidationSource(IpPart2, IPAddressContainer);
      RegisterValidationSource(IpPart3, IPAddressContainer);
      RegisterValidationSource(IpPart4, IPAddressContainer);
      COMContainer.SettingsChanged += OnComSettingsChanged;
    }

    /// <summary>
    /// Связывает редактируемое поле с секцией, подсветку которой необходимо снять.
    /// </summary>
    /// <param name="source">Редактируемое поле формы.</param>
    /// <param name="section">Секция формы, содержащая поле.</param>
    private void RegisterValidationSource(FrameworkElement source, FrameworkElement section)
    {
      _validationSections[source] = section;

      if (source is TextBox textBox)
      {
        textBox.TextChanged -= OnValidationSourceChanged;
        textBox.TextChanged += OnValidationSourceChanged;
      }

      if (source is ComboBox comboBox)
      {
        comboBox.SelectionChanged -= OnValidationSourceChanged;
        comboBox.SelectionChanged += OnValidationSourceChanged;
      }
    }

    /// <summary>
    /// Снимает подсветку секции при изменении связанного поля.
    /// </summary>
    /// <param name="sender">Изменённое поле формы.</param>
    /// <param name="e">Аргументы события изменения.</param>
    private void OnValidationSourceChanged(object sender, RoutedEventArgs e)
    {
      if (sender == DeviceModelSelectionBox)
      {
        ClearValidationHighlights();
        return;
      }

      if (sender is FrameworkElement source &&
          _validationSections.TryGetValue(source, out FrameworkElement? section))
      {
        ClearValidationHighlight(section);
      }
    }

    /// <summary>
    /// Снимает подсветку секции COM-порта при изменении её параметров.
    /// </summary>
    /// <param name="sender">Компонент параметров COM-порта.</param>
    /// <param name="e">Аргументы события.</param>
    private void OnComSettingsChanged(object? sender, EventArgs e)
    {
      ClearValidationHighlight(COMContainer);
    }

    /// <summary>
    /// Проверяет обязательные параметры перед сохранением устройства.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если все обязательные параметры заполнены корректно.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    private bool ValidateRequiredParameters()
    {
      ClearValidationHighlights();

      var issues = new List<RequiredParameterIssue>();

      AddIssueIf(
        issues,
        DeviceModelSelectionBox.SelectedItem is not string,
        "Модель устройства",
        DeviceModelContainer,
        DeviceModelSelectionBox);

      AddIssueIf(
        issues,
        DeviceNumberContainer.Visibility == Visibility.Visible && NumberDevice < 0,
        "Номер устройства",
        DeviceNumberContainer,
        DeviceNumberTextBox);

      AddIssueIf(
        issues,
        BusTypeContainer.Visibility == Visibility.Visible && BusTypeSelectionBox.SelectedItem is not SwitchingBusNew,
        "Тип структурной шины",
        BusTypeContainer,
        BusTypeSelectionBox);

      AddIssueIf(
        issues,
        RelayPointCountContainer.Visibility == Visibility.Visible && RelayPointCount <= 0,
        "Количество точек (каналов)",
        RelayPointCountContainer,
        RelayPointCountTextBox);

      AddIssueIf(
        issues,
        ResistanceContainer.Visibility == Visibility.Visible && !DeviceRequiredParameterValidator.IsNonNegativeNumber(ResistanceTextBox.Text),
        "Сопротивление коммутатора",
        ResistanceContainer,
        ResistanceTextBox);

      AddIssueIf(
        issues,
        CapacitanceContainer.Visibility == Visibility.Visible && !DeviceRequiredParameterValidator.IsNonNegativeNumber(CapacitanceTextBox.Text),
        "Емкость коммутатора",
        CapacitanceContainer,
        CapacitanceTextBox);

      string connectionType = GetSelectedConnectionType();
      AddIssueIf(
        issues,
        ConnectionTypeContainer.Visibility == Visibility.Visible && string.IsNullOrWhiteSpace(connectionType),
        "Тип подключения устройства",
        ConnectionTypeContainer,
        ConnectionTypeSelectionBox);

      AddConnectionDetailsIssues(issues, connectionType);
      AddAdditionalSettingsIssues(issues);

      if (issues.Count == 0)
      {
        return true;
      }

      ShowValidationIssues(issues);
      return false;
    }

    /// <summary>
    /// Добавляет ошибки параметров выбранного подключения.
    /// </summary>
    /// <param name="issues">Список обнаруженных ошибок обязательных параметров.</param>
    /// <param name="connectionType">Тип подключения устройства.</param>
    private void AddConnectionDetailsIssues(List<RequiredParameterIssue> issues, string connectionType)
    {
      if (string.Equals(connectionType, "IP", StringComparison.OrdinalIgnoreCase))
      {
        AddIssueIf(
          issues,
          IPAddressContainer.Visibility == Visibility.Visible && !IsValidIpAddress(),
          "IP Address",
          IPAddressContainer,
          IpPart1);
        return;
      }

      if (string.Equals(connectionType, "COM", StringComparison.OrdinalIgnoreCase))
      {
        bool isInvalid = COMContainer.Visibility == Visibility.Visible &&
          (string.IsNullOrWhiteSpace(COMContainer.PortName) ||
           COMContainer.BaudRate < 0 ||
           COMContainer.DataBits < 0);

        AddIssueIf(
          issues,
          isInvalid,
          "Параметры COM-порта",
          COMContainer,
          COMContainer);
        return;
      }

      if (string.Equals(connectionType, "USB", StringComparison.OrdinalIgnoreCase))
      {
        AddIssueIf(
          issues,
          USBContainer.Visibility == Visibility.Visible && string.IsNullOrWhiteSpace(UsbConnectionDetails),
          "USB устройство",
          USBContainer,
          USBContainer);
      }
    }

    /// <summary>
    /// Добавляет ошибки дополнительных параметров выбранной модели устройства.
    /// </summary>
    /// <param name="issues">Список обнаруженных ошибок обязательных параметров.</param>
    private void AddAdditionalSettingsIssues(List<RequiredParameterIssue> issues)
    {
      AddIssueIf(
        issues,
        _acwPpuDividerCoefficientPercentTextBox != null && !DeviceRequiredParameterValidator.IsPositiveNumber(_acwPpuDividerCoefficientPercentTextBox.Text),
        "Коэффициент делителя ППУ ACW",
        AdditionalSettingsContainer.Content as FrameworkElement ?? AdditionalSettingsContainer,
        _acwPpuDividerCoefficientPercentTextBox!);

      AddIssueIf(
        issues,
        _dcwPpuDividerCoefficientPercentTextBox != null && !DeviceRequiredParameterValidator.IsPositiveNumber(_dcwPpuDividerCoefficientPercentTextBox.Text),
        "Коэффициент делителя ППУ DCW",
        AdditionalSettingsContainer.Content as FrameworkElement ?? AdditionalSettingsContainer,
        _dcwPpuDividerCoefficientPercentTextBox!);

      AddIssueIf(
        issues,
        _systemInsulationResistanceGOhmTextBox != null && GetSystemInsulationResistanceGOhm() is < 1 or > 60,
        "Сопротивление изоляции системы",
        AdditionalSettingsContainer.Content as FrameworkElement ?? AdditionalSettingsContainer,
        _systemInsulationResistanceGOhmTextBox!);
    }

    /// <summary>
    /// Возвращает выбранный поддерживаемый тип подключения.
    /// </summary>
    /// <returns>Тип подключения или пустая строка, если поддерживаемый тип не выбран.</returns>
    private string GetSelectedConnectionType()
    {
      if (ConnectionTypeSelectionBox.SelectedItem is not ComboBoxItem item ||
          item.IsEnabled == false)
      {
        return string.Empty;
      }

      return DeviceRequiredParameterValidator.NormalizeConnectionType(
        item.Content?.ToString(),
        item.IsEnabled);
    }

    /// <summary>
    /// Проверяет заполненный в форме IPv4-адрес.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если каждый октет находится в диапазоне от 0 до 255.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    private bool IsValidIpAddress()
    {
      return DeviceRequiredParameterValidator.IsValidIpAddress(
        IpPart1Value,
        IpPart2Value,
        IpPart3Value,
        IpPart4Value);
    }

    /// <summary>
    /// Добавляет ошибку обязательного параметра при выполнении заданного условия.
    /// </summary>
    /// <param name="issues">Список обнаруженных ошибок обязательных параметров.</param>
    /// <param name="condition">Признак некорректного значения.</param>
    /// <param name="name">Отображаемое имя обязательного параметра.</param>
    /// <param name="section">Секция формы, подлежащая подсветке.</param>
    /// <param name="focusTarget">Элемент, получающий фокус при переходе к ошибке.</param>
    private void AddIssueIf(
      List<RequiredParameterIssue> issues,
      bool condition,
      string name,
      FrameworkElement section,
      FrameworkElement focusTarget)
    {
      if (condition)
      {
        issues.Add(new RequiredParameterIssue(name, section, focusTarget));
      }
    }

    /// <summary>
    /// Подсвечивает ошибочные секции и показывает уведомление о незаполненных параметрах.
    /// </summary>
    /// <param name="issues">Обнаруженные ошибки обязательных параметров.</param>
    private void ShowValidationIssues(IReadOnlyList<RequiredParameterIssue> issues)
    {
      foreach (RequiredParameterIssue issue in issues)
      {
        HighlightInvalidSection(issue.Section);
      }

      ScrollToIssue(issues[0]);
      DeviceConfigNotifications.ShowRequiredParametersMissing(issues.Select(issue => issue.Name));
    }

    /// <summary>
    /// Удаляет подсветку ранее обнаруженных ошибок.
    /// </summary>
    private void ClearValidationHighlights()
    {
      foreach (Border border in EnumerateVisualChildren<Border>(this))
      {
        if (Equals(border.Tag, "DeviceSettingsValidation"))
        {
          border.ClearValue(Border.BorderBrushProperty);
          border.ClearValue(Border.BorderThicknessProperty);
          border.Tag = null;
        }
      }

      COMContainer.SetValidationHighlight(false);
    }

    /// <summary>
    /// Удаляет подсветку ошибки у заданной секции.
    /// </summary>
    /// <param name="section">Секция формы.</param>
    private void ClearValidationHighlight(FrameworkElement section)
    {
      if (section == COMContainer)
      {
        COMContainer.SetValidationHighlight(false);
        return;
      }

      if (section is Border border &&
          Equals(border.Tag, "DeviceSettingsValidation"))
      {
        border.ClearValue(Border.BorderBrushProperty);
        border.ClearValue(Border.BorderThicknessProperty);
        border.Tag = null;
      }
    }

    /// <summary>
    /// Подсвечивает секцию с некорректным значением.
    /// </summary>
    /// <param name="section">Секция формы с некорректным значением.</param>
    private void HighlightInvalidSection(FrameworkElement section)
    {
      if (section == COMContainer)
      {
        COMContainer.SetValidationHighlight(true);
        return;
      }

      if (section is not Border border)
      {
        return;
      }

      border.BorderBrush = GetValidationBrush();
      border.BorderThickness = new Thickness(2);
      border.Tag = "DeviceSettingsValidation";
    }

    /// <summary>
    /// Возвращает кисть для подсветки некорректных параметров.
    /// </summary>
    /// <returns>Кисть из ресурсов приложения или резервная красная кисть.</returns>
    private Brush GetValidationBrush()
    {
      return TryFindResource("RedColorSolidColorBrush") as Brush ?? Brushes.IndianRed;
    }

    /// <summary>
    /// Прокручивает форму к ошибочному параметру и устанавливает на него фокус.
    /// </summary>
    /// <param name="issue">Ошибка обязательного параметра.</param>
    private void ScrollToIssue(RequiredParameterIssue issue)
    {
      issue.Section.BringIntoView();
      SettingsScrollViewer.UpdateLayout();
      issue.Section.BringIntoView();
      issue.FocusTarget.Focus();
    }

    /// <summary>
    /// Перечисляет дочерние элементы заданного типа в визуальном дереве.
    /// </summary>
    /// <typeparam name="T">Тип искомых элементов.</typeparam>
    /// <param name="root">Корневой элемент визуального дерева.</param>
    /// <returns>Последовательность дочерних элементов заданного типа.</returns>
    private IEnumerable<T> EnumerateVisualChildren<T>(DependencyObject root)
      where T : DependencyObject
    {
      int count = VisualTreeHelper.GetChildrenCount(root);
      for (int index = 0; index < count; index++)
      {
        DependencyObject child = VisualTreeHelper.GetChild(root, index);
        if (child is T typedChild)
        {
          yield return typedChild;
        }

        foreach (T descendant in EnumerateVisualChildren<T>(child))
        {
          yield return descendant;
        }
      }
    }

    /// <summary>
    /// Описывает ошибку обязательного параметра и связанные элементы формы.
    /// </summary>
    private sealed class RequiredParameterIssue
    {
      /// <summary>
      /// Инициализирует описание ошибки обязательного параметра.
      /// </summary>
      /// <param name="name">Отображаемое имя обязательного параметра.</param>
      /// <param name="section">Секция формы, подлежащая подсветке.</param>
      /// <param name="focusTarget">Элемент, получающий фокус при переходе к ошибке.</param>
      public RequiredParameterIssue(
        string name,
        FrameworkElement section,
        FrameworkElement focusTarget)
      {
        Name = name;
        Section = section;
        FocusTarget = focusTarget;
      }

      /// <summary>
      /// Отображаемое имя обязательного параметра.
      /// </summary>
      public string Name { get; }

      /// <summary>
      /// Секция формы, подлежащая подсветке.
      /// </summary>
      public FrameworkElement Section { get; }

      /// <summary>
      /// Элемент, получающий фокус при переходе к ошибке.
      /// </summary>
      public FrameworkElement FocusTarget { get; }
    }
  }
}
