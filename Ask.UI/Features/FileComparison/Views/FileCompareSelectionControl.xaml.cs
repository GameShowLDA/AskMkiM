using Ask.Core.Shared.DTO.TextEditor;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Features.FileComparison.Views
{
  public partial class FileCompareSelectionControl : UserControl, INotifyPropertyChanged
  {
    private readonly Func<IReadOnlyList<OpenTextEditorDescriptor>> _openFilesProvider;
    private CancellationTokenSource? _compareCancellation;
    private bool _isSynchronizingScroll;
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

    /// <summary>
    /// Количество файлов, сравнённых с исходным документом.
    /// </summary>
    public int ComparedFilesCount => ComparisonResults.Count;

    /// <summary>
    /// Общее количество добавленных строк в результатах сравнения.
    /// </summary>
    public int AddedLinesCount => ComparisonResults.Sum(result => result.AddedLines);

    /// <summary>
    /// Общее количество удалённых строк в результатах сравнения.
    /// </summary>
    public int RemovedLinesCount => ComparisonResults.Sum(result => result.RemovedLines);

    /// <summary>
    /// Общее количество изменённых строк в результатах сравнения.
    /// </summary>
    public int ModifiedLinesCount => ComparisonResults.Sum(result => result.ModifiedLines);

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
        NotifyComparisonStatisticsChanged();
        StateText = "Сравнение завершено.";
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
      NotifyComparisonStatisticsChanged();
      OnPropertyChanged(nameof(SelectionPanelVisibility));
      OnPropertyChanged(nameof(ResultsPanelVisibility));
    }

    /// <summary>
    /// Обновляет привязки счётчиков после замены результатов сравнения.
    /// </summary>
    private void NotifyComparisonStatisticsChanged()
    {
      OnPropertyChanged(nameof(ComparedFilesCount));
      OnPropertyChanged(nameof(AddedLinesCount));
      OnPropertyChanged(nameof(RemovedLinesCount));
      OnPropertyChanged(nameof(ModifiedLinesCount));
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

    private void SourceLinesListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      if (_isSynchronizingScroll || (e.VerticalChange == 0 && e.HorizontalChange == 0))
      {
        return;
      }

      SyncScroll(SourceLinesListBox, GetVisibleTargetLinesListBox());
    }

    private void TargetLinesListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      if (_isSynchronizingScroll || (e.VerticalChange == 0 && e.HorizontalChange == 0))
      {
        return;
      }

      if (sender is not ListBox targetListBox)
      {
        return;
      }

      SyncScroll(targetListBox, SourceLinesListBox);
    }

    private void SyncScroll(DependencyObject source, DependencyObject? target)
    {
      if (target == null)
      {
        return;
      }

      var sourceViewer = FindScrollViewer(source);
      var targetViewer = FindScrollViewer(target);
      if (sourceViewer == null || targetViewer == null)
      {
        return;
      }

      _isSynchronizingScroll = true;
      try
      {
        targetViewer.ScrollToVerticalOffset(sourceViewer.VerticalOffset);
        targetViewer.ScrollToHorizontalOffset(sourceViewer.HorizontalOffset);
      }
      finally
      {
        _isSynchronizingScroll = false;
      }
    }

    private ListBox? GetVisibleTargetLinesListBox()
    {
      return FindVisualChildren<ListBox>(ComparisonTabControl)
        .FirstOrDefault(listBox => string.Equals(listBox.Name, "TargetLinesListBox", StringComparison.Ordinal) && listBox.IsVisible);
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject? root)
    {
      if (root == null)
      {
        return null;
      }

      if (root is ScrollViewer viewer)
      {
        return viewer;
      }

      for (var childIndex = 0; childIndex < VisualTreeHelper.GetChildrenCount(root); childIndex++)
      {
        var child = VisualTreeHelper.GetChild(root, childIndex);
        var result = FindScrollViewer(child);
        if (result != null)
        {
          return result;
        }
      }

      return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject? root)
      where T : DependencyObject
    {
      if (root == null)
      {
        yield break;
      }

      var count = VisualTreeHelper.GetChildrenCount(root);
      for (var childIndex = 0; childIndex < count; childIndex++)
      {
        var child = VisualTreeHelper.GetChild(root, childIndex);
        if (child is T result)
        {
          yield return result;
        }

        foreach (var nested in FindVisualChildren<T>(child))
        {
          yield return nested;
        }
      }
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
    public DiffLineViewModel(DiffLineKind kind, DiffPaneSide paneSide, int displayLineNumber, int? originalLineNumber, string text)
    {
      Kind = kind;
      PaneSide = paneSide;
      DisplayLineNumber = displayLineNumber;
      OriginalLineNumber = originalLineNumber;
      LineNumberText = originalLineNumber?.ToString() ?? string.Empty;
      Text = text;
      Prefix = DiffPresentation.GetPrefix(kind, paneSide);
      BackgroundBrush = DiffPresentation.GetBackground(kind, paneSide);
      ForegroundBrush = DiffPresentation.GetForeground(kind, paneSide);
      BorderBrush = DiffBrushes.Border;
    }

    public DiffLineKind Kind { get; }

    public DiffPaneSide PaneSide { get; }

    public int DisplayLineNumber { get; }

    public int? OriginalLineNumber { get; }

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

  public enum DiffPaneSide
  {
    Source,
    Target
  }

  internal static class FileDiffEngine
  {
    public static FileComparisonResult Compare(
      OpenTextEditorDescriptor sourceFile,
      OpenTextEditorDescriptor targetFile,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var diff = SideBySideDiffBuilder.Diff(
        sourceFile.TextContent,
        targetFile.TextContent,
        ignoreWhiteSpace: false,
        ignoreCase: false);

      var sourceViewLines = new List<DiffLineViewModel>(diff.OldText.Lines.Count);
      var targetViewLines = new List<DiffLineViewModel>(diff.NewText.Lines.Count);
      for (int index = 0; index < diff.OldText.Lines.Count; index++)
      {
        cancellationToken.ThrowIfCancellationRequested();
        var sourceLine = diff.OldText.Lines[index];
        var targetLine = diff.NewText.Lines[index];

        sourceViewLines.Add(CreateViewLine(sourceLine, DiffPaneSide.Source, index + 1));
        targetViewLines.Add(CreateViewLine(targetLine, DiffPaneSide.Target, index + 1));
      }

      return new FileComparisonResult(sourceFile, targetFile, sourceViewLines, targetViewLines);
    }

    private static DiffLineViewModel CreateViewLine(DiffPiece piece, DiffPaneSide paneSide, int displayLineNumber)
    {
      var kind = piece.Type switch
      {
        ChangeType.Unchanged => DiffLineKind.Unchanged,
        ChangeType.Deleted => DiffLineKind.Removed,
        ChangeType.Inserted => DiffLineKind.Added,
        ChangeType.Modified => DiffLineKind.Modified,
        ChangeType.Imaginary => DiffLineKind.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(piece), piece.Type, "Неизвестный тип строки diff."),
      };

      return new DiffLineViewModel(kind, paneSide, displayLineNumber, piece.Position, piece.Text ?? string.Empty);
    }
  }
  internal static class DiffBrushes
  {
    public static readonly Brush Border = CreateBrush(218, 225, 233);
    private static readonly Brush UnchangedBackground = Brushes.Transparent;
    private static readonly Brush PresentBackground = CreateBrush(220, 252, 231);
    private static readonly Brush MissingBackground = CreateBrush(254, 226, 226);
    private static readonly Brush EmptyFallbackBackground = CreateBrush(235, 240, 246);
    private static readonly Brush DefaultForeground = CreateBrush(30, 41, 59);
    private static readonly Brush PresentForeground = CreateBrush(22, 101, 52);
    private static readonly Brush MissingForeground = CreateBrush(153, 27, 27);

    public static Brush GetPresentBackground() => PresentBackground;

    public static Brush GetMissingBackground() => MissingBackground;

    public static Brush GetUnchangedBackground() => UnchangedBackground;

    public static Brush GetDefaultForeground() => DefaultForeground;

    public static Brush GetThemeForegroundOrDefault()
    {
      var resource = Application.Current?.TryFindResource("ForegrounfBrushes");
      return resource as Brush ?? DefaultForeground;
    }

    public static Brush GetMutedThemeForegroundOrDefault()
    {
      var resource = Application.Current?.TryFindResource("ForegrounfBrushes45");
      return resource as Brush ?? DefaultForeground;
    }

    public static Brush GetPresentForeground() => PresentForeground;

    public static Brush GetMissingForeground() => MissingForeground;

    public static Brush GetEmptyBackgroundOrFallback()
    {
      var resource = Application.Current?.TryFindResource("SettingsNestedGroupBackgroundSolidColorBrush");
      return resource as Brush ?? EmptyFallbackBackground;
    }

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
      var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
      brush.Freeze();
      return brush;
    }
  }

  internal static class DiffPresentation
  {
    public static string GetPrefix(DiffLineKind kind, DiffPaneSide paneSide) => kind switch
    {
      DiffLineKind.Unchanged => string.Empty,
      DiffLineKind.Empty => string.Empty,
      DiffLineKind.Added => "+",
      DiffLineKind.Removed => "-",
      DiffLineKind.Modified when paneSide == DiffPaneSide.Source => "-",
      DiffLineKind.Modified => "+",
      _ => string.Empty,
    };

    public static Brush GetBackground(DiffLineKind kind, DiffPaneSide paneSide)
    {
      if (kind == DiffLineKind.Unchanged)
      {
        return DiffBrushes.GetUnchangedBackground();
      }

      if (kind == DiffLineKind.Empty)
      {
        return DiffBrushes.GetEmptyBackgroundOrFallback();
      }

      return kind switch
      {
        DiffLineKind.Added => DiffBrushes.GetPresentBackground(),
        DiffLineKind.Removed => DiffBrushes.GetMissingBackground(),
        DiffLineKind.Modified when paneSide == DiffPaneSide.Source => DiffBrushes.GetMissingBackground(),
        DiffLineKind.Modified => DiffBrushes.GetPresentBackground(),
        _ => DiffBrushes.GetUnchangedBackground(),
      };
    }

    public static Brush GetForeground(DiffLineKind kind, DiffPaneSide paneSide)
    {
      if (kind == DiffLineKind.Unchanged)
      {
        return DiffBrushes.GetThemeForegroundOrDefault();
      }

      if (kind == DiffLineKind.Empty)
      {
        return DiffBrushes.GetMutedThemeForegroundOrDefault();
      }

      return kind switch
      {
        DiffLineKind.Added => DiffBrushes.GetPresentForeground(),
        DiffLineKind.Removed => DiffBrushes.GetMissingForeground(),
        DiffLineKind.Modified when paneSide == DiffPaneSide.Source => DiffBrushes.GetMissingForeground(),
        DiffLineKind.Modified => DiffBrushes.GetPresentForeground(),
        _ => DiffBrushes.GetThemeForegroundOrDefault(),
      };
    }
  }
}
