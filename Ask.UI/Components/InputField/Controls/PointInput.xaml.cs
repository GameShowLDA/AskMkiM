using Ask.Core.Services.Errors.Metrology;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ask.UI.Components.InputField.Controls
{
  /// <summary>
  /// Предоставляет поле ввода точки с настраиваемыми заголовком и заполнителем.
  /// </summary>
  public partial class PointInput : UserControl
  {
    /// <summary>
    /// Свойство зависимости для заголовка поля точки.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
      nameof(Header),
      typeof(string),
      typeof(PointInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для введённого значения точки.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text),
      typeof(string),
      typeof(PointInput),
      new FrameworkPropertyMetadata(
        string.Empty,
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>
    /// Свойство зависимости для текста заполнителя.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
      nameof(Placeholder),
      typeof(string),
      typeof(PointInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для видимости поля с заполнителем.
    /// </summary>
    public static readonly DependencyProperty PlaceholderVisibilityProperty = DependencyProperty.Register(
      nameof(PlaceholderVisibility),
      typeof(Visibility),
      typeof(PointInput),
      new PropertyMetadata(Visibility.Visible, OnPresentationPropertyChanged));

    /// <summary>
    /// Свойство зависимости для заголовка точки в режиме выполнения.
    /// </summary>
    public static readonly DependencyProperty ExecutionHeaderProperty = DependencyProperty.Register(
      nameof(ExecutionHeader),
      typeof(string),
      typeof(PointInput),
      new PropertyMetadata(string.Empty));

    /// <summary>
    /// Свойство зависимости для режима отображения выполняемого шага.
    /// </summary>
    public static readonly DependencyProperty IsExecutingProperty = DependencyProperty.Register(
      nameof(IsExecuting),
      typeof(bool),
      typeof(PointInput),
      new PropertyMetadata(false, OnPresentationPropertyChanged));

    /// <summary>
    /// Свойство зависимости для назначения поля точки.
    /// </summary>
    public static readonly DependencyProperty RoleProperty = DependencyProperty.Register(
      nameof(Role),
      typeof(PointInputRole),
      typeof(PointInput),
      new PropertyMetadata(PointInputRole.First));

    /// <summary>
    /// Заголовок поля точки.
    /// </summary>
    public string Header
    {
      get => (string)GetValue(HeaderProperty);
      set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Введённое значение точки.
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
    /// Видимость поля с заполнителем.
    /// </summary>
    public Visibility PlaceholderVisibility
    {
      get => (Visibility)GetValue(PlaceholderVisibilityProperty);
      set => SetValue(PlaceholderVisibilityProperty, value);
    }

    /// <summary>
    /// Заголовок точки в режиме выполнения.
    /// </summary>
    public string ExecutionHeader
    {
      get => (string)GetValue(ExecutionHeaderProperty);
      set => SetValue(ExecutionHeaderProperty, value);
    }

    /// <summary>
    /// Признак отображения значения точки вместо поля ввода.
    /// </summary>
    public bool IsExecuting
    {
      get => (bool)GetValue(IsExecutingProperty);
      set => SetValue(IsExecutingProperty, value);
    }

    /// <summary>
    /// Назначение поля точки.
    /// </summary>
    public PointInputRole Role
    {
      get => (PointInputRole)GetValue(RoleProperty);
      set => SetValue(RoleProperty, value);
    }

    /// <summary>
    /// Создаёт поле ввода точки.
    /// </summary>
    public PointInput()
    {
      InitializeComponent();
      SetLocalizationBinding(HeaderProperty, "LS_Input_First_Title");
      SetLocalizationBinding(PlaceholderProperty, "LS_Input_Placeholder");
      UpdatePresentation();
    }

    public void DataError() => PointTextBox.DataError();

    /// <summary>
    /// Проверяет локальный формат точки и отображает состояние ошибки.
    /// </summary>
    /// <returns>Ошибка формата либо <see langword="null"/>, если значение корректно.</returns>
    public ErrorItem? Validate()
    {
      if (PointModel.ParsePointString(Text) != null)
      {
        PointTextBox.ClearError();
        return null;
      }

      DataError();
      return Role == PointInputRole.First
        ? MetrologyValidationErrors.InvalidFirstPoint().Error
        : MetrologyValidationErrors.InvalidSecondPoint().Error;
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
      if (dependencyObject is PointInput control && control.PointTextBox != null)
        control.UpdatePresentation();
    }

    private void UpdatePresentation()
    {
      EditingHeaderTextBlock.Visibility = IsExecuting ? Visibility.Collapsed : Visibility.Visible;
      ExecutionHeaderTextBlock.Visibility = IsExecuting ? Visibility.Visible : Visibility.Collapsed;
      PointTextBox.Visibility = IsExecuting ? Visibility.Collapsed : PlaceholderVisibility;
      if (IsExecuting)
      {
        HeaderBorder.CornerRadius = new CornerRadius(10, 10, 10, 10);
      }
      else
      {
        HeaderBorder.CornerRadius = new CornerRadius(10, 10, 0, 0);
      }
    }
  }
}
