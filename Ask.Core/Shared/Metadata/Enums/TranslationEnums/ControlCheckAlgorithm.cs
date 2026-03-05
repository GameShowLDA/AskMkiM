using System.ComponentModel;

namespace Ask.Core.Shared.Metadata.Enums.TranslationEnums
{
  /// <summary>
  /// Алгоритмы выполнения проверок в программах контроля.
  /// </summary>
  public enum ControlCheckAlgorithm
  {
    /// <summary>
    /// Проверка на разобщение методом накапливающего узла.
    /// </summary>
    [Description("Проверка на разобщение методом накапливающего узла")]
    AccumulatingNode,

    /// <summary>
    /// Проверка на разобщение методом полного узла.
    /// </summary>
    [Description("Проверка на разобщение методом полного узла")]
    FullNode,

    /// <summary>
    /// Проверка на разобщение групповым методом.
    /// </summary>
    [Description("Проверка на разобщение групповым методом")]
    Group,

    /// <summary>
    /// Проверка на разобщение относительно первой точки.
    /// </summary>
    [Description("Проверка на разобщение относительно первой точки")]
    DisconnectionRelativeToFirstPoint,

    /// <summary>
    /// Проверка на сообщение относительно первой точки.
    /// </summary>
    [Description("Проверка на сообщение относительно первой точки")]
    MessageRelativeToFirstPoint,

    /// <summary>
    /// Контроль сопротивления относительно первой точки.
    /// </summary>
    [Description("Контроль сопротивления относительно первой точки")]
    ResistanceRelativeToFirstPoint
  }
}
