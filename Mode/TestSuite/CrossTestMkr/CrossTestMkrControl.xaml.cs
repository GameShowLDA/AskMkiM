using Core.Communication;
using Mode.Base.SearchDevices;
using Mode.Models;
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
using UI.Components.Invoke;
using Utilities.Models;
using static AppConfig.Config.LoopConfig;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.TestSuite.CrossTestMkr
{
  public partial class CrossTestMkrControl : UserControl
  {
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;

    // Пример: поля для ввода "Номер проверяемого X.X", "Номер проверяющего X.X", "Диапазон проверки"
    private InvokeBorder testedNumberBorder = new InvokeBorder();
    private InvokeTextBox testedNumberData = new InvokeTextBox();

    private InvokeBorder testerNumberBorder = new InvokeBorder();
    private InvokeTextBox testerNumberData = new InvokeTextBox();

    private InvokeBorder rangeBorder = new InvokeBorder();
    private InvokeTextBox rangeData = new InvokeTextBox();

    //private InvokeBorder delayBorder = new InvokeBorder();
    //private InvokeTextBox delayData = new InvokeTextBox();

    public CrossTestMkrControl()
    {
      InitializeComponent();

      // По аналогии с NodeMethod:
      // Асинхронно настраиваем ProtocolSelfCheckControl
      _ = InitializeSettingsAsync();
    }

    /// <summary>
    /// Асинхронная настройка UI, добавление полей, запуск ProtocolSelfCheckControl.
    /// </summary>
    public async Task InitializeSettingsAsync()
    {
      try
      {
        LogInformation("Настройка CrossTestMKRControl");

        // 1. Настраиваем ProtocolUI 
        //    Параметры: (this, метод старта, показывать ли кнопки, метод стопа?)
        ProtocolSelfCheckControl.SetSettings(this, ExecuteTestProcess, true, Stop);
        ProtocolSelfCheckControl.Header = "CrossTestMKR";

        // 2. Очищаем предыдущий контент
        await ProtocolSelfCheckControl.ClearContent();
                 
        // 3. Создаём StackPanel (можно использовать InputControlSettings при желании)
        //    Или вручную:
        StackPanel contentStack = new StackPanel();
        contentStack.Orientation = Orientation.Vertical;

        // 4. Добавляем поля
        AddInputFields(contentStack);

        // 5. Добавляем StackPanel в ProtocolUI
        ProtocolSelfCheckControl.AddContent(contentStack);

        LogInformation("Настройка CrossTestMKRControl завершена");
      }
      catch (Exception ex)
      {
        var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        LogError($"Ошибка при настройке CrossTestMKR в {methodName}: {ex.Message}");
      }
    }

    /// <summary>
    /// Сюда добавляем три поля: "Номер проверяемого X.X", "Номер проверяющего X.X", "Диапазон проверки".
    /// Аналогично тому, как NodeMethodControl добавляет TimeBorder, VoltageBorder.
    /// </summary>
    private void AddInputFields(StackPanel contentStack)
    {
      // Поле "Номер проверяемого X.X"
      testedNumberBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      Grid testedGrid = new Grid();
      testedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
      testedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

      testedNumberData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      testedNumberData.Text = "Номер проверяемого X.X";
      InputControlSettings.DefaultGotAndLostEvent(testedNumberData, "Номер проверяемого X.X");
      testedNumberData.PreviewTextInput += DevicesNumberData_PreviewTextInput;

      // testedNumberData.PreviewTextInput += ... // если нужна валидация

      //var testedLabel = new TextBox
      //{
      //  Style = (Style)Application.Current.Resources["MetrologyTextBox"],
      //  Text = "(X.X)",
      //  IsReadOnly = true
      //};

      Grid.SetColumn(testedNumberData, 0);
      //Grid.SetColumn(testedLabel, 1);
      testedGrid.Children.Add(testedNumberData);
      //testedGrid.Children.Add(testedLabel);

      testedNumberBorder.Child = testedGrid;
      contentStack.Children.Add(testedNumberBorder);

      // Поле "Номер проверяющего X.X"
      testerNumberBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      Grid testerGrid = new Grid();
      testerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
      testerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

      testerNumberData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      testerNumberData.Text = "Номер проверяющего X.X";
      InputControlSettings.DefaultGotAndLostEvent(testerNumberData, "Номер проверяющего X.X");
      testerNumberData.PreviewTextInput += DevicesNumberData_PreviewTextInput;

      //var testerLabel = new TextBox
      //{
      //  Style = (Style)Application.Current.Resources["MetrologyTextBox"],
      //  Text = "(X.X)",
      //  IsReadOnly = true
      //};

      Grid.SetColumn(testerNumberData, 0);
      //Grid.SetColumn(testerLabel, 1);
      testerGrid.Children.Add(testerNumberData);
      //testerGrid.Children.Add(testerLabel);

      testerNumberBorder.Child = testerGrid;
      contentStack.Children.Add(testerNumberBorder);

      // Поле "Диапазон проверки 1, 2-25"
      rangeBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      Grid rangeGrid = new Grid();
      rangeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
      rangeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

      rangeData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      rangeData.Text = "Диапазон проверки 1, 2-25";
      InputControlSettings.DefaultGotAndLostEvent(rangeData, "Диапазон проверки 1, 2-25");
      rangeData.PreviewTextInput += RangeData_PreviewTextInput;

      //var rangeLabel = new TextBox
      //{
      //  Style = (Style)Application.Current.Resources["MetrologyTextBox"],
      //  Text = "(1, 2-25)",
      //  IsReadOnly = true
      //};

      Grid.SetColumn(rangeData, 0);
      //Grid.SetColumn(rangeLabel, 1);
      rangeGrid.Children.Add(rangeData);
      //rangeGrid.Children.Add(rangeLabel);

      rangeBorder.Child = rangeGrid;
      contentStack.Children.Add(rangeBorder);


      //// Поле "Задержка измерения ,мсек."
      //delayBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      //Grid delayGrid = new Grid();
      //delayGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      //delayGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

      //// Настраиваем TextBox
      //delayData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      //delayData.Text = "Задержка измерения ,мсек.";
      //InputControlSettings.DefaultGotAndLostEvent(delayData, "Задержка измерения ,мсек.");
      //delayData.PreviewTextInput += DelayData_PreviewTextInput;

      //Grid.SetColumn(delayData, 0);
      //delayGrid.Children.Add(delayData);

      //// Упаковываем в Border
      //delayBorder.Child = delayGrid;
      //contentStack.Children.Add(delayBorder);

    }

    private void DelayData_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      // Разрешаем только цифры
      foreach (char c in e.Text)
      {
        if (!char.IsDigit(c))
        {
          e.Handled = true;
          break;
        }
      }
    }

    private void DevicesNumberData_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      // Разрешаем только цифры и точку
      foreach (char c in e.Text)
      {
        if (!char.IsDigit(c) && c != '.')
        {
          e.Handled = true;
          break;
        }
      }
    }

    private void RangeData_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      // Разрешаем только цифры, запятую, тире
      foreach (char c in e.Text)
      {
        if (!char.IsDigit(c) && c != ',' && c != '-')
        {
          e.Handled = true;
          break;
        }
      }
    }

    /// <summary>
    /// Метод, который вызывается при нажатии кнопки "Старт" (ShowOnlyStartButton).
    /// </summary>
    private async Task ExecuteTestProcess(CancellationToken cancellationToken)
    {
      LogInformation("Запуск теста CrossTestMKR...");

      List<int> points = ParseRange(rangeData.Text);

      await InitializeModule(testedNumberData.Text);
      await InitializeModule(testerNumberData.Text);

      await MeterEnableAsync(testerNumberData.Text);

      await RunPart1(testedNumberData.Text, testerNumberData.Text, points, cancellationToken);
      await RunPart2(testedNumberData.Text, testerNumberData.Text, points, cancellationToken);
      await RunPart3(testedNumberData.Text, testerNumberData.Text, points, cancellationToken);

      await MeterDisableAsync(testerNumberData.Text);
    }

    /// <summary>
    /// Принудительно останавливает выполнение теста CrossTestMKR:
    ///  • выключает измеритель;
    ///  • сбрасывает оба модуля;
    ///  • выполняет общий Reset всей системы.
    /// </summary>
    private async Task Stop(CancellationToken cancellationToken)
    {
      // 1. Отключаем измеритель, если он был задействован
      await MeterDisableAsync(testerNumberData.Text);

      // 2. Сбрасываем модули в исходное состояние
      await ResetModule(testedNumberData.Text);
      await ResetModule(testerNumberData.Text);

      // 3. Общий сброс системы (как в NodeMethodControl)
      await CommunicationManager.ResetAllSystem();
    }

  }
}