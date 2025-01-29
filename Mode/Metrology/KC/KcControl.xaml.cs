using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Core.Abstract;
using Mode.Base.SearchDevices;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.ProtocolConfig;
using static AppConfig.Config.LoopConfig;
using static AppConfig.Config.SystemStateManager;
using static AppConfig.EventAggregator;
using static AppConfig.SettingsFileReader;
using static Utilities.Models.ShowMessageModel;
using static Utilities.LoggerUtility;
using Mode.Models;


namespace Mode.Metrology.KC
{

  /// <summary>
  /// Логика взаимодействия для KcControl.xaml
  /// </summary>
  public partial class KcControl : UserControl
  {
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;

    private DataElectricModel measurementDataModel;

    private Core.DeviceBusCommutation.Model deviceBusCommutation;
    private MeterBase meter;

    private bool completed;
    public KcControl()
    {
      InitializeComponent();
      InitializeSettingsAsync().ConfigureAwait(true);
    }


    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public async Task InitializeSettingsAsync()
    {
      try
      {
        LogInformation("Настройка элементов управления режима КС");

        ProtocolSelfCheckControl.SetSettings(this, StartDelegate: ExecuteMeasurementProcess, true, null, ReturnDelegate: PerformResistanceMeasurement);
        ProtocolSelfCheckControl.Header = "Режим КС";

        await ProtocolSelfCheckControl.ClearContent();

        StackPanel contentStack = InputControlSettings.InitializeSettings(out measurementDataModel, InputControlSettings.ElectricParameter.Resistance);
        ProtocolSelfCheckControl.AddContent(contentStack);

        await ConfigureProtocolSelfCheckControlAsync();
        LogInformation("Настройка элементов управления режима КС завершена");
      }
      catch (Exception ex)
      {
        var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        LogError($"Ошибка загрузки элемента метрологии КС в методе {methodName}: {ex.Message}");
      }
    }

    /// <summary>
    /// Конфигурирует видимые элементы управления ProtocolSelfCheckControl.
    /// Скрывает ненужные кнопки и задает заголовок для компонента.
    /// </summary>
    private async Task ConfigureProtocolSelfCheckControlAsync()
    {
      ProtocolSelfCheckControl.ProtocolTextBox.IsReadOnly = true;
      await SetLoopMeasurement(false);
      ProtocolSelfCheckControl.ShowOnlyStartButton();
    }
  }
}
