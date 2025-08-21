using AppConfiguration.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Controls.Settings
{
  internal interface ISettingsManager<T> where T : class
  {
    public event EventHandler ChangedData;
    public T GetActiveModel();
  }
}
