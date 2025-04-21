using System.Windows;
using System.Windows.Controls;
using UI.Controls.Protocol;
using Utilities.Events;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для InputFieldLightweight.xaml
  /// </summary>
  public partial class InputFieldLightweight : UserControl
  {
    #region Свойства доступа к данным

    /// <summary>
    /// Номер проверяемого устройства
    /// </summary>
    public string TestedNumber
    {
      get => TestedNumberBox.Text;
      set => TestedNumberBox.Text = value;
    }

    /// <summary>
    /// Номер проверяющего устройства
    /// </summary>
    public string TesterNumber
    {
      get => TesterNumberBox.Text;
      set => TesterNumberBox.Text = value;
    }

    /// <summary>
    /// Диапазон проверки
    /// </summary>
    public string TestRange
    {
      get => TestRangeBox.Text;
      set => TestRangeBox.Text = value;
    }

    #endregion

    /// <summary>
    /// Инициализирует новый экземпляр класса
    /// </summary>
    public InputFieldLightweight()
    {
      InitializeComponent();
      SubscribeToEvents();
    }

    /// <summary>
    /// Подписка на необходимые события
    /// </summary>
    private void SubscribeToEvents()
    {
      ActionExecutor.StartProcessing += ActionExecutor_StartProcessing;
    }

    /// <summary>
    /// Обработчик запуска процесса обработки
    /// </summary>
    private void ActionExecutor_StartProcessing(bool isProcessing)
    {
      var testedBaseText = "Номер проверяемого";
      var testerBaseText = "Номер проверяющего";
      var rangeBaseText = "Диапазон проверки";

      var visibility = isProcessing ? Visibility.Collapsed : Visibility.Visible;
      TestedNumberBox.Visibility = visibility;
      TesterNumberBox.Visibility = visibility;
      TestRangeBox.Visibility = visibility;

      if (isProcessing)
      {
        headerTestedNumber.Text = $"{testedBaseText}: {TestedNumber}";
        headerTesterNumber.Text = $"{testerBaseText}: {TesterNumber}";
        headerTestRange.Text = $"{rangeBaseText}: {TestRange}";
      }
      else
      {
        headerTestedNumber.Text = $"{testedBaseText}: вида a.b";
        headerTesterNumber.Text = $"{testerBaseText}: вида a.b";
        headerTestRange.Text = rangeBaseText;
      }
    }

    #region Методы подсветки ошибок

    /// <summary>
    /// Подсветка поля проверяемого номера
    /// </summary>
    public void HighlightTestedNumber()
    {
      TestedNumberBox.DataError();
    }

    /// <summary>
    /// Подсветка поля проверяющего номера
    /// </summary>
    public void HighlightTesterNumber()
    {
      TesterNumberBox.DataError();
    }

    /// <summary>
    /// Подсветка поля диапазона проверки
    /// </summary>
    public void HighlightTestRange()
    {
      TestRangeBox.DataError();
    }

    #endregion
  }
}