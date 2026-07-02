using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using MainWindowProgram.Init;
using MainWindowProgram.Services;

namespace MainWindowProgram.Engine
{
  internal class CommandLineParser
  {
    internal void ProcessCommandLineArgs()
    {
      ResetDefaults();

      var filesToOpen = new List<string>();
      var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var raw in App.CommandLineArgs)
      {
        if (IsSwitch(raw, "admin"))
        {
          continue;
        }

        if (IsSwitch(raw, "debug"))
        {
          AdminConfig.SetDebugRights(true).ConfigureAwait(false);
        }
        else if (SupportedFileExtensions.TryResolveSupportedExistingFile(raw, out var filePath))
        {
          if (seenPaths.Add(filePath))
          {
            filesToOpen.Add(filePath);
          }
        }
        else
        {
          HandleUnknownArgument(raw);
        }
      }

      OpenRequestedFiles(filesToOpen);
    }

    private static void ResetDefaults()
    {
      AdminConfig.SetAdminRights(RoleAuthorizationConfig.CurrentRole == Ask.Core.Shared.Metadata.Enums.RoleEnums.RoleType.Root);
    }

    private static bool IsSwitch(string rawArg, string switchName)
    {
      if (string.IsNullOrWhiteSpace(rawArg))
      {
        return false;
      }

      var token = rawArg.Trim().TrimStart('-', '/');
      return token.Equals(switchName, StringComparison.OrdinalIgnoreCase);
    }

    private static void HandleUnknownArgument(string arg)
    {
      Console.WriteLine($"[Warning] Неизвестный аргумент: {arg}");
    }

    private static void OpenRequestedFiles(IEnumerable<string> filesToOpen)
    {
      foreach (var filePath in filesToOpen)
      {
        FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(filePath);
      }
    }
  }
}
