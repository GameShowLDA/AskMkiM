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
using UI.Components.ProtocolListBox;
using UI.Controls.Protocol;
using Utilities.Models;

namespace MainWindowProgram.Test.Protocol
{
  /// <summary>
  /// Логика взаимодействия для TestProtocol.xaml
  /// </summary>
  public partial class TestProtocol : UserControl
  {

    public TestProtocol()
    {
      InitializeComponent();
      InitializeSettings();
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings()
    {
      Test.SetSettings(
        this,
        StartDelegate: ExecuteMeasurementProcess,
        true);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      await Task.Run(async () =>
       {
         int i = 0;
         while (true)
         {
           Test.GetCancellationToken().ThrowIfCancellationRequested();

           await Test.ShowMessageAsync(new ShowMessageModel("Тест", message: i.ToString() + $"[{ShowMessageModel.SuccessMessage.Title}]", messageColor: ShowMessageModel.SuccessMessage.TitleColor));
           i++;
         }
       });
    }
  }
}
