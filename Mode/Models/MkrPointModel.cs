using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Models
{
  public class MkrPointModel : INotifyPropertyChanged
  {
    private bool _a;
    private bool _b;
    public short PointNumber { get; private set; }

    public bool changeFlag { get; private set; }

    public bool A
    {
      get => _a;
      set
      {
        if (_a != value)
        {
          changeFlag = true;
          _a = value;
          OnPropertyChanged(nameof(A));
        }
      }
    }

    public bool B
    {
      get => _b;
      set
      {
        if (_b != value)
        {
          changeFlag = true;
          _b = value;
          OnPropertyChanged(nameof(B));
        }
      }
    }

    public MkrPointModel(short number, bool A = false, bool B = false)
    {
      PointNumber = number;
      _a = A;
      _b = B;
      changeFlag = false;
    }

    public void ResetChangeFlag() => changeFlag = false;

    public void ResetAll()
    {
      _a = false;
      _b = false;
      changeFlag = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
  }
}
