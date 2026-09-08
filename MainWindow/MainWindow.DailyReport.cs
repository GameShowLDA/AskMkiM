using Ask.Core.Services.App;
using Ask.Core.Services.Protocols;
using Ask.Diagnostics.Configuration;
using Ask.Diagnostics.Services;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using UI.Controls;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram;

public partial class MainWindow
{
  private async void DailyReportButton_Click(object sender, RoutedEventArgs e)
  {
    string historyRoot = ExecutionProtocolHistoryService.GetHistoryDirectory();
    string logsRoot = Path.Combine(AppContext.BaseDirectory, "logs");
    var picker = new DailyReportDateWindow(historyRoot, logsRoot) { Owner = this };
    if (picker.ShowDialog() != true) return;
    var dialog = new SaveFileDialog
    {
      Title = "Сохранить отчёт за день", Filter = "ZIP-архив (*.zip)|*.zip",
      FileName = $"AskMkiM_{picker.SelectedDate:yyyy-MM-dd}.zip", DefaultExt = ".zip",
      AddExtension = true, OverwritePrompt = true
    };
    if (dialog.ShowDialog(this) != true) return;
    DailyReportButton.IsEnabled = false;
    try
    {
      var options = ServiceLocator.Services.GetRequiredService<IOptions<CrashPackageOptions>>().Value;
      var date = picker.SelectedDate;
      var issues = await Task.Run(async () =>
      {
        NLog.LogManager.Flush(TimeSpan.FromSeconds(2));
        return await new DailyReportService().CreateAsync(date, dialog.FileName, historyRoot,
          logsRoot,
          Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.Path)));
      });
      NotificationHostService.Instance.Show(
        "Отчёт за день",
        $"Архив сохранён:{Environment.NewLine}{dialog.FileName}" +
        (issues.Count > 0
          ? $"{Environment.NewLine}Замечаний: {issues.Count}. Подробности в report.json внутри ZIP."
          : string.Empty),
        issues.Count > 0 ? NotificationType.Warning : NotificationType.Success);
    }
    catch (Exception ex)
    {
      LogException("Ошибка формирования отчёта за день", ex);
      NotificationHostService.Instance.Show(
        "Не удалось сформировать отчёт",
        ex.Message,
        NotificationType.Error);
    }
    finally
    {
      DailyReportButton.IsEnabled = true;
    }
  }
}
