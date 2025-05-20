using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UI.Components
{
  /// <summary>
  /// Элемент управления для выбора одного устройства из предоставленного списка.
  /// Отображает список на основе переданных подписей и возвращает выбранный элемент.
  /// </summary>
  public partial class ChoiceDevice : UserControl, INotifyPropertyChanged
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ChoiceDevice"/>.
    /// </summary>
    public ChoiceDevice()
    {
      InitializeComponent();
      DataContext = this;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Уведомляет об изменении значения свойства.
    /// </summary>
    /// <param name="name">Имя измененного свойства.</param>
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Свойство зависимостей для источника данных, содержащего список выбираемых объектов.
    /// </summary>
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ChoiceDevice), new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>
    /// Список доступных объектов для выбора.
    /// </summary>
    public IEnumerable ItemsSource
    {
      get => (IEnumerable)GetValue(ItemsSourceProperty);
      set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Обработчик изменения источника данных. Обновляет визуальный список.
    /// </summary>
    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ChoiceDevice control)
      {
        control.RefreshList();
      }
    }

    /// <summary>
    /// Свойство зависимостей для выбранного объекта.
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(ChoiceDevice), new PropertyMetadata(null, OnSelectedItemChanged));

    /// <summary>
    /// Текущий выбранный объект из списка.
    /// </summary>
    public object SelectedItem
    {
      get => GetValue(SelectedItemProperty);
      set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Обработчик изменения выбранного элемента. Обновляет отображение и вызывает событие.
    /// </summary>
    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ChoiceDevice control)
      {
        control.UpdateDisplayText();
        control.DeviceSelected?.Invoke(e.NewValue);
      }
    }

    /// <summary>
    /// Свойство зависимостей, содержащее список подписей, отображаемых в списке выбора.
    /// Индекс подписи должен соответствовать индексу объекта в <see cref="ItemsSource"/>.
    /// </summary>
    public static readonly DependencyProperty DisplayFieldsProperty =
        DependencyProperty.Register(nameof(DisplayFields), typeof(List<string>), typeof(ChoiceDevice), new PropertyMetadata(null, OnDisplayFieldsChanged));

    /// <summary>
    /// Список строк, отображаемых в качестве подписей к каждому объекту.
    /// Количество должно соответствовать количеству элементов в ItemsSource.
    /// </summary>
    public List<string> DisplayFields
    {
      get => (List<string>)GetValue(DisplayFieldsProperty);
      set => SetValue(DisplayFieldsProperty, value);
    }

    /// <summary>
    /// Обработчик изменения <see cref="DisplayFields"/>. Перестраивает список и обновляет отображение.
    /// </summary>
    private static void OnDisplayFieldsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ChoiceDevice control)
      {
        control.RefreshList();
        control.UpdateDisplayText();
      }
    }

    // 📣 Отображаемый текст
    private string _selectedDisplayText = "Выберите устройство";

    /// <summary>
    /// Текст, отображаемый на кнопке до и после выбора.
    /// </summary>
    public string SelectedDisplayText
    {
      get => _selectedDisplayText;
      set { _selectedDisplayText = value; OnPropertyChanged(nameof(SelectedDisplayText)); }
    }

    /// <summary>
    /// Обновляет отображаемый текст на основе текущего выбора и списка подписей.
    /// </summary>
    private void UpdateDisplayText()
    {
      if (SelectedItem == null || ItemsSource == null || DisplayFields == null)
      {
        SelectedDisplayText = "Выберите устройство";
        return;
      }

      var items = ItemsSource.Cast<object>().ToList();
      int index = items.IndexOf(SelectedItem);

      if (index >= 0 && index < DisplayFields.Count)
        SelectedDisplayText = DisplayFields[index];
      else
        SelectedDisplayText = SelectedItem.ToString(); // fallback
    }

    /// <summary>
    /// Перестраивает визуальный список элементов, основываясь на <see cref="ItemsSource"/> и <see cref="DisplayFields"/>.
    /// </summary>
    private void RefreshList()
    {
      deviceList.Items.Clear();

      if (ItemsSource == null || DisplayFields == null)
        return;

      var items = ItemsSource.Cast<object>().ToList();

      for (int i = 0; i < items.Count; i++)
      {
        string label = i < DisplayFields.Count ? DisplayFields[i] : items[i].ToString();
        deviceList.Items.Add(label);
      }
    }

    /// <summary>
    /// Обработчик выбора элемента в списке. Назначает выбранный элемент в <see cref="SelectedItem"/>.
    /// </summary>
    private void deviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (deviceList.SelectedIndex >= 0 && ItemsSource != null)
      {
        var selected = ItemsSource.Cast<object>().ElementAt(deviceList.SelectedIndex);
        SelectedItem = selected;
        toggleButton.IsChecked = false;
      }
    }

    /// <summary>
    /// Событие, возникающее при выборе нового устройства.
    /// </summary>
    public event Action<object> DeviceSelected;
  }
}
