using Ask.DataBase.Engine.Static.Settings;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.HotkeyBindings
{
  /// <summary>
  /// Центральный менеджер привязки всех горячих клавиш в главном окне.
  /// </summary>
  public static class HotkeyBinderManager
  {
    /// <summary>
    /// Выполняет привязку всех поддерживаемых горячих клавиш в интерфейсе.
    /// </summary>
    /// <param name="window">Главное окно, в которое добавляются бинды.</param>
    /// <param name="dataContext">Контекст данных, содержащий команды.</param>
    public static void AttachAllHotkeys(MainWindow window, object dataContext)
    {
      if (dataContext == null)
      {
        LogWarning("❗ DataContext не установлен — горячие клавиши не будут привязаны.");
        return;
      }

      var hotkeys = FileHotkeys.GetAllAsync().GetAwaiter().GetResult();

      MenuHotkeyBinder.Attach(window, dataContext, hotkeys);
      UniversalHotkeyBinder.Attach(window, hotkeys);

      window.PreviewKeyDown += (s, e) =>
      {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
            && e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl)
        {
          string combo = $"CTRL + {e.Key}";
          window.messageHandler?.SetInfoMessage($"Нажата комбинация: {combo}", clearMessage: true);
        }
      };
    }
  }
}
