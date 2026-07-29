using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Features.ServiceTools.Gpt
{
  /// <summary>
  /// Контрол для управления режимом GPTPunch.
  /// </summary>
  public partial class GPTPunchControl : UserControl
  {
    private readonly Func<Task<IBreakdownTester?>> deviceProvider;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="GPTPunchControl"/>.
    /// </summary>
    /// <param name="deviceProvider">Функция получения настроенной пробойной установки.</param>
    public GPTPunchControl(Func<Task<IBreakdownTester?>> deviceProvider)
    {
      this.deviceProvider = deviceProvider
        ?? throw new ArgumentNullException(nameof(deviceProvider));
      InitializeComponent();
      Controller.Visibility = Visibility.Visible;
      Loaded += GPTPunchControl_Loaded;
    }

    /// <summary>
    /// Асинхронно загружает пробойную установку из конфигурации оборудования.
    /// </summary>
    /// <param name="sender">Загруженный элемент управления.</param>
    /// <param name="e">Данные события загрузки.</param>
    private async void GPTPunchControl_Loaded(object sender, RoutedEventArgs e)
    {
      Loaded -= GPTPunchControl_Loaded;

      try
      {
        var device = await deviceProvider();
        Controller.Device = device;

        if (device == null)
        {
          Ask.LogLib.LoggerUtility.LogError(
            "GPT — устройство не найдено в конфигурации шасси № 1.",
            isDeviceLog: true);
          return;
        }

        Ask.LogLib.LoggerUtility.LogInformation(
          $"GPT — для ручного управления выбрано устройство «{device.Name}».",
          isDeviceLog: true);
      }
      catch (Exception ex)
      {
        Controller.Device = null;
        GptUiOperation.ReportError("загрузка устройства", ex);
      }
    }
  }
}
