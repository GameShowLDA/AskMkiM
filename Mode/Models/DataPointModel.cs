using NewCore.Base.Interface.Main;
using UI.Components.Invoke;

namespace Mode.Models
{
  /// <summary>
  /// Представляет базовую модель данных для измерения, содержащую информацию о двух точках измерения,
  /// а также элементы управления для ввода данных и управления отображением.
  /// </summary>
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
    /// Используется для визуального выделения и группировки элемента ввода данных первой точки.
    /// </summary>
    internal InvokeBorder FirstPointBorder { get; set; } = new InvokeBorder();

    /// <summary>
    /// Граница для второй точки измерения.
    /// Используется для визуального выделения и группировки элемента ввода данных второй точки.
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

    /// <summary>
    /// Модель менеджера шасси, используемая для работы с устройством.
    /// </summary>
    internal IChassisManager ManagerShassy { get; set; }

    /// <summary>
    /// Модель первого модуля реле для подключения измерительных точек.
    /// </summary>
    internal IRelaySwitchModule FirstModuleRelayControl { get; set; }

    /// <summary>
    /// Модель второго модуля реле для подключения измерительных точек.
    /// </summary>
    internal IRelaySwitchModule LastModuleRelayControl { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DataPointModel"/> с заданными элементами управления.
    /// </summary>
    /// <param name="firstBorder">Граница для первой точки измерения.</param>
    /// <param name="secondBorder">Граница для второй точки измерения.</param>
    /// <param name="firstData">Поле для ввода данных первой точки измерения.</param>
    /// <param name="secondData">Поле для ввода данных второй точки измерения.</param>
    internal DataPointModel(InvokeBorder firstBorder, InvokeBorder secondBorder, InvokeTextBox firstData, InvokeTextBox secondData)
    {
      FirstPointBorder = firstBorder;
      LastPointBorder = secondBorder;
      FirstPointData = firstData;
      LastPointData = secondData;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DataPointModel"/> с элементами управления по умолчанию.
    /// </summary>
    internal DataPointModel()
    {
      FirstPointBorder = new InvokeBorder();
      LastPointBorder = new InvokeBorder();
      FirstPointData = new InvokeTextBox();
      LastPointData = new InvokeTextBox();
    }
  }
}
