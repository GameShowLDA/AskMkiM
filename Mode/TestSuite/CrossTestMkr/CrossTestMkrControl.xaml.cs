using Core.Communication;
using Mode.Base.SearchDevices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI.Components.Invoke;
using Utilities.Models;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.TestSuite.CrossTestMkr
{
  public partial class CrossTestMkrControl : UserControl
  {
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;

    private InvokeBorder testedNumberBorder = new InvokeBorder();
    private InvokeTextBox testedNumberData = new InvokeTextBox();

    private InvokeBorder verificatNumberBorder = new InvokeBorder();
    private InvokeTextBox verificatNumberData = new InvokeTextBox();

    private InvokeBorder rangeBorder = new InvokeBorder();
    private InvokeTextBox rangeData = new InvokeTextBox();

    public CrossTestMkrControl()
    {
      InitializeComponent();
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
        ProtocolSelfCheckControl.Header = "Перекрёстный тест";

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
      #region Поле "Номер проверяемого X.X"

      testedNumberBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      Grid testedGrid = new Grid();
      testedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
      testedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

      testedNumberData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      testedNumberData.Text = "Номер проверяемого X.X";
      InputControlSettings.DefaultGotAndLostEvent(testedNumberData, "Номер проверяемого X.X");
      testedNumberData.PreviewTextInput += DevicesNumberData_PreviewTextInput;

      Grid.SetColumn(testedNumberData, 0);
      testedGrid.Children.Add(testedNumberData);

      testedNumberBorder.Child = testedGrid;
      contentStack.Children.Add(testedNumberBorder);

      #endregion

      #region Поле "Номер проверяющего X.X"

      verificatNumberBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      Grid verificatGrid = new Grid();
      verificatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
      verificatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

      verificatNumberData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      verificatNumberData.Text = "Номер проверяющего X.X";
      InputControlSettings.DefaultGotAndLostEvent(verificatNumberData, "Номер проверяющего X.X");
      verificatNumberData.PreviewTextInput += DevicesNumberData_PreviewTextInput;

      Grid.SetColumn(verificatNumberData, 0);
      verificatGrid.Children.Add(verificatNumberData);

      verificatNumberBorder.Child = verificatGrid;
      contentStack.Children.Add(verificatNumberBorder);

      #endregion

      #region Поле "Диапазон проверки 1, 2-25"

      rangeBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
      Grid rangeGrid = new Grid();
      rangeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
      rangeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

      rangeData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
      rangeData.Text = "Диапазон проверки 1, 2-25";
      InputControlSettings.DefaultGotAndLostEvent(rangeData, "Диапазон проверки 1, 2-25");
      rangeData.PreviewTextInput += RangeData_PreviewTextInput;

      Grid.SetColumn(rangeData, 0);
      rangeGrid.Children.Add(rangeData);

      rangeBorder.Child = rangeGrid;
      contentStack.Children.Add(rangeBorder);

      #endregion
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

    // Добавляем новый метод в класс CrossTestMkrControl
    private async Task<bool> ValidateInputFieldsAsync()
    {
      // Стандартные (исходные) строки для проверки
      const string defaultTested = "Номер проверяемого X.X";
      const string defaultVerificat = "Номер проверяющего X.X";
      const string defaultRange = "Диапазон проверки 1, 2-25";

      // Разделяем строки на фрагменты для проверки формата номеров
      string[] testedSplit = testedNumberData.Text.Split('.');
      string[] verificatSplit = verificatNumberData.Text.Split('.');

      // Проверка поля "Номер проверяемого"
      if (
          testedNumberData.Text.Trim() == defaultTested ||
          testedSplit.Length != 2 ||
          string.IsNullOrWhiteSpace(testedSplit[0]) ||
          string.IsNullOrWhiteSpace(testedSplit[1])
        )
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel("Поле 'Номер проверяемого' не заполнено корректно!", errorText.Item2));
        return false;
      }

      // Проверка поля "Номер проверяющего"
      if (
          verificatNumberData.Text.Trim() == defaultVerificat ||
          verificatSplit.Length != 2 ||
          string.IsNullOrWhiteSpace(verificatSplit[0]) ||
          string.IsNullOrWhiteSpace(verificatSplit[1])
        )
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel("Поле 'Номер проверяющего' не заполнено корректно!", errorText.Item2));
        return false;
      }

      // Проверка поля "Диапазон проверки" на наличие стандартного текста
      if (rangeData.Text.Trim() == defaultRange)
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel("Поле 'Диапазон проверки' не заполнено корректно!", errorText.Item2));
        return false;
      }

      // Дополнительная проверка диапазона на корректность формата и значений
      if (!ValidateRangeInput(rangeData.Text.Trim(), out string rangeError))
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel($"Поле 'Диапазон проверки' не заполнено корректно: {rangeError}", errorText.Item2));
        return false;
      }

      // Проверка, что значения в полях номеров не совпадают
      if (testedNumberData.Text.Trim() == verificatNumberData.Text.Trim())
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel("Значения полей 'Номер проверяемого' и 'Номер проверяющего' не должны совпадать!", errorText.Item2));
        return false;
      }

      return true;
    }

    /// <summary>
    /// Проверяет корректность строки диапазона.
    /// Требования:
    ///  - Не должно быть пустых сегментов после разделения запятой.
    ///  - Если сегмент содержит тире, оба числа успешно конвертируются, и первое число меньше второго.
    ///  - Все числа не больше 350.
    /// </summary>
    /// <param name="rangeText">Введённый диапазон, например "1, 2-25"</param>
    /// <param name="errorMessage">Сообщение об ошибке при некорректном вводе</param>
    /// <returns>True, если ввод корректный, иначе false.</returns>
    private bool ValidateRangeInput(string rangeText, out string errorMessage)
    {
      errorMessage = "";
      // Разбиваем по запятой
      string[] segments = rangeText.Split(',');
      foreach (string segment in segments)
      {
        string trimmed = segment.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
          errorMessage = "Обнаружено пустое значение в диапазоне.";
          return false;
        }

        // Если сегмент содержит тире, обрабатываем как диапазон (например, "2-25")
        if (trimmed.Contains("-"))
        {
          string[] bounds = trimmed.Split('-');
          // Проверяем, что есть ровно два элемента и ни один из них не пустой
          if (bounds.Length != 2 ||
              string.IsNullOrWhiteSpace(bounds[0]) ||
              string.IsNullOrWhiteSpace(bounds[1]))
          {
            errorMessage = $"Неправильный формат диапазона: \"{trimmed}\".";
            return false;
          }

          if (!int.TryParse(bounds[0].Trim(), out int start) ||
              !int.TryParse(bounds[1].Trim(), out int end))
          {
            errorMessage = $"Невозможно преобразовать числа в диапазоне: \"{trimmed}\".";
            return false;
          }

          if (start >= end)
          {
            errorMessage = $"В диапазоне \"{trimmed}\": значение до тире ({start}) должно быть меньше значения после тире ({end}).";
            return false;
          }

          if (start > 350 || end > 350)
          {
            errorMessage = $"В диапазоне \"{trimmed}\" одно из значений больше 350.";
            return false;
          }
        }
        else
        {
          // Обрабатываем одиночное число
          if (!int.TryParse(trimmed, out int number))
          {
            errorMessage = $"Невозможно преобразовать число: \"{trimmed}\".";
            return false;
          }
          if (number > 350)
          {
            errorMessage = $"Число {number} больше 350.";
            return false;
          }
        }
      }
      return true;
    }

    /// <summary>
    /// Метод, который вызывается при нажатии кнопки "Старт" (ShowOnlyStartButton).
    /// </summary>
    private async Task ExecuteTestProcess(CancellationToken cancellationToken)
    {
      // Выполняем проверку корректности введённых данных
      if (!await ValidateInputFieldsAsync())
      {
        // Если данные некорректны, прерываем выполнение теста
        LogError("Ошибка: введены некорректные данные. Тест не будет запущен.");
        return;
      }

      LogInformation("Запуск теста CrossTestMKR...");

      List<int> points = ParseRange(rangeData.Text);

      await InitializeModule(testedNumberData.Text);
      await InitializeModule(verificatNumberData.Text);

      await MeterEnableAsync(verificatNumberData.Text);

      await RunPart1(testedNumberData.Text, verificatNumberData.Text, points, cancellationToken);
      await RunPart2(testedNumberData.Text, verificatNumberData.Text, points, cancellationToken);
      await RunPart3(testedNumberData.Text, verificatNumberData.Text, points, cancellationToken);

      await MeterDisableAsync(verificatNumberData.Text);
    }

    /// <summary>
    /// Принудительно останавливает выполнение теста CrossTestMKR:
    ///  • выключает измеритель;
    ///  • сбрасывает оба модуля;
    ///  • выполняет общий Reset всей системы.
    /// </summary>
    private async Task Stop(CancellationToken cancellationToken)
    {
      await MeterDisableAsync(verificatNumberData.Text);

      await ResetModule(testedNumberData.Text);
      await ResetModule(verificatNumberData.Text);

      await CommunicationManager.ResetAllSystem();
    }

  }
}