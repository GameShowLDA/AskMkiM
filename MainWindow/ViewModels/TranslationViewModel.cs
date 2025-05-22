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
    /// Команда открытия интерфейса управления ППУ (пробойной установкой).
    /// </summary>
    public ICommand StartTranslationCommand { get; }

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
      StartTranslationCommand = new AsyncRelayCommand(_service.StartTranslationAsync);
    }
  }
}
