using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Models
{
  public class RelayModel : INotifyPropertyChanged
  {
    public short RelayNum { get; private set; }
    private bool _isOn;
    public bool IsOn
    {
      get => _isOn;
      set
      {
        if (_isOn != value)
        {
          _isOn = value;
          OnPropertyChanged(nameof(IsOn));
        }
      }
    }
    public RelayModel(short number)
    {
      RelayNum = number;
    }
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
  }
}
