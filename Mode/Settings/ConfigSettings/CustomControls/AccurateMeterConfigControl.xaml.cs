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
  /// Логика взаимодействия для AccurateMeterConfigControl.xaml
  /// </summary>
  public partial class AccurateMeterConfigControl : UserControl
  {
    /// <summary>
    /// Класс аргументов события для модели точного измерителя.
    /// </summary>
    public class AccurateMeterModelEventArgs : EventArgs
    {
      /// <summary>
      /// Получает модель измерителя.
      /// </summary>
      public MeterBase Model { get; private set; }

      /// <summary>
      /// Инициализирует новый экземпляр класса AccurateMeterModelEventArgs.
      /// </summary>
      /// <param name="model">Модель измерителя.</param>
      public AccurateMeterModelEventArgs(MeterBase model)
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
    public event EventHandler<AccurateMeterModelEventArgs> SaveButtonClicked;

    readonly DeviceEnum.Type Type;
    readonly Tuple<string, string> info;

    /// <summary>
    /// Инициализирует новый экземпляр класса AccurateMeterConfigControl.
    /// </summary>
    public AccurateMeterConfigControl()
    {
      InitializeComponent();

      Type = DeviceEnum.Type.AccurateMeter;
      info = DeviceEnum.GetInfoDevice(Type);

      ShowMessage(info.Item1, info.Item2, Color.FromArgb(255, 255, 0, 0));
    }

    /// <summary>
    /// Обрабатывает предварительный ввод текста для поля количества точек.
    /// </summary>
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
        SaveButtonClicked(this, new AccurateMeterModelEventArgs(model));
      }
    }

    /// <summary>
    /// Собирает данные для создания модели измерителя.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию, которая возвращает созданную модель измерителя.</returns>
    private async Task<MeterBase> CollectData()
    {
      DeviceEnum.Type type = DeviceEnum.Type.AccurateMeter;
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
