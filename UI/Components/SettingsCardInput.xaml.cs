using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using System.Globalization;
using System.Windows.Controls;
using UI.Controls.AdminPanel;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для SettingsCardInput.xaml
  /// </summary>
  public partial class SettingsResistanceInput : UserControl
  {
    public Action<double> ChangeTextEvent;
    private bool _internalChange;
    private double defaultValue;
    public SettingsResistanceInput(IRelaySwitchModule relaySwitchModule, CheckResistanceControl baseControl)
    {
      InitializeComponent();
      DataContext = relaySwitchModule;
      defaultValue = relaySwitchModule.SwitchResistance;

      ResistanceDataTexBox.TextChanged += ResistanceDevice_TextChanged;
      baseControl.SetDefaultValue += SetDefaultValue;
    }

    public void SetDefaultValue()
    {
      ResistanceDataTexBox.Text = defaultValue.ToString();
    }

    private void ResistanceDevice_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (_internalChange)
        return;

      if (sender is not TextBox textBox)
        return;

      _internalChange = true;

      string text = textBox.Text.Replace(',', '.');

      if (text.StartsWith("."))
      {
        textBox.Text = string.Empty;
        _internalChange = false;
        return;
      }

      if (text.Count(c => c == '.') > 1)
      {
        textBox.Text = text.Remove(text.LastIndexOf('.'), 1);
      }

      textBox.Text = text;
      textBox.CaretIndex = textBox.Text.Length;

      _internalChange = false;

      if (double.TryParse(
            text,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out double result))
      {
        ChangeTextEvent?.Invoke(result);
      }
    }
  }
}
