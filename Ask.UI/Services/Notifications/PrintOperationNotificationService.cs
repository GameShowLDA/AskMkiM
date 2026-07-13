using Ask.Core.Services.FilesUtility;

namespace Ask.UI.Services.Notifications
{
  public static class PrintOperationNotificationService
  {
    private const string NotificationTitle = "Печать документа";

    public static async Task PrintTextAsync(string text, string printJobTitle)
    {
      try
      {
        var result = await TextPrintHelper.PrintTextAsync(text, printJobTitle).ConfigureAwait(false);

        switch (result.Status)
        {
          case TextPrintStatus.Completed:
            OperationNotificationService.ShowSuccess(NotificationTitle, "Документ отправлен на печать.");
            break;

          case TextPrintStatus.Failed:
            OperationNotificationService.ShowError(
              NotificationTitle,
              $"Не удалось выполнить печать: {result.ErrorMessage}");
            break;
        }
      }
      catch (Exception ex)
      {
        OperationNotificationService.ShowError(NotificationTitle, $"Не удалось выполнить печать: {ex.Message}");
      }
    }
  }
}
