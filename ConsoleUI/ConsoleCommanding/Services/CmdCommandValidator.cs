namespace ConsoleUI.ConsoleCommanding.Services
{
  public class CmdCommandValidator
  {
    public bool IsValid(string input)
    {
      return !string.IsNullOrWhiteSpace(input);
    }
  }
}
