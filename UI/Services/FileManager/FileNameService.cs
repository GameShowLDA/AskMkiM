using System.IO;

namespace UI.Services.FileManager
{
  /// <summary>
  /// Сервис для управления именами файлов в редакторе.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Контроль уникальности открытых файлов с одинаковыми именами.</item>
  ///   <item>Автоматическая генерация уникального имени при конфликте.</item>
  ///   <item>Формирование различий в путях для добавления контекста к имени файла.</item>
  /// </list>
  /// 
  /// Сервис используется при открытии или регистрации новых файлов, чтобы избежать конфликтов имен.
  /// </summary>
  public class FileNameService
  {
    private readonly EditorWorkspaceModel _context;

    /// <summary>
    /// Создаёт новый экземпляр сервиса управления именами файлов.
    /// </summary>
    /// <param name="editorWorkspaceModel">Контекст рабочего пространства редактора, содержащий информацию об открытых файлах.</param>
    public FileNameService(EditorWorkspaceModel editorWorkspaceModel)
    {
      _context = editorWorkspaceModel;
    }

    /// <summary>
    /// Регистрирует новый файл в системе и гарантирует уникальность его имени.
    /// 
    /// Если файл с таким именем уже существует, но путь отличается, к имени добавляется часть пути.
    /// </summary>
    /// <param name="path">Полный путь к новому файлу.</param>
    /// <param name="fileName">Исходное имя файла.</param>
    /// <returns>Уникальное имя файла, под которым он будет зарегистрирован в системе.</returns>
    internal string EnsureUniqueFileName(string path, string fileName)
    {
      if (!FileAlreadyRegistered(fileName))
      {
        RegisterFile(fileName, path);
        return fileName;
      }

      if (IsSameFileRegistered(fileName, path))
      {
        // Файл с таким именем и путем уже зарегистрирован — возвращаем его имя как есть
        return fileName;
      }

      // Разные пути — генерируем уникальное имя
      var uniqueName = GenerateUniqueFileName(fileName, path);
      RegisterFile(uniqueName, path);
      return uniqueName;
    }

    #region 🔧 Подметоды (SRP)

    /// <summary>
    /// Проверяет, зарегистрирован ли файл с указанным именем.
    /// </summary>
    private bool FileAlreadyRegistered(string fileName)
    {
      return _context.FilePaths.ContainsKey(fileName);
    }

    /// <summary>
    /// Проверяет, зарегистрирован ли файл с тем же именем и тем же путём.
    /// </summary>
    private bool IsSameFileRegistered(string fileName, string path)
    {
      return _context.FilePaths.TryGetValue(fileName, out var existingPath) &&
             string.Equals(existingPath, path, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Добавляет запись о новом файле в словарь <see cref="EditorWorkspaceModel.FilePaths"/>.
    /// </summary>
    private void RegisterFile(string fileName, string path)
    {
      _context.FilePaths[fileName] = path;
    }

    /// <summary>
    /// Генерирует уникальное имя файла на основе различий между существующим путём и новым.
    /// </summary>
    private string GenerateUniqueFileName(string fileName, string newPath)
    {
      var existingPath = _context.FilePaths[fileName];
      return BuildUniqueNameFromPaths(existingPath, newPath);
    }

    #endregion

    /// <summary>
    /// Формирует уникальное имя файла на основе различий между его путями.
    /// </summary>
    /// <param name="existingPath">Путь к уже открытому файлу с тем же именем.</param>
    /// <param name="newPath">Путь к открываемому файлу.</param>
    /// <returns>Строка с уникальным именем, включающая часть пути.</returns>
    public string BuildUniqueNameFromPaths(string existingPath, string newPath)
    {
      var existingParts = existingPath.Split(Path.DirectorySeparatorChar);
      var newParts = newPath.Split(Path.DirectorySeparatorChar);

      int commonLength = GetCommonPathLength(existingParts, newParts);

      // Минимум — последняя папка + файл
      int startIndex = Math.Max(0, newParts.Length - 2);

      // Если пути совпадают частично — берём часть после общего пути
      if (commonLength < newParts.Length - 1)
        startIndex = commonLength;

      return string.Join(Path.DirectorySeparatorChar.ToString(), newParts.Skip(startIndex));
    }

    /// <summary>
    /// Определяет длину общего префикса двух путей.
    /// </summary>
    private int GetCommonPathLength(string[] existingParts, string[] newParts)
    {
      int minLength = Math.Min(existingParts.Length, newParts.Length);
      int commonLength = 0;

      for (int i = 0; i < minLength; i++)
      {
        if (!string.Equals(existingParts[i], newParts[i], StringComparison.OrdinalIgnoreCase))
          break;
        commonLength++;
      }

      return commonLength;
    }
  }
}
