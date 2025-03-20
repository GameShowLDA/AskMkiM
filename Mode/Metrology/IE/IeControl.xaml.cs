using System.Windows.Controls;
using System.Windows.Media;
using Core.Abstract;
using Mode.Base.SearchDevices;
using Mode.Models;
using static AppConfig.Config.LoopConfig;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.Metrology.IE
{
  /// <summary>
  /// Логика взаимодействия для IeControl.xaml.
  /// </summary>
  public partial class IeControl : UserControl
  {
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;

    private DataElectricModel measurementDataModel;

    private Core.DeviceBusCommutation.Model deviceBusCommutation;
    private MeterBase meter;

    private bool completed;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="IeControl"/>.
    /// </summary>
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