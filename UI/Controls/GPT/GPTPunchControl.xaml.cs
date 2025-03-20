using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NewCore.Base.Interface.Main;

namespace UI.Controls.GPT
{
  /// <summary>
  /// Контрол для управления режимом GPTPunch.
  /// </summary>
  public partial class GPTPunchControl : UserControl
  {
    /// <summary>
    /// Статическая модель GPT, используемая для подключения и проверки связи.
    /// </summary>
    static internal IBreakdownTester ModelGPT { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="GPTPunchControl"/>.
    /// </summary>
    public GPTPunchControl()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Обрабатывает событие нажатия левой кнопки мыши на элементе ConnectMenuItem.
    /// Создает и подключает новую модель GPT, затем обновляет видимость элементов управления.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события мыши.</param>
    private void ConnectMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      ModelGPT = new Core.GptLibrary.Model();
      ModelGPT.Connect();

      if (ModelGPT.CheckConnection())
      {
        ConnectMenuItem.Visibility = Visibility.Collapsed;
        DisconnectMenuItem.Visibility = Visibility.Visible;
        Controller.Visibility = Visibility.Visible;
      }
    }

    /// <summary>
    /// Обрабатывает событие нажатия левой кнопки мыши на элементе DisconnectMenuItem.
    /// Если связь установлена, отключает модель GPT и обновляет видимость элементов управления.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события мыши.</param>
    private void DisconnectMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (ModelGPT.CheckConnection())
      {
        ModelGPT.Disconnect();
        ConnectMenuItem.Visibility = Visibility.Visible;
        DisconnectMenuItem.Visibility = Visibility.Collapsed;
        Controller.Visibility = Visibility.Collapsed;
      }
    }
  }
}
