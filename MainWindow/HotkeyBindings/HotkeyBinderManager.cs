using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DataBaseConfiguration;
using static Utilities.LoggerUtility;

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

      MenuHotkeyBinder.Attach(window, dataContext, DataBaseConfig.Context);

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