using Ask.Core.Services.Config.AppSettings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для работы с переводом (сборкой и запуском программ контроля).
  /// </summary>
  public partial class TranslationViewModel : ObservableObject
  {
    private readonly TranslationServices _service;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="TranslationViewModel"/>.
    /// </summary>
    public TranslationViewModel(TranslationServices service)
    {
      _service = service;
    }

    /// <summary>
    /// Команда запуска сборки программы контроля.
    /// </summary>
    [RelayCommand]
    private async Task BuildAsync() => await _service.BuildAsync();

    /// <summary>
    /// Команда запуска исполнителя программы контроля.
    /// </summary>
    [RelayCommand]
    private async Task RunAsync()
    {
      await ExecutionConfig.SetStepByStepMode(false);
      await _service.RunAsync();
    }

    /// <summary>
    /// Команда запуска исполнителя программы контроля.
    /// </summary>
    [RelayCommand]
    private async Task RunStepByStepModeAsync()
    {
      await ExecutionConfig.SetStepByStepMode(true);
      await _service.RunAsync();
    }
  }
}
