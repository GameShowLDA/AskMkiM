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
        var existingHotkey = context.FileHotKeys.FirstOrDefault(x => x.ActionName == actionName);
        if (existingHotkey == null)
        {
          var entity = new FileHotkeyEntity
          {
            ActionName = actionName,
            KeyCombination = keyCombo,
            IsEnabled = true,
            Description = GetDescription(actionName)
          };

          context.FileHotKeys.Add(entity);
          continue;
        }

        if (string.IsNullOrWhiteSpace(existingHotkey.Description))
        {
          existingHotkey.Description = GetDescription(actionName);
        }
      }

      context.SaveChanges();
    }

    private static string? GetDescription(string actionName) => actionName switch
    {
      "SwitchUser" => "Сменить пользователя",
      _ => null,
    };
  }
}
