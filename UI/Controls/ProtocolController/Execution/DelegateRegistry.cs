using static Utilities.DelegateManager;

namespace UI.Controls.ProtocolController.Execution
{
  /// <summary>
  /// Регистр делегатов, используемых для выполнения действий в протоколе.
  /// Хранит стартовый, стоповый, возвратный и предварительный делегаты, а также флаг повтора.
  /// </summary>
  public class DelegateRegistry
  {
    /// <summary>
    /// Делегат, вызываемый для начала действия.
    /// </summary>
    public StartDelegate StartDelegate { get; set; }

    /// <summary>
    /// Делегат, вызываемый для остановки действия.
    /// </summary>
    public StopDelegate StopDelegate { get; set; }

    /// <summary>
    /// Делегат, вызываемый для возврата к предыдущему состоянию.
    /// </summary>
    public ReturnDelegate ReturnDelegate { get; set; }

    /// <summary>
    /// Делегат предварительных действий перед запуском.
    /// </summary>
    public PreActionDelegate PreActionDelegate { get; set; }

    /// <summary>
    /// Флаг, разрешающий повторное выполнение действия.
    /// </summary>
    public bool IsRepeatEnabled { get; set; }
  }
}
