using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;

namespace Ask.Core.Shared.Metadata.Atributes
{
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public sealed class MetrologyModeAttribute : Attribute
  {
    public MetrologyType Type { get; }
    public string Title { get; }

    public MetrologyModeAttribute(MetrologyType type, string title)
    {
      Type = type;
      Title = title;
    }
  }
}
