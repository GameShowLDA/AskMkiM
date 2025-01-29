using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using Core.Enum;

namespace Mode.Settings.ConfigSettings.CustomControls
{
  /// <summary>
  /// Логика взаимодействия для DeviceBusCommutationConfigControl.xaml
  /// </summary>
  public partial class DeviceBusCommutationConfigControl : UserControl
  {
    /// <summary>
    /// Класс аргументов события для модели DeviceBusCommutation.
    /// </summary>
    public class DeviceBusCommutationModelEventArgs : EventArgs
    {
      /// <summary>
      /// Получает модель DeviceBusCommutation.
      /// </summary>
      public Core.DeviceBusCommutation.Model Model { get; private set; }

      /// <summary>
      /// Инициализирует новый экземпляр класса DeviceBusCommutationModelEventArgs.
      /// </summary>
      /// <param name="model">Модель DeviceBusCommutation.</param>
      public DeviceBusCommutationModelEventArgs(Core.DeviceBusCommutation.Model model)
      {
        Model = model;
      }
    }

    /// <summary>
    /// Событие, возникающее при нажатии кнопки отмены.
    /// </summary>
    public event EventHandler CancelButtonClicked;

    /// <summary>
    /// Событие, возникающее при нажатии кнопки сохранения.
    /// </summary>
    public event EventHandler<DeviceBusCommutationModelEventArgs> SaveButtonClicked;

    /// <summary>
    /// Тип устройства.
    /// </summary>
    readonly DeviceEnum.Type Type;

    /// <summary>
    /// Информация об устройстве.
    /// </summary>
    readonly Tuple<string, string> info;

    /// <summary>
    /// Инициализирует новый экземпляр класса DeviceBusCommutationConfigControl.
    /// </summary>
    public DeviceBusCommutationConfigControl()
    {
      InitializeComponent();
      Type = DeviceEnum.Type.DeviceBusCommutation;
      info = DeviceEnum.GetInfoDevice(Type);
      ShowMessage(info.Item1, info.Item2, Color.FromArgb(255, 255, 0, 0));
    }

    private void CountPoints_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      CheckIsNumeric(e);
    }

    /// <summary>
    /// Проверка на цифры.
    /// </summary>
    /// <param name="e">Аргументы события предварительного ввода текста.</param>
    private void CheckIsNumeric(TextCompositionEventArgs e)
    {
      if (!(int.TryParse(e.Text, out _)))
      {
        e.Handled = true;
      }
    }

    private void Exit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      CancelButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    private void Save_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (SaveButtonClicked != null)
      {
        Core.DeviceBusCommutation.Model model = CollectData();
        SaveButtonClicked(this, new DeviceBusCommutationModelEventArgs(model));
      }
    }

    /// <summary>
    /// Собирает данные для создания модели DeviceBusCommutation.
    /// </summary>
    /// <returns>Модель DeviceBusCommutation.</returns>
    private Core.DeviceBusCommutation.Model CollectData()
    {
      DeviceEnum.Type type = DeviceEnum.Type.DeviceBusCommutation;
      Tuple<string, string> info = DeviceEnum.GetInfoDevice(type);
      string numberDevice = number.Text;

      string name = $"{info.Item1}";
      string description = info.Item2;
      string ip = $"192.168.1.{numberDevice}";
      bool moduleActive = false;

      Core.DeviceBusCommutation.Model model = new Core.DeviceBusCommutation.Model(type, name, description, IPAddress.Parse(ip), numberDevice, moduleActive);
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
