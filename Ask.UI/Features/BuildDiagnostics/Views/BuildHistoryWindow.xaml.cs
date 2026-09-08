using Ask.Core.Services.App;
using System.Text;
using System.Windows;

namespace Ask.UI.Features.BuildDiagnostics.Views;

/// <summary>
/// Показывает происхождение запущенной сборки и встроенную историю коммитов.
/// </summary>
public partial class BuildHistoryWindow : Window
{
  private readonly string _diagnosticText;

  /// <summary>
  /// Создаёт окно сведений о сборке.
  /// </summary>
  /// <param name="buildInfo">Сведения о запущенной сборке приложения.</param>
  public BuildHistoryWindow(ApplicationBuildInfo buildInfo)
  {
    InitializeComponent();

    BuildSummary = $"Версия: {buildInfo.BuildIdentifier}\n"
      + $"Дата сборки: {buildInfo.BuildDate}\n"
      + $"Ревизия: {buildInfo.GitCommit}"
      + (buildInfo.IsDirty ? " (есть незакоммиченные изменения)" : string.Empty);
    CommitHistory = FormatCommitHistory(buildInfo.RecentCommits);
    Commits = buildInfo.RecentCommits;
    _diagnosticText = $"{BuildSummary}\n\nПоследние коммиты на момент сборки:\n{CommitHistory}";
    DataContext = this;
  }

  /// <summary>Основные сведения о запущенной сборке.</summary>
  public string BuildSummary { get; }

  /// <summary>Отформатированная история последних коммитов.</summary>
  public string CommitHistory { get; }

  /// <summary>Последние коммиты, доступные на момент сборки.</summary>
  public IReadOnlyList<BuildCommitInfo> Commits { get; }

  private static string FormatCommitHistory(IReadOnlyList<BuildCommitInfo> commits)
  {
    if (commits.Count == 0)
    {
      return "История коммитов недоступна для этой сборки.";
    }

    var result = new StringBuilder();
    foreach (BuildCommitInfo commit in commits)
    {
      result.Append(commit.Hash)
        .Append("  ")
        .Append(commit.Date)
        .Append("  ")
        .AppendLine(commit.Subject);
    }

    return result.ToString().TrimEnd();
  }

  private void CopyButton_Click(object sender, RoutedEventArgs e)
  {
    Clipboard.SetText(_diagnosticText);
  }

  private void CloseButton_Click(object sender, RoutedEventArgs e)
  {
    Close();
  }

  private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
  {
    if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
    {
      DragMove();
    }
  }
}
