using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.UI.Features.ProtocolNew.Hotkeys;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;

namespace Ask.UI.Features.ProtocolNew.Protocol
{
  /// <summary>
  /// Управляет паузой и пошаговым выполнением после отображения одной записи протокола.
  /// Сохраняет запись видимой до перехода исполнителя в ожидание оператора.
  /// </summary>
  internal sealed class ProtocolPostOutputController
  {
    /// <summary>Контекст текущего исполнителя и его элементов управления.</summary>
    private readonly IProtocolPostOutputContext _context;

    /// <summary>
    /// Создаёт контроллер действий, выполняемых после вывода записи.
    /// </summary>
    /// <param name="context">Контекст исполнителя и элементов управления.</param>
    public ProtocolPostOutputController(IProtocolPostOutputContext context)
    {
      _context = context;
    }

    /// <summary>
    /// Последовательно обрабатывает активную паузу, остановку при ошибке и пошаговый режим.
    /// </summary>
    /// <param name="message">Уже отображённая запись протокола.</param>
    /// <param name="isBlockStart">Признак начала блока команды программы контроля.</param>
    /// <param name="skipStepModeCheck">Признак пропуска ожидания пошаговой команды.</param>
    /// <param name="skipPause">Признак пропуска автоматической паузы при ошибке.</param>
    public async Task ProcessAsync(
      ShowMessageModel message,
      bool isBlockStart,
      bool skipStepModeCheck,
      bool skipPause)
    {
      if (_context.IsPaused)
      {
        await _context.WaitWhilePausedAsync(_context.GetCancellationToken());
      }

      if (!skipPause)
      {
        await PauseOnErrorAsync(message.Status);
      }

      if (StepControlManager.StepMode && !skipStepModeCheck && ShouldWaitForStep(message, isBlockStart))
      {
        _context.ShowPauseButtons();
        await KeyboardManager.WaitForNextStepKeyAsync(_context.GetCancellationToken());

        var showStepButtons = StepControlManager.IsStepInto
          && !StepControlManager.StepOverUntilNextControlCommand;
        _context.ShowRunningButtons(showStepButtons);
      }

    }

    /// <summary>Устанавливает паузу после ошибочной записи, если это разрешено настройками.</summary>
    private async Task PauseOnErrorAsync(MessageType? status)
    {
      if (status == MessageType.Error && await ExecutionConfig.GetIsStopOnErrorEnabled())
      {
        await _context.PauseAsync();
      }
    }

    /// <summary>Определяет, требуется ли ожидать следующую пошаговую команду оператора.</summary>
    private static bool ShouldWaitForStep(ShowMessageModel message, bool isBlockStart)
    {
      if (StepControlManager.IsStepInto)
      {
        return true;
      }

      if (!StepControlManager.StepOverUntilNextControlCommand)
      {
        return false;
      }

      if (!IsControlProgramCommandStart(message, isBlockStart))
      {
        return false;
      }

      StepControlManager.CompleteStepOverUntilNextControlCommand();
      return true;
    }

    /// <summary>Проверяет, является ли запись заголовком новой команды программы контроля.</summary>
    private static bool IsControlProgramCommandStart(ShowMessageModel message, bool isBlockStart)
    {
      return isBlockStart
        && message.Status == MessageType.Command
        && message.IsControlProgramCommandHeader;
    }
  }
}
