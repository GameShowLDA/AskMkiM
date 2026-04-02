using Ask.Core.Shared.Entity.UI;
using Ask.Core.Shared.Metadata.Dictonary;
using DataBaseConfiguration.Context;

namespace DataBaseConfiguration.Services.Hotkey.Defaults
{
  /// <summary>
  /// Выполняет добавление горячих клавиш по умолчанию в базу данных, если они отсутствуют.
  /// </summary>
  internal static class FileHotkeySeeder
  {
    /// <summary>
    /// Выполняет инициализацию горячих клавиш по умолчанию.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public static void Seed(AppDbContext context)
    {
      foreach (var (actionName, keyCombo) in UiDictonary.DefaultsHotKeys)
      {
        bool exists = context.FileHotKeys.Any(x => x.ActionName == actionName);
        if (!exists)
        {
          var entity = new FileHotkeyEntity
          {
            ActionName = actionName,
            KeyCombination = keyCombo,
            IsEnabled = true,
            Description = null
          };

          context.FileHotKeys.Add(entity);
        }
      }

      context.SaveChanges();
    }
  }
}