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
  /// Логика взаимодействия для ModuleVoltageCurrentSourceConfigControl.xaml
  /// </summary>
  public partial class ModuleVoltageCurrentSourceConfigControl : UserControl
  {

    /// <summary>
    /// Класс аргументов события для модели ModuleVoltageCurrentSource
    /// </summary>
    public class ModuleVoltageCurrentSourceModelEventArgs : EventArgs
    {
      /// <summary>
      /// Получает модель ModuleVoltageCurrentSource
      /// </summary>
      public Core.ModuleVoltageCurrentSource.Model Model { get; private set; }

      /// <summary>
      /// Инициализирует новый экземпляр класса ModuleVoltageCurrentSourceModelEventArgs
      /// </summary>
      /// <param name="model">Модель ModuleVoltageCurrentSource</param>
      public ModuleVoltageCurrentSourceModelEventArgs(Core.ModuleVoltageCurrentSource.Model model)
      {
        Model = model;
      }
    }

    /// <summary>
    /// Событие, возникающее при нажатии кнопки отмены
    /// </summary>
    public event EventHandler CancelButtonClicked;

    /// <summary>
    /// Событие, возникающее при нажатии кнопки сохранения
    /// </summary>
    public event EventHandler<ModuleVoltageCurrentSourceModelEventArgs> SaveButtonClicked;

    /// <summary>
    /// Тип устройства
    /// </summary>
    readonly DeviceEnum.Type Type;

    /// <summary>
    /// Информация об устройстве
    /// </summary>
    readonly Tuple<string, string> info;

    /// <summary>
    /// Инициализирует новый экземпляр класса ModuleVoltageCurrentSourceConfigControl
    /// </summary>
    public ModuleVoltageCurrentSourceConfigControl()
    {
      InitializeComponent();
      Type = DeviceEnum.Type.ModuleVoltageCurrentSource;
      info = DeviceEnum.GetInfoDevice(Type);
      ShowMessage(info.Item1, info.Item2, Color.FromArgb(255, 255, 255, 255));
    }

    /// <summary>
    /// Обрабатывает ввод текста для поля количества точек
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие</param>
    /// <param name="e">Аргументы события</param>
    private void CountPoints_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      CheckIsNumeric(e);
    }

    /// <summary>
    /// Проверка на цифры.
    /// </summary>
    /// <param name="e">Аргументы события ввода текста</param>
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
    /// <param name="sender">Объект, вызвавший событие</param>
    /// <param name="e">Аргументы события</param>
    private void Exit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      CancelButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки сохранения
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие</param>
    /// <param name="e">Аргументы события</param>
    private void Save_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (SaveButtonClicked != null)
      {
        Core.ModuleVoltageCurrentSource.Model model = CollectData();
        SaveButtonClicked(this, new ModuleVoltageCurrentSourceModelEventArgs(model));
      }
    }

    /// <summary>
    /// Собирает данные для создания модели
    /// </summary>
    /// <returns>Модель ModuleVoltageCurrentSource</returns>
    private Core.ModuleVoltageCurrentSource.Model CollectData()
    {
      DeviceEnum.Type type = DeviceEnum.Type.ModuleVoltageCurrentSource;
      Tuple<string, string> info = DeviceEnum.GetInfoDevice(type);
      string numberDevice = number.Text;

      string name = $"{info.Item1}";
      string description = info.Item2;
      string ip = $"192.168.1.{numberDevice}";
      bool moduleActive = false;

      Core.ModuleVoltageCurrentSource.Model model = new Core.ModuleVoltageCurrentSource.Model(type, name, description, IPAddress.Parse(ip), numberDevice, moduleActive);
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
