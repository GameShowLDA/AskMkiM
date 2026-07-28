using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.DataBase.Engine.Static.Devices;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.GPT
{
  /// <summary>
  /// Контрол для управления режимом GPTPunch.
  /// </summary>
  public partial class GPTPunchControl : UserControl
  {
    /// <summary>
    /// Статическая модель GPT, используемая для подключения и проверки связи.
    /// </summary>
    static internal IBreakdownTester? ModelGPT { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="GPTPunchControl"/>.
    /// </summary>
    public GPTPunchControl()
    {
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
        ModelGPT = (await BreakdownTesters.GetDevicesByNumberChassisAsync(1))
          .FirstOrDefault();

        if (ModelGPT == null)
        {
          Ask.LogLib.LoggerUtility.LogError(
            "GPT — устройство не найдено в конфигурации шасси № 1.",
            isDeviceLog: true);
          return;
        }

        Ask.LogLib.LoggerUtility.LogInformation(
          $"GPT — для ручного управления выбрано устройство «{ModelGPT.Name}».",
          isDeviceLog: true);
      }
      catch (Exception ex)
      {
        ModelGPT = null;
        GptUiOperation.ReportError("загрузка устройства", ex);
      }
    }
  }
}
