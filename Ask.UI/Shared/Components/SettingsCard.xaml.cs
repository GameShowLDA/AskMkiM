using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Ask.UI.Shared.Components
{
  /// <summary>Карточка настройки с чекбоксом, заголовком и описанием.</summary>
  public partial class SettingsCard : UserControl
  {
    private bool _suppressEvents = true;

    public SettingsCard()
    {
      InitializeComponent();
      CardBorder.MouseLeftButtonUp += CardBorder_MouseLeftButtonUp;
      Loaded += (_, __) => _suppressEvents = false;
      Unloaded += (_, __) => _suppressEvents = true;
    }

    public static readonly DependencyProperty TitleProperty =
      DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingsCard), new PropertyMetadata("Заголовок"));

    public static readonly DependencyProperty DescriptionProperty =
      DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCard), new PropertyMetadata("Описание"));

    public static readonly DependencyProperty IsCheckedProperty =
      DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(SettingsCard), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

    /// <summary>Заголовок карточки.</summary>
    public string Title
    {
      get => (string)GetValue(TitleProperty);
      set => SetValue(TitleProperty, value);
    }

    /// <summary>Описание карточки.</summary>
    public string Description
    {
      get => (string)GetValue(DescriptionProperty);
      set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Состояние флага карточки.</summary>
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

    private static bool IsClickInsideCheckBox(DependencyObject? source)
    {
      while (source != null)
      {
        if (source is CheckBox)
          return true;

        source = source switch
        {
          Visual visual => VisualTreeHelper.GetParent(visual),
          Visual3D visual3D => VisualTreeHelper.GetParent(visual3D),
          _ => LogicalTreeHelper.GetParent(source)
        };
      }

      return false;
    }

    protected virtual void OnCheckedChanged(bool newValue)
    {
      CheckedChanged?.Invoke(this, newValue);
    }

    private void CardBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton != MouseButton.Left || IsClickInsideCheckBox(e.OriginalSource as DependencyObject))
        return;

      IsChecked = !IsChecked;
      e.Handled = true;
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
      base.OnVisualParentChanged(oldParent);
      if (VisualParent == null)
        _suppressEvents = true;
    }
  }
}
