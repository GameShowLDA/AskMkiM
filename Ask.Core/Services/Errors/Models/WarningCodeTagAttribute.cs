namespace Ask.Core.Services.Errors.Models
{
  [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
  public sealed class WarningCodeTagAttribute : Attribute
  {
    public string Tag { get; }
    public WarningCodeTagAttribute(string tag) => Tag = tag;
  }
}
