using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Models
{
  public class ViewModel : INotifyPropertyChanged
  {
    private string _selectedComboBoxItem;
    public ObservableCollection<string> ComboBoxItems { get; set; }

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

    public ViewModel()
    {
      ComboBoxItems = new ObservableCollection<string>
            {
                "<пусто>",
                "Элемент 1",
                "Элемент 2",
                "Элемент 3",
                "Элемент 4"
            };
      _selectedComboBoxItem = "<пусто>";
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}