using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Engine.UnitTests.ProtocolMessages;

public sealed class SelfTestMessagesCompatibilityTests
{
  [Fact]
  public async Task PublishCommandAsync_LegacySignature_RemainsCallable()
  {
    Task publication = SelfTestMessages.PublishCommandAsync(
      "Проверка",
      null,
      "Описание",
      1,
      true,
      nameof(PublishCommandAsync_LegacySignature_RemainsCallable),
      "compatibility-test.cs",
      1);

    await publication;
  }

  [Fact]
  public void PublishCommandAsync_LegacyBinarySignature_Exists()
  {
    Type[] parameterTypes =
    {
      typeof(string),
      typeof(IMessageOutputService),
      typeof(string),
      typeof(int),
      typeof(bool),
      typeof(string),
      typeof(string),
      typeof(int)
    };

    Assert.NotNull(typeof(SelfTestMessages).GetMethod(
      nameof(SelfTestMessages.PublishCommandAsync),
      parameterTypes));
  }

  [Fact]
  public async Task MultimeterSelfTestCommand_CurrentPath_RemainsCallable()
  {
    Task publication = MultimeterMessages.PublishSelfTestCommandAsync(
      "Проверка мультиметра",
      null!,
      onlyWhenStepMode: true);

    await publication;
  }
}
