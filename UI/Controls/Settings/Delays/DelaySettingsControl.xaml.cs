using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Ask.Core.Shared.Metadata.Static.Delays;
using Message;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls.Settings.Delays
{
  /// <summary>
  /// Редактирует фиксированные задержки оборудования.
  /// </summary>
  public partial class DelaySettingsControl : UserControl
  {
    private DelaySettings _savedSettings = new();
    private bool _isUpdatingText;

    /// <summary>
    /// Инициализирует экран настройки задержек оборудования.
    /// </summary>
    public DelaySettingsControl()
    {
      InitializeComponent();
      Loaded += DelaySettingsControl_Loaded;
    }

    private void DelaySettingsControl_Loaded(object sender, RoutedEventArgs e)
    {
      _savedSettings = AppDelays.GetSettings();
      RestoreSavedValues();
    }

    private void SaveIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (!CanEditDelaySettings())
      {
        MessageBoxCustom.Show(
          "Изменять задержки оборудования могут только root и администратор.",
          "Недостаточно прав",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
        return;
      }

      if (!TryReadSettings(out DelaySettings settings))
      {
        MessageBoxCustom.Show(
          "Все задержки должны быть целыми неотрицательными числами.",
          "Некорректные значения",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
        return;
      }

      try
      {
        AppDelays.SaveAndApply(settings);
        _savedSettings = AppDelays.GetSettings();
        SetChangeActionsVisible(false);
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show(
          $"Не удалось сохранить настройки задержек: {ex.Message}",
          "Ошибка сохранения",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
    }

    private void CancelIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
      => RestoreSavedValues();

    private void DelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (_isUpdatingText)
      {
        return;
      }

      SetChangeActionsVisible(!MatchesSavedSettings());
    }

    private void DelayTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
      => e.Handled = e.Text.Any(character => !char.IsDigit(character));

    private void DelayTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      if (!e.DataObject.GetDataPresent(DataFormats.Text))
      {
        e.CancelCommand();
        return;
      }

      string text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
      if (string.IsNullOrEmpty(text) || text.Any(character => !char.IsDigit(character)))
      {
        e.CancelCommand();
      }
    }

    private void RestoreSavedValues()
    {
      _isUpdatingText = true;
      try
      {
        BreakdownPostTestDelayTextBox.Text =
          _savedSettings.BreakdownTester.PostTestDelay.ToString(CultureInfo.InvariantCulture);
        ModulePreCommandDelayTextBox.Text =
          _savedSettings.ModuleRelayControl.PreCommandDelay.ToString(CultureInfo.InvariantCulture);
        ModulePostCommandDelayTextBox.Text =
          _savedSettings.ModuleRelayControl.PostCommandDelay.ToString(CultureInfo.InvariantCulture);
      }
      finally
      {
        _isUpdatingText = false;
      }

      SetChangeActionsVisible(false);
    }

    private bool MatchesSavedSettings()
    {
      return TryReadSettings(out DelaySettings settings)
        && settings.BreakdownTester.PostTestDelay == _savedSettings.BreakdownTester.PostTestDelay
        && settings.ModuleRelayControl.PreCommandDelay == _savedSettings.ModuleRelayControl.PreCommandDelay
        && settings.ModuleRelayControl.PostCommandDelay == _savedSettings.ModuleRelayControl.PostCommandDelay;
    }

    private bool TryReadSettings(out DelaySettings settings)
    {
      settings = new DelaySettings();
      if (!TryReadDelay(BreakdownPostTestDelayTextBox, out int breakdownPostTestDelay)
        || !TryReadDelay(ModulePreCommandDelayTextBox, out int modulePreCommandDelay)
        || !TryReadDelay(ModulePostCommandDelayTextBox, out int modulePostCommandDelay))
      {
        return false;
      }

      settings.BreakdownTester.PostTestDelay = breakdownPostTestDelay;
      settings.ModuleRelayControl.PreCommandDelay = modulePreCommandDelay;
      settings.ModuleRelayControl.PostCommandDelay = modulePostCommandDelay;
      return true;
    }

    private static bool TryReadDelay(TextBox textBox, out int delay)
      => int.TryParse(
        textBox.Text,
        NumberStyles.None,
        CultureInfo.InvariantCulture,
        out delay);

    private void SetChangeActionsVisible(bool visible)
    {
      Visibility visibility = visible ? Visibility.Visible : Visibility.Collapsed;
      SaveIcon.Visibility = visibility;
      CancelIcon.Visibility = visibility;
    }

    private static bool CanEditDelaySettings()
      => RoleAuthorizationConfig.CurrentRole is RoleType.Root or RoleType.Administrator;
  }
}
