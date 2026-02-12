namespace Ask.Core.Shared.Metadata.Dictonary

{
  /// <summary>
  /// Содержит список горячих клавиш по умолчанию для управления файлами.
  /// </summary>
  public static class UiDictonary
  {
    /// <summary>
    /// Словарь горячих клавиш по умолчанию. Ключ — логическое имя действия, значение — строковое представление комбинации клавиш.
    /// </summary>
    public static readonly Dictionary<string, string> DefaultsHotKeys = new()
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
        { "SearchReplace", "Ctrl+H" },
        { "CompareFile", "Ctrl+K" },
        { "Build", "F9" },
        { "Run", "Ctrl+F5" },
        { "RunStepByStepMode", "Ctrl+F10" },

        // Включение питания
        { "Power", "Ctrl+Shift+P" },

        // Выход
        { "ExitApplication", "Alt+F4" }
      };
  }
}
