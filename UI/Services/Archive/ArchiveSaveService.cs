using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Static;
using Ask.Engine.ControlCommandAnalyser;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services.Archive
{
  public sealed class ArchiveSaveService
  {
    private static readonly string[] ArchiveSaveFolderCandidates = new[]
    {
      Path.Combine(AppContext.BaseDirectory, FileLocations.ArchiveDirectory),
      Path.Combine(Directory.GetCurrentDirectory(), FileLocations.ArchiveDirectory),
    };

    public bool SaveFileToArchive(FrameworkElement ownerElement, List<BaseCommandModel> models, string sourceFilePath)
    {
      if (ownerElement == null)
      {
        throw new ArgumentNullException(nameof(ownerElement));
      }

      try
      {
        if (models == null || models.Count == 0)
        {
          LogWarning($"Нет данных для сохранения в архив.");
          ShowArchiveNotification(
            "Сохранение в архив",
            "Нет данных для сохранения в архив.",
            NotificationType.Warning);

          return false;
        }

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
          LogError($"Не удалось определить имя файла для сохранения.");
          ShowArchiveNotification(
            "Сохранение в архив",
            "Не удалось определить имя файла для сохранения.",
            NotificationType.Error);
          return false;
        }

        var manager = new CommandTranslationManager();
        var modelList = models.ToList();

        var sourceLines = new List<List<string>>(modelList.Count);
        foreach (var model in modelList)
        {
          sourceLines.Add(manager.GetSourceLines(model, out int _));
        }

        var fileName = Path.GetFileNameWithoutExtension(sourceFilePath) + ".opkw";
        var archivePath = GetArchivePathForSave(ownerElement, Path.GetFileNameWithoutExtension(fileName));

        if (string.IsNullOrWhiteSpace(archivePath))
        {
          return false;
        }

        using var archiveManager = new ArchiveManager();
        archiveManager.OpenArchive(archivePath);
        archiveManager.AddFileToArchive(sourceLines, archivePath, fileName);

        LogInformation($"Файл {fileName} добавлен в архив '{Path.GetFileNameWithoutExtension(archivePath)}'.");
        ShowArchiveNotification(
          "Сохранение в архив",
          $"Файл {fileName} добавлен в архив '{Path.GetFileNameWithoutExtension(archivePath)}'.",
          NotificationType.Success);


        return true;
      }
      catch (Exception ex)
      {
        LogError($"{GetUserFriendlySaveErrorMessage(ex)}");
        ShowArchiveNotification(
          "Сохранение в архив",
          GetUserFriendlySaveErrorMessage(ex),
          NotificationType.Error);
        return false;
      }
    }

    private string GetArchivePathForSave(FrameworkElement ownerElement, string suggestedArchiveName)
    {
      var archivesFolderPath = ResolveArchiveSaveFolderPath();
      var existingArchives = Directory.EnumerateFiles(archivesFolderPath, "*.apkw", SearchOption.TopDirectoryOnly)
        .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
        .ToList();

      return PromptForArchiveSelection(ownerElement, existingArchives, archivesFolderPath, suggestedArchiveName);
    }

    private string PromptForArchiveSelection(
      FrameworkElement ownerElement,
      IReadOnlyList<string> archivePaths,
      string archivesFolderPath,
      string suggestedArchiveName)
    {
      var dialog = new Window
      {
        Title = "Сохранение в архив",
        Owner = Window.GetWindow(ownerElement),
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
        Background = GetThemeBrush(ownerElement, "IsCheckedColorSolidColorBrush", Color.FromRgb(230, 232, 236)),
        BorderBrush = GetThemeBrush(ownerElement, "ForegroundSolidColorBrush60", Color.FromRgb(120, 130, 140)),
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
        Foreground = GetThemeBrush(ownerElement, "ForegroundSolidColorBrush", Colors.Black),
        FontFamily = Application.Current?.Resources["WinstonMedium"] as FontFamily,
        FontSize = 16,
        TextWrapping = TextWrapping.Wrap,
      };

      var listBackground = GetThemeBrush(ownerElement, "PrimarySolidColorBrush", Color.FromRgb(239, 239, 224));
      var listAccent = GetThemeBrush(ownerElement, "ActiveForegroundSolidColorBrush80", Color.FromArgb(120, 164, 235, 158));
      var listForeground = GetThemeBrush(ownerElement, "ForegroundSolidColorBrush", Colors.Black);
      var listBorder = new Border
      {
        Background = listBackground,
        BorderBrush = GetThemeBrush(ownerElement, "ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0)),
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
      ApplyDialogButtonStyle(ownerElement, createArchiveButton);
      createArchiveButton.Click += (_, _) =>
      {
        while (true)
        {
          var archiveName = PromptForArchiveName(
            ownerElement,
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
            var message = GetUserFriendlyCreateArchiveErrorMessage(ex);
            LogError(message);
            ShowArchiveNotification(
              "Создание архива",
              message,
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
      ApplyDialogButtonStyle(ownerElement, saveButton);
      saveButton.Click += (_, _) =>
      {
        if (archivesListBox.SelectedItem is ListBoxItem)
        {
          dialog.DialogResult = true;
          return;
        }

        var message = "Выберите архив из списка или создайте новый.";
        LogWarning(message);
        ShowArchiveNotification(
          "Сохранение в архив",
          message,
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
      ApplyDialogButtonStyle(ownerElement, cancelButton);

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

    private string PromptForArchiveName(FrameworkElement ownerElement, string suggestedArchiveName, bool isFirstArchive = false)
    {
      var dialog = new Window
      {
        Title = "Создание архива",
        Owner = Window.GetWindow(ownerElement),
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
        Background = GetThemeBrush(ownerElement, "IsCheckedColorSolidColorBrush", Color.FromRgb(230, 232, 236)),
        BorderBrush = GetThemeBrush(ownerElement, "ForegroundSolidColorBrush60", Color.FromRgb(120, 130, 140)),
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
        Foreground = GetThemeBrush(ownerElement, "ForegroundSolidColorBrush", Colors.Black),
        FontFamily = Application.Current?.Resources["WinstonMedium"] as FontFamily,
        FontSize = 16,
        TextWrapping = TextWrapping.Wrap,
      };

      var inputBorder = new Border
      {
        Background = GetThemeBrush(ownerElement, "PrimarySolidColorBrush", Color.FromRgb(239, 239, 224)),
        BorderBrush = GetThemeBrush(ownerElement, "ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0)),
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
        Foreground = GetThemeBrush(ownerElement, "ForegroundSolidColorBrush", Colors.Black),
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
      ApplyDialogButtonStyle(ownerElement, createButton);
      createButton.Click += (_, _) => dialog.DialogResult = true;

      var cancelButton = new Button
      {
        Content = "Отмена",
        MinWidth = 120,
        IsCancel = true,
      };
      ApplyDialogButtonStyle(ownerElement, cancelButton);

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

      var message = "Не удалось открыть папку архивов.";
      LogError(message);
      throw new DirectoryNotFoundException(message);
    }

    private static string CreateArchiveInFolder(string archivesFolderPath, string archiveName)
    {
      var normalizedArchiveName = NormalizeArchiveName(archiveName);
      var archivePath = Path.Combine(archivesFolderPath, normalizedArchiveName + ".apkw");

      if (File.Exists(archivePath))
      {
        var message = $"Архив '{Path.GetFileName(archivePath)}' уже существует.";
        LogError(message);
        throw new InvalidOperationException(message);
      }

      using (var archiveStream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        ArchiveManifestService.WriteManifest(archive, new List<ArchiveManifestFileRecord>());
      }

      FileEncryptionManager.EncryptFile(archivePath);

      return archivePath;
    }

    private static string NormalizeArchiveName(string archiveName)
    {
      if (string.IsNullOrWhiteSpace(archiveName))
      {
        var message = "Название архива обязательно.";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveName));
      }

      var normalizedName = Path.GetFileNameWithoutExtension(archiveName.Trim());
      foreach (var invalidChar in Path.GetInvalidFileNameChars())
      {
        normalizedName = normalizedName.Replace(invalidChar, '_');
      }

      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        var message = "Название архива содержит только недопустимые символы.";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveName));
      }

      return normalizedName;
    }

    private static void ApplyDialogButtonStyle(FrameworkElement ownerElement, Button button)
    {
      if (ownerElement.TryFindResource("ButtonStyleV10") is Style style)
      {
        button.Style = style;
      }

      button.Height = 44;
      button.Padding = new Thickness(14, 6, 14, 6);
      button.FontSize = 16;
    }

    private static Brush GetThemeBrush(FrameworkElement ownerElement, string key, Color fallbackColor)
    {
      if (ownerElement.TryFindResource(key) is Brush brush)
      {
        return brush;
      }

      if (Application.Current?.Resources[key] is Brush appBrush)
      {
        return appBrush;
      }

      return new SolidColorBrush(fallbackColor);
    }

    private static void ShowArchiveNotification(string title, string message, NotificationType notificationType)
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
  }
}
