using System.IO;
using System.Text;
using System.Text.Unicode;
using Ude;

namespace Utilities.Services
{
  /// <summary>
  /// Предоставляет методы для автоматического определения кодировки текстовых файлов.
  /// </summary>
  /// <remarks>
  /// Класс использует библиотеку <see href="https://github.com/errepi/ude">Ude (Universal Detector)</see> —
  /// порт детектора кодировок Mozilla Universal Charset Detector для .NET.
  /// Подходит для определения кодировки текстовых файлов, полученных из неизвестных источников.
  /// 
  /// Если определить кодировку не удалось или она не поддерживается в среде выполнения,
  /// возвращается кодировка по умолчанию — <see cref="Encoding.UTF8"/>.
  /// </remarks>
  public static class EncodingService
  {
    /// <summary>
    /// Определяет кодировку указанного текстового файла.
    /// </summary>
    /// <param name="filePath">Полный путь к файлу, кодировку которого необходимо определить.</param>
    /// <returns>
    /// Объект <see cref="Encoding"/>, соответствующий определённой кодировке файла.
    /// Если кодировка не может быть определена или не поддерживается, возвращается <see cref="Encoding.UTF8"/>.
    /// </returns>
    /// <exception cref="FileNotFoundException">Выбрасывается, если файл по указанному пути не существует.</exception>
    /// <exception cref="UnauthorizedAccessException">Выбрасывается, если нет прав на чтение файла.</exception>
    public static Encoding DetectEncodingFromFile(string filePath)
    {
      using var fileStream = File.OpenRead(filePath);
      return DetectEncodingFromText(fileStream);
    }

    /// <summary>
    /// Определяет кодировку текста на основе содержимого потока файла.
    /// </summary>
    /// <param name="fileStream">Поток <see cref="FileStream"/>, содержащий текстовые данные.</param>
    /// <returns>
    /// Объект <see cref="Encoding"/>, соответствующий определённой кодировке текста.
    /// Если кодировка не может быть определена или не поддерживается, возвращается <see cref="Encoding.UTF8"/>.
    /// </returns>
    /// <remarks>
    /// Метод не изменяет позицию потока после завершения работы.
    /// Перед вызовом рекомендуется убедиться, что поток поддерживает чтение.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="fileStream"/> равен <c>null</c>.</exception>
    public static Encoding DetectEncodingFromText(FileStream fileStream)
    {
      if (fileStream == null)
        throw new ArgumentNullException(nameof(fileStream));

      var detector = new CharsetDetector();
      detector.Feed(fileStream);
      detector.DataEnd();

      if (detector.Charset != null)
      {
        try
        {
          var enc = Encoding.GetEncoding(detector.Charset);
          return enc; 
        }
        catch
        {
          return Encoding.GetEncoding(866);
        }
      }

      return Encoding.UTF8;
    }
  }
}
