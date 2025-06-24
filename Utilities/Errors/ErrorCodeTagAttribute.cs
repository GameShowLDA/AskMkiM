using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Errors
{
  [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
  public sealed class ErrorCodeTagAttribute : Attribute
  {
    public string Tag { get; }
    public ErrorCodeTagAttribute(string tag) => Tag = tag;
  }
}
