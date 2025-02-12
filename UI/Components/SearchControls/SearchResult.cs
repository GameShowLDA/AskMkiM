using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Components.SearchControls
{
  public class SearchResult
  {
    public int StartOffset { get; }
    public int Length { get; }

    public SearchResult(int startOffset, int length)
    {
      StartOffset = startOffset;
      Length = length;
    }
  }
}
