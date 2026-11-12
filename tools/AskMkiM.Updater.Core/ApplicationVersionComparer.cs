using AskMkiM.Updater.Core.Models;

namespace AskMkiM.Updater.Core;

/// <summary>
/// Сравнивает установленную сборку с пакетом обновления.
/// </summary>
public static class ApplicationVersionComparer
{
  /// <summary>
  /// Проверяет, можно ли устанавливать пакет поверх указанной версии.
  /// </summary>
  /// <returns>Пустая строка либо причина, по которой установка запрещена.</returns>
  public static string GetBlockingReason(
    ApplicationVersionInfo current,
    UpdateManifest update)
  {
    if (string.Equals(current.BuildIdentifier, update.BuildIdentifier, StringComparison.OrdinalIgnoreCase))
    {
      return "Эта сборка AskMkiM уже установлена.";
    }

    if (TryParseVersion(current.Version, out Version? currentVersion)
        && TryParseVersion(update.Version, out Version? updateVersion)
        && updateVersion < currentVersion)
    {
      return $"Пакет версии {update.Version} старее установленной версии {current.Version}.";
    }

    return string.Empty;
  }

  private static bool TryParseVersion(string value, out Version? version)
  {
    string normalized = value.Split('+', '-', StringSplitOptions.RemoveEmptyEntries)[0];
    return Version.TryParse(normalized, out version);
  }
}
