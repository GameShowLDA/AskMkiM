using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Settings;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls.Settings.Protocol
{
  /// <summary>
  /// Контрол раздела «Протокол»: отображает настройки и отслеживает несохранённые изменения.
  /// Показывает иконки «✔/✖» при отличии текущих значений от сохранённой (базовой) модели.
  /// </summary>
  public partial class ProtocolControl : UserControl
  {
    /// <summary>
    /// Базовая (сохранённая) модель протокола, считанная при загрузке.
    /// Используется как эталон для сравнения с текущими значениями UI.
    /// </summary>
    private SettingsProtocolDto _baseProtocolModel { get; set; }
    private bool _isInitialized;
    private bool _hasProtocolChanges;
    private bool _hasDeviceDisplayChanges;

    /// <summary>
    /// Создаёт экземпляр контрола протокола.
    /// </summary>
    public ProtocolControl()
    {
      InitializeComponent();
      Loaded += ProtocolControl_Loaded;
    }

    /// <summary>
    /// Глобальный флаг наличия несохранённых изменений в разделе.
    /// <para>True — есть отличия от сохранённой модели; False — всё совпадает.</para>
    /// </summary>
    public bool HasUnsavedChanges { get; private set; }

    /// <summary>
    /// Обработчик события загрузки контрола.
    /// Подгружает сохранённую модель, заполняет UI и подписывается на изменения.
    /// </summary>
    private void ProtocolControl_Loaded(object sender, RoutedEventArgs e)
    {
      _baseProtocolModel = ProtocolConfig.GetProtocolModel();
      DeviceDisplaySettingsCon.SetBaseModel();
      DefalultData();
      DeviceDisplaySettingsCon.DefalultData();

      if (!_isInitialized)
      {
        AutoSave.CheckedChanged += CheckedChanged;
        CommandHeadersCheckBox.CheckedChanged += CheckedChanged;
        AutoPrint.CheckedChanged += CheckedChanged;
        OperationTime.CheckedChanged += CheckedChanged;
        ProtocolFromPO.CheckedChanged += CheckedChanged;
        ProtocolGeneration.CheckedChanged += CheckedChanged;
        Header.CheckedChanged += CheckedChanged;
        TestStepChecker.CheckedChanged += CheckedChanged;
        BaseTextProtocol.TextChanged += (s, ev) => CheckedChanged(s, true);
        BaseTextProtocolErrors.TextChanged += (s, ev) => CheckedChanged(s, true);

        Success.PreviewMouseDown += Success_PreviewMouseDown;
        Error.PreviewMouseDown += Error_PreviewMouseDown;
        DeviceDisplaySettingsCon.DeviceDisplayModelChanged += DeviceDisplaySettingsCon_DeviceDisplayModelChanged;

        _isInitialized = true;
      }

      _hasProtocolChanges = false;
      _hasDeviceDisplayChanges = false;
      UpdateUnsavedState();
      UpdateTemplateResetButtonsVisibility();
    }

    private void DeviceDisplaySettingsCon_DeviceDisplayModelChanged(bool changed)
    {
      _hasDeviceDisplayChanges = changed;
      UpdateUnsavedState();
    }

    /// <summary>
    /// Клик по галочке «сохранить»: сохраняет текущую модель,
    /// перечитывает базу и скрывает индикаторы изменений.
    /// </summary>
    private async void Success_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await SaveData();
    public async Task SaveData()
    {
      DeviceDisplaySettingsDto model = DeviceDisplaySettingsCon.GetModel();

      await DeviceDisplayConfig.SaveSettingsAsync(model);
      DeviceDisplaySettingsCon.SetBaseModel();
      await ProtocolConfig.SaveProtocolModel(GetModel());
      _baseProtocolModel = ProtocolConfig.GetProtocolModel();
      _hasProtocolChanges = false;
      _hasDeviceDisplayChanges = false;
      UpdateUnsavedState();
      UpdateTemplateResetButtonsVisibility();
    }

    /// <summary>
    /// Клик по кресту «отменить»: откатывает значения к сохранённой модели
    /// и скрывает индикаторы изменений.
    /// </summary>
    private void Error_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      DefalultData();
      DeviceDisplaySettingsCon.SetBaseModel();
      DeviceDisplaySettingsCon.DefalultData();

      _hasProtocolChanges = false;
      _hasDeviceDisplayChanges = false;
      UpdateUnsavedState();
      UpdateTemplateResetButtonsVisibility();
    }

    /// <summary>
    /// Унифицированный обработчик изменений любого переключателя.
    /// Сравнивает текущую модель с сохранённой и показывает/скрывает индикаторы.
    /// </summary>
    private void CheckedChanged(object? sender, bool e)
    {
      _hasProtocolChanges = !ProtocolEquals(_baseProtocolModel, GetModel());
      UpdateUnsavedState();
      UpdateTemplateResetButtonsVisibility();
    }

    private void UpdateUnsavedState()
    {
      bool hasUnsaved = _hasProtocolChanges || _hasDeviceDisplayChanges;
      Error.Visibility = hasUnsaved ? Visibility.Visible : Visibility.Collapsed;
      Success.Visibility = hasUnsaved ? Visibility.Visible : Visibility.Collapsed;
      HasUnsavedChanges = hasUnsaved;
    }

    private void UpdateTemplateResetButtonsVisibility()
    {
      RestartClearProtocol.Visibility =
        BaseTextProtocol.Text != ProtocolConfig.GetBaseTextProtocol()
          ? Visibility.Visible
          : Visibility.Collapsed;

      RestartClearProtocolErrors.Visibility =
        BaseTextProtocolErrors.Text != ProtocolConfig.GetBaseTextErrorsProtocol()
          ? Visibility.Visible
          : Visibility.Collapsed;
    }

    /// <summary>
    /// Формирует модель протокола из текущих значений элементов UI.
    /// </summary>
    private SettingsProtocolDto GetModel()
    {
      var model = ProtocolConfig.GetProtocolModel();
      model.AutoSaveProtocol = AutoSave.IsChecked;
      model.AutoPrintProtocol = AutoPrint.IsChecked;
      model.DisplayOperationTime = OperationTime.IsChecked;
      model.ShowProtocolInSoftware = ProtocolFromPO.IsChecked;
      model.GenerateProtocol = ProtocolGeneration.IsChecked;
      model.ShowHeaderInfo = Header.IsChecked;
      model.CleanTextProtocol = BaseTextProtocol.Text;
      model.CleanTextErrorsProtocol = BaseTextProtocolErrors.Text;
      model.ShowCommandHeadersInProtocol = CommandHeadersCheckBox.IsChecked;
      model.ShowTestStepMessagesInProtocol = TestStepChecker.IsChecked;
      return model;
    }

    /// <summary>
    /// Сравнивает две модели протокола по всем флагам.
    /// </summary>
    private static bool ProtocolEquals(SettingsProtocolDto a, SettingsProtocolDto b) =>
      a.ShowDeviceInfo == b.ShowDeviceInfo &&
      a.ShowHeaderInfo == b.ShowHeaderInfo &&
      a.AutoSaveProtocol == b.AutoSaveProtocol &&
      a.AutoPrintProtocol == b.AutoPrintProtocol &&
      a.ShowProtocolInSoftware == b.ShowProtocolInSoftware &&
      a.GenerateProtocol == b.GenerateProtocol &&
      a.CleanTextProtocol == b.CleanTextProtocol &&
      a.CleanTextErrorsProtocol == b.CleanTextErrorsProtocol &&
      a.ShowTestStepMessagesInProtocol == b.ShowTestStepMessagesInProtocol &&
      a.ShowCommandHeadersInProtocol == b.ShowCommandHeadersInProtocol &&
      a.DisplayOperationTime == b.DisplayOperationTime;

    /// <summary>
    /// Заполняет элементы UI значениями из базовой (сохранённой) модели.
    /// </summary>
    private void DefalultData()
    {
      AutoSave.IsChecked = _baseProtocolModel.AutoSaveProtocol;
      CommandHeadersCheckBox.IsChecked = _baseProtocolModel.ShowCommandHeadersInProtocol;
      AutoPrint.IsChecked = _baseProtocolModel.AutoPrintProtocol;
      OperationTime.IsChecked = _baseProtocolModel.DisplayOperationTime;
      ProtocolFromPO.IsChecked = _baseProtocolModel.ShowProtocolInSoftware;
      ProtocolGeneration.IsChecked = _baseProtocolModel.GenerateProtocol;
      Header.IsChecked = _baseProtocolModel.ShowHeaderInfo;
      TestStepChecker.IsChecked = _baseProtocolModel.ShowTestStepMessagesInProtocol;
      BaseTextProtocol.Text = _baseProtocolModel.CleanTextProtocol;
      BaseTextProtocolErrors.Text = _baseProtocolModel.CleanTextErrorsProtocol;
    }

    private void RepeatIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      BaseTextProtocol.Text = ProtocolConfig.GetBaseTextProtocol();
      RestartClearProtocol.Visibility = Visibility.Collapsed;
    }

    private void RepeatErrorIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      BaseTextProtocolErrors.Text = ProtocolConfig.GetBaseTextErrorsProtocol();
      RestartClearProtocolErrors.Visibility = Visibility.Collapsed;
    }
  }
}
