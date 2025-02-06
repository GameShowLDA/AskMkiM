using System.Globalization;
using System.Net;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Core.Enum;

namespace Mode.Settings.ConfigSettings.CustomControls
{
  /// <summary>
  /// Логика взаимодействия для ModuleRelayConfigControl.xaml
  /// </summary>
  public partial class ModuleRelayConfigControl : UserControl
  {
    /// <summary>
    /// Класс аргументов события для модели управления модульным реле.
    /// </summary>
    public class ModuleRelayControlModelEventArgs : EventArgs
    {
      /// <summary>
      /// Получает модель управления модульным реле.
      /// </summary>
      public Core.ModuleRelayControl.Model Model { get; private set; }

      /// <summary>
      /// Инициализирует новый экземпляр класса ModuleRelayControlModelEventArgs.
      /// </summary>
      /// <param name="model">Модель управления модульным реле.</param>
      public ModuleRelayControlModelEventArgs(Core.ModuleRelayControl.Model model)
      {
        Model = model;
      }
    }

    readonly DeviceEnum.Type Type;
    readonly Tuple<string, string> info;

    /// <summary>
    /// Событие, возникающее при нажатии кнопки отмены.
    /// </summary>
    public event EventHandler CancelButtonClicked;
    /// <summary>
    /// Событие, возникающее при нажатии кнопки сохранения.
    /// </summary>
    public event EventHandler<ModuleRelayControlModelEventArgs> SaveButtonClicked;

    /// <summary>
    /// Инициализирует новый экземпляр класса ModuleRelayConfigControl.
    /// </summary>
    public ModuleRelayConfigControl()
    {
      InitializeComponent();
      Type = DeviceEnum.Type.ModuleRelayControl;
      info = DeviceEnum.GetInfoDevice(Type);
      ShowMessage(info.Item1, info.Item2, Color.FromArgb(255, 255, 255, 255));
    }

    /// <summary>
    /// Обрабатывает предварительный ввод текста в поле количества точек.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события.</param>
    private void CountPoints_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      CheckIsNumeric(e);
    }

    /// <summary>
    /// Проверка на цифры.
    /// </summary>
    /// <param name="e">Аргументы события текстового ввода.</param>
    private void CheckIsNumeric(TextCompositionEventArgs e)
    {
      if (!(int.TryParse(e.Text, out _)))
      {
        e.Handled = true;
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки выхода.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события.</param>
    private void Exit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      CancelButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки сохранения.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события.</param>
    private void Save_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (SaveButtonClicked != null)
      {
        Core.ModuleRelayControl.Model model = CollectData();
        SaveButtonClicked(this, new ModuleRelayControlModelEventArgs(model));
      }
    }

    /// <summary>
    /// Собирает данные для создания модели управления модульным реле.
    /// </summary>
    /// <returns>Модель управления модульным реле.</returns>
    private Core.ModuleRelayControl.Model CollectData()
    {
      DeviceEnum.Type type = DeviceEnum.Type.ModuleRelayControl;

      Tuple<string, string> info = DeviceEnum.GetInfoDevice(type);

      string numberMKR = number.Text;
      string name = $"{info.Item1}";
      string description = info.Item2;
      string ip = $"192.168.1.{numberMKR}";
      bool moduleActive = false;
      if (!int.TryParse(countPoints.Text, out int count))
      {
        return null;
      }

      DeviceEnum.VoltageType voltageType;
      if (voltage.Text.ToLower(CultureInfo.CurrentCulture).Contains("высокое"))
      {
        voltageType = DeviceEnum.VoltageType.HightVoltage;
      }
      else
      {
        voltageType = DeviceEnum.VoltageType.LowVoltage;
      }

      var model = new Core.ModuleRelayControl.Model(type, name, description, IPAddress.Parse(ip), numberMKR, moduleActive, count, voltageType);
      return model;
    }

    /// <summary>
    /// Устанавливает описание устройства.
    /// </summary>
    /// <param name="header">Заголовок (имя устройства).</param>
    /// <param name="description">Описание.</param>
    /// <param name="headerColor">Цвет заголовка.</param>
    private void ShowMessage(string header, string description, Color headerColor) => DefaultConfigControls.ShowMessage(InfoDevice, header, description, headerColor);
  }
}
