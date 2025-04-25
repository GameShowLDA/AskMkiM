using Mode.Base;
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
    private readonly (string Title, Color TitleColor) goodText = SuccessMessage;
    private readonly (string Title, Color TitleColor) errorText = ErrorMessage;

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

        //// 2. Очищаем предыдущий контент
        ////await ProtocolSelfCheckControl.ClearContent();
                 
        //// 3. Создаём StackPanel (можно использовать InputControlSettings при желании)
        ////    Или вручную:
        //StackPanel contentStack = new StackPanel();
        //contentStack.Orientation = Orientation.Vertical;

        //// 4. Добавляем поля
        //AddInputFields(contentStack);

        //// 5. Добавляем StackPanel в ProtocolUI
        ////ProtocolSelfCheckControl.AddContent(contentStack);

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
    //private void AddInputFields(StackPanel contentStack)
    //{
    //  #region Поле "Номер проверяемого X.X"

    //  testedNumberBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
    //  Grid testedGrid = new Grid();
    //  testedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
    //  testedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

    //  testedNumberData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
    //  testedNumberData.Text = "Номер проверяемого X.X";
    //  //InputControlSettings.DefaultGotAndLostEvent(testedNumberData, "Номер проверяемого X.X");
    //  testedNumberData.PreviewTextInput += DevicesNumberData_PreviewTextInput;

    //  Grid.SetColumn(testedNumberData, 0);
    //  testedGrid.Children.Add(testedNumberData);

    //  testedNumberBorder.Child = testedGrid;
    //  contentStack.Children.Add(testedNumberBorder);

    //  #endregion

    //  #region Поле "Номер проверяющего X.X"

    //  verificatNumberBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
    //  Grid verificatGrid = new Grid();
    //  verificatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
    //  verificatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

    //  verificatNumberData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
    //  verificatNumberData.Text = "Номер проверяющего X.X";
    //  //InputControlSettings.DefaultGotAndLostEvent(verificatNumberData, "Номер проверяющего X.X");
    //  verificatNumberData.PreviewTextInput += DevicesNumberData_PreviewTextInput;

    //  Grid.SetColumn(verificatNumberData, 0);
    //  verificatGrid.Children.Add(verificatNumberData);

    //  verificatNumberBorder.Child = verificatGrid;
    //  contentStack.Children.Add(verificatNumberBorder);

    //  #endregion

    //  #region Поле "Диапазон проверки 1, 2-25"

    //  rangeBorder.Style = (Style)Application.Current.Resources["MetrologyTextBoxBorder"];
    //  Grid rangeGrid = new Grid();
    //  rangeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
    //  rangeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

    //  rangeData.Style = (Style)Application.Current.Resources["MetrologyTextBox"];
    //  rangeData.Text = "Диапазон проверки 1, 2-25";
    //  //InputControlSettings.DefaultGotAndLostEvent(rangeData, "Диапазон проверки 1, 2-25");
    //  rangeData.PreviewTextInput += RangeData_PreviewTextInput;

    //  Grid.SetColumn(rangeData, 0);
    //  rangeGrid.Children.Add(rangeData);

    //  rangeBorder.Child = rangeGrid;
    //  contentStack.Children.Add(rangeBorder);

    //  #endregion
    //}

    //private void DevicesNumberData_PreviewTextInput(object sender, TextCompositionEventArgs e)
    //{
    //  // Разрешаем только цифры и точку
    //  foreach (char c in e.Text)
    //  {
    //    if (!char.IsDigit(c) && c != '.')
    //    {
    //      e.Handled = true;
    //      break;
    //    }
    //  }
    //}

    //private void RangeData_PreviewTextInput(object sender, TextCompositionEventArgs e)
    //{
    //  // Разрешаем только цифры, запятую, тире
    //  foreach (char c in e.Text)
    //  {
    //    if (!char.IsDigit(c) && c != ',' && c != '-')
    //    {
    //      e.Handled = true;
    //      break;
    //    }
    //  }
    //}

    // Добавляем новый метод в класс CrossTestMkrControl
    //private async Task<bool> ValidateInputFieldsAsync()
    //{
    //  // Оригинальные плейсхолдеры
    //  const string defaultTested = "Номер проверяемого a.b";
    //  const string defaultVerificat = "Номер проверяющего a.b";
    //  const string defaultRange = "Диапазон проверки";

    //  // Считаем текстовые значения из лёгкого поля
    //  var tested = InputField.TestedNumber?.Trim() ?? "";
    //  var tester = InputField.TesterNumber?.Trim() ?? "";
    //  var range = InputField.TestRange?.Trim() ?? "";

    //  // 1) Проверка «Номер проверяемого»
    //  var testedSplit = tested.Split('.');
    //  if (tested == defaultTested
    //   || testedSplit.Length != 2
    //   || string.IsNullOrWhiteSpace(testedSplit[0])
    //   || string.IsNullOrWhiteSpace(testedSplit[1]))
    //  {
    //    await ProtocolSelfCheckControl.ShowMessageAsync(
    //        new ShowMessageModel("Поле 'Номер проверяемого' заполнено некорректно!", ShowMessageModel.ErrorMessage.TitleColor));
    //    InputField.HighlightTestedNumber();
    //    return false;
    //  }

    //  // 2) Проверка «Номер проверяющего»
    //  var verificatSplit = tester.Split('.');
    //  if (tester == defaultVerificat
    //   || verificatSplit.Length != 2
    //   || string.IsNullOrWhiteSpace(verificatSplit[0])
    //   || string.IsNullOrWhiteSpace(verificatSplit[1]))
    //  {
    //    await ProtocolSelfCheckControl.ShowMessageAsync(
    //        new ShowMessageModel("Поле 'Номер проверяющего' заполнено некорректно!", ShowMessageModel.ErrorMessage.TitleColor));
    //    InputField.HighlightTesterNumber();
    //    return false;
    //  }

    //  // 3) Проверка «Диапазон проверки»
    //  if (range == defaultRange)
    //  {
    //    await ProtocolSelfCheckControl.ShowMessageAsync(
    //        new ShowMessageModel("Поле 'Диапазон проверки' не заполнено!", ShowMessageModel.ErrorMessage.TitleColor));
    //    InputField.HighlightTestRange();
    //    return false;
    //  }

    //  // 4) Дополнительная проверка формата диапазона
    //  if (!ValidateRangeInput(range, out string rangeError))
    //  {
    //    await ProtocolSelfCheckControl.ShowMessageAsync(
    //        new ShowMessageModel($"Диапазон указан неверно: {rangeError}", ShowMessageModel.ErrorMessage.TitleColor));
    //    InputField.HighlightTestRange();
    //    return false;
    //  }

    //  // 5) Убедимся, что проверяемый и проверяющий номера не совпадают
    //  if (tested == tester)
    //  {
    //    await ProtocolSelfCheckControl.ShowMessageAsync(
    //        new ShowMessageModel("Номера «проверяемого» и «проверяющего» не должны совпадать!", ShowMessageModel.ErrorMessage.TitleColor));
    //    InputField.HighlightTestedNumber();
    //    InputField.HighlightTesterNumber();
    //    return false;
    //  }

    //  return true;
    //}





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
    /// Метод, который вызывается при нажатии кнопки "Старт".
    /// </summary>
    private async Task ExecuteTestProcess(CancellationToken cancellationToken)
    {
      // 1. Валидация и парсинг трёх полей
      var (ok, message, tested, tester, range) =
          UIValidationHelperLightweight.TryValidateAndParseInput(ProtocolSelfCheckControl);
      if (!ok)
      {
        LogError($"Валидация не пройдена: {message}");
        return;
      }

      LogInformation("Запуск теста CrossTestMKR...");

      // 2. Преобразуем диапазон в список точек
      List<int> points = ParseRange(range);

      // 3. Подготовка оборудования
      await InitializeModule(tested);
      await InitializeModule(tester);
      await MeterEnableAsync(tester);

      // 4. Собственно сам тест
      await RunPart1(tested, tester, points, cancellationToken);
      await RunPart2(tested, tester, points, cancellationToken);
      await RunPart3(tested, tester, points, cancellationToken);

      // 5. Отключение измерителя
      await MeterDisableAsync(tester);
    }

    /// <summary>
    /// Принудительно останавливает выполнение теста CrossTestMKR:
    ///  • выключает измеритель;
    ///  • сбрасывает оба модуля;
    ///  • выполняет общий Reset всей системы.
    /// </summary>
    private async Task Stop(CancellationToken cancellationToken)
    {
      // Получаем контрол, чтобы достать текущие значения
      var input = ProtocolSelfCheckControl.GetInputFieldLightweightSafe();
      if (input == null) return;

      await MeterDisableAsync(input.TesterNumber);
      await ResetModule(input.TestedNumber);
      await ResetModule(input.TesterNumber);
    }
  }
}