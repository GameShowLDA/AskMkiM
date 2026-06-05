namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  public interface IHasUnparsedParameters
  {
    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    string? UnparsedParameters { get; set; }
  }
}
