using System.Windows;
using System.Windows.Controls;

namespace UI.Components
{
  /// <summary> Карточка настройки с чекбоксом, заголовком и описанием. </summary>
  public partial class SettingsCard : UserControl
  {
    private bool _suppressEvents = true; // по умолчанию гасим до Loaded

    public SettingsCard()
    {
      InitializeComponent();
      Loaded += (_, __) => _suppressEvents = false;   
      Unloaded += (_, __) => _suppressEvents = true; 
    }

    // Заголовок
    public static readonly DependencyProperty TitleProperty =
      DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingsCard), new PropertyMetadata("Заголовок"));

    public string Title
    {
      get => (string)GetValue(TitleProperty);
      set => SetValue(TitleProperty, value);
    }

    // Описание
    public static readonly DependencyProperty DescriptionProperty =
      DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCard), new PropertyMetadata("Описание"));

    public string Description
    {
      get => (string)GetValue(DescriptionProperty);
      set => SetValue(DescriptionProperty, value);
    }

    // Состояние чекбокса
    public static readonly DependencyProperty IsCheckedProperty =
      DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(SettingsCard), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

    public bool IsChecked
    {
      get => (bool)GetValue(IsCheckedProperty);
      set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Событие при изменении состояния чекбокса.</summary>
    public event EventHandler<bool>? CheckedChanged;

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var control = (SettingsCard)d;

      if (control._suppressEvents || PresentationSource.FromVisual(control) == null)
        return;

      control.OnCheckedChanged((bool)e.NewValue);
    }

    protected virtual void OnCheckedChanged(bool newValue)
    {
      CheckedChanged?.Invoke(this, newValue);
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
      base.OnVisualParentChanged(oldParent);
      if (VisualParent == null) _suppressEvents = true;
    }
  }
}
