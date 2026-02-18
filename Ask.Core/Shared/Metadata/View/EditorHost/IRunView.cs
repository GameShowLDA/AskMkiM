using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  public interface IRunView
  {
    public string FileName { get; }

    string OpkFilePath { get; set; }
    public UserControl View { get; }
  }
}
