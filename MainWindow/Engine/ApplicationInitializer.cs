using Ask.Core.Services.EventCore.Services;
using static Ask.Core.Services.EventCore.Events.Message;

namespace MainWindowProgram.Engine
{
  internal class ApplicationInitializer
  {
    private readonly MessageHandler messageHandler;

    /// <summary>
    /// Конструктор инициализатора приложения.
    /// </summary>
    /// <param name="messageHandler">Обработчик сообщений, используемый для отображения ошибок, предупреждений и информации.</param>
    public ApplicationInitializer(MessageHandler messageHandler)
    {
      this.messageHandler = messageHandler;
    }

    /// <summary>
    /// Запускает полную процедуру инициализации приложения.
    /// </summary>
    public void SubscribeToMessageEvents()
    {
      EventAggregator.Subscribe<Error>(e =>
         messageHandler.SetErrorMessage(e.Text, e.ClearPrevious));

      EventAggregator.Subscribe<Warning>(e =>
        messageHandler.SetWarningMessage(e.Text, e.ClearPrevious));

      EventAggregator.Subscribe<Info>(e =>
        messageHandler.SetInfoMessage(e.Text, e.ClearPrevious));

      EventAggregator.Subscribe<Clear>(_ =>
        messageHandler.ClearMessage());
    }
  }
}
