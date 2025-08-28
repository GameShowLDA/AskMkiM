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
  public class TranslationViewModel
  {
    /// <summary>
    /// Команда запуска сборки программы контроля.
    /// </summary>
    public ICommand BuildCommand { get; }

    /// <summary>
    /// Команда запуска исполнителя команды контроля.
    /// </summary>
    public ICommand RunCommand { get; }

    /// <summary>
    /// Сервис административных функций.
    /// </summary>
    private readonly TranslationServices _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TranslationViewModel"/>.
    /// </summary>
    /// <param name="service">Сервис административных функций.</param>
    public TranslationViewModel(TranslationServices service)
    {
      _service = service;
      BuildCommand = new AsyncRelayCommand(_service.BuildAsync);
      RunCommand = new AsyncRelayCommand(_service.RunAsync);
    }
  }
}
