using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
