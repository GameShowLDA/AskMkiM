namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected
{
  public class UsbConnectedProfile : ConnectedBaseProfile
  {
    public string LastResolvedDevicePath { get; set; } = string.Empty;

    public string VisaResourcePattern { get; set; } = "USB?*INSTR";

    public int OpenRetryCount { get; set; } = 3;

    public int OpenRetryDelayMs { get; set; } = 150;

    public int ReadBufferSize { get; set; } = 4096;

    public bool SendEndEnabled { get; set; } = true;

    public byte TerminationCharacter { get; set; } = (byte)'\n';

    public bool TerminationCharacterEnabled { get; set; } = true;

    public bool AppendLineEnding { get; set; } = true;

    public bool UseViewPower { get; set; }
  }
}
