using System;
using System.Threading.Tasks;
using System.Windows;
using static Utilities.DelegateManager;

namespace UI.Controls.ProtocolController
{
  /// <summary>
  /// Модель настроек выполнения с делегатами и параметрами.
  /// </summary>
  public class ProtocolExecutionSettings
  {
    /// <summary>
    /// Главное окно, к которому привязано выполнение.
    /// </summary>
    public UIElement MainWindow { get; set; }

    /// <summary>
    /// Делегат запуска основной логики.
    /// </summary>
    public StartDelegate StartDelegate { get; set; }

    /// <summary>
    /// Делегат остановки выполнения (опционально).
    /// </summary>
    public StopDelegate StopDelegate { get; set; }

    /// <summary>
    /// Делегат возврата к предыдущему состоянию (опционально).
    /// </summary>
    public ReturnDelegate ReturnDelegate { get; set; }

    /// <summary>
    /// Делегат предварительных действий перед запуском (опционально).
    /// </summary>
    public PreActionDelegate PreActionDelegate { get; set; }

    /// <summary>
    /// Флаг, разрешающий повторное выполнение.
    /// </summary>
    public bool IsRepeatEnabled { get; set; }

    /// <summary>
    /// Инициализация с обязательным делегатом запуска.
    /// </summary>
    public ProtocolExecutionSettings(UIElement mainWindow, StartDelegate startDelegate)
    {
      MainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
      StartDelegate = startDelegate ?? throw new ArgumentNullException(nameof(startDelegate));
    }

    /// <summary>
    /// Пустой конструктор, если требуется конфигурация вручную.
    /// </summary>
    public ProtocolExecutionSettings() { }
  }
}
