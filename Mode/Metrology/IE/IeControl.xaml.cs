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


namespace Mode.Metrology.IE
{
  /// <summary>
  /// Логика взаимодействия для IeControl.xaml
  /// </summary>
  public partial class IeControl : UserControl
  {
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;

    private DataElectricModel measurementDataModel;

    private Core.DeviceBusCommutation.Model deviceBusCommutation;
    private MeterBase meter;

    private bool completed;
    public IeControl()
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
      LogInformation("Настройка элементов управления режима ИЕ");

      ProtocolSelfCheckControl.SetSettings(this, StartDelegate: ExecuteMeasurementProcess, true, null, ReturnDelegate: PerformCapacityMeasurement);
      ProtocolSelfCheckControl.Header = "Режим ИЕ";

      await ProtocolSelfCheckControl.ClearContent();
      StackPanel contentStack = InputControlSettings.InitializeSettings(out measurementDataModel, InputControlSettings.ElectricParameter.Capacitance);
      ProtocolSelfCheckControl.AddContent(contentStack);

      await ConfigureProtocolSelfCheckControlAsync();
      LogInformation("Настройка элементов управления режима ИЕ завершена");
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

