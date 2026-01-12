namespace ConsoleUI.ConsoleCommanding.Core
{
  public interface ITableFormatter
  {
    string FormatTable(string[] headers, List<string[]> rows);
  }
}
