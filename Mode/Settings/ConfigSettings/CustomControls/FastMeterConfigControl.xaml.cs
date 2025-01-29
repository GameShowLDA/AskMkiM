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
using Core.Enum;

namespace Mode.Settings.ConfigSettings.CustomControls
{
  /// <summary>
  /// Логика взаимодействия для FastMeterConfigControl.xaml
  /// </summary>
  public partial class FastMeterConfigControl : UserControl
  {

    /// <summary>
    /// Класс аргументов события для модели быстрого измерителя.
    /// </summary>
    public class FastMeterModelEventArgs : EventArgs
    {
      /// <summary>
      /// Получает модель измерителя.
      /// </summary>
      public MeterBase Model { get; private set; }

      /// <summary>
      /// Инициализирует новый экземпляр класса FastMeterModelEventArgs.
      /// </summary>
      /// <param name="model">Модель измерителя.</param>
      public FastMeterModelEventArgs(MeterBase model)
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
    public event EventHandler<FastMeterModelEventArgs> SaveButtonClicked;

    /// <summary>
    /// Тип устройства.
    /// </summary>
    readonly DeviceEnum.Type Type;

    /// <summary>
    /// Информация об устройстве.
    /// </summary>
    readonly Tuple<string, string> info;

    /// <summary>
    /// Инициализирует новый экземпляр класса FastMeterConfigControl.
    /// </summary>
    public FastMeterConfigControl()
    {
      InitializeComponent();
      Type = DeviceEnum.Type.FastMeter;
      info = DeviceEnum.GetInfoDevice(Type);
      ShowMessage(info.Item1, info.Item2, Color.FromArgb(255, 255, 0, 0));
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки выхода.
    /// </summary>
    private void Exit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      CancelButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки сохранения.
    /// </summary>
    private async void Save_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (SaveButtonClicked != null)
      {
        MeterBase model = await CollectData();
        SaveButtonClicked(this, new FastMeterModelEventArgs(model));
      }
    }

    /// <summary>
    /// Собирает данные для создания модели измерителя.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию, которая возвращает созданную модель измерителя.</returns>
    private async Task<MeterBase> CollectData()
    {
      DeviceEnum.Type type = DeviceEnum.Type.FastMeter;
      string numberDevice = number.Text;

      MeterBase model;

      if (number.Text.Contains("34465"))
      {
        var modelKeysight = await Core.KeysightLibrary.Model.CreateAsync();
        bool moduleActive = true;
        if (modelKeysight.IPAddress == null)
        {
          moduleActive = false;
        }
        model = MeterBase.CreateMeter<Core.KeysightLibrary.Model>(type, modelKeysight.Name, modelKeysight.Description, modelKeysight.IPAddress, numberDevice, moduleActive);
      }
      else
      {
        model = MeterBase.CreateMeter<Core.KeysightLibrary.Model>(type, number.Text, null, null, numberDevice, false);
      }

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
