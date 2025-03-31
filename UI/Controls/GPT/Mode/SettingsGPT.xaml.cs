using System.Windows;
using System.Windows.Controls;
using YamlDotNet.Core.Tokens;

namespace UI.Controls.GPT.Mode
{
  /// <summary>
  /// Компонент для управления настройками устройства GPT.
  /// При инициализации загружает текущую конфигурацию и устанавливает режим DCW.
  /// </summary>
  public partial class SettingsGPT : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SettingsGPT"/>.
    /// </summary>
    public SettingsGPT()
    {
      InitializeComponent();
      LoadConfigurationAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Асинхронно загружает конфигурацию устройства и обновляет элементы управления.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию загрузки конфигурации.</returns>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.SystemManger.ReadConfigurationAsync();

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
    /// Перебирает элементы в ComboBox и выбирает тот, у которого содержимое совпадает с переданным значением.
    /// </summary>
    /// <param name="contrastValue">Значение контраста дисплея.</param>
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
    /// Перебирает элементы в ComboBox и выбирает тот, у которого первая часть содержимого совпадает с переданным значением.
    /// </summary>
    /// <param name="brightnessValue">Значение яркости дисплея.</param>
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
    /// Перебирает элементы ComboBox и выбирает тот, который соответствует переданному состоянию.
    /// </summary>
    /// <param name="isEnabled">Если <c>true</c>, выбирается значение "ON", иначе "OFF".</param>
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
    /// Перебирает элементы ComboBox и выбирает тот, который соответствует переданному состоянию.
    /// </summary>
    /// <param name="isEnabled">Если <c>true</c>, выбирается значение "ON", иначе "OFF".</param>
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
    /// <param name="duration">Длительность сигнала (в секундах).</param>
    private void SetSuccessSoundDuration(double duration)
    {
      SuccessSoundSlider.Value = duration;
    }

    /// <summary>
    /// Устанавливает продолжительность звука ошибочного теста.
    /// </summary>
    /// <param name="duration">Длительность сигнала (в секундах).</param>
    private void SetErrorSoundDuration(double duration)
    {
      ErrorSoundSlider.Value = duration;
    }

    /// <summary>
    /// Обрабатывает изменение выбранного элемента в ComboBox для контраста дисплея.
    /// Вызывает изменение значения на устройстве.
    /// </summary>
    private async void ContrastComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ContrastComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        string contrastValue = selectedItem.Content.ToString();
        double contrast = double.Parse(contrastValue);
        OnValueChanged("LCD_CONTRAST", contrast);

        await GPTPunchControl.ModelGPT.SystemManger.SetLcdContrastAsync(contrast);
      }
    }

    /// <summary>
    /// Обрабатывает изменение выбранного элемента в ComboBox для яркости дисплея.
    /// Вызывает изменение значения на устройстве.
    /// </summary>
    private async void BrightnessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (BrightnessComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        string brightnessValue = selectedItem.Content.ToString().Split('-')[0].Trim();
        double brightness = double.Parse(brightnessValue);
        OnValueChanged("LCD_BRIGHTNESS", brightness);

        await GPTPunchControl.ModelGPT.SystemManger.SetLcdBrightnessAsync(brightness);
      }
    }

    /// <summary>
    /// Обрабатывает изменение выбранного элемента в ComboBox для звука успешного теста.
    /// Вызывает изменение состояния звука на устройстве.
    /// </summary>
    private async void SuccessSoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (SuccessSoundComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        string soundState = selectedItem.Content.ToString();
        double value = soundState == "ON" ? 1 : 0;
        OnValueChanged("BUZZER_PSOUND", value);

        await GPTPunchControl.ModelGPT.SystemManger.SetBuzzerPrimarySound(value == 1 ? true : false);
      }
    }

    /// <summary>
    /// Обрабатывает изменение выбранного элемента в ComboBox для звука ошибочного теста.
    /// Вызывает изменение состояния звука на устройстве.
    /// </summary>
    private async void ErrorSoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ErrorSoundComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        string soundState = selectedItem.Content.ToString();
        double value = soundState == "ON" ? 1 : 0;
        OnValueChanged("BUZZER_FSOUND", value);

        await GPTPunchControl.ModelGPT.SystemManger.SetBuzzerFeedbackSound(value == 1 ? true : false);
      }
    }

    /// <summary>
    /// Обрабатывает изменение значения слайдера для продолжительности звука успешного теста.
    /// Вызывает изменение продолжительности сигнала на устройстве.
    /// </summary>
    private async void SuccessSoundSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double duration = SuccessSoundSlider.Value;
      OnValueChanged("BUZZER_PTIME", duration);
      await GPTPunchControl.ModelGPT.SystemManger.SetBuzzerPrimaryTime(duration);
    }

    /// <summary>
    /// Обрабатывает изменение значения слайдера для продолжительности звука ошибочного теста.
    /// Вызывает изменение продолжительности сигнала на устройстве.
    /// </summary>
    private async void ErrorSoundSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double duration = ErrorSoundSlider.Value;
      OnValueChanged("BUZZER_FTIME", duration);
      await GPTPunchControl.ModelGPT.SystemManger.SetBuzzerFeedbackTime(duration);
    }

    /// <summary>
    /// Вызывает обработчик изменения значения для указанного свойства.
    /// </summary>
    /// <param name="propertyName">Имя свойства.</param>
    /// <param name="value">Новое значение свойства.</param>
    private void OnValueChanged(string propertyName, double value)
    {
      Console.WriteLine($"{propertyName}: {value}");
    }

    /// <summary>
    /// Получает значение свойства по его имени.
    /// </summary>
    /// <param name="propertyName">Имя свойства.</param>
    /// <returns>Значение свойства.</returns>
    /// <exception cref="ArgumentException">Выбрасывается, если свойство не найдено.</exception>
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