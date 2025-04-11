using System.Windows.Controls;
using UI.Components.SearchControls;

namespace UI.Controls.ProtocolController
{
  /// <summary>
  /// Предоставляет свойства доступа к кнопкам управления в интерфейсе ProtocolController.
  /// Используется для получения ссылок на визуальные элементы из других компонентов.
  /// </summary>
  public partial class ProtocolController
  {
    /// <summary>
    /// Кнопка "Запустить".
    /// </summary>
    public Button StartButton => startButton;

    /// <summary>
    /// Кнопка "Остановить".
    /// </summary>
    public Button PauseButton => pauseButton;

    /// <summary>
    /// Кнопка "Продолжить".
    /// </summary>
    public Button ContinueButton => continueButton;

    /// <summary>
    /// Кнопка "Завершить".
    /// </summary>
    public Button ExitButton => exitButton;

    /// <summary>
    /// Кнопка "Вглубь (F11)".
    /// </summary>
    public Button StepIntoButton => stepIntoButton;

    /// <summary>
    /// Кнопка "Поверх (F10)".
    /// </summary>
    public Button StepOverButton => stepOverButton;

    /// <summary>
    /// Кнопка "Зациклить".
    /// </summary>
    public Button LoopButton => loopButton;

    /// <summary>
    /// Кнопка "Повторить".
    /// </summary>
    public Button ReturnButton => returnButton;

    /// <summary>
    /// Возвращает текстовый элемент.
    /// </summary>
    public Components.Invoke.InvokeRichTextBox.InvokeRichTextBoxUI ProtocolTextBox => protocolTextBox;
  }
}
