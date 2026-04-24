using Ask.Core.Shared.DTO.TextEditor;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Controls.FileCompare
{
  public partial class FileCompareSelectionControl : UserControl, INotifyPropertyChanged
  {
    private readonly Func<IReadOnlyList<OpenTextEditorDescriptor>> _openFilesProvider;
    private CancellationTokenSource? _compareCancellation;
    private OpenTextEditorDescriptor? _selectedSourceFile;
    private FileComparisonResult? _activeComparison;
    private bool _hasEnoughFiles;
    private bool _canStartCompare;
    private bool _isComparing;
    private string _selectionSummary = "Файлы для сравнения пока не выбраны.";
    private string _stateText = "Откройте минимум два файла в текстовом редакторе.";
    private string _comparisonSummary = string.Empty;

    public FileCompareSelectionControl(Func<IReadOnlyList<OpenTextEditorDescriptor>> openFilesProvider)
    {
      InitializeComponent();
      DataContext = this;
      _openFilesProvider = openFilesProvider;

      Loaded += (_, _) => RefreshOpenFiles();
      IsVisibleChanged += (_, _) =>
      {
        if (IsVisible)
        {
          RefreshOpenFiles();
        }
      };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<OpenTextEditorDescriptor> OpenFiles { get; } = new();

    public ObservableCollection<FileCompareTargetItem> TargetFiles { get; } = new();

    public ObservableCollection<OpenTextEditorDescriptor> SelectedTargetFiles { get; } = new();

    public ObservableCollection<FileComparisonResult> ComparisonResults { get; } = new();

    public OpenTextEditorDescriptor? SelectedSourceFile
    {
      get => _selectedSourceFile;
      set
      {
        if (ReferenceEquals(_selectedSourceFile, value))
        {
          return;
        }

        _selectedSourceFile = value;
        OnPropertyChanged();
        RebuildTargetFiles();
        ClearComparisonResults();
      }
    }

    public FileComparisonResult? ActiveComparison
    {
      get => _activeComparison;
      set
      {
        if (ReferenceEquals(_activeComparison, value))
        {
          return;
        }

        _activeComparison = value;
        OnPropertyChanged();
      }
    }

    public bool HasEnoughFiles
    {
      get => _hasEnoughFiles;
      private set
      {
        if (_hasEnoughFiles == value)
        {
          return;
        }

        _hasEnoughFiles = value;
        OnPropertyChanged();
      }
    }

    public bool CanStartCompare
    {
      get => _canStartCompare;
      private set
      {
        if (_canStartCompare == value)
        {
          return;
        }

        _canStartCompare = value;
        OnPropertyChanged();
      }
    }

    public bool IsComparing
    {
      get => _isComparing;
      private set
      {
        if (_isComparing == value)
        {
          return;
        }

        _isComparing = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(CanRefresh));
      }
    }

    public bool CanRefresh => !IsComparing;

    public Visibility SelectionPanelVisibility => ComparisonResults.Count == 0
      ? Visibility.Visible
      : Visibility.Collapsed;

    public Visibility ResultsPanelVisibility => ComparisonResults.Count == 0
      ? Visibility.Collapsed
      : Visibility.Visible;

    public string SelectionSummary
    {
      get => _selectionSummary;
      private set
      {
        if (_selectionSummary == value)
        {
          return;
        }

        _selectionSummary = value;
        OnPropertyChanged();
      }
    }

    public string StateText
    {
      get => _stateText;
      private set
      {
        if (_stateText == value)
        {
          return;
        }

        _stateText = value;
        OnPropertyChanged();
      }
    }

    public string ComparisonSummary
    {
      get => _comparisonSummary;
      private set
      {
        if (_comparisonSummary == value)
        {
          return;
        }

        _comparisonSummary = value;
        OnPropertyChanged();
      }
    }

    public void RefreshOpenFiles()
    {
      if (IsComparing)
      {
        return;
      }

      var selectedSourceKey = SelectedSourceFile?.IdentityKey;
      var selectedTargetKeys = TargetFiles
        .Where(item => item.IsSelected)
        .Select(item => item.File.IdentityKey)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

      var openFiles = (_openFilesProvider?.Invoke() ?? Array.Empty<OpenTextEditorDescriptor>())
        .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToList();

      OpenFiles.Clear();
      foreach (var file in openFiles)
      {
        OpenFiles.Add(file);
      }

      HasEnoughFiles = OpenFiles.Count > 1;
      if (!HasEnoughFiles)
      {
        _selectedSourceFile = OpenFiles.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedSourceFile));
        TargetFiles.Clear();
        SelectedTargetFiles.Clear();
        CanStartCompare = false;
        ClearComparisonResults();
        SelectionSummary = "Файлы для сравнения пока не выбраны.";
        StateText = OpenFiles.Count == 0
          ? "Нет открытых файлов в текстовом редакторе."
          : "Откройте минимум два файла в текстовом редакторе.";
        return;
      }

      _selectedSourceFile = OpenFiles.FirstOrDefault(file => string.Equals(file.IdentityKey, selectedSourceKey, StringComparison.OrdinalIgnoreCase))
        ?? OpenFiles.FirstOrDefault();
      OnPropertyChanged(nameof(SelectedSourceFile));

      RebuildTargetFiles(selectedTargetKeys);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
      RefreshOpenFiles();
    }

    private async void StartCompareButton_Click(object sender, RoutedEventArgs e)
    {
      if (SelectedSourceFile == null || SelectedTargetFiles.Count == 0)
      {
        Message.MessageBoxCustom.Show(
          "Выберите исходный файл и хотя бы один файл для сравнения.",
          "Сравнение файлов",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
        return;
      }

      _compareCancellation?.Cancel();
      _compareCancellation = new CancellationTokenSource();
      var cancellationToken = _compareCancellation.Token;
      var source = SelectedSourceFile;
      var targets = SelectedTargetFiles.ToList();

      IsComparing = true;
      CanStartCompare = false;
      ClearComparisonResults();
      StateText = $"Выполняется сравнение: {source.DisplayName} -> {targets.Count} файлов...";

      try
      {
        var results = await Task.Run(
          () => targets
            .Select(target => FileDiffEngine.Compare(source, target, cancellationToken))
            .ToList(),
          cancellationToken);

        ComparisonResults.Clear();
        foreach (var result in results)
        {
          ComparisonResults.Add(result);
        }

        ActiveComparison = ComparisonResults.FirstOrDefault();
        ComparisonSummary = BuildComparisonSummary(source, results);
        StateText = $"Сравнение завершено. Файлов справа: {ComparisonResults.Count}.";
        OnPropertyChanged(nameof(SelectionPanelVisibility));
        OnPropertyChanged(nameof(ResultsPanelVisibility));
      }
      catch (OperationCanceledException)
      {
        StateText = "Сравнение отменено.";
      }
      catch (Exception ex)
      {
        StateText = $"Ошибка сравнения: {ex.Message}";
        Message.MessageBoxCustom.Show(StateText, "Сравнение файлов", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      finally
      {
        IsComparing = false;
        RefreshSelectedTargets();
      }
    }

    private void RebuildTargetFiles(ISet<string>? selectedTargetKeys = null)
    {
      foreach (var item in TargetFiles)
      {
        item.PropertyChanged -= TargetItem_PropertyChanged;
      }

      TargetFiles.Clear();
      SelectedTargetFiles.Clear();
      CanStartCompare = false;

      if (SelectedSourceFile == null)
      {
        SelectionSummary = "Выберите исходный файл.";
        StateText = "Исходный файл для сравнения пока не выбран.";
        return;
      }

      foreach (var file in OpenFiles)
      {
        if (string.Equals(file.IdentityKey, SelectedSourceFile.IdentityKey, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        var targetItem = new FileCompareTargetItem(
          file,
          selectedTargetKeys?.Contains(file.IdentityKey) == true);
        targetItem.PropertyChanged += TargetItem_PropertyChanged;
        TargetFiles.Add(targetItem);
      }

      RefreshSelectedTargets();
    }

    private void TargetItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(FileCompareTargetItem.IsSelected))
      {
        RefreshSelectedTargets();
        ClearComparisonResults();
      }
    }

    private void RefreshSelectedTargets()
    {
      SelectedTargetFiles.Clear();
      foreach (var file in TargetFiles
                 .Where(item => item.IsSelected)
                 .Select(item => item.File)
                 .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase))
      {
        SelectedTargetFiles.Add(file);
      }

      CanStartCompare = !IsComparing && SelectedSourceFile != null && SelectedTargetFiles.Count > 0;

      if (SelectedSourceFile == null)
      {
        SelectionSummary = "Выберите исходный файл.";
        StateText = "Исходный файл для сравнения пока не выбран.";
        return;
      }

      SelectionSummary = SelectedTargetFiles.Count == 0
        ? $"Исходный файл: {SelectedSourceFile.DisplayName}. Пока не выбрано ни одного файла для сравнения."
        : $"Исходный файл: {SelectedSourceFile.DisplayName}. Выбрано файлов для сравнения: {SelectedTargetFiles.Count}.";

      if (ComparisonResults.Count == 0)
      {
        StateText = SelectedTargetFiles.Count == 0
          ? "Отметьте один или несколько файлов справа от исходного файла."
          : $"Список сформирован: {SelectedSourceFile.DisplayName} будет сравниваться с {SelectedTargetFiles.Count} файлами.";
      }
    }

    private void ClearComparisonResults()
    {
      if (ComparisonResults.Count == 0 && ActiveComparison == null)
      {
        return;
      }

      ComparisonResults.Clear();
      ActiveComparison = null;
      ComparisonSummary = string.Empty;
      OnPropertyChanged(nameof(SelectionPanelVisibility));
      OnPropertyChanged(nameof(ResultsPanelVisibility));
    }

    private static string BuildComparisonSummary(OpenTextEditorDescriptor source, IReadOnlyCollection<FileComparisonResult> results)
    {
      var changedFiles = results.Count(item => item.HasChanges);
      var added = results.Sum(item => item.AddedLines);
      var removed = results.Sum(item => item.RemovedLines);
      var modified = results.Sum(item => item.ModifiedLines);

      return $"Исходный файл: {source.DisplayName}. Сравнено файлов: {results.Count}. "
        + $"Файлов с изменениями: {changedFiles}. Добавлено строк: {added}, удалено: {removed}, изменено: {modified}.";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }

  public sealed class FileCompareTargetItem : INotifyPropertyChanged
  {
    private bool _isSelected;

    public FileCompareTargetItem(OpenTextEditorDescriptor file, bool isSelected)
    {
      File = file;
      _isSelected = isSelected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public OpenTextEditorDescriptor File { get; }

    public bool IsSelected
    {
      get => _isSelected;
      set
      {
        if (_isSelected == value)
        {
          return;
        }

        _isSelected = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
      }
    }
  }

  public sealed class FileComparisonResult
  {
    public FileComparisonResult(
      OpenTextEditorDescriptor sourceFile,
      OpenTextEditorDescriptor targetFile,
      IReadOnlyList<DiffLineViewModel> sourceLines,
      IReadOnlyList<DiffLineViewModel> targetLines)
    {
      SourceFile = sourceFile;
      TargetFile = targetFile;
      SourceLines = sourceLines;
      TargetLines = targetLines;
      AddedLines = targetLines.Count(line => line.Kind == DiffLineKind.Added);
      RemovedLines = sourceLines.Count(line => line.Kind == DiffLineKind.Removed);
      ModifiedLines = Math.Max(
        sourceLines.Count(line => line.Kind == DiffLineKind.Modified),
        targetLines.Count(line => line.Kind == DiffLineKind.Modified));
    }

    public OpenTextEditorDescriptor SourceFile { get; }

    public OpenTextEditorDescriptor TargetFile { get; }

    public string SourceTitle => $"Исходный файл: {SourceFile.DisplayName}";

    public string TargetTitle => TargetFile.DisplayName;

    public string TargetHeader => $"Сравнение с: {TargetFile.DisplayName}";

    public IReadOnlyList<DiffLineViewModel> SourceLines { get; }

    public IReadOnlyList<DiffLineViewModel> TargetLines { get; }

    public int AddedLines { get; }

    public int RemovedLines { get; }

    public int ModifiedLines { get; }

    public bool HasChanges => AddedLines > 0 || RemovedLines > 0 || ModifiedLines > 0;
  }

  public sealed class DiffLineViewModel
  {
    public DiffLineViewModel(DiffLineKind kind, int? lineNumber, string text)
    {
      Kind = kind;
      LineNumberText = lineNumber?.ToString() ?? string.Empty;
      Text = text;
      Prefix = kind switch
      {
        DiffLineKind.Added => "+",
        DiffLineKind.Removed => "-",
        DiffLineKind.Modified => "~",
        _ => string.Empty,
      };
      BackgroundBrush = DiffBrushes.GetBackground(kind);
      ForegroundBrush = DiffBrushes.GetForeground(kind);
      BorderBrush = DiffBrushes.Border;
    }

    public DiffLineKind Kind { get; }

    public string Prefix { get; }

    public string LineNumberText { get; }

    public string Text { get; }

    public Brush BackgroundBrush { get; }

    public Brush ForegroundBrush { get; }

    public Brush BorderBrush { get; }
  }

  public enum DiffLineKind
  {
    Unchanged,
    Added,
    Removed,
    Modified,
    Empty
  }

  internal static class FileDiffEngine
  {
    private const long MaxDynamicProgrammingCells = 12_000_000;

    public static FileComparisonResult Compare(
      OpenTextEditorDescriptor sourceFile,
      OpenTextEditorDescriptor targetFile,
      CancellationToken cancellationToken)
    {
      var sourceLines = SplitLines(sourceFile.TextContent);
      var targetLines = SplitLines(targetFile.TextContent);
      var operations = BuildOperations(sourceLines, targetLines, cancellationToken);

      var sourceViewLines = new List<DiffLineViewModel>();
      var targetViewLines = new List<DiffLineViewModel>();

      for (int i = 0; i < operations.Count;)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (operations[i].Kind == RawDiffKind.Equal)
        {
          AddPair(
            sourceViewLines,
            targetViewLines,
            DiffLineKind.Unchanged,
            operations[i].OldLineNumber,
            operations[i].NewLineNumber,
            operations[i].Text,
            operations[i].Text);
          i++;
          continue;
        }

        var deleted = new List<RawDiffLine>();
        var inserted = new List<RawDiffLine>();
        while (i < operations.Count && operations[i].Kind != RawDiffKind.Equal)
        {
          if (operations[i].Kind == RawDiffKind.Delete)
          {
            deleted.Add(operations[i]);
          }
          else
          {
            inserted.Add(operations[i]);
          }

          i++;
        }

        var pairedCount = Math.Min(deleted.Count, inserted.Count);
        for (int pairIndex = 0; pairIndex < pairedCount; pairIndex++)
        {
          AddPair(
            sourceViewLines,
            targetViewLines,
            DiffLineKind.Modified,
            deleted[pairIndex].OldLineNumber,
            inserted[pairIndex].NewLineNumber,
            deleted[pairIndex].Text,
            inserted[pairIndex].Text);
        }

        for (int deleteIndex = pairedCount; deleteIndex < deleted.Count; deleteIndex++)
        {
          sourceViewLines.Add(new DiffLineViewModel(DiffLineKind.Removed, deleted[deleteIndex].OldLineNumber, deleted[deleteIndex].Text));
          targetViewLines.Add(new DiffLineViewModel(DiffLineKind.Empty, null, string.Empty));
        }

        for (int insertIndex = pairedCount; insertIndex < inserted.Count; insertIndex++)
        {
          sourceViewLines.Add(new DiffLineViewModel(DiffLineKind.Empty, null, string.Empty));
          targetViewLines.Add(new DiffLineViewModel(DiffLineKind.Added, inserted[insertIndex].NewLineNumber, inserted[insertIndex].Text));
        }
      }

      return new FileComparisonResult(sourceFile, targetFile, sourceViewLines, targetViewLines);
    }

    private static void AddPair(
      ICollection<DiffLineViewModel> sourceViewLines,
      ICollection<DiffLineViewModel> targetViewLines,
      DiffLineKind kind,
      int? sourceLineNumber,
      int? targetLineNumber,
      string sourceText,
      string targetText)
    {
      sourceViewLines.Add(new DiffLineViewModel(kind, sourceLineNumber, sourceText));
      targetViewLines.Add(new DiffLineViewModel(kind, targetLineNumber, targetText));
    }

    private static List<RawDiffLine> BuildOperations(
      IReadOnlyList<string> sourceLines,
      IReadOnlyList<string> targetLines,
      CancellationToken cancellationToken)
    {
      var cellCount = (long)(sourceLines.Count + 1) * (targetLines.Count + 1);
      if (cellCount > MaxDynamicProgrammingCells)
      {
        return BuildIndexBasedOperations(sourceLines, targetLines, cancellationToken);
      }

      return BuildLcsOperations(sourceLines, targetLines, cancellationToken);
    }

    private static List<RawDiffLine> BuildLcsOperations(
      IReadOnlyList<string> sourceLines,
      IReadOnlyList<string> targetLines,
      CancellationToken cancellationToken)
    {
      var oldCount = sourceLines.Count;
      var newCount = targetLines.Count;
      var lcs = new int[oldCount + 1, newCount + 1];

      for (int oldIndex = oldCount - 1; oldIndex >= 0; oldIndex--)
      {
        cancellationToken.ThrowIfCancellationRequested();
        for (int newIndex = newCount - 1; newIndex >= 0; newIndex--)
        {
          lcs[oldIndex, newIndex] = sourceLines[oldIndex] == targetLines[newIndex]
            ? lcs[oldIndex + 1, newIndex + 1] + 1
            : Math.Max(lcs[oldIndex + 1, newIndex], lcs[oldIndex, newIndex + 1]);
        }
      }

      var result = new List<RawDiffLine>();
      var oldCursor = 0;
      var newCursor = 0;
      while (oldCursor < oldCount && newCursor < newCount)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (sourceLines[oldCursor] == targetLines[newCursor])
        {
          result.Add(new RawDiffLine(RawDiffKind.Equal, sourceLines[oldCursor], oldCursor + 1, newCursor + 1));
          oldCursor++;
          newCursor++;
        }
        else if (lcs[oldCursor + 1, newCursor] >= lcs[oldCursor, newCursor + 1])
        {
          result.Add(new RawDiffLine(RawDiffKind.Delete, sourceLines[oldCursor], oldCursor + 1, null));
          oldCursor++;
        }
        else
        {
          result.Add(new RawDiffLine(RawDiffKind.Insert, targetLines[newCursor], null, newCursor + 1));
          newCursor++;
        }
      }

      while (oldCursor < oldCount)
      {
        result.Add(new RawDiffLine(RawDiffKind.Delete, sourceLines[oldCursor], oldCursor + 1, null));
        oldCursor++;
      }

      while (newCursor < newCount)
      {
        result.Add(new RawDiffLine(RawDiffKind.Insert, targetLines[newCursor], null, newCursor + 1));
        newCursor++;
      }

      return result;
    }

    private static List<RawDiffLine> BuildIndexBasedOperations(
      IReadOnlyList<string> sourceLines,
      IReadOnlyList<string> targetLines,
      CancellationToken cancellationToken)
    {
      var result = new List<RawDiffLine>();
      var commonLength = Math.Min(sourceLines.Count, targetLines.Count);

      for (int index = 0; index < commonLength; index++)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (sourceLines[index] == targetLines[index])
        {
          result.Add(new RawDiffLine(RawDiffKind.Equal, sourceLines[index], index + 1, index + 1));
          continue;
        }

        result.Add(new RawDiffLine(RawDiffKind.Delete, sourceLines[index], index + 1, null));
        result.Add(new RawDiffLine(RawDiffKind.Insert, targetLines[index], null, index + 1));
      }

      for (int index = commonLength; index < sourceLines.Count; index++)
      {
        result.Add(new RawDiffLine(RawDiffKind.Delete, sourceLines[index], index + 1, null));
      }

      for (int index = commonLength; index < targetLines.Count; index++)
      {
        result.Add(new RawDiffLine(RawDiffKind.Insert, targetLines[index], null, index + 1));
      }

      return result;
    }

    private static IReadOnlyList<string> SplitLines(string text)
    {
      return (text ?? string.Empty)
        .Replace("\r\n", "\n")
        .Replace('\r', '\n')
        .Split('\n');
    }
  }

  internal sealed record RawDiffLine(RawDiffKind Kind, string Text, int? OldLineNumber, int? NewLineNumber);

  internal enum RawDiffKind
  {
    Equal,
    Delete,
    Insert
  }

  internal static class DiffBrushes
  {
    public static readonly Brush Border = CreateBrush(218, 225, 233);
    private static readonly Brush UnchangedBackground = Brushes.Transparent;
    private static readonly Brush EmptyBackground = CreateBrush(248, 250, 252);
    private static readonly Brush AddedBackground = CreateBrush(220, 252, 231);
    private static readonly Brush RemovedBackground = CreateBrush(254, 226, 226);
    private static readonly Brush ModifiedBackground = CreateBrush(254, 243, 199);
    private static readonly Brush DefaultForeground = CreateBrush(30, 41, 59);
    private static readonly Brush AddedForeground = CreateBrush(22, 101, 52);
    private static readonly Brush RemovedForeground = CreateBrush(153, 27, 27);
    private static readonly Brush ModifiedForeground = CreateBrush(146, 64, 14);

    public static Brush GetBackground(DiffLineKind kind) => kind switch
    {
      DiffLineKind.Added => AddedBackground,
      DiffLineKind.Removed => RemovedBackground,
      DiffLineKind.Modified => ModifiedBackground,
      DiffLineKind.Empty => EmptyBackground,
      _ => UnchangedBackground,
    };

    public static Brush GetForeground(DiffLineKind kind) => kind switch
    {
      DiffLineKind.Added => AddedForeground,
      DiffLineKind.Removed => RemovedForeground,
      DiffLineKind.Modified => ModifiedForeground,
      _ => DefaultForeground,
    };

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
      var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
      brush.Freeze();
      return brush;
    }
  }
}
