using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  public class ArrowItem
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public Geometry GeometryData { get; set; }
  
  public override string ToString()
    {
      return Name;
    }
  }
}
