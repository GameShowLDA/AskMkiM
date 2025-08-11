using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseConfiguration.Services.Hotkey.Defaults
{
  using System.Collections.Generic;

  namespace DataBaseConfiguration.Services.Hotkey.Defaults
  {
    /// <summary>
    /// Содержит список горячих клавиш по умолчанию для управления файлами.
    /// </summary>
    internal static class FileHotkeyDefaults
    {
      /// <summary>
      /// Словарь горячих клавиш по умолчанию. Ключ — логическое имя действия, значение — строковое представление комбинации клавиш.
      /// </summary>
      internal static readonly Dictionary<string, string> Defaults = new()
      {
        // Открытие архива
        { "OpenArchive", "Ctrl+Shift+O" },

        // Работа с файлами
        { "OpenFile", "Ctrl+O" },
        { "CreateNewFile", "Ctrl+N" },
        { "SaveFile", "Ctrl+S" },
        { "SaveFileAs", "Ctrl+Shift+S" },
        { "PrintFile", "Ctrl+P" },
        { "SearchFile", "Ctrl+F" },
        { "CompareFile", "Ctrl+K" },
        { "Build", "Ctrl+F9" },
        { "Run", "Ctrl+F5" },

        // Выход
        { "ExitApplication", "Alt+F4" }
      };
    }
  }

}