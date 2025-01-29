using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mode.Base.SearchDevices;
using Mode.Models;
using UI.Components.Invoke;
using static AppConfig.Config.LoopConfig;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;


namespace Mode.Metrology.CI
{
  /// <summary>
  /// Логика взаимодействия для CiControl.xaml
  /// </summary>
  public partial class CiControl : UserControl
  {
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;

    private DataElectricModel measurementDataModel;

    private Core.DeviceBusCommutation.Model deviceBusCommutation;
    private Core.Abstract.BreakdownBase gptLibrary;

    InvokeBorder TimeBorder = new InvokeBorder();
    InvokeTextBox TimeData = new InvokeTextBox();

    InvokeBorder VoltageBorder = new InvokeBorder();
    InvokeTextBox VoltageData = new InvokeTextBox();

    private bool completed;
    public CiControl()
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
      LogInformation("Настройка элементов управления режима СИ");

      ProtocolSelfCheckControl.SetSettings(this, StartDelegate: ExecuteMeasurementProcess, true, null, ReturnDelegate: PerformInsulationResistanceMeasurement);
      ProtocolSelfCheckControl.Header = "Режим СИ";

      await ProtocolSelfCheckControl.ClearContent();
      StackPanel contentStack = InputControlSettings.InitializeSettings(out measurementDataModel, InputControlSettings.ElectricParameter.InsulationResistance);

      AddVoltageParameterControls(contentStack);
      AddTimeMeasurementControls(contentStack);

      InputControlSettings.DefaultGotAndLostEvent(VoltageData, "Напряжение");
      InputControlSettings.DefaultGotAndLostEvent(TimeData, "Время измерения");

      ProtocolSelfCheckControl.AddContent(contentStack);

      await ConfigureProtocolSelfCheckControlAsync();
      LogInformation("Настройка элементов управления режима СИ завершена");
    }

    private void AddVoltageParameterControls(StackPanel contentStack)
    {
      LogInformation("Создание панели ввода времени измерения");

      // Устанавливаем стиль для границы
      VoltageBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];

      // Создаем сетку для размещения элементов
      Grid timeGrid = new Grid();
      var timeColumn1 = new ColumnDefinition
      {
        Width = new GridLength(1, GridUnitType.Star)
      };
      var timeColumn2 = new ColumnDefinition
      {
        Width = new GridLength(1, GridUnitType.Auto)
      };

      timeGrid.ColumnDefinitions.Add(timeColumn1);
      timeGrid.ColumnDefinitions.Add(timeColumn2);

      // Настраиваем TextBox для ввода времени
      VoltageData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      VoltageData.Tag = "Напряжение";
      VoltageData.Text = "Напряжение";
      VoltageData.PreviewTextInput += VoltageData_PreviewTextInput;

      // TextBox для единицы измерения времени
      var timeUnitTextBox = new TextBox
      {
        Style = (Style)Application.Current.Resources["MetrologyTextBox"],
        Text = ",В.", // Единица измерения времени
        IsReadOnly = true, // Делаем поле только для чтения
        IsTabStop = false
      };

      // Удаляем предыдущего родителя, если есть
      if (VoltageBorder.Parent is Panel parent)
      {
        parent.Children.Remove(VoltageBorder);
      }

      // Добавляем сетку в границу
      VoltageBorder.Child = timeGrid;

      // Устанавливаем расположение элементов в сетке
      Grid.SetColumn(VoltageData, 0);
      Grid.SetColumn(timeUnitTextBox, 1);
      timeGrid.Children.Add(VoltageData);
      timeGrid.Children.Add(timeUnitTextBox);

      // Добавляем границу в StackPanel
      contentStack.Children.Add(VoltageBorder);
    }

    /// <summary>
    /// Добавляет элементы управления для ввода времени измерения в переданный StackPanel.
    /// Настраивает сетку для правильного расположения полей ввода и текста.
    /// </summary>
    /// <param name="contentStack">StackPanel, в который добавляются элементы управления.</param>
    private void AddTimeMeasurementControls(StackPanel contentStack)
    {
      LogInformation("Создание панели ввода времени измерения");

      // Устанавливаем стиль для границы
      TimeBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];

      // Создаем сетку для размещения элементов
      Grid timeGrid = new Grid();
      var timeColumn1 = new ColumnDefinition
      {
        Width = new GridLength(1, GridUnitType.Star)
      };
      var timeColumn2 = new ColumnDefinition
      {
        Width = new GridLength(1, GridUnitType.Auto)
      };

      timeGrid.ColumnDefinitions.Add(timeColumn1);
      timeGrid.ColumnDefinitions.Add(timeColumn2);

      // Настраиваем TextBox для ввода времени
      TimeData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      TimeData.Tag = "Введите время измерения";
      TimeData.Text = "Время измерения";
      TimeData.PreviewTextInput += TimeData_PreviewTextInput;

      // TextBox для единицы измерения времени
      var timeUnitTextBox = new TextBox
      {
        Style = (Style)Application.Current.Resources["MetrologyTextBox"],
        Text = ",сек.", // Единица измерения времени
        IsReadOnly = true, // Делаем поле только для чтения
        IsTabStop = false
      };

      // Удаляем предыдущего родителя, если есть
      if (TimeBorder.Parent is Panel parent)
      {
        parent.Children.Remove(TimeBorder);
      }

      // Добавляем сетку в границу
      TimeBorder.Child = timeGrid;

      // Устанавливаем расположение элементов в сетке
      Grid.SetColumn(TimeData, 0);
      Grid.SetColumn(timeUnitTextBox, 1);
      timeGrid.Children.Add(TimeData);
      timeGrid.Children.Add(timeUnitTextBox);

      // Добавляем границу в StackPanel
      contentStack.Children.Add(TimeBorder);
    }

    /// <summary>
    /// Обработчик события для ограничения ввода только цифрами.
    /// </summary>
    private void TimeData_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
      e.Handled = !IsTextAllowed(e.Text, sender);
    }

    /// <summary>
    /// Обработчик события для ограничения ввода только цифрами.
    /// </summary>
    private void VoltageData_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
      e.Handled = !IsVoltageTextAllowed(e.Text, sender);
    }

    /// <summary>
    /// Проверяет, является ли текст допустимым (только цифры).
    /// </summary>
    private bool IsTextAllowed(string text, object sender)
    {
      foreach (char c in text)
      {
        if (!char.IsDigit(c))
        {
          return false;
        }

        if (int.Parse((sender as InvokeTextBox).Text + text) / 1000 != 0)
        {
          return false;
        }
      }
      return true;
    }

    /// <summary>
    /// Проверяет, является ли текст допустимым (только цифры).
    /// </summary>
    private bool IsVoltageTextAllowed(string text, object sender)
    {
      foreach (char c in text)
      {
        if (!char.IsDigit(c))
        {
          return false;
        }

        if (int.Parse((sender as InvokeTextBox).Text + text) > 1000)
        {
          return false;
        }
      }
      return true;
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
