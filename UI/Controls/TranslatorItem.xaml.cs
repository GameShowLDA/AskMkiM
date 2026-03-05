using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.Engine.ControlCommandAnalyser;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Ask.UI.Shared.Contracts.Ask.UI.Shared.Contracts;
using ICSharpCode.AvalonEdit;
using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI.Components;
using UI.Controls.TextEditor;
using UI.Services;
using UI.Services.Archive;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для TranslatorContainer.xaml
  /// </summary>
  public partial class TranslatorItem : UserControl
  {
    public string FirstFilePath { get; set; }
    public string SecondFilePath { get; set; }

    public int ErrorCount { get; private set; } = 0;
    public int WarningCount { get; private set; } = 0;
    public int GeneralCount => ErrorCount + WarningCount;

    private static readonly string[] ArchiveSaveFolderCandidates = new[]
    {
      Path.Combine(@"D:\AskMkiM\Bin", FileLocations.ArchiveDirectory),
      Path.Combine(AppContext.BaseDirectory, FileLocations.ArchiveDirectory),
      Path.Combine(Directory.GetCurrentDirectory(), FileLocations.ArchiveDirectory),
    };

    private List<BaseCommandModel> translationModels = new List<BaseCommandModel>();

    public List<BaseCommandModel> TranslationModels
    {
      get
      {
        return translationModels;
      }
      set
      {
        translationModels = value;
        ErrorClear();
        ErrorListBoxVertical.ClearAll();

        foreach (var model in value)
        {
          if (model.Errors.Count > 0)
          {
            ErrorListBoxVertical.AddErrors(model.Errors);
            ErrorCount += model.Errors.Count;
          }

          if (model.Warnings.Count > 0)
          {
            ErrorListBoxVertical.AddWarnings(model.Warnings);
            WarningCount += model.Warnings.Count;
          }
        }
        MessageEventAdapter.RaiseInfoMessage(
               $"Общее кол-во ошибок и предупреждений: {GeneralCount}");
      }
    }

    public TranslatorItem()
    {
      InitializeComponent();
      ErrorListBoxVertical.ItemDoubleClicked += ErrorListBoxVertical_ErrorItemDoubleClicked;

      EventAggregator.Subscribe<BreakpointEvents.BreakpointSet>(e => BreakpointSet(e));
      EventAggregator.Subscribe<BreakpointEvents.BreakpointRemoved>(e => BreakpointRemoved(e));
    }

    private void ErrorListBoxVertical_ErrorItemDoubleClicked(IDisplayIssue item)
    {
      var leftEditor = GetLeftBox().GetTextEditor();
      leftEditor.GoToLine(item.SourceLineNumber);

      var rightEditor = GetRightBox().GetTextEditor();
      rightEditor.GoToLine(item.FormattedLineNumber);
    }

    private void ErrorClear()
    {
      ErrorCount = 0;
      WarningCount = 0;
    }

    public void SetLeftEditor(ITextEditorView editor)
    {
      if (editor is not IUiViewAdapter adapter)
        return;

      if (adapter.NativeView is not UIElement element)
        return;

      DetachFromParent(element);

      if (LeftBox == null || element == null)
      {
        return;
      }

      var leftEditor = GetLeftBox();
      if (leftEditor == null)
      {
        LeftBox.Children.Clear();
        leftEditor = new TranslatorTextEditor();
        LeftBox.Children.Add(leftEditor);
      }

      leftEditor.SetEditor(editor);
      leftEditor.SaveRequested -= LeftEditor_SaveRequested;
      leftEditor.SaveRequested += LeftEditor_SaveRequested;
      leftEditor.OpenFolderRequested -= LeftEditor_OpenFolderRequested;
      leftEditor.OpenFolderRequested += LeftEditor_OpenFolderRequested;
      leftEditor.PrintRequested -= LeftEditor_PrintRequested;
      leftEditor.PrintRequested += LeftEditor_PrintRequested;
    }

    public void SetRightEditor(ITextEditorView textEditorUI)
    {
      if (RightBox == null || textEditorUI == null)
      {
        return;
      }

      var rightEditor = GetRightBox();
      if (rightEditor == null)
      {
        RightBox.Children.Clear();
        rightEditor = new TranslatorEditor();
        RightBox.Children.Add(rightEditor);
      }

      rightEditor.SetEditor(textEditorUI);
      rightEditor.BackRequested -= RightEditor_BackRequestedAsync;
      rightEditor.BackRequested += RightEditor_BackRequestedAsync;
      rightEditor.SaveRequested -= RightEditor_SaveRequestedAsync;
      rightEditor.SaveRequested += RightEditor_SaveRequestedAsync;
    }

    private void RightEditor_BackRequestedAsync(object? sender, EventArgs e)
    {
      if (TranslatorNavigationService.TryOpenSourceFileFromTranslator(GetRightBox()))
      {
        CloseTranslatorTab();
      }
    }

    private void RightEditor_SaveRequestedAsync(object? sender, EventArgs e)
    {
      try
      {
        var rightBox = GetRightBox();
        var rightTextEditor = rightBox.GetTextEditor();

        var manager = new CommandTranslationManager();
        var models = this.TranslationModels;
        manager.SetSourseLines(models);
        var sourceLines = new List<List<string>>();
        foreach (var model in models)
        {
          sourceLines.Add(manager.GetSourceLines(model, out int startSourceLineNumber));
        }

        var fileName = Path.GetFileNameWithoutExtension(rightTextEditor.TextEditorModel.FilePath) + ".opkw";
        var archivePath = GetArchivePathForSave(Path.GetFileNameWithoutExtension(fileName));

        if (string.IsNullOrWhiteSpace(archivePath))
        {
          return;
        }

        using var archiveManager = new ArchiveManager();
        archiveManager.OpenArchive(archivePath);
        archiveManager.AddFileToArchive(sourceLines, archivePath, fileName);

        NotificationHostService.Instance.Show(
          "Сохранение в архив",
          $"Файл {fileName} добавлен в архив '{Path.GetFileNameWithoutExtension(archivePath)}'.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification(
          "Сохранение в архив",
          GetUserFriendlySaveErrorMessage(ex),
          NotificationType.Error);
      }
    }

    private string GetArchivePathForSave(string suggestedArchiveName)
    {
      var archivesFolderPath = ResolveArchiveSaveFolderPath();
      var existingArchives = Directory.EnumerateFiles(archivesFolderPath, "*.apkw", SearchOption.TopDirectoryOnly)
        .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
        .ToList();

      return PromptForArchiveSelection(existingArchives, archivesFolderPath, suggestedArchiveName);
    }

    private string PromptForArchiveSelection(
      IReadOnlyList<string> archivePaths,
      string archivesFolderPath,
      string suggestedArchiveName)
    {
      var dialog = new Window
      {
        Title = "Сохранение в архив",
        Owner = Window.GetWindow(this),
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        SizeToContent = SizeToContent.WidthAndHeight,
        ShowInTaskbar = false,
        WindowStyle = WindowStyle.None,
        AllowsTransparency = true,
        Background = Brushes.Transparent,
      };

      var shell = new Border
      {
        Background = GetThemeBrush("IsCheckedColorSolidColorBrush", Color.FromRgb(230, 232, 236)),
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromRgb(120, 130, 140)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(20),
        Padding = new Thickness(20),
      };

      var layout = new Grid
      {
        MinWidth = 420,
      };
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var label = new TextBlock
      {
        Text = archivePaths.Count == 0
          ? "Архивы не найдены. Создайте новый архив:"
          : "Выберите архив для сохранения:",
        Margin = new Thickness(0, 0, 0, 4),
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontFamily = Application.Current?.Resources["WinstonMedium"] as FontFamily,
        FontSize = 16,
        TextWrapping = TextWrapping.Wrap,
      };

      var listBackground = GetThemeBrush("PrimarySolidColorBrush", Color.FromRgb(239, 239, 224));
      var listAccent = GetThemeBrush("ActiveForegroundSolidColorBrush80", Color.FromArgb(120, 164, 235, 158));
      var listForeground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black);
      var listBorder = new Border
      {
        Background = listBackground,
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Margin = new Thickness(0, 8, 0, 0),
        Padding = new Thickness(6),
      };

      var archivesListBox = new ListBox
      {
        MinWidth = 380,
        MinHeight = 180,
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Foreground = listForeground,
        FontSize = 15,
      };

      var itemStyle = new Style(typeof(ListBoxItem));
      itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 8, 10, 8)));
      itemStyle.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 2, 0, 2)));
      itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
      itemStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
      itemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
      var hoverTrigger = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
      hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, listAccent));
      itemStyle.Triggers.Add(hoverTrigger);
      var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
      selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, listAccent));
      selectedTrigger.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
      itemStyle.Triggers.Add(selectedTrigger);
      archivesListBox.ItemContainerStyle = itemStyle;

      foreach (var archivePath in archivePaths)
      {
        archivesListBox.Items.Add(new ListBoxItem
        {
          Content = Path.GetFileName(archivePath),
          Tag = archivePath,
        });
      }

      if (archivesListBox.Items.Count > 0)
      {
        archivesListBox.SelectedIndex = 0;
      }

      var buttonsPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 12, 0, 0),
      };

      var createArchiveButton = new Button
      {
        Content = "Создать архив",
        MinWidth = 160,
        Margin = new Thickness(0, 0, 8, 0),
      };
      ApplyDialogButtonStyle(createArchiveButton);
      createArchiveButton.Click += (_, _) =>
      {
        while (true)
        {
          var archiveName = PromptForArchiveName(
            suggestedArchiveName,
            isFirstArchive: archivesListBox.Items.Count == 0);

          if (string.IsNullOrWhiteSpace(archiveName))
          {
            return;
          }

          try
          {
            var createdArchivePath = CreateArchiveInFolder(archivesFolderPath, archiveName);
            var createdItem = new ListBoxItem
            {
              Content = Path.GetFileName(createdArchivePath),
              Tag = createdArchivePath,
            };

            archivesListBox.Items.Add(createdItem);
            archivesListBox.SelectedItem = createdItem;
            archivesListBox.ScrollIntoView(createdItem);
            return;
          }
          catch (Exception ex)
          {
            ShowArchiveNotification(
              "Создание архива",
              GetUserFriendlyCreateArchiveErrorMessage(ex),
              NotificationType.Error);
          }
        }
      };

      var saveButton = new Button
      {
        Content = "Сохранить",
        MinWidth = 140,
        IsDefault = true,
        Margin = new Thickness(0, 0, 8, 0),
      };
      ApplyDialogButtonStyle(saveButton);
      saveButton.Click += (_, _) =>
      {
        if (archivesListBox.SelectedItem is ListBoxItem)
        {
          dialog.DialogResult = true;
          return;
        }

        ShowArchiveNotification(
          "Сохранение в архив",
          "Выберите архив из списка или создайте новый.",
          NotificationType.Warning);
      };

      saveButton.IsEnabled = archivesListBox.SelectedItem is ListBoxItem;
      archivesListBox.SelectionChanged += (_, _) =>
      {
        saveButton.IsEnabled = archivesListBox.SelectedItem is ListBoxItem;
      };

      var cancelButton = new Button
      {
        Content = "Отмена",
        MinWidth = 120,
        IsCancel = true,
      };
      ApplyDialogButtonStyle(cancelButton);

      buttonsPanel.Children.Add(createArchiveButton);
      buttonsPanel.Children.Add(saveButton);
      buttonsPanel.Children.Add(cancelButton);

      listBorder.Child = archivesListBox;

      Grid.SetRow(label, 0);
      Grid.SetRow(listBorder, 1);
      Grid.SetRow(buttonsPanel, 2);
      layout.Children.Add(label);
      layout.Children.Add(listBorder);
      layout.Children.Add(buttonsPanel);
      shell.Child = layout;
      dialog.Content = shell;

      if (dialog.ShowDialog() != true)
      {
        return null;
      }

      return (archivesListBox.SelectedItem as ListBoxItem)?.Tag as string;
    }

    private string PromptForArchiveName(string suggestedArchiveName, bool isFirstArchive = false)
    {
      var dialog = new Window
      {
        Title = "Создание архива",
        Owner = Window.GetWindow(this),
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        SizeToContent = SizeToContent.WidthAndHeight,
        ShowInTaskbar = false,
        WindowStyle = WindowStyle.None,
        AllowsTransparency = true,
        Background = Brushes.Transparent,
      };

      var shell = new Border
      {
        Background = GetThemeBrush("IsCheckedColorSolidColorBrush", Color.FromRgb(230, 232, 236)),
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromRgb(120, 130, 140)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(20),
        Padding = new Thickness(20),
      };

      var layout = new Grid
      {
        MinWidth = 420,
      };
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var label = new TextBlock
      {
        Text = isFirstArchive
          ? "Архивы не найдены. Введите название нового архива:"
          : "Введите название нового архива:",
        Margin = new Thickness(0, 0, 0, 4),
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontFamily = Application.Current?.Resources["WinstonMedium"] as FontFamily,
        FontSize = 16,
        TextWrapping = TextWrapping.Wrap,
      };

      var inputBorder = new Border
      {
        Background = GetThemeBrush("PrimarySolidColorBrush", Color.FromRgb(239, 239, 224)),
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Margin = new Thickness(0, 8, 0, 0),
        Padding = new Thickness(10, 8, 10, 8),
      };

      var inputBox = new TextBox
      {
        MinWidth = 360,
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Text = string.IsNullOrWhiteSpace(suggestedArchiveName) ? "new_archive" : suggestedArchiveName,
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontSize = 15,
      };

      inputBorder.Child = inputBox;

      var buttonsPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 12, 0, 0),
      };

      var createButton = new Button
      {
        Content = "Создать",
        MinWidth = 140,
        IsDefault = true,
        Margin = new Thickness(0, 0, 8, 0),
      };
      ApplyDialogButtonStyle(createButton);
      createButton.Click += (_, _) => dialog.DialogResult = true;

      var cancelButton = new Button
      {
        Content = "Отмена",
        MinWidth = 120,
        IsCancel = true,
      };
      ApplyDialogButtonStyle(cancelButton);

      buttonsPanel.Children.Add(createButton);
      buttonsPanel.Children.Add(cancelButton);

      Grid.SetRow(label, 0);
      Grid.SetRow(inputBorder, 1);
      Grid.SetRow(buttonsPanel, 2);
      layout.Children.Add(label);
      layout.Children.Add(inputBorder);
      layout.Children.Add(buttonsPanel);
      shell.Child = layout;
      dialog.Content = shell;

      dialog.Loaded += (_, _) =>
      {
        inputBox.Focus();
        inputBox.SelectAll();
      };

      return dialog.ShowDialog() == true
        ? inputBox.Text?.Trim()
        : null;
    }

    private static string ResolveArchiveSaveFolderPath()
    {
      foreach (var candidatePath in ArchiveSaveFolderCandidates)
      {
        try
        {
          Directory.CreateDirectory(candidatePath);
          return candidatePath;
        }
        catch
        {
        }
      }

      throw new DirectoryNotFoundException("Не удалось открыть папку архивов.");
    }

    private static string CreateArchiveInFolder(string archivesFolderPath, string archiveName)
    {
      var normalizedArchiveName = NormalizeArchiveName(archiveName);
      var archivePath = Path.Combine(archivesFolderPath, normalizedArchiveName + ".apkw");

      if (File.Exists(archivePath))
      {
        throw new InvalidOperationException($"Архив '{Path.GetFileName(archivePath)}' уже существует.");
      }

      using (var archiveStream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        ArchiveManifestService.WriteManifest(archive, new List<ArchiveManifestFileRecord>());
      }

      return archivePath;
    }

    private static string NormalizeArchiveName(string archiveName)
    {
      if (string.IsNullOrWhiteSpace(archiveName))
      {
        throw new ArgumentException("Название архива обязательно.", nameof(archiveName));
      }

      var normalizedName = Path.GetFileNameWithoutExtension(archiveName.Trim());
      foreach (var invalidChar in Path.GetInvalidFileNameChars())
      {
        normalizedName = normalizedName.Replace(invalidChar, '_');
      }

      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        throw new ArgumentException("Название архива содержит только недопустимые символы.", nameof(archiveName));
      }

      return normalizedName;
    }

    private void ApplyDialogButtonStyle(Button button)
    {
      if (TryFindResource("ButtonStyleV10") is Style style)
      {
        button.Style = style;
      }

      button.Height = 44;
      button.Padding = new Thickness(14, 6, 14, 6);
      button.FontSize = 16;
    }

    private Brush GetThemeBrush(string key, Color fallbackColor)
    {
      if (TryFindResource(key) is Brush brush)
      {
        return brush;
      }

      if (Application.Current?.Resources[key] is Brush appBrush)
      {
        return appBrush;
      }

      return new SolidColorBrush(fallbackColor);
    }

    private void ShowArchiveNotification(string title, string message, NotificationType notificationType)
    {
      NotificationHostService.Instance.Show(title, message, notificationType);
    }

    private static string GetUserFriendlySaveErrorMessage(Exception ex)
    {
      if (ex is InvalidOperationException invalidOperation &&
          invalidOperation.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
      {
        return "Файл с таким именем уже существует в выбранном архиве.";
      }

      if (ex is FileNotFoundException)
      {
        return "Выбранный архив не найден.";
      }

      if (ex is IOException ioException &&
          ioException.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
      {
        return "Архив сейчас занят другим процессом. Повторите попытку.";
      }

      return "Не удалось сохранить файл в архив.";
    }

    private static string GetUserFriendlyCreateArchiveErrorMessage(Exception ex)
    {
      if (ex is InvalidOperationException invalidOperation &&
          invalidOperation.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
      {
        return "Архив с таким именем уже существует. Выберите другое имя.";
      }

      if (ex is ArgumentException)
      {
        return "Имя архива содержит недопустимые символы.";
      }

      return "Не удалось создать архив.";
    }

    private void LeftEditor_SaveRequested(object? sender, EventArgs e)
    {
      FindMultiEditorControl()?.EditorDocumentService.SaveFile();
    }

    private void LeftEditor_OpenFolderRequested(object? sender, EventArgs e)
    {
      FindMultiEditorControl()?.EditorDocumentService.OpenFolder();
    }

    private void LeftEditor_PrintRequested(object? sender, EventArgs e)
    {
      FindMultiEditorControl()?.EditorDocumentService.PrintFile();
    }

    private void CloseTranslatorTab()
    {
      var textEditorContainer = FindTextEditorContainer();
      if (textEditorContainer == null)
      {
        return;
      }

      var translatorDockItem = textEditorContainer.DockManager.DockItems
        .FirstOrDefault(item => item.Content == this);

      translatorDockItem?.PerformClose();
    }

    private TextEditorContainer? FindTextEditorContainer()
    {
      DependencyObject? current = this;

      while (current != null)
      {
        if (current is TextEditorContainer textEditorContainer)
        {
          return textEditorContainer;
        }

        current = current is FrameworkElement frameworkElement && frameworkElement.Parent != null
          ? frameworkElement.Parent
          : VisualTreeHelper.GetParent(current);
      }

      return null;
    }

    private MultiEditorControl? FindMultiEditorControl()
    {
      DependencyObject? current = this;

      while (current != null)
      {
        if (current is MultiEditorControl multiEditorControl)
        {
          return multiEditorControl;
        }

        current = current is FrameworkElement frameworkElement && frameworkElement.Parent != null
          ? frameworkElement.Parent
          : VisualTreeHelper.GetParent(current);
      }

      return null;
    }

    public TranslatorEditor GetRightBox()
    {
      if (RightBox == null)
      {
        return null;
      }

      return RightBox.Children[0] as TranslatorEditor;
    }

    public TranslatorTextEditor GetLeftBox()
    {
      if (LeftBox == null)
      {
        return null;
      }

      return LeftBox.Children[0] as TranslatorTextEditor;
    }

    public string GetLeftEditorName()
    {
      return GetLeftBox().FileName.Text;
    }

    public string GetRightEditorName()
    {
      return GetRightBox().TranslationFileName.Text;
    }

    public void SetRightEditorName(string newText)
    {
      GetRightBox().TranslationFileName.Text = newText;
    }

    public void SetLeftEditorName(string newText)
    {
      GetLeftBox().FileName.Text = newText;
    }

    private static void DetachFromParent(UIElement element)
    {
      switch (element)
      {
        case FrameworkElement fe when fe.Parent is Panel panel:
          panel.Children.Remove(element);
          break;

        case FrameworkElement fe when fe.Parent is ContentControl content:
          content.Content = null;
          break;

        case FrameworkElement fe when fe.Parent is Decorator decorator:
          decorator.Child = null;
          break;
      }
    }

    /// <summary>
    /// Обработчик события установки точки.
    /// Обновляет модель команды и синхронизирует точки в обоих редакторах
    /// без повторной генерации событий.
    /// </summary>
    /// <param name="obj">Событие установки точки (содержит номер команды).</param>
    private void BreakpointSet(BreakpointEvents.BreakpointSet obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null) return;

      model.HasBreakpoint = true;

      var left = GetLeftBox().GetTextEditor();
      var right = GetRightBox().GetTextEditor();

      int leftLine = model.StartLineNumber + 1;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnsureBreakpoint(leftLine, obj.CommandNumber, isSet: true, raiseEvents: false);

      right.EnsureBreakpoint(rightLine, obj.CommandNumber, isSet: true, raiseEvents: false);

      if (obj.LineNumber == leftLine - 1)
        right.GoToLineWithoutSelection(rightLine);
    }

    /// <summary>
    /// Обработчик события снятия точки.
    /// Обновляет модель команды и синхронизирует снятие точки в обоих редакторах
    /// без повторной генерации событий.
    /// </summary>
    /// <param name="obj">Событие снятия точки (содержит номер команды).</param>
    private void BreakpointRemoved(BreakpointEvents.BreakpointRemoved obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null) return;

      model.HasBreakpoint = false;

      var left = GetLeftBox().GetTextEditor();
      var right = GetRightBox().GetTextEditor();

      int leftLine = model.StartLineNumber + 1;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnsureBreakpoint(leftLine, obj.CommandNumber, isSet: false, raiseEvents: false);
      right.EnsureBreakpoint(rightLine, obj.CommandNumber, isSet: false, raiseEvents: false);

      if (obj.LineNumber == leftLine - 1)
        right.GoToLineWithoutSelection(rightLine);
    }

    private BaseCommandModel? GetCommandByNumber(int commandNumber)
    {
      return translationModels
          .FirstOrDefault(x =>
              int.TryParse(x.CommandNumber, out var num) &&
              num == commandNumber);
    }
  }
}
