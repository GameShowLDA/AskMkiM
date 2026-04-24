using Ask.Core.Shared.DTO.TextEditor;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.FileCompare
{
  /// <summary>
  /// Единый контрол выбора уже открытых файлов для последующего сравнения.
  /// </summary>
  public partial class FileCompareSelectionControl : UserControl, INotifyPropertyChanged
  {
    private readonly Func<IReadOnlyList<OpenTextEditorDescriptor>> _openFilesProvider;
    private OpenTextEditorDescriptor? _selectedSourceFile;
    private bool _hasEnoughFiles;
    private string _selectionSummary = "Файлы для сравнения пока не выбраны.";
    private string _stateText = "Откройте минимум два файла в текстовом редакторе.";

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

    public void RefreshOpenFiles()
    {
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

    private void RebuildTargetFiles(ISet<string>? selectedTargetKeys = null)
    {
      foreach (var item in TargetFiles)
      {
        item.PropertyChanged -= TargetItem_PropertyChanged;
      }

      TargetFiles.Clear();
      SelectedTargetFiles.Clear();

      if (SelectedSourceFile == null)
      {
        SelectionSummary = "Выберите основной файл.";
        StateText = "Основной файл для сравнения пока не выбран.";
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

      if (SelectedSourceFile == null)
      {
        SelectionSummary = "Выберите основной файл.";
        StateText = "Основной файл для сравнения пока не выбран.";
        return;
      }

      SelectionSummary = SelectedTargetFiles.Count == 0
        ? $"Основной файл: {SelectedSourceFile.DisplayName}. Пока не выбрано ни одного файла для сравнения."
        : $"Основной файл: {SelectedSourceFile.DisplayName}. Выбрано файлов для сравнения: {SelectedTargetFiles.Count}.";

      StateText = SelectedTargetFiles.Count == 0
        ? "Отметьте один или несколько файлов справа от основного файла."
        : $"Список сформирован: {SelectedSourceFile.DisplayName} будет сравниваться с {SelectedTargetFiles.Count} файлами.";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }

  /// <summary>
  /// Элемент выбора файла для сравнения.
  /// </summary>
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
}
