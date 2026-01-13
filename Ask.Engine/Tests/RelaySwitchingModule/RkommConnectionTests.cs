using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.Tests.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.RelaySwitchingModule
{
  public class RkommConnectionTests
  {

    private IExecutionController _controller;

    private IUserInteractionService _userInteractionService;

    private IMessageOutputService _messageOutputService;

    /// <summary>
    /// Флаг, указывающий на необходимость сброса модулей и системы после теста.
    /// </summary>
    private bool needReset = false;

    /// <summary>
    /// Асинхронная настройка UI, добавление полей, запуск ProtocolSelfCheckControl.
    /// </summary>
    public async Task InitializeSettingsAsync(IExecutionController executionController, IUserInteractionService userInteractionService, IMessageOutputService messageOutputService)
    {
      _controller = executionController;
      _userInteractionService = userInteractionService;
      _messageOutputService = messageOutputService;

      _controller.SetSettings(
        StartDelegate: ExecuteTestProcess,
        true,
        StopDelegate: Stop);
    }

    /// <summary>
    /// Выполняет основную логику теста: валидация, инициализация модулей,
    /// подготовка диапазона точек, выполнение трёх этапов перекрёстного теста.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task ExecuteTestProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      var data = await UIValidationHelper.EnsureValidMetrologyInputAsync(inputFieldProvider, _messageOutputService/*, timeCheck: true, timeRampCheck: true, voltageCheck: true, busCheck: true*/, pairBusCheck: true);
      var e = _messageService.GetLastLineNumber();
      //var (ok, message, tested, tester, range) = UIValidationHelperLightweight.TryValidateAndParseInput(_messageService, inputFieldProvider, inputHighlightService);
      //if (!ok)
      //{
      //  LogError($"Валидация не пройдена: {message}");
      //  return;
      //}
    }

    /// <summary>
    /// Принудительно останавливает выполнение теста CrossTestMKR:
    ///  • выключает измеритель;
    ///  • сбрасывает оба модуля;
    ///  • выполняет общий Reset всей системы.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task Stop(CancellationToken cancellationToken)
    {
      needReset = false;
    }
  }
}
