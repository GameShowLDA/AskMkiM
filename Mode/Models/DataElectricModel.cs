using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Components.Invoke;

namespace Mode.Models
{
  internal class DataElectricModel : DataPointModel
  {
    /// <summary>
    /// Граница для измерения сопротивления.
    /// </summary>
    internal InvokeBorder ElectricParameterBorder { get; set; } = new InvokeBorder();

    /// <summary>
    /// Поле для ввода данных измерения сопротивления.
    /// </summary>
    internal InvokeTextBox ElectricParameterData { get; set; } = new InvokeTextBox();
    internal DataElectricModel(InvokeBorder firstBorder, InvokeBorder secondBorder, InvokeBorder electricBorder, InvokeTextBox firstData, InvokeTextBox secondData, InvokeTextBox electricData)
    : base(firstBorder, secondBorder, firstData, secondData)
    {
      ElectricParameterBorder = electricBorder;
      ElectricParameterData = electricData;
    }

    internal DataElectricModel() : base()
    {
      ElectricParameterBorder = new InvokeBorder();
      ElectricParameterData = new InvokeTextBox();
    }
  }
}
