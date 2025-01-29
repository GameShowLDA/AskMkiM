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
  /// Логика взаимодействия для ManagerShassyConfigControl.xaml
  /// </summary>
  public partial class ManagerShassyConfigControl : UserControl
  {
    /// <summary>
    /// Класс аргументов события для модели ManagerShassy
    /// </summary>
    public class ManagerShassyModelModelEventArgs : EventArgs
    {
      /// <summary>
      /// Получает модель ManagerShassy
      /// </summary>
      public Core.ManagerShassy.Model Model { get; private set; }

      /// <summary>
      /// Инициализирует новый экземпляр класса ManagerShassyModelModelEventArgs
      /// </summary>
      /// <param name="model">Модель ManagerShassy</param>
      public ManagerShassyModelModelEventArgs(Core.ManagerShassy.Model model)
      {
        Model = model;
      }
    }

    readonly DeviceEnum.Type Type;
    readonly Tuple<string, string> info;

    /// <summary>
    /// Событие, возникающее при нажатии кнопки отмены
    /// </summary>
    public event EventHandler CancelButtonClicked;

    /// <summary>
    /// Событие, возникающее при нажатии кнопки сохранения
    /// </summary>
    public event EventHandler<ManagerShassyModelModelEventArgs> SaveButtonClicked;


    /// <summary>
    /// Инициализирует новый экземпляр класса ManagerShassyConfigControl
    /// </summary>
    public ManagerShassyConfigControl()
    {
      InitializeComponent();
      Type = DeviceEnum.Type.ManagerShassy;
      info = DeviceEnum.GetInfoDevice(Type);
      ShowMessage(info.Item1, info.Item2, Color.FromArgb(255, 255, 255, 255));
    }

    /// <summary>
    /// Обрабатывает предварительный ввод текста для поля количества точек
    /// </summary>
    private void CountPoints_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      CheckIsNumeric(e);
    }

    /// <summary>
    /// Проверка на цифры.
    /// </summary>
    /// <param name="e">Аргументы события предварительного ввода текста</param>
    private void CheckIsNumeric(TextCompositionEventArgs e)
    {
      if (!(int.TryParse(e.Text, out _)))
      {
        e.Handled = true;
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки выхода
    /// </summary>
    private void Exit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      CancelButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки сохранения
    /// </summary>
    private void Save_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (SaveButtonClicked != null)
      {
        Core.ManagerShassy.Model model = CollectData();
        SaveButtonClicked(this, new ManagerShassyModelModelEventArgs(model));
      }
    }

    /// <summary>
    /// Собирает данные для создания модели ManagerShassy
    /// </summary>
    /// <returns>Модель ManagerShassy</returns>
    private Core.ManagerShassy.Model CollectData()
    {
      DeviceEnum.Type type = DeviceEnum.Type.ManagerShassy;
      Tuple<string, string> info = DeviceEnum.GetInfoDevice(type);
      string numberMKR = number.Text;

      string name = $"{info.Item1}";
      string description = info.Item2;
      string ip = $"192.168.1.{numberMKR}";
      bool moduleActive = false;

      var model = new Core.ManagerShassy.Model(type, name, description, IPAddress.Parse(ip), numberMKR, moduleActive);
      return model;
    }

    /// <summary>
    /// Устанавливает описание устройсва.
    /// </summary>
    /// <param name="header">Заголовок (имя устройства).</param>
    /// <param name="description">Описаие.</param>
    /// <param name="headerColor">Цвет заголовка.</param>
    private void ShowMessage(string header, string description, Color headerColor) => DefaultConfigControls.ShowMessage(InfoDevice, header, description, headerColor);
  }
}
