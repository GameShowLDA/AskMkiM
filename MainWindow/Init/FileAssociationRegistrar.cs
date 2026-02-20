using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.Init
{
  /// <summary>
  /// Регистрирует ассоциации поддерживаемых расширений в текущем профиле пользователя.
  /// </summary>
  internal static class FileAssociationRegistrar
  {
    private const string ClassesRoot = @"Software\Classes";
    private const string ProgId = "AskMkiM.Document";

    private const uint ShcneAssocChanged = 0x08000000;
    private const uint ShcnfIdList = 0x0000;

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    internal static void RegisterCurrentUserAssociations()
    {
      try
      {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
          return;
        }

        var command = $"\"{executablePath}\" \"%1\"";
        var changed = false;

        using var classesKey = Registry.CurrentUser.CreateSubKey(ClassesRoot);
        if (classesKey == null)
        {
          return;
        }

        using (var progIdKey = classesKey.CreateSubKey(ProgId))
        {
          changed |= SetDefaultValue(progIdKey, "AskMkiM document");
        }

        using (var commandKey = classesKey.CreateSubKey($@"{ProgId}\shell\open\command"))
        {
          changed |= SetDefaultValue(commandKey, command);
        }

        foreach (var extension in SupportedFileExtensions.ExplorerAssociationExtensions)
        {
          using var extensionKey = classesKey.CreateSubKey(extension);
          changed |= SetDefaultValue(extensionKey, ProgId);
        }

        if (changed)
        {
          SHChangeNotify(ShcneAssocChanged, ShcnfIdList, IntPtr.Zero, IntPtr.Zero);
        }
      }
      catch (Exception ex)
      {
        LogException("Не удалось зарегистрировать ассоциации файлов.", ex);
      }
    }

    private static bool SetDefaultValue(RegistryKey? key, string value)
    {
      if (key == null)
      {
        return false;
      }

      var current = key.GetValue(string.Empty) as string;
      if (string.Equals(current, value, StringComparison.Ordinal))
      {
        return false;
      }

      key.SetValue(string.Empty, value);
      return true;
    }
  }
}
