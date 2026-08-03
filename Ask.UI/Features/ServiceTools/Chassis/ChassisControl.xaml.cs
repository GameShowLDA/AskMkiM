using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.LogLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Features.ServiceTools.Chassis
{
  /// <summary>
  /// Предоставляет ручное управление контроллером и питанием шасси.
  /// </summary>
  public partial class ChassisControl : UserControl
  {
    private readonly Func<Task<IChassisManager?>> chassisProvider;
    private IChassisManager? chassis;
    private bool operationInProgress;

    /// <summary>
    /// Инициализирует панель сервисного управления шасси.
    /// </summary>
    /// <param name="chassisProvider">Функция получения настроенного контроллера шасси.</param>
    public ChassisControl(Func<Task<IChassisManager?>> chassisProvider)
    {
      this.chassisProvider = chassisProvider
        ?? throw new ArgumentNullException(nameof(chassisProvider));
      InitializeComponent();
      Loaded += ChassisControl_Loaded;
    }

    private async void ChassisControl_Loaded(object sender, RoutedEventArgs e)
    {
      Loaded -= ChassisControl_Loaded;
      await LoadChassisAsync();
    }

    private async Task LoadChassisAsync()
    {
      try
      {
        chassis = await chassisProvider();
        DeviceStatusText.Text = chassis is null
          ? "Контроллер шасси не найден в конфигурации."
          : $"{chassis.Name}, шасси №{chassis.Number}";
        DeviceStatusIndicator.Background = chassis is null
          ? Brushes.IndianRed
          : Brushes.MediumSeaGreen;
      }
      catch (Exception exception)
      {
        chassis = null;
        DeviceStatusIndicator.Background = Brushes.IndianRed;
        DeviceStatusText.Text = "Не удалось загрузить контроллер шасси.";
        ReportError("загрузка контроллера", exception);
      }
    }

    private async Task ExecuteAsync(string operation, Func<IChassisManager, Task<string>> action)
    {
      if (operationInProgress)
      {
        return;
      }

      try
      {
        operationInProgress = true;
        IsEnabled = false;
        IChassisManager current = chassis
          ?? throw new InvalidOperationException("Контроллер шасси не найден в конфигурации.");
        string result = await action(current);
        OperationResultText.Text = $"{operation}: {result}";
        DeviceStatusIndicator.Background = Brushes.MediumSeaGreen;
        LoggerUtility.LogInformation($"Шасси — {operation}: {result}.", isDeviceLog: true);
      }
      catch (Exception exception)
      {
        ReportError(operation, exception);
      }
      finally
      {
        IsEnabled = true;
        operationInProgress = false;
      }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadChassisAsync();

    private async void InitializeButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("Инициализация", async current =>
      {
        var result = await current.ConnectableManager.InitializeAsync();
        if (!result.Connect)
        {
          throw new InvalidOperationException(result.Answer);
        }

        return "успешно";
      });
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("Полный сброс", async current =>
      {
        if (!await current.ConnectableManager.ResetAsync())
        {
          throw new InvalidOperationException("Контроллер не подтвердил полный сброс.");
        }

        return "успешно";
      });
    }

    private async void StartPowerButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("Включение питания", async current =>
      {
        await current.PowerManager.StartPowerAsync();
        return "команда выполнена";
      });
    }

    private async void StopPowerButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("Выключение питания", async current =>
      {
        await current.PowerManager.StopPowerAsync();
        return "команда выполнена";
      });
    }

    private async void VerifyPowerButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("Проверка питания", async current =>
        await current.PowerManager.VerifyPowerAsync() ? "питание включено" : "питание выключено");
    }

    private void ReportError(string operation, Exception exception)
    {
      OperationResultText.Text = $"{operation}: ошибка — {exception.Message}";
      DeviceStatusIndicator.Background = Brushes.IndianRed;
      LoggerUtility.LogException(exception, $"Шасси — {operation}", isDeviceLog: true);
    }
  }
}
