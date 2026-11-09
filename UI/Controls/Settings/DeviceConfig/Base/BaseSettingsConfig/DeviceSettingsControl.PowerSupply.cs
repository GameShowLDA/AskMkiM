using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;

/// <summary>
/// Дополняет общую IP-форму TCP-портом внешнего источника питания.
/// </summary>
public partial class DeviceSettingsControl
{
  private TextBox? _powerSupplyPortTextBox;

  /// <summary>
  /// Возвращает TCP-порт из дополнительных параметров источника.
  /// </summary>
  /// <returns>Порт от 1 до 65535 или ноль при некорректном вводе.</returns>
  public int GetPowerSupplyPort()
    => int.TryParse(_powerSupplyPortTextBox?.Text, out int port) && port is >= 1 and <= 65535 ? port : 0;

  private void ShowPowerSupplyAdditionalSettings()
  {
    _powerSupplyPortTextBox = new TextBox
    {
      Style = (Style)FindResource("DeviceSettingsUnifiedTextBoxStyle"),
      Text = "80",
      MaxLength = 5,
    };
    _powerSupplyPortTextBox.PreviewTextInput += IntegerDevice_PreviewTextInput;
    RegisterValidationSource(_powerSupplyPortTextBox, AdditionalSettingsContainer);
    var grid = new Grid();
    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
    var title = new TextBlock { Text = "TCP-порт", FontSize = 20, FontWeight = FontWeights.Bold,
      Margin = new Thickness(7, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center };
    title.SetResourceReference(ForegroundProperty, "ForegrounfBrushes");
    var titleBar = new Border { Style = (Style)FindResource("DeviceInputSectionTitleBarStyle"), Child = title };
    var input = new Border { Style = (Style)FindResource("DeviceSettingsUnifiedInputBorderStyle"), Child = _powerSupplyPortTextBox };
    Grid.SetRow(input, 1);
    grid.Children.Add(titleBar);
    grid.Children.Add(input);
    AdditionalSettingsContainer.Content = new Border { Style = (Style)FindResource("DeviceInputSectionCardStyle"), Child = grid };
  }
}
