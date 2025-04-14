using UI.Components.Invoke;

namespace Mode.Models
{
  /// <summary>
  /// Представляет модель данных для измерения электрического параметра,
  /// расширяя базовую модель измерения точек (DataPointModel) и добавляя элементы управления для измерения сопротивления.
  /// </summary>
  internal class DataElectricModel : DataPointModel
  {
    /// <summary>
    /// Граница, используемая для отображения и ввода данных измерения сопротивления.
    /// </summary>
    internal InvokeBorder ElectricParameterBorder { get; set; } = new InvokeBorder();

    /// <summary>
    /// Поле для ввода данных измерения сопротивления.
    /// </summary>
    internal InvokeTextBox ElectricParameterData { get; set; } = new InvokeTextBox();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DataElectricModel"/> с заданными элементами управления.
    /// </summary>
    /// <param name="firstBorder">Граница для первой точки измерения.</param>
    /// <param name="secondBorder">Граница для второй точки измерения.</param>
    /// <param name="electricBorder">Граница для измерения сопротивления.</param>
    /// <param name="firstData">Поле для ввода данных первой точки измерения.</param>
    /// <param name="secondData">Поле для ввода данных второй точки измерения.</param>
    /// <param name="electricData">Поле для ввода данных измерения сопротивления.</param>
    internal DataElectricModel(InvokeBorder firstBorder, InvokeBorder secondBorder, InvokeBorder electricBorder, InvokeTextBox firstData, InvokeTextBox secondData, InvokeTextBox electricData)
      : base(firstBorder, secondBorder, firstData, secondData)
    {
      ElectricParameterBorder = electricBorder;
      ElectricParameterData = electricData;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DataElectricModel"/> с элементами управления по умолчанию.
    /// </summary>
    internal DataElectricModel() : base()
    {
      ElectricParameterBorder = new InvokeBorder();
      ElectricParameterData = new InvokeTextBox();
    }
  }
}
