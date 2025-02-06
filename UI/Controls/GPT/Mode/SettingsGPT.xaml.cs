using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.GPT.Mode
{
  public partial class SettingsGPT : UserControl
  {
    public SettingsGPT()
    {
      InitializeComponent();
      LoadConfigurationAsync().ConfigureAwait(true);
    }


    /// <summary>
    /// Метод для загрузки конфигурации и заполнения элементов управления.
    /// </summary>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        // Считываем текущую конфигурацию устройства
        var systemData = await Core.GptLibrary.SystemSettings.ReadConfigurationAsync(GPTPunchControl.ModelGPT);

        // Заполняем элементы управления
        SetContrast(systemData.LcdContrast);
        SetBrightness(systemData.LcdBrightness);
        SetSuccessSound(systemData.BuzzerPrimarySound);
        SetErrorSound(systemData.BuzzerFeedbackSound);
        SetSuccessSoundDuration(systemData.BuzzerPrimaryTime);
        SetErrorSoundDuration(systemData.BuzzerFeedbackTime);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при загрузке конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Устанавливает значение контраста дисплея.
    /// </summary>
    private void SetContrast(int contrastValue)
    {
      foreach (ComboBoxItem item in ContrastComboBox.Items)
      {
        if (item.Content.ToString() == contrastValue.ToString())
        {
          ContrastComboBox.SelectedItem = item;
          break;
        }
      }
    }

    /// <summary>
    /// Устанавливает значение яркости дисплея.
    /// </summary>
    private void SetBrightness(int brightnessValue)
    {
      foreach (ComboBoxItem item in BrightnessComboBox.Items)
      {
        string value = item.Content.ToString().Split('-')[0].Trim();
        if (value == brightnessValue.ToString())
        {
          BrightnessComboBox.SelectedItem = item;
          break;
        }
      }
    }

    /// <summary>
    /// Устанавливает состояние звука успешного теста.
    /// </summary>
    private void SetSuccessSound(bool isEnabled)
    {
      foreach (ComboBoxItem item in SuccessSoundComboBox.Items)
      {
        if ((item.Content.ToString() == "ON" && isEnabled) || (item.Content.ToString() == "OFF" && !isEnabled))
        {
          SuccessSoundComboBox.SelectedItem = item;
          break;
        }
      }
    }

    /// <summary>
    /// Устанавливает состояние звука ошибочного теста.
    /// </summary>
    private void SetErrorSound(bool isEnabled)
    {
      foreach (ComboBoxItem item in ErrorSoundComboBox.Items)
      {
        if ((item.Content.ToString() == "ON" && isEnabled) || (item.Content.ToString() == "OFF" && !isEnabled))
        {
          ErrorSoundComboBox.SelectedItem = item;
          break;
        }
      }
    }

    /// <summary>
    /// Устанавливает продолжительность звука успешного теста.
    /// </summary>
    private void SetSuccessSoundDuration(double duration)
    {
      SuccessSoundSlider.Value = duration;
    }

    /// <summary>
    /// Устанавливает продолжительность звука ошибочного теста.
    /// </summary>
    private void SetErrorSoundDuration(double duration)
    {
      ErrorSoundSlider.Value = duration;
    }

    // Обработчик изменения контраста
    private async void ContrastComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ContrastComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        string contrastValue = selectedItem.Content.ToString();
        double contrast = double.Parse(contrastValue);
        OnValueChanged("LCD_CONTRAST", contrast);

        await Core.GptLibrary.SystemSettings.SetLcdContrastAsync(GPTPunchControl.ModelGPT, contrast);
      }
    }

    // Обработчик изменения яркости
    private async void BrightnessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (BrightnessComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        string brightnessValue = selectedItem.Content.ToString().Split('-')[0].Trim();
        double brightness = double.Parse(brightnessValue);
        OnValueChanged("LCD_BRIGHTNESS", brightness);

        await Core.GptLibrary.SystemSettings.SetLcdBrightnessAsync(GPTPunchControl.ModelGPT, brightness);
      }
    }

    // Обработчик изменения звука успешного теста
    private async void SuccessSoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (SuccessSoundComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        string soundState = selectedItem.Content.ToString();
        double value = soundState == "ON" ? 1 : 0;
        OnValueChanged("BUZZER_PSOUND", value);

        await Core.GptLibrary.SystemSettings.SetBuzzerPrimarySound(GPTPunchControl.ModelGPT, value == 1 ? true : false);
      }
    }

    // Обработчик изменения звука ошибочного теста
    private async void ErrorSoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ErrorSoundComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        string soundState = selectedItem.Content.ToString();
        double value = soundState == "ON" ? 1 : 0;
        OnValueChanged("BUZZER_FSOUND", value);

        await Core.GptLibrary.SystemSettings.SetBuzzerFeedbackSound(GPTPunchControl.ModelGPT, value == 1 ? true : false);
      }
    }

    // Обработчик изменения продолжительности звука успешного теста
    private async void SuccessSoundSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double duration = SuccessSoundSlider.Value;
      OnValueChanged("BUZZER_PTIME", duration);

      await Core.GptLibrary.SystemSettings.SetBuzzerPrimaryTime(GPTPunchControl.ModelGPT, duration);
    }

    // Обработчик изменения продолжительности звука ошибочного теста
    private async void ErrorSoundSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double duration = ErrorSoundSlider.Value;
      OnValueChanged("BUZZER_FTIME", duration);

      await Core.GptLibrary.SystemSettings.SetBuzzerFeedbackTime(GPTPunchControl.ModelGPT, duration);
    }

    // Метод для обработки изменений значений
    private void OnValueChanged(string propertyName, double value)
    {
      // Здесь вы можете реализовать дальнейшую логику
      Console.WriteLine($"{propertyName}: {value}");
    }

    // Метод для получения значения по имени свойства
    public double GetValue(string propertyName)
    {
      return propertyName switch
      {
        "LCD_CONTRAST" => double.Parse((ContrastComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()),
        "LCD_BRIGHTNESS" => double.Parse((BrightnessComboBox.SelectedItem as ComboBoxItem)?.Content.ToString().Split('-')[0].Trim()),
        "BUZZER_PSOUND" => (SuccessSoundComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() == "ON" ? 1 : 0,
        "BUZZER_FSOUND" => (ErrorSoundComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() == "ON" ? 1 : 0,
        "BUZZER_PTIME" => SuccessSoundSlider.Value,
        "BUZZER_FTIME" => ErrorSoundSlider.Value,
        _ => throw new ArgumentException($"Unknown property: {propertyName}")
      };
    }
  }
}