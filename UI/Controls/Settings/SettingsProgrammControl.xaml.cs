using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.Localization;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Controls.Settings.Configuration;

namespace UI.Controls.Settings
{
  /// <summary>
  /// Логика взаимодействия для SettingsProgrammControl.xaml
  /// </summary>
  public partial class SettingsProgrammControl : UserControl
  {
    private readonly Action<SystemStateEvents.AdminRightsChanged> _adminRightsChangedHandler;
    private bool _isAdminRightsSubscribed;

    public SettingsProgrammControl()
    {
      InitializeComponent();
      _adminRightsChangedHandler = OnAdminRightsChanged;
      Loaded += SettingsProgrammControl_Loaded;
      Unloaded += SettingsProgrammControl_Unloaded;
    }

    private void SettingsProgrammControl_Loaded(object sender, RoutedEventArgs e)
    {
      LocalizationService.RefreshCurrentLanguage();
      UpdateImportExportVisibility(AdminConfig.GetAdminRights());

      if (_isAdminRightsSubscribed)
      {
        return;
      }

      EventAggregator.Subscribe(_adminRightsChangedHandler);
      _isAdminRightsSubscribed = true;
    }

    private void SettingsProgrammControl_Unloaded(object sender, RoutedEventArgs e)
    {
      if (!_isAdminRightsSubscribed)
      {
        return;
      }

      EventAggregator.Unsubscribe(_adminRightsChangedHandler);
      _isAdminRightsSubscribed = false;
    }

    private void OnAdminRightsChanged(SystemStateEvents.AdminRightsChanged eventData)
    {
      if (Dispatcher.CheckAccess())
      {
        UpdateImportExportVisibility(eventData.IsAdmin);
      }
      else
      {
        Dispatcher.Invoke(() => UpdateImportExportVisibility(eventData.IsAdmin));
      }
    }

    private void UpdateImportExportVisibility(bool isAdmin)
    {
      var visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
      ExportConfigButton.Visibility = visibility;
      ImportConfigButton.Visibility = visibility;
    }

    private async void PrintConfig(object sender, MouseButtonEventArgs e)
    {
      try
      {
        string printableText = await DeviceConfigurationPrintService.BuildPrintableConfigurationAsync();
        DeviceConfigurationPrintService.Print(printableText);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при формировании конфигурации: {ex.Message}", "Ошибка",
          MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void ExportConfig(object sender, MouseButtonEventArgs e)
    {
      try
      {
        var saveDialog = new SaveFileDialog
        {
          Title = "Экспорт конфигурации",
          Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*",
          DefaultExt = ".json",
          AddExtension = true,
          OverwritePrompt = true,
          FileName = $"askmkim-config-{DateTime.Now:yyyyMMdd-HHmmss}.json"
        };

        if (saveDialog.ShowDialog() != true)
        {
          return;
        }

        await DeviceConfigurationService.ExportToFileAsync(saveDialog.FileName);

        NotificationHostService.Instance.Show(
          "Экспорт конфигурации",
          $"Конфигурация сохранена в файл:\n{saveDialog.FileName}",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        NotificationHostService.Instance.Show(
          "Ошибка экспорта конфигурации",
          ex.Message,
          NotificationType.Error);
      }
    }

    private async void ImportConfig(object sender, MouseButtonEventArgs e)
    {
      try
      {
        var openDialog = new OpenFileDialog
        {
          Title = "Импорт конфигурации",
          Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*",
          DefaultExt = ".json",
          CheckFileExists = true,
          Multiselect = false
        };

        if (openDialog.ShowDialog() != true)
        {
          return;
        }

        var confirmation = Message.MessageBoxCustom.Show(
          "При импорте текущая конфигурация устройств будет полностью удалена и заменена содержимым JSON-файла. Продолжить?",
          "Импорт конфигурации",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
          return;
        }

        await DeviceConfigurationService.ImportFromFileAsync(openDialog.FileName);

        DeviceConfigManager?.ReloadConfiguration();

        NotificationHostService.Instance.Show(
          "Импорт конфигурации",
          "Конфигурация успешно импортирована.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        NotificationHostService.Instance.Show(
          "Ошибка импорта конфигурации",
          ex.Message,
          NotificationType.Error);
      }
    }
  }
}
