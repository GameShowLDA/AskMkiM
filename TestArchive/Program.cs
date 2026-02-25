using System;
using System.Windows;

namespace TestArchive
{
  internal static class Program
  {
    [STAThread]
    private static void Main()
    {
      var app = new Application();
      app.Run(new ArchiveExplorerWindow());
    }
  }
}
