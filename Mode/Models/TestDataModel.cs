using UI.Components.Invoke;

namespace Mode.Models
{
  /// <summary>
  /// Модель тестовых данных, расширяющая базовую модель измерения точек (DataPointModel),
  /// и добавляющая свойства для управления релейными модулями.
  /// </summary>
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

    /// <summary>
    /// Список моделей релейных модулей.
    /// </summary>
    internal List<Core.ModuleRelayControl.Model> ModuleRelayControls = new List<Core.ModuleRelayControl.Model>();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TestDataModel"/> с заданными элементами управления.
    /// </summary>
    /// <param name="firstBorder">Граница для первой точки измерения.</param>
    /// <param name="secondBorder">Граница для второй точки измерения.</param>
    /// <param name="firstData">Поле для ввода данных первой точки измерения.</param>
    /// <param name="secondData">Поле для ввода данных второй точки измерения.</param>
    internal TestDataModel(InvokeBorder firstBorder, InvokeBorder secondBorder, InvokeTextBox firstData, InvokeTextBox secondData)
      : base(firstBorder, secondBorder, firstData, secondData)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TestDataModel"/> с элементами управления по умолчанию.
    /// </summary>
    internal TestDataModel() : base()
    {
    }
  }
}
