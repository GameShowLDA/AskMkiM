using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MainWindowProgram.Infrastructure;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  public class SelfTestViewModel
  {
    private readonly SelfTestServices _selfTestServices;

    /// <summary>
    /// Команда отображения метода узла СИ.
    /// </summary>
    public ICommand SelfTestModuleCommand { get; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TestViewModel"/>.
    /// </summary>
    /// <param name="testService">Сервис для работы с тестами.</param>
    public SelfTestViewModel(SelfTestServices testService)
    {
      _selfTestServices = testService;

      SelfTestModuleCommand = new AsyncRelayCommand(_selfTestServices.AddSelfTestModuleAsync);
    }
  }
}
