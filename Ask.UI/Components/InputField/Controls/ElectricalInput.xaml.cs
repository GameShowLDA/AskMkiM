using Ask.Core.Services.Errors.Metrology;
using Ask.Core.Services.Errors.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ask.UI.Components.InputField.Controls
{
  /// <summary>
  /// Предоставляет поле ввода электрической величины с настраиваемой единицей измерения.
  /// </summary>
  public partial class ElectricalInput : UserControl
  {
    /// <summary>
    /// Свойство зависимости для заголовка поля.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
      nameof(Header),
      typeof(string),
      typeof(ElectricalInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для введённого значения.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text),
      typeof(string),
      typeof(ElectricalInput),
      new FrameworkPropertyMetadata(
        string.Empty,
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>
    /// Свойство зависимости для текста заполнителя.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
      nameof(Placeholder),
      typeof(string),
      typeof(ElectricalInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для единицы измерения.
    /// </summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
      nameof(Unit),
      typeof(string),
      typeof(ElectricalInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для видимости поля с заполнителем.
    /// </summary>
    public static readonly DependencyProperty PlaceholderVisibilityProperty = DependencyProperty.Register(
      nameof(PlaceholderVisibility),
      typeof(Visibility),
      typeof(ElectricalInput),
      new PropertyMetadata(Visibility.Visible, OnPresentationPropertyChanged));

    /// <summary>
    /// Свойство зависимости для заголовка в режиме выполнения.
    /// </summary>
    public static readonly DependencyProperty ExecutionHeaderProperty = DependencyProperty.Register(
      nameof(ExecutionHeader),
      typeof(string),
      typeof(ElectricalInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для режима отображения выполняемого шага.
    /// </summary>
    public static readonly DependencyProperty IsExecutingProperty = DependencyProperty.Register(
      nameof(IsExecuting),
      typeof(bool),
      typeof(ElectricalInput),
      new PropertyMetadata(false, OnPresentationPropertyChanged));

    /// <summary>
    /// Свойство зависимости для назначения электрической величины.
    /// </summary>
    public static readonly DependencyProperty RoleProperty = DependencyProperty.Register(
      nameof(Role),
      typeof(ElectricalInputRole),
      typeof(ElectricalInput),
      new PropertyMetadata(ElectricalInputRole.Parameter));

    /// <summary>
    /// Заголовок поля.
    /// </summary>
    public string Header
    {
      get => (string)GetValue(HeaderProperty);
      set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Введённое значение.
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
    /// Единица измерения.
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
    /// Заголовок в режиме выполнения.
    /// </summary>
    public string ExecutionHeader
    {
      get => (string)GetValue(ExecutionHeaderProperty);
      set => SetValue(ExecutionHeaderProperty, value);
    }

    /// <summary>
    /// Признак отображения значения вместо поля ввода.
    /// </summary>
    public bool IsExecuting
    {
      get => (bool)GetValue(IsExecutingProperty);
      set => SetValue(IsExecutingProperty, value);
    }

    /// <summary>
    /// Назначение электрической величины.
    /// </summary>
    public ElectricalInputRole Role
    {
      get => (ElectricalInputRole)GetValue(RoleProperty);
      set => SetValue(RoleProperty, value);
    }

    /// <summary>
    /// Создаёт поле ввода электрической величины.
    /// </summary>
    public ElectricalInput()
    {
      InitializeComponent();
      SetLocalizationBinding(PlaceholderProperty, "LS_Input_Placeholder");
      UpdatePresentation();
    }

    /// <summary>
    /// Подсвечивает поле как некорректное.
    /// </summary>
    public void DataError() => ValueTextBox.DataError();

    /// <summary>
    /// Проверяет локальный числовой формат электрической величины и отображает состояние ошибки.
    /// </summary>
    /// <returns>Ошибка формата либо <see langword="null"/>, если значение корректно.</returns>
    public ErrorItem? Validate()
    {
      var isValid = Role == ElectricalInputRole.Parameter
        ? double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
        : double.TryParse(Text, out _);

      if (isValid)
      {
        ValueTextBox.ClearError();
        return null;
      }

      DataError();
      return Role == ElectricalInputRole.Parameter
        ? MetrologyValidationErrors.InvalidElectricalValue().Error
        : MetrologyValidationErrors.InvalidVoltage().Error;
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
      if (dependencyObject is ElectricalInput control && control.ValueTextBox != null)
        control.UpdatePresentation();
    }

    private void UpdatePresentation()
    {
      EditingHeaderTextBlock.Visibility = IsExecuting ? Visibility.Collapsed : Visibility.Visible;
      ExecutionHeaderTextBlock.Visibility = IsExecuting ? Visibility.Visible : Visibility.Collapsed;
      ValueTextBox.Visibility = IsExecuting ? Visibility.Collapsed : PlaceholderVisibility;
      HeaderBorder.CornerRadius = IsExecuting
        ? new CornerRadius(10)
        : new CornerRadius(10, 10, 0, 0);
    }
  }
}
