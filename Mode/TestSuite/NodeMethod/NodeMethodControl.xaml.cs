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
using Mode.Base.SearchDevices;
using Mode.Models;
using UI.Components.Invoke;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.ProtocolConfig;
using static AppConfig.Config.LoopConfig;
using static AppConfig.Config.SystemStateManager;
using static AppConfig.EventAggregator;
using static AppConfig.SettingsFileReader;
using static Utilities.Models.ShowMessageModel;
using static Utilities.LoggerUtility;
using static AppConfig.Config.MeasurementErrorConfig;

namespace Mode.TestSuite.NodeMethod
{

  /// <summary>
  /// Логика взаимодействия для NodeMethodControl.xaml
  /// Элемент управления для теста "Метод Узла".
  /// </summary>
  public partial class NodeMethodControl : UserControl
  {
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;
    DataElectricModel testDataModel;

    InvokeBorder TimeBorder = new InvokeBorder();
    InvokeTextBox TimeData = new InvokeTextBox();

    InvokeBorder VoltageBorder = new InvokeBorder();
    InvokeTextBox VoltageData = new InvokeTextBox();

    private Core.DeviceBusCommutation.Model deviceBusCommutation;
    private Core.GptLibrary.Model gptLibrary;

    private static CheckBox checkBoxA;
    private static CheckBox checkBoxB;


    public static bool IsBusAActive
    {
      get;
      set;
    }
    public NodeMethodControl()
    {
      InitializeComponent();
      InitializeSettingsAsync().ConfigureAwait(true);

      IsBusAActive = true;
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public async Task InitializeSettingsAsync()
    {
      try
      {
        LogInformation("Настройка элементов управления теста \"Метод узла\"");

        ProtocolSelfCheckControl.SetSettings(this, ExecuteTestProcess, true, Stop);
        ProtocolSelfCheckControl.Header = "Метод узла";

        await ProtocolSelfCheckControl.ClearContent();
        StackPanel contentStack = InputControlSettings.InitializeSettings(out testDataModel, InputControlSettings.ElectricParameter.InsulationResistance);

        AddVoltageParameterControls(contentStack);
        InputControlSettings.DefaultGotAndLostEvent(VoltageData, "Напряжение");

        InputControlSettings.DefaultGotAndLostEvent(TimeData, "Время измерения");
        ProtocolSelfCheckControl.AddContent(contentStack);

        AddTimeMeasurementControls(contentStack);
        AddCustomGridsToStackPanel(contentStack);

        await ConfigureProtocolSelfCheckControlAsync();
        LogInformation("Настройка элементов управления теста \"Метод узла\" завершена");
      }
      catch (Exception ex)
      {
        var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        LogError($"Ошибка загрузки элемента метрологии КС в методе {methodName}: {ex.Message}");
      }
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
    public static void AddCustomGridsToStackPanel(StackPanel stackPanel)
    {
      void AddGrid(string busName, ref CheckBox checkBox, bool isChecked, string checkBoxName)
      {
        var grid = new Grid
        {
          Margin = new Thickness(5)
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        checkBox = new CheckBox
        {
          Name = checkBoxName, // Используем допустимое имя
          VerticalAlignment = VerticalAlignment.Center,
          HorizontalAlignment = HorizontalAlignment.Left,
          Width = 20,
          Style = (Style)Application.Current.Resources["AnimatedSwitch"],
          IsChecked = isChecked
        };
        checkBox.Checked += Switch_Checked;
        checkBox.Unchecked += Switch_Unchecked;

        var textBlock = new TextBlock
        {
          Text = busName,
          Foreground = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"],
          FontSize = 18
        };
        Grid.SetColumn(textBlock, 1);

        grid.Children.Add(checkBox);
        grid.Children.Add(textBlock);

        stackPanel.Children.Add(grid);
      }

      AddGrid("Шина А", ref checkBoxA, true, "checkBoxA");
      AddGrid("Шина В", ref checkBoxB, false, "checkBoxB");
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
        IsReadOnly = true // Делаем поле только для чтения
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
      e.Handled = !IsTextAllowed(e.Text);
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
    /// Проверяет, является ли текст допустимым (только цифры).
    /// </summary>
    private bool IsTextAllowed(string text)
    {
      foreach (char c in text)
      {
        if (!char.IsDigit(c))
        {
          return false;
        }

        if (int.Parse(TimeData.Text + text) / 1000 != 0)
        {
          return false;
        }
      }
      return true;
    }

    private static void Switch_Checked(object sender, RoutedEventArgs e)
    {
      if (sender == checkBoxA && checkBoxB.IsChecked == true)
      {
        checkBoxB.IsChecked = false;
      }
      else if (sender == checkBoxB && checkBoxA.IsChecked == true)
      {
        checkBoxA.IsChecked = false;
        IsBusAActive = false;
      }
    }

    private static void Switch_Unchecked(object sender, RoutedEventArgs e)
    {
      if (sender == checkBoxA && checkBoxB.IsChecked == false)
      {
        checkBoxB.IsChecked = true;
      }
      else if (sender == checkBoxB && checkBoxA.IsChecked == false)
      {
        checkBoxA.IsChecked = true;
        IsBusAActive = true;
      }
    }
  }
}
