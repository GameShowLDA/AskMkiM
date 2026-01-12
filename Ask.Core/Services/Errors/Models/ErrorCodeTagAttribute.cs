namespace Ask.Core.Services.Errors.Models
{
  [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
  public sealed class ErrorCodeTagAttribute : Attribute
  {
    public string Tag { get; }
    public ErrorCodeTagAttribute(string tag) => Tag = tag;
  }
}
