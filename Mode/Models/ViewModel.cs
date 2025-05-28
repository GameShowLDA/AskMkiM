using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Mode.Models
{
  /// <summary>
  /// ViewModel для привязки элементов ComboBox и управления выбором.
  /// Реализует <see cref="INotifyPropertyChanged"/> для уведомления UI об изменениях свойств.
  /// </summary>
  public class ViewModel : INotifyPropertyChanged
  {
    /// <summary>
    /// Поле для хранения текущего выбранного элемента ComboBox.
    /// </summary>
    private string _selectedComboBoxItem;

    /// <summary>
    /// Коллекция элементов для отображения в ComboBox.
    /// Первый элемент — "<пусто>".
    /// </summary>
    public ObservableCollection<string> ComboBoxItems { get; } =
        new ObservableCollection<string> { "<пусто>" };

    /// <summary>
    /// Выбранный элемент ComboBox.
    /// При изменении вызывается событие <see cref="PropertyChanged"/>.
    /// </summary>
    public string SelectedComboBoxItem
    {
      get => _selectedComboBoxItem;
      set
      {
        if (_selectedComboBoxItem != value)
        {
          _selectedComboBoxItem = value;
          OnPropertyChanged(nameof(SelectedComboBoxItem));
        }
      }
    }

    /// <summary>
    /// Конструктор ViewModel.
    /// Устанавливает значение <see cref="SelectedComboBoxItem"/> в первый элемент <see cref="ComboBoxItems"/>.
    /// </summary>
    public ViewModel()
    {
      _selectedComboBoxItem = ComboBoxItems[0];
    }

    /// <summary>
    /// Событие, возникающее при изменении значения свойства.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Вызывает событие <see cref="PropertyChanged"/> для указанного свойства.
    /// </summary>
    /// <param name="propertyName">Имя свойства, которое изменилось.</param>
    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}