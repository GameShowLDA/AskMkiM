using System.Windows.Controls;
using System.Windows.Input;
using AppConfig.Data.MeasurementError;
using static AppConfig.Config.MeasurementErrorConfig;
using static AppConfig.Data.MeasurementError.MeasurementErrorModel;

namespace Mode.Settings.MeasurementError
{
  /// <summary>
  /// Логика взаимодействия для MeasurementErrorControl.xaml.
  /// </summary>
  public partial class MeasurementErrorControl : UserControl
  {
    /// <summary>
    /// Модель измерения ошибки для команды KC.
    /// </summary>
    private MeasurementErrorModel KcModel { get; set; }

    /// <summary>
    /// Модель измерения ошибки для команды CI.
    /// </summary>
    private MeasurementErrorModel CiModel { get; set; }

    /// <summary>
    /// Модель измерения ошибки для команды PR.
    /// </summary>
    private MeasurementErrorModel PrModel { get; set; }

    /// <summary>
    /// Модель измерения ошибки для команды IE.
    /// </summary>
    private MeasurementErrorModel IeModel { get; set; }

    /// <summary>
    /// Флаг, указывающий, запущен ли процесс инициализации.
    /// </summary>
    private bool start = true;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeasurementErrorControl"/>.
    /// </summary>
    public MeasurementErrorControl()
    {
      InitializeComponent();
      Task.Run(() => InitAsync());
      start = false;
    }

    /// <summary>
    /// Обрабатывает событие предварительного ввода текста в текстовое поле.
    /// Проверяет, является ли вводимый текст числовым.
    /// </summary>
    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (e.Text == "." || e.Text == ",")
      {
        e.Handled = true;

        if (sender is TextBox textBox)
        {
          int cursorPosition = textBox.SelectionStart;
          textBox.Text = textBox.Text.Insert(cursorPosition, ",");
          textBox.SelectionStart = cursorPosition + 1;
          textBox.SelectionLength = 0;
        }
      }
      else
      {
        CheckIsNumeric(e);
      }
    }

    /// <summary>
    /// Обрабатывает событие изменения текста.
    /// Вызывает метод сохранения новых данных.
    /// </summary>
    private async void TextChanged(object sender, TextChangedEventArgs e)
    {
      await UpdateDataAsync();
    }

    /// <summary>
    /// Проверяет, является ли введенный текст числовым значением.
    /// </summary>
    /// <param name="e">Аргументы события предварительного ввода текста.</param>
    /// <returns>True, если текст не является числовым; в противном случае - false.</returns>
    private bool CheckIsNumeric(TextCompositionEventArgs e)
    {
      if (!double.TryParse(e.Text, out _))
      {
        e.Handled = true;
        return true;
      }

      return false;
    }

    /// <summary>
    /// Инициализирует модели и обновляет файл, если необходимо.
    /// </summary>
    private async Task InitAsync()
    {
      bool kcUpdated = UpdateKCModel();
      bool prUpdated = UpdatePRModel();
      bool ieUpdated = UpdateIEModel();
      bool ciUpdated = UpdateCIModel();

      if (!kcUpdated || !prUpdated || !ieUpdated || !ciUpdated)
      {
        await UpdateFileAsync();
      }
    }

    /// <summary>
    /// Обновляет модель KC и возвращает true, если обновление прошло успешно.
    /// </summary>
    private bool UpdateKCModel()
    {
      bool success = false;

      if (GetNumericError(TypeCommand.KC) != 0 && GetPercentageError(TypeCommand.KC) != 0)
      {
        KcModel = new MeasurementErrorModel(TypeCommand.KC, GetPercentageError(TypeCommand.KC), GetNumericError(TypeCommand.KC));
        success = true;
      }
      else
      {
        KcModel = new MeasurementErrorModel(TypeCommand.KC, 1, 5);
      }

      absoluteErrorKC.Text = KcModel.NumericError.ToString();
      percentErrorKC.Text = KcModel.PercentageError.ToString();

      return success;
    }

    /// <summary>
    /// Обновляет модель PR и возвращает true, если обновление прошло успешно.
    /// </summary>
    private bool UpdatePRModel()
    {
      bool success = false;

      if (GetNumericError(TypeCommand.PR) != 0 && GetPercentageError(TypeCommand.PR) != 0)
      {
        PrModel = new MeasurementErrorModel(TypeCommand.PR, GetPercentageError(TypeCommand.PR), GetNumericError(TypeCommand.PR));
        success = true;
      }
      else
      {
        PrModel = new MeasurementErrorModel(TypeCommand.PR, 1, 5);
      }

      absoluteErrorPR.Text = PrModel.NumericError.ToString();
      percentErrorPR.Text = PrModel.PercentageError.ToString();

      return success;
    }

    /// <summary>
    /// Обновляет модель IE и возвращает true, если обновление прошло успешно.
    /// </summary>
    private bool UpdateIEModel()
    {
      bool success = false;
      if (GetNumericError(TypeCommand.IE) != 0 && GetPercentageError(TypeCommand.IE) != 0)
      {
        IeModel = new MeasurementErrorModel(TypeCommand.IE, GetPercentageError(TypeCommand.IE), GetNumericError(TypeCommand.IE));
        success = true;
      }
      else
      {
        IeModel = new MeasurementErrorModel(TypeCommand.IE, 1, 5);
      }

      absoluteErrorIE.Text = IeModel.NumericError.ToString();
      percentErrorIE.Text = IeModel.PercentageError.ToString();

      return success;
    }

    /// <summary>
    /// Обновляет модель CI и возвращает true, если обновление прошло успешно.
    /// </summary>
    private bool UpdateCIModel()
    {
      bool success = false;

      if (GetNumericError(TypeCommand.CI) != 0 && GetPercentageError(TypeCommand.CI) != 0)
      {
        CiModel = new MeasurementErrorModel(TypeCommand.CI, GetPercentageError(TypeCommand.CI), GetNumericError(TypeCommand.CI));
        success = true;
      }
      else
      {
        CiModel = new MeasurementErrorModel(TypeCommand.CI, 1, 5);
      }

      absoluteErrorCI.Text = CiModel.NumericError.ToString();
      percentErrorCI.Text = CiModel.PercentageError.ToString();

      return success;
    }

    /// <summary>
    /// Собирает все модели в список и перезаписывает файл с помощью JsonHelper.
    /// </summary>
    private async Task UpdateFileAsync()
    {
      var models = new List<MeasurementErrorModel>
      {
        KcModel,
        CiModel,
        PrModel,
        IeModel,
      };

      MeasurementErrorFileManage measurementErrorFileManage = new MeasurementErrorFileManage(AppConfig.FileLocations.MeasurementErrorConfigPath);
      await measurementErrorFileManage.RewriteFileAsync(models);
    }

    /// <summary>
    /// Обновляет данные моделей на основе значений из текстовых полей и перезаписывает файл.
    /// </summary>
    private async Task UpdateDataAsync()
    {
      if (!start)
      {
        double.TryParse(absoluteErrorKC.Text, out double kcNumericError);
        double.TryParse(percentErrorKC.Text, out double kcPercentageError);
        KcModel = new MeasurementErrorModel(TypeCommand.KC, kcPercentageError, kcNumericError);

        double.TryParse(absoluteErrorPR.Text, out double prNumericError);
        double.TryParse(percentErrorPR.Text, out double prPercentageError);
        PrModel = new MeasurementErrorModel(TypeCommand.PR, prPercentageError, prNumericError);

        double.TryParse(absoluteErrorIE.Text, out double ieNumericError);
        double.TryParse(percentErrorIE.Text, out double iePercentageError);
        IeModel = new MeasurementErrorModel(TypeCommand.IE, iePercentageError, ieNumericError);

        double.TryParse(absoluteErrorCI.Text, out double ciNumericError);
        double.TryParse(percentErrorCI.Text, out double ciPercentageError);
        CiModel = new MeasurementErrorModel(TypeCommand.CI, ciPercentageError, ciNumericError);

        await UpdateFileAsync();
      }
    }
  }
}
