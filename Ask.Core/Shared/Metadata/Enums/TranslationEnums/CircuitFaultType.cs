using System.ComponentModel;

namespace Ask.Core.Shared.Metadata.Enums.TranslationEnums
{
  /// <summary>
  /// Тип состояния цепи при проверке: замыкание или разрыв.
  /// </summary>
  public enum CircuitFaultType
  {
    /// <summary>
    /// Разрыв цепи (отсутствует электрическая связь между точками).
    /// </summary>
    [Description("Разрыв цепи")]
    OpenCircuit,

    /// <summary>
    /// Замыкание / сообщение между цепями (наличие нежелательной связи).
    /// </summary>
    [Description("Замыкание между цепями")]
    ShortCircuit
  }
}
