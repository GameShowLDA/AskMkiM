using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Components.Invoke
{
  public class InvokeTextBox : TextBox
  {
    public new string Text
    {
      get
      {
        string text = string.Empty;
        Application.Current.Dispatcher.Invoke(() => text = base.Text);
        return text;
      }
      set
      {
        Application.Current.Dispatcher.Invoke(() => base.Text = value);
      }
    }

    public new Brush BorderBrush
    {
      get
      {
        Brush brush = null;
        Application.Current.Dispatcher.Invoke(() => brush = base.BorderBrush);
        return brush;
      }
      set
      {
        Application.Current.Dispatcher.Invoke(() => base.BorderBrush = value);
      }
    }

    public new bool IsReadOnly
    {
      get
      {
        bool readOnly = false;
        Application.Current.Dispatcher.Invoke(() => readOnly = base.IsReadOnly);
        return readOnly;
      }
      set
      {
        Application.Current.Dispatcher.Invoke(() => base.IsReadOnly = value);
      }
    }
  }
}
