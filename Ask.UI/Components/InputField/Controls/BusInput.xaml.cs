using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Components.InputField.Controls
{
  /// <summary>
  /// Предоставляет взаимоисключающий выбор шины A/B или пары шин AB1–AB4.
  /// </summary>
  public partial class BusInput : UserControl
  {
    private bool _isSynchronizing;

    /// <summary>
    /// Свойство зависимости для заголовка поля.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
      nameof(Header), typeof(string), typeof(BusInput), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для заголовка в режиме выполнения.
    /// </summary>
    public static readonly DependencyProperty ExecutionHeaderProperty = DependencyProperty.Register(
      nameof(ExecutionHeader), typeof(string), typeof(BusInput), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для режима выбора шин.
    /// </summary>
    public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
      nameof(SelectionMode),
      typeof(BusSelectionMode),
      typeof(BusInput),
      new PropertyMetadata(BusSelectionMode.Bus, OnPresentationPropertyChanged));

    /// <summary>
    /// Свойство зависимости для выбранной шины A/B.
    /// </summary>
    public static readonly DependencyProperty SelectedBusProperty = DependencyProperty.Register(
      nameof(SelectedBus),
      typeof(BusPoint),
      typeof(BusInput),
      new FrameworkPropertyMetadata(
        BusPoint.A,
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
        OnSelectionPropertyChanged));

    /// <summary>
    /// Свойство зависимости для выбранной пары шин.
    /// </summary>
    public static readonly DependencyProperty SelectedBusGroupProperty = DependencyProperty.Register(
      nameof(SelectedBusGroup),
      typeof(SwitchingBusNew),
      typeof(BusInput),
      new FrameworkPropertyMetadata(
        SwitchingBusNew.AB1,
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
        OnSelectionPropertyChanged));

    /// <summary>
    /// Свойство зависимости для режима отображения выполняемого шага.
    /// </summary>
    public static readonly DependencyProperty IsExecutingProperty = DependencyProperty.Register(
      nameof(IsExecuting),
      typeof(bool),
      typeof(BusInput),
      new PropertyMetadata(false, OnPresentationPropertyChanged));

    /// <summary>
    /// Свойство зависимости для отображаемого выбранного значения.
    /// </summary>
    public static readonly DependencyProperty ExecutionValueProperty = DependencyProperty.Register(
      nameof(ExecutionValue),
      typeof(string),
      typeof(BusInput),
      new PropertyMetadata(string.Empty));

    /// <summary>Заголовок поля.</summary>
    public string Header
    {
      get => (string)GetValue(HeaderProperty);
      set => SetValue(HeaderProperty, value);
    }

    /// <summary>Заголовок в режиме выполнения.</summary>
    public string ExecutionHeader
    {
      get => (string)GetValue(ExecutionHeaderProperty);
      set => SetValue(ExecutionHeaderProperty, value);
    }

    /// <summary>Режим выбора шин.</summary>
    public BusSelectionMode SelectionMode
    {
      get => (BusSelectionMode)GetValue(SelectionModeProperty);
      set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>Выбранная шина A/B.</summary>
    public BusPoint SelectedBus
    {
      get => (BusPoint)GetValue(SelectedBusProperty);
      set => SetValue(SelectedBusProperty, value);
    }

    /// <summary>Выбранная пара шин AB1–AB4.</summary>
    public SwitchingBusNew SelectedBusGroup
    {
      get => (SwitchingBusNew)GetValue(SelectedBusGroupProperty);
      set => SetValue(SelectedBusGroupProperty, value);
    }

    /// <summary>Признак отображения выбранного значения вместо переключателей.</summary>
    public bool IsExecuting
    {
      get => (bool)GetValue(IsExecutingProperty);
      set => SetValue(IsExecutingProperty, value);
    }

    /// <summary>Текст выбранного значения.</summary>
    public string ExecutionValue
    {
      get => (string)GetValue(ExecutionValueProperty);
      private set => SetValue(ExecutionValueProperty, value);
    }

    /// <summary>
    /// Создаёт элемент выбора шин.
    /// </summary>
    public BusInput()
    {
      InitializeComponent();
      SynchronizeSelection();
      UpdatePresentation();
    }

    private static void OnSelectionPropertyChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs eventArgs)
    {
      if (dependencyObject is BusInput control && control.BusACheckBox != null)
      {
        control.SynchronizeSelection();
        control.UpdateExecutionValue();
      }
    }

    private static void OnPresentationPropertyChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs eventArgs)
    {
      if (dependencyObject is BusInput control && control.BusPanel != null)
        control.UpdatePresentation();
    }

    private void Bus_Checked(object sender, RoutedEventArgs e)
    {
      if (_isSynchronizing)
        return;

      SelectedBus = sender == BusACheckBox ? BusPoint.A : BusPoint.B;
    }

    private void Bus_Unchecked(object sender, RoutedEventArgs e)
    {
      if (_isSynchronizing)
        return;

      SelectedBus = sender == BusACheckBox ? BusPoint.B : BusPoint.A;
    }

    private void BusGroup_Checked(object sender, RoutedEventArgs e)
    {
      if (_isSynchronizing)
        return;

      SelectedBusGroup = sender switch
      {
        _ when sender == BusAB1CheckBox => SwitchingBusNew.AB1,
        _ when sender == BusAB2CheckBox => SwitchingBusNew.AB2,
        _ when sender == BusAB3CheckBox => SwitchingBusNew.AB3,
        _ => SwitchingBusNew.AB4
      };
    }

    private void BusGroup_Unchecked(object sender, RoutedEventArgs e)
    {
      if (_isSynchronizing)
        return;

      var uncheckedBus = sender switch
      {
        _ when sender == BusAB1CheckBox => SwitchingBusNew.AB1,
        _ when sender == BusAB2CheckBox => SwitchingBusNew.AB2,
        _ when sender == BusAB3CheckBox => SwitchingBusNew.AB3,
        _ => SwitchingBusNew.AB4
      };

      SelectedBusGroup = uncheckedBus == SelectedBusGroup
        ? GetNextBusGroup(SelectedBusGroup)
        : SelectedBusGroup;
      SynchronizeSelection();
    }

    private void SynchronizeSelection()
    {
      _isSynchronizing = true;
      BusACheckBox.IsChecked = SelectedBus == BusPoint.A;
      BusBCheckBox.IsChecked = SelectedBus == BusPoint.B;
      BusAB1CheckBox.IsChecked = SelectedBusGroup == SwitchingBusNew.AB1;
      BusAB2CheckBox.IsChecked = SelectedBusGroup == SwitchingBusNew.AB2;
      BusAB3CheckBox.IsChecked = SelectedBusGroup == SwitchingBusNew.AB3;
      BusAB4CheckBox.IsChecked = SelectedBusGroup == SwitchingBusNew.AB4;
      _isSynchronizing = false;
    }

    private void UpdatePresentation()
    {
      BusPanel.Visibility = SelectionMode == BusSelectionMode.Bus
        ? Visibility.Visible
        : Visibility.Collapsed;
      BusGroupPanel.Visibility = SelectionMode == BusSelectionMode.BusGroup
        ? Visibility.Visible
        : Visibility.Collapsed;
      SelectionBorder.Visibility = IsExecuting ? Visibility.Collapsed : Visibility.Visible;
      EditingHeaderTextBlock.Visibility = IsExecuting ? Visibility.Collapsed : Visibility.Visible;
      ExecutionHeaderTextBlock.Visibility = IsExecuting ? Visibility.Visible : Visibility.Collapsed;
      HeaderBorder.CornerRadius = IsExecuting
        ? new CornerRadius(10)
        : new CornerRadius(10, 10, 0, 0);
      UpdateExecutionValue();
    }

    private void UpdateExecutionValue()
    {
      ExecutionValue = SelectionMode == BusSelectionMode.Bus
        ? SelectedBus.ToString()
        : SelectedBusGroup.ToString();
    }

    private static SwitchingBusNew GetNextBusGroup(SwitchingBusNew current) => current switch
    {
      SwitchingBusNew.AB1 => SwitchingBusNew.AB2,
      SwitchingBusNew.AB2 => SwitchingBusNew.AB3,
      SwitchingBusNew.AB3 => SwitchingBusNew.AB4,
      _ => SwitchingBusNew.AB1
    };
  }
}
