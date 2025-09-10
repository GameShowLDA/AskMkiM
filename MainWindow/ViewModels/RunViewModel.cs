using MainWindowProgram.Infrastructure;
using MainWindowProgram.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MainWindowProgram.ViewModels
{
  public class RunViewModel
  {
    /// <summary>
    /// Сервис административных функций.
    /// </summary>
    private readonly RunServices _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RunViewModel"/>.
    /// </summary>
    /// <param name="service">Сервис административных функций.</param>
    public RunViewModel(RunServices service)
    {
      _service = service;
      //RunCommand = new AsyncRelayCommand(_service.RunAsync);
    }
  }
}
