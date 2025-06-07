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
  /// <summary>
  /// ViewModel для управления сервисами.
  /// Содержит команды для отображения элементов управления различных типов сервисов в редакторе.
  /// </summary>
  public class ServiceViewModel
  {
    /// <summary>
    /// Сервис для работы с вкладкой сервиса.
    /// </summary>
    private readonly ServiceMode _serviceMode;

    /// <summary>
    /// Команда отображения сервисного режима модуля типа МеШ.
    /// </summary>
    public ICommand ServicesTestMeshCommand { get; }

    /// <summary>
    /// Команда отображения сервисного режима модуля типа МИНТ.
    /// </summary>
    public ICommand ServicesTestMintCommand { get; }

    /// <summary>
    /// Команда отображения сервисного режима модуля типа МКР.
    /// </summary>
    public ICommand ServicesTestMkrCommand { get; }

    /// <summary>
    /// Команда отображения сервисного режима модуля типа УКШ.
    /// </summary>
    public ICommand ServicesTestUkshCommand { get; }

    public ServiceViewModel(ServiceMode serviceMode)
    {
      _serviceMode = serviceMode;

      ServicesTestMeshCommand = new AsyncRelayCommand(_serviceMode.AddServicesTestMeshControlAsync);
      ServicesTestMintCommand = new AsyncRelayCommand(_serviceMode.AddServicesTestMintControlAsync);
      ServicesTestMkrCommand = new AsyncRelayCommand(_serviceMode.AddServicesTestMkrControlAsync);
      ServicesTestUkshCommand = new AsyncRelayCommand(_serviceMode.AddServicesTestUkshControlAsync);
    }
  }
}
