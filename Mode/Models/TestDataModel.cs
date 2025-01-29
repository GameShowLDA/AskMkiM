using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Components.Invoke;

namespace Mode.Models
{
  internal class TestDataModel : DataPointModel
  {
    /// <summary>
    /// Модель первого релейного управления.
    /// </summary>
    internal Core.ModuleRelayControl.Model FirstModelRelayControl { get; set; }

    /// <summary>
    /// Модель второго релейного управления.
    /// </summary>
    internal Core.ModuleRelayControl.Model SecondModelRelayControl { get; set; }

    internal List<Core.ModuleRelayControl.Model> ModuleRelayControls = new List<Core.ModuleRelayControl.Model>();
    internal TestDataModel(InvokeBorder firstBorder, InvokeBorder secondBorder, InvokeTextBox firstData, InvokeTextBox secondData)
    : base(firstBorder, secondBorder, firstData, secondData)
    {
    }

    internal TestDataModel() : base()
    {

    }
  }
}
