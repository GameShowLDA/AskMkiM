namespace TestConsole
{
  public static class FolderTreePrinter
  {
    private static readonly string[] IgnoredDirectories =
    {
      "bin",
      "obj"
    };

    public static void Run()
    {
      Console.WriteLine("Введите путь к папке:");
      var rootPath = Console.ReadLine();

      if (string.IsNullOrWhiteSpace(rootPath))
      {
        Console.WriteLine("Путь не задан.");
        return;
      }

      if (!Directory.Exists(rootPath))
      {
        Console.WriteLine("Папка не существует.");
        return;
      }

      var rootDirectory = new DirectoryInfo(rootPath);
      var basePath = rootDirectory.Parent?.FullName;

      if (basePath is null)
      {
        Console.WriteLine("Невозможно определить родительскую папку.");
        return;
      }

      PrintDirectory(rootDirectory, basePath);

    }

    private static void PrintDirectory(DirectoryInfo directory, string rootPath)
    {
      if (IsIgnored(directory))
        return;

      FileInfo[] files;
      DirectoryInfo[] subDirectories;

      try
      {
        files = directory.GetFiles();
        subDirectories = directory.GetDirectories();
      }
      catch (UnauthorizedAccessException)
      {
        return;
      }

      foreach (var file in files)
      {
        var relativePath = Path.GetRelativePath(rootPath, file.FullName)
          .Replace('\\', '/');

        Console.WriteLine(relativePath);
      }

      foreach (var subDir in subDirectories)
      {
        PrintDirectory(subDir, rootPath);
      }
    }

    private static bool IsIgnored(DirectoryInfo directory)
    {
      foreach (var ignored in IgnoredDirectories)
      {
        if (string.Equals(directory.Name, ignored, StringComparison.OrdinalIgnoreCase))
          return true;
      }

      return false;
    }
  }
}
