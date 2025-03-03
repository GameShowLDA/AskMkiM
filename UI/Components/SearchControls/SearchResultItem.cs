using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Components.SearchControls
{
  public class SearchResultItem
  {
    public string FileName { get; set; }
    public int LineNumber { get; set; }
    public string LineText { get; set; }

    public override string ToString()
    {
      return $"{FileName} - {LineNumber}: {LineText}";
    }
  }
}
