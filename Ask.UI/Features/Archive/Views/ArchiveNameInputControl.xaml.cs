using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Features.Archive.Views
{
  public partial class ArchiveNameInputControl : UserControl
  {
    public ArchiveNameInputControl()
    {
      InitializeComponent();
      Loaded += OnLoaded;
    }

    public event RoutedEventHandler? ConfirmRequested;

    public event TextChangedEventHandler? TextChanged;

    public string? ArchiveName => ArchiveNameTextBox.Text?.Trim();

    public void Initialize(FrameworkElement ownerElement, string suggestedArchiveName, bool isFirstArchive)
    {
      ApplyTheme(ownerElement);
      PromptTextBlock.Text = isFirstArchive
        ? "Архивы не найдены. Введите название нового архива:"
        : "Введите название нового архива:";
      ArchiveNameTextBox.Text = string.IsNullOrWhiteSpace(suggestedArchiveName) ? "new_archive" : suggestedArchiveName;
    }

    public void ClearError()
    {
      ErrorTextBlock.Text = string.Empty;
      ErrorTextBlock.Visibility = Visibility.Collapsed;
    }

    public void ShowError(string message)
    {
      ErrorTextBlock.Text = message;
      ErrorTextBlock.Visibility = string.IsNullOrWhiteSpace(message)
        ? Visibility.Collapsed
        : Visibility.Visible;
      ArchiveNameTextBox.Focus();
      ArchiveNameTextBox.SelectAll();
    }

    private void ApplyTheme(FrameworkElement ownerElement)
    {
      Resources["DialogShellBackgroundBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "IsCheckedColorSolidColorBrush", Color.FromRgb(230, 232, 236));
      Resources["DialogShellBorderBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "ForegroundSolidColorBrush60", Color.FromRgb(120, 130, 140));
      Resources["DialogForegroundBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "ForegroundSolidColorBrush", Colors.Black);
      Resources["DialogInputBackgroundBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "PrimarySolidColorBrush", Color.FromRgb(239, 239, 224));
      Resources["DialogInputBorderBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0));
      Resources["DialogErrorBrush"] = ArchiveDialogStyling.GetBrush(ownerElement, "RedColorSolidColorBrush", Color.FromRgb(178, 58, 72));

      PromptTextBlock.FontFamily = ArchiveDialogStyling.GetMediumFontFamily();
      ArchiveDialogStyling.TryApplyButtonStyle(ownerElement, CreateButton);
      ArchiveDialogStyling.TryApplyButtonStyle(ownerElement, CancelButton);
    }

    private void CreateButton_OnClick(object sender, RoutedEventArgs e)
    {
      ConfirmRequested?.Invoke(this, e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      ArchiveNameTextBox.Focus();
      ArchiveNameTextBox.SelectAll();
    }

    private void ArchiveNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
      TextChanged?.Invoke(this, e);
    }
  }
}
