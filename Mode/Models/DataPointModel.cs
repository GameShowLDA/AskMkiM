using UI.Components.Invoke;

namespace Mode.Models
{
  internal class DataPointModel
  {
    /// <summary>
    /// Первая точка измерения.
    /// </summary>
    internal PointModel FirstPointModel { get; set; }

    /// <summary>
    /// Вторая точка измерения.
    /// </summary>
    internal PointModel LastPointModel { get; set; }

    /// <summary>
    /// Граница для первой точки измерения.
    /// </summary>
    internal InvokeBorder FirstPointBorder { get; set; } = new InvokeBorder();

    /// <summary>
    /// Граница для второй точки измерения.
    /// </summary>
    internal InvokeBorder LastPointBorder { get; set; } = new InvokeBorder();

    /// <summary>
    /// Поле для ввода данных первой точки измерения.
    /// </summary>
    internal InvokeTextBox FirstPointData { get; set; } = new InvokeTextBox();

    /// <summary>
    /// Поле для ввода данных последней точки измерения.
    /// </summary>
    internal InvokeTextBox LastPointData { get; set; } = new InvokeTextBox();

    internal Core.ManagerShassy.Model ManagerShassy { get; set; }

    internal Core.ModuleRelayControl.Model FirstModuleRelayControl { get; set; }
    internal Core.ModuleRelayControl.Model LastModuleRelayControl { get; set; }
    internal DataPointModel(InvokeBorder firstBorder, InvokeBorder secondBorder, InvokeTextBox firstData, InvokeTextBox secondData)
    {
      FirstPointBorder = firstBorder;
      LastPointBorder = secondBorder;

      FirstPointData = firstData;
      LastPointData = secondData;
    }

    internal DataPointModel()
    {
      FirstPointBorder = new InvokeBorder();
      LastPointBorder = new InvokeBorder();

      FirstPointData = new InvokeTextBox();
      LastPointData = new InvokeTextBox();
    }

  }
}
