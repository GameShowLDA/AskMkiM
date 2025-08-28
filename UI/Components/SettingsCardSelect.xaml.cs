using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI.Components
{
  /// <summary>
  /// Карточка настройки с выпадающим списком (выбором) и поддержкой значения по умолчанию.
  /// </summary>
  public partial class SettingsCardSelect : UserControl
  {
    public SettingsCardSelect()
    {
      InitializeComponent();
      Loaded += (_, __) => ApplyDefaultIfNeeded();
    }

    // ---- Текст ----
    /// <summary>Заголовок карточки.</summary>
    public static readonly DependencyProperty TitleProperty =
      DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingsCardSelect),
        new PropertyMetadata("Заголовок"));
    public string Title
    {
      get => (string)GetValue(TitleProperty);
      set => SetValue(TitleProperty, value);
    }

    /// <summary>Описание карточки (подзаголовок).</summary>
    public static readonly DependencyProperty DescriptionProperty =
      DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCardSelect),
        new PropertyMetadata("Описание"));
    public string Description
    {
      get => (string)GetValue(DescriptionProperty);
      set => SetValue(DescriptionProperty, value);
    }

    // ---- Данные для выбора ----
    /// <summary>Источник элементов для ComboBox.</summary>
    public static readonly DependencyProperty ItemsSourceProperty =
      DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SettingsCardSelect),
        new PropertyMetadata(null));
    public IEnumerable? ItemsSource
    {
      get => (IEnumerable?)GetValue(ItemsSourceProperty);
      set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Путь отображаемого свойства (как в обычном ComboBox).</summary>
    public static readonly DependencyProperty DisplayMemberPathProperty =
      DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(SettingsCardSelect),
        new PropertyMetadata(string.Empty));
    public string DisplayMemberPath
    {
      get => (string)GetValue(DisplayMemberPathProperty);
      set => SetValue(DisplayMemberPathProperty, value);
    }

    /// <summary>Путь значения элемента (если используешь SelectedValue).</summary>
    public static readonly DependencyProperty SelectedValuePathProperty =
      DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(SettingsCardSelect),
        new PropertyMetadata(string.Empty));
    public string SelectedValuePath
    {
      get => (string)GetValue(SelectedValuePathProperty);
      set => SetValue(SelectedValuePathProperty, value);
    }

    /// <summary>Выбранный элемент (TwoWay).</summary>
    public static readonly DependencyProperty SelectedItemProperty =
      DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(SettingsCardSelect),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public object? SelectedItem
    {
      get => GetValue(SelectedItemProperty);
      set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Выбранное значение (TwoWay). Работает совместно с <see cref="SelectedValuePath"/>.</summary>
    public static readonly DependencyProperty SelectedValueProperty =
      DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(SettingsCardSelect),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public object? SelectedValue
    {
      get => GetValue(SelectedValueProperty);
      set => SetValue(SelectedValueProperty, value);
    }

    // ---- Значение по умолчанию ----
    /// <summary>
    /// Значение по умолчанию. Если к моменту загрузки ничего не выбрано,
    /// будет проставлено это значение (в <see cref="SelectedValue"/> при наличии <see cref="SelectedValuePath"/>,
    /// иначе — в <see cref="SelectedItem"/>).
    /// </summary>
    public static readonly DependencyProperty DefaultValueProperty =
      DependencyProperty.Register(nameof(DefaultValue), typeof(object), typeof(SettingsCardSelect),
        new PropertyMetadata(null, OnDefaultValueChanged));
    public object? DefaultValue
    {
      get => GetValue(DefaultValueProperty);
      set => SetValue(DefaultValueProperty, value);
    }

    private static void OnDefaultValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      // Если DefaultValue пришёл после загрузки — применим его, если ещё ничего не выбрано
      ((SettingsCardSelect)d).ApplyDefaultIfNeeded();
    }

    private void ApplyDefaultIfNeeded()
    {
      // если уже есть выбор — ничего не делаем
      if (SelectedItem != null || SelectedValue != null) return;
      if (DefaultValue == null) return;

      // если задан SelectedValuePath — считаем, что DefaultValue — это "значение", иначе — "элемент"
      if (!string.IsNullOrWhiteSpace(SelectedValuePath))
        SelectedValue = DefaultValue;
      else
        SelectedItem = DefaultValue;
    }

    // ---- Событие изменения выбора ----
    /// <summary>
    /// Событие, возникающее при смене выбора в ComboBox. Отдаёт текущее выбранное значение
    /// (<see cref="SelectedValue"/> при установленном <see cref="SelectedValuePath"/>, иначе — <see cref="SelectedItem"/>).
    /// </summary>
    public event System.EventHandler<object?>? ValueChanged;

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      // отдаём наружу «осмысленное» значение
      var payload = !string.IsNullOrWhiteSpace(SelectedValuePath) ? SelectedValue : SelectedItem;
      ValueChanged?.Invoke(this, payload);
    }
  }
}
