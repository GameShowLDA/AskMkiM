using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ask.UI.Components.InputField.Controls
{
  /// <summary>
  /// Предоставляет поле ввода номера модуля.
  /// </summary>
  public partial class ModuleInput : UserControl
  {
    /// <summary>Свойство зависимости для заголовка поля.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
      nameof(Header), typeof(string), typeof(ModuleInput), new PropertyMetadata(string.Empty));

    /// <summary>Свойство зависимости для введённого номера модуля.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
      nameof(Text),
      typeof(string),
      typeof(ModuleInput),
      new FrameworkPropertyMetadata(
        string.Empty,
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Свойство зависимости для текста заполнителя.</summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
      nameof(Placeholder), typeof(string), typeof(ModuleInput), new PropertyMetadata(string.Empty));

    /// <summary>Свойство зависимости для видимости поля с заполнителем.</summary>
    public static readonly DependencyProperty PlaceholderVisibilityProperty = DependencyProperty.Register(
      nameof(PlaceholderVisibility),
      typeof(Visibility),
      typeof(ModuleInput),
      new PropertyMetadata(Visibility.Visible, OnPresentationPropertyChanged));

    /// <summary>Свойство зависимости для ограничения ввода числовыми символами.</summary>
    public static readonly DependencyProperty IsNumberInputEnabledProperty = DependencyProperty.Register(
      nameof(IsNumberInputEnabled),
      typeof(bool),
      typeof(ModuleInput),
      new PropertyMetadata(true));

    /// <summary>Свойство зависимости для заголовка в режиме выполнения.</summary>
    public static readonly DependencyProperty ExecutionHeaderProperty = DependencyProperty.Register(
      nameof(ExecutionHeader), typeof(string), typeof(ModuleInput), new PropertyMetadata(string.Empty));

    /// <summary>Свойство зависимости для режима отображения выполняемого шага.</summary>
    public static readonly DependencyProperty IsExecutingProperty = DependencyProperty.Register(
      nameof(IsExecuting),
      typeof(bool),
      typeof(ModuleInput),
      new PropertyMetadata(false, OnPresentationPropertyChanged));

    /// <summary>Заголовок поля.</summary>
    public string Header
    {
      get => (string)GetValue(HeaderProperty);
      set => SetValue(HeaderProperty, value);
    }

    /// <summary>Введённый номер модуля.</summary>
    public string Text
    {
      get => (string)GetValue(TextProperty);
      set => SetValue(TextProperty, value);
    }

    /// <summary>Текст заполнителя.</summary>
    public string Placeholder
    {
      get => (string)GetValue(PlaceholderProperty);
      set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Видимость поля с заполнителем.</summary>
    public Visibility PlaceholderVisibility
    {
      get => (Visibility)GetValue(PlaceholderVisibilityProperty);
      set => SetValue(PlaceholderVisibilityProperty, value);
    }

    /// <summary>Признак ограничения ввода числовыми символами.</summary>
    public bool IsNumberInputEnabled
    {
      get => (bool)GetValue(IsNumberInputEnabledProperty);
      set => SetValue(IsNumberInputEnabledProperty, value);
    }

    /// <summary>Заголовок в режиме выполнения.</summary>
    public string ExecutionHeader
    {
      get => (string)GetValue(ExecutionHeaderProperty);
      set => SetValue(ExecutionHeaderProperty, value);
    }

    /// <summary>Признак отображения номера вместо поля ввода.</summary>
    public bool IsExecuting
    {
      get => (bool)GetValue(IsExecutingProperty);
      set => SetValue(IsExecutingProperty, value);
    }

    /// <summary>
    /// Создаёт поле ввода номера модуля.
    /// </summary>
    public ModuleInput()
    {
      InitializeComponent();
      SetLocalizationBinding(PlaceholderProperty, "LS_Input_Placeholder");
      UpdatePresentation();
    }

    /// <summary>
    /// Подсвечивает поле номера модуля как некорректное.
    /// </summary>
    public void DataError() => ModuleTextBox.DataError();

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
      if (dependencyObject is ModuleInput control && control.ModuleTextBox != null)
        control.UpdatePresentation();
    }

    private void UpdatePresentation()
    {
      EditingHeaderTextBlock.Visibility = IsExecuting ? Visibility.Collapsed : Visibility.Visible;
      ExecutionHeaderTextBlock.Visibility = IsExecuting ? Visibility.Visible : Visibility.Collapsed;
      ModuleTextBox.Visibility = IsExecuting ? Visibility.Collapsed : PlaceholderVisibility;
      HeaderBorder.CornerRadius = IsExecuting
        ? new CornerRadius(10)
        : new CornerRadius(10, 10, 0, 0);
    }
  }
}
