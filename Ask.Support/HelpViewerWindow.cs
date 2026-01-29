using Photino.NET;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Support
{
  public static class HelpViewerWindow
  {
    private static PhotinoWindow? _helpWindow;
    public static bool _IsClose { get; private set; } = true;
    public static void Load(string page) => _helpWindow?.Load($"http://localhost:{HelpServer.Port}" + page);
    public static void Show() => _helpWindow?.WaitForClose();
    public static void Close() => _helpWindow?.Close();
    public static void SetSettings()
    {
      _helpWindow
        .SetTitle("Справочная система")
        .SetUseOsDefaultLocation(true)
        .SetSize(1024, 768)
        .SetMinSize(600, 600)
        .RegisterWebMessageReceivedHandler((sender, message) =>
        {
          LogDebug(
            $"A JavaScript message from the HELP-system:\n" +
            $"Object: {sender}\n" +
            $"Message: {message}"
            );
        }
        )
        .Center();

      _helpWindow.RegisterWindowClosingHandler((sender, e) =>
      {
        _IsClose = true;
        _helpWindow = null;
        return false;
      });
    }
    public static void LoadAndShow(string page)
    {
      if (_helpWindow == null)
      {
        _helpWindow = new PhotinoWindow();
        SetSettings();
        _IsClose = true;
      }

      Load(page);

      if (!_IsClose) return;
      try
      {
        _IsClose = false;
        Show();
      }
      catch (Exception ex)
      {
        LogError($"Ошмбка в HelpViewerWindow\n{ex}");
      }
    }
  }
}