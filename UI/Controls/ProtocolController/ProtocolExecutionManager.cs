using System.Windows;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;

namespace UI.Controls.ProtocolController
{
  /// <summary>
  /// Управляет запуском, остановкой и возвратом выполнения через делегаты.
  /// </summary>
  public class ProtocolExecutionManager
  {
    private UIElement _mainWindow;
    private StartDelegate _startDelegate;
    private StopDelegate _stopDelegate;
    private ReturnDelegate _returnDelegate;
    private PreActionDelegate _preActionDelegate;
    private bool _isRepeatEnabled;

    /// <summary>
    /// Устанавливает основные настройки выполнения действий.
    /// </summary>
    public void SetSettings(ProtocolExecutionSettings protocolExecutionSettings)
    {
      try
      {
        _mainWindow = protocolExecutionSettings.MainWindow;
        _startDelegate = protocolExecutionSettings.StartDelegate ?? throw new ArgumentNullException(nameof(protocolExecutionSettings.StartDelegate));
        _stopDelegate = protocolExecutionSettings.StopDelegate;
        _returnDelegate = protocolExecutionSettings.ReturnDelegate;
        _preActionDelegate = protocolExecutionSettings.PreActionDelegate;

        _isRepeatEnabled = protocolExecutionSettings.IsRepeatEnabled || protocolExecutionSettings.ReturnDelegate != null;
      }
      catch (Exception ex)
      {
        LogException("Ошибка установки настроек выполнения", ex);
        throw;
      }
    }
  }
}
