using Ask.Core.Services.Errors.Metrology;
using Ask.Core.Services.Errors.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ask.UI.Components.InputField.Controls
{
  /// <summary>
  /// Предоставляет поле ввода времени с настраиваемыми заголовками, заполнителем и единицей измерения.
  /// </summary>
  public partial class TimeInput : UserControl
  {
    /// <summary>
    /// Свойство зависимости для заголовка поля времени.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
      nameof(Header),
      typeof(string),
      typeof(TimeInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для введённого значения времени.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text),
      typeof(string),
      typeof(TimeInput),
      new FrameworkPropertyMetadata(
        string.Empty,
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>
    /// Свойство зависимости для текста заполнителя.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
      nameof(Placeholder),
      typeof(string),
      typeof(TimeInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для единицы измерения.
    /// </summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
      nameof(Unit),
      typeof(string),
      typeof(TimeInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для видимости поля с заполнителем.
    /// </summary>
    public static readonly DependencyProperty PlaceholderVisibilityProperty = DependencyProperty.Register(
      nameof(PlaceholderVisibility),
      typeof(Visibility),
      typeof(TimeInput),
      new PropertyMetadata(Visibility.Visible, OnPresentationPropertyChanged));

    /// <summary>
    /// Свойство зависимости для заголовка времени в режиме выполнения.
    /// </summary>
    public static readonly DependencyProperty ExecutionHeaderProperty = DependencyProperty.Register(
      nameof(ExecutionHeader),
      typeof(string),
      typeof(TimeInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для режима отображения выполняемого шага.
    /// </summary>
    public static readonly DependencyProperty IsExecutingProperty = DependencyProperty.Register(
      nameof(IsExecuting),
      typeof(bool),
      typeof(TimeInput),
      new PropertyMetadata(false, OnPresentationPropertyChanged));

    /// <summary>
    /// Свойство зависимости для назначения поля времени.
    /// </summary>
    public static readonly DependencyProperty RoleProperty = DependencyProperty.Register(
      nameof(Role),
      typeof(TimeInputRole),
      typeof(TimeInput),
      new PropertyMetadata(TimeInputRole.ExecutionTime));

    /// <summary>
    /// Заголовок поля времени.
    /// </summary>
    public string Header
    {
      get => (string)GetValue(HeaderProperty);
      set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Введённое значение времени.
    /// </summary>
    public string Text
    {
      get => (string)GetValue(TextProperty);
      set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Текст заполнителя.
    /// </summary>
    public string Placeholder
    {
      get => (string)GetValue(PlaceholderProperty);
      set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// Единица измерения времени.
    /// </summary>
    public string Unit
    {
      get => (string)GetValue(UnitProperty);
      set => SetValue(UnitProperty, value);
    }

    /// <summary>
    /// Видимость поля с заполнителем.
    /// </summary>
    public Visibility PlaceholderVisibility
    {
      get => (Visibility)GetValue(PlaceholderVisibilityProperty);
      set => SetValue(PlaceholderVisibilityProperty, value);
    }

    /// <summary>
    /// Заголовок времени в режиме выполнения.
    /// </summary>
    public string ExecutionHeader
    {
      get => (string)GetValue(ExecutionHeaderProperty);
      set => SetValue(ExecutionHeaderProperty, value);
    }

    /// <summary>
    /// Признак отображения значения времени вместо поля ввода.
    /// </summary>
    public bool IsExecuting
    {
      get => (bool)GetValue(IsExecutingProperty);
      set => SetValue(IsExecutingProperty, value);
    }

    /// <summary>
    /// Назначение поля времени.
    /// </summary>
    public TimeInputRole Role
    {
      get => (TimeInputRole)GetValue(RoleProperty);
      set => SetValue(RoleProperty, value);
    }

    /// <summary>
    /// Создаёт поле ввода времени.
    /// </summary>
    public TimeInput()
    {
      InitializeComponent();
      SetLocalizationBinding(HeaderProperty, "LS_Input_Time_Title");
      SetLocalizationBinding(PlaceholderProperty, "LS_Input_Placeholder_Time");
      SetLocalizationBinding(UnitProperty, "LS_Unit_Seconds");
      UpdatePresentation();
    }

    /// <summary>
    /// Подсвечивает поле времени как некорректное.
    /// </summary>
    public void DataError() => TimeTextBox.DataError();

    /// <summary>
    /// Проверяет локальный числовой формат времени и отображает состояние ошибки.
    /// </summary>
    /// <returns>Ошибка формата либо <see langword="null"/>, если значение корректно.</returns>
    public ErrorItem? Validate()
    {
      var value = Role == TimeInputRole.RampTime
        ? Text.Replace(',', '.')
        : Text;

      var isValid = Role == TimeInputRole.ExecutionTime
        ? int.TryParse(value, out var executionTime) &&
          executionTime is >= 1 and <= 60
        : double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var rampTime) &&
          rampTime is >= 0.1 and <= 10;

      if (isValid)
      {
        TimeTextBox.ClearError();
        return null;
      }

      DataError();
      return Role == TimeInputRole.ExecutionTime
        ? MetrologyValidationErrors.InvalidExecutionTime().Error
        : MetrologyValidationErrors.InvalidRampTime().Error;
    }

    private void SetLocalizationBinding(DependencyProperty property, string resourceKey)
    {
      var resource = TryFindResource(resourceKey);
      if (resource != null)
        SetBinding(property, new Binding("Value") { Source = resource });
    }

    private static void OnPresentationPropertyChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs eventArgs)
    {
      if (dependencyObject is TimeInput control && control.TimeTextBox != null)
        control.UpdatePresentation();
    }

    private void UpdatePresentation()
    {
      EditingHeaderTextBlock.Visibility = IsExecuting ? Visibility.Collapsed : Visibility.Visible;
      ExecutionHeaderTextBlock.Visibility = IsExecuting ? Visibility.Visible : Visibility.Collapsed;
      TimeTextBox.Visibility = IsExecuting ? Visibility.Collapsed : PlaceholderVisibility;
      HeaderBorder.CornerRadius = IsExecuting
        ? new CornerRadius(10)
        : new CornerRadius(10, 10, 0, 0);
    }
  }
}
