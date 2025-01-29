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
  /// Логика взаимодействия для BreakdownControl.xaml
  /// </summary>
  public partial class BreakdownControl : UserControl
  {
    /// <summary>
    /// Класс аргументов события для модели быстрого измерителя.
    /// </summary>
    public class BreakdownModelEventArgs : EventArgs
    {
      /// <summary>
      /// Получает модель измерителя.
      /// </summary>
      public BreakdownBase Model { get; private set; }

      /// <summary>
      /// Инициализирует новый экземпляр класса BreakdownModelEventArgs.
      /// </summary>
      /// <param name="model">Модель измерителя.</param>
      public BreakdownModelEventArgs(BreakdownBase model)
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
    public event EventHandler<BreakdownModelEventArgs> SaveButtonClicked;

    /// <summary>
    /// Тип устройства.
    /// </summary>
    readonly DeviceEnum.Type Type;

    /// <summary>
    /// Информация об устройстве.
    /// </summary>
    readonly Tuple<string, string> info;
    public BreakdownControl()
    {
      InitializeComponent();
      Type = DeviceEnum.Type.Breakdown;
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
        BreakdownBase model = await CollectData();
        SaveButtonClicked(this, new BreakdownModelEventArgs(model));
      }
    }

    /// <summary>
    /// Собирает данные для создания модели измерителя.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию, которая возвращает созданную модель измерителя.</returns>
    private async Task<BreakdownBase> CollectData()
    {
      DeviceEnum.Type type = DeviceEnum.Type.Breakdown;
      string numberDevice = number.Text;

      BreakdownBase model = null;

      if (number.Text.Contains("79904"))
      {
        model = Core.GptLibrary.Model.CreateAsync();
        bool moduleActive = true;
        if (!model.CheckConnection())
        {
          moduleActive = false;
        }
        model.DeviceType = type;
      }
      else
      {
        //model = MeterBase.CreateMeter<DeviceConfiguration.Keysight.Model>(type, number.Text, null, null, numberDevice, false);
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
