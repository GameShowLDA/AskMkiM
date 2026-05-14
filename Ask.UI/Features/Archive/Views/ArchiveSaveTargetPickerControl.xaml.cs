using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Features.Archive.Views
{
  public partial class ArchiveSaveTargetPickerControl : UserControl
  {
    public ArchiveSaveTargetPickerControl()
    {
      InitializeComponent();
    }

    public event RoutedEventHandler? CreateArchiveRequested;

    public event RoutedEventHandler? ConfirmRequested;

    public string? SelectedArchivePath => (ArchivesListBox.SelectedItem as ArchiveItem)?.ArchivePath;

    public void Initialize(FrameworkElement ownerElement, IReadOnlyList<string> archivePaths)
    {
      ApplyTheme(ownerElement);
      PromptTextBlock.Text = archivePaths.Count == 0
        ? "Архивы не найдены. Создайте новый архив:"
        : "Выберите архив для сохранения:";

      ArchivesListBox.Items.Clear();
      foreach (var archivePath in archivePaths)
      {
        ArchivesListBox.Items.Add(new ArchiveItem(archivePath));
      }

      if (ArchivesListBox.Items.Count > 0)
      {
        ArchivesListBox.SelectedIndex = 0;
      }

      UpdateSaveState();
    }

    public void AddArchive(string archivePath)
    {
      var archiveItem = new ArchiveItem(archivePath);
      ArchivesListBox.Items.Add(archiveItem);
      ArchivesListBox.SelectedItem = archiveItem;
      ArchivesListBox.ScrollIntoView(archiveItem);
      UpdateSaveState();
    }

    private void ApplyTheme(FrameworkElement ownerElement)
    {
      Resources["DialogShellBackgroundBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "IsCheckedColorSolidColorBrush", Color.FromRgb(230, 232, 236));
      Resources["DialogShellBorderBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "ForegroundSolidColorBrush60", Color.FromRgb(120, 130, 140));
      Resources["DialogForegroundBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "ForegroundSolidColorBrush", Colors.Black);
      Resources["DialogListBackgroundBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "PrimarySolidColorBrush", Color.FromRgb(239, 239, 224));
      Resources["DialogListBorderBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0));
      Resources["DialogListAccentBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "ActiveForegroundSolidColorBrush80", Color.FromArgb(120, 164, 235, 158));

      PromptTextBlock.FontFamily = ArchiveDialogStyling.GetMediumFontFamily();
      ArchiveDialogStyling.TryApplyButtonStyle(ownerElement, CreateArchiveButton);
      ArchiveDialogStyling.TryApplyButtonStyle(ownerElement, SaveButton);
      ArchiveDialogStyling.TryApplyButtonStyle(ownerElement, CancelButton);
    }

    private void ArchivesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      UpdateSaveState();
    }

    private void CreateArchiveButton_OnClick(object sender, RoutedEventArgs e)
    {
      CreateArchiveRequested?.Invoke(this, e);
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
      ConfirmRequested?.Invoke(this, e);
    }

    private void UpdateSaveState()
    {
      SaveButton.IsEnabled = SelectedArchivePath != null;
    }

    private sealed class ArchiveItem
    {
      public ArchiveItem(string archivePath)
      {
        ArchivePath = archivePath;
        DisplayName = Path.GetFileName(archivePath);
      }

      public string ArchivePath { get; }

      public string DisplayName { get; }

      public override string ToString()
      {
        return DisplayName;
      }
    }
  }
}
