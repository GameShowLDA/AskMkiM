namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected
{
  public class ComConnectedProfile : ConnectedBaseProfile
  {
    public string Reset { get; set; } = "*RST";
    public string Clear { get; set; } = "*CLS";
  }
}
