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
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using Mode.Models;
using UI.Controls.Protocol;
using Utilities.Models;
using static Utilities.LoggerUtility;

namespace TestWPF
{
  /// <summary>
  /// Логика взаимодействия для TestProtocolControl.xaml
  /// </summary>
  public partial class TestProtocolControl : UserControl
  {
    public TestProtocolControl()
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
      try
      {
        ProtocolUI.SetSettings(this, StartDelegate: ExecuteMeasurementProcess, true, null);
      }
      catch (Exception ex)
      {
        var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        LogError($"Ошибка загрузки элемента метрологии КС в методе {methodName}: {ex.Message}");
      }
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      var (ok, msg, first, second, param) = await UIValidationHelper.TryValidateAndParseInputAsync<TestMeasurement>(ProtocolUI);
      if (!ok)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, msg));
        return;
      }
    }
  }
  public class TestMeasurement : BaseMeasurement
  {
    public TestMeasurement() : base() { }

    /// <inheritdoc />
    protected override void ConfigureMultimeter()
    {
      // Заглушка для теста
    }
  }
}
