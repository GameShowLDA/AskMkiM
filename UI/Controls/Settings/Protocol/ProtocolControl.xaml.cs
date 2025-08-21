using AppConfiguration.Protocol;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static AppConfiguration.Protocol.ProtocolConfig;

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
    private ProtocolModel _baseProtocolModel { get; set; }

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
    private async void ProtocolControl_Loaded(object sender, RoutedEventArgs e)
    {
      _baseProtocolModel = await GetProtocolModel();
      DefalultData();

      DeviceInfo.CheckedChanged += CheckedChanged;
      AutoSave.CheckedChanged += CheckedChanged;
      AutoPrint.CheckedChanged += CheckedChanged;
      OperationTime.CheckedChanged += CheckedChanged;

      Success.PreviewMouseDown += Success_PreviewMouseDown;
      Error.PreviewMouseDown += Error_PreviewMouseDown;

      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;
    }

    /// <summary>
    /// Клик по галочке «сохранить»: сохраняет текущую модель,
    /// перечитывает базу и скрывает индикаторы изменений.
    /// </summary>
    private async void Success_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      await SaveProtocolModel(GetModel());
      _baseProtocolModel = await GetProtocolModel();

      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;
    }

    /// <summary>
    /// Клик по кресту «отменить»: откатывает значения к сохранённой модели
    /// и скрывает индикаторы изменений.
    /// </summary>
    private void Error_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      DefalultData();

      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;
    }

    /// <summary>
    /// Унифицированный обработчик изменений любого переключателя.
    /// Сравнивает текущую модель с сохранённой и показывает/скрывает индикаторы.
    /// </summary>
    private void CheckedChanged(object? sender, bool e)
    {
      if (!ProtocolEquals(_baseProtocolModel, GetModel()))
      {
        Error.Visibility = Visibility.Visible;
        Success.Visibility = Visibility.Visible;
        HasUnsavedChanges = true;
      }
      else
      {
        Error.Visibility = Visibility.Collapsed;
        Success.Visibility = Visibility.Collapsed;
        HasUnsavedChanges = false;
      }
    }

    /// <summary>
    /// Формирует модель протокола из текущих значений элементов UI.
    /// </summary>
    private ProtocolModel GetModel()
    {
      var model = new ProtocolModel
      {
        ShowDeviceInfo = DeviceInfo.IsChecked,
        AutoSaveProtocol = AutoSave.IsChecked,
        AutoPrintProtocol = AutoPrint.IsChecked,
        DisplayOperationTime = OperationTime.IsChecked
      };
      return model;
    }

    /// <summary>
    /// Сравнивает две модели протокола по всем флагам.
    /// </summary>
    private static bool ProtocolEquals(ProtocolModel a, ProtocolModel b) =>
      a.ShowDeviceInfo == b.ShowDeviceInfo &&
      a.AutoSaveProtocol == b.AutoSaveProtocol &&
      a.AutoPrintProtocol == b.AutoPrintProtocol &&
      a.DisplayOperationTime == b.DisplayOperationTime;

    /// <summary>
    /// Заполняет элементы UI значениями из базовой (сохранённой) модели.
    /// </summary>
    private void DefalultData()
    {
      DeviceInfo.IsChecked = _baseProtocolModel.ShowDeviceInfo;
      AutoSave.IsChecked = _baseProtocolModel.AutoSaveProtocol;
      AutoPrint.IsChecked = _baseProtocolModel.AutoPrintProtocol;
      OperationTime.IsChecked = _baseProtocolModel.DisplayOperationTime;
    }
  }
}
