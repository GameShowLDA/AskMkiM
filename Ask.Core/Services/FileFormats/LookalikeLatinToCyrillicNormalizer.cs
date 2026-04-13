using System.Text;

namespace Ask.Core.Services.FileFormats
{
  /// <summary>
  /// Выполняет замену визуально похожих латинских символов на кириллические аналоги.
  /// </summary>
  public sealed class LookalikeLatinToCyrillicNormalizer
  {
    /// <summary>
    /// Содержит таблицу соответствия латинских символов кириллическим аналогам.
    /// </summary>
    private static readonly IReadOnlyDictionary<char, char> CharacterMap = new Dictionary<char, char>
    {
      ['A'] = 'А',
      ['a'] = 'а',
      ['B'] = 'В',
      ['C'] = 'С',
      ['c'] = 'с',
      ['E'] = 'Е',
      ['e'] = 'е',
      ['H'] = 'Н',
      ['K'] = 'К',
      ['k'] = 'к',
      ['M'] = 'М',
      ['m'] = 'м',
      ['O'] = 'О',
      ['o'] = 'о',
      ['P'] = 'Р',
      ['p'] = 'р',
      ['T'] = 'Т',
      ['X'] = 'Х',
      ['x'] = 'х',
      ['Y'] = 'У',
      ['y'] = 'у',
    };

    /// <summary>
    /// Хранит кодировку, используемую для преобразования байтов в текст и обратно.
    /// </summary>
    private readonly Encoding _encoding;

    /// <summary>
    /// Инициализирует новый экземпляр нормализатора похожих латинских символов.
    /// </summary>
    /// <param name="encoding">Кодировка, используемая для преобразования байтовых данных.</param>
    public LookalikeLatinToCyrillicNormalizer(Encoding encoding)
    {
      _encoding = encoding;
    }

    /// <summary>
    /// Нормализует байтовое представление строки, заменяя похожие латинские символы на кириллицу.
    /// </summary>
    /// <param name="bytes">Байтовое представление исходной строки.</param>
    /// <returns>Нормализованное байтовое представление строки.</returns>
    public byte[] Normalize(byte[] bytes)
    {
      if (bytes.Length == 0)
      {
        return bytes;
      }

      var text = _encoding.GetString(bytes);
      var normalizedText = Normalize(text);

      return ReferenceEquals(text, normalizedText)
        ? bytes
        : _encoding.GetBytes(normalizedText);
    }

    /// <summary>
    /// Нормализует строку, заменяя похожие латинские символы на кириллицу.
    /// </summary>
    /// <param name="text">Исходная строка.</param>
    /// <returns>Нормализованная строка.</returns>
    public string Normalize(string text)
    {
      if (string.IsNullOrEmpty(text))
      {
        return text;
      }

      StringBuilder? builder = null;

      for (var index = 0; index < text.Length; index++)
      {
        var sourceCharacter = text[index];
        if (!CharacterMap.TryGetValue(sourceCharacter, out var targetCharacter))
        {
          builder?.Append(sourceCharacter);
          continue;
        }

        builder ??= new StringBuilder(text.Length).Append(text, 0, index);
        builder.Append(targetCharacter);
      }

      return builder?.ToString() ?? text;
    }
  }
}
