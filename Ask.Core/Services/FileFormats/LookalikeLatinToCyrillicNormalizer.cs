using System.Text;

namespace Ask.Core.Services.FileFormats
{
  public sealed class LookalikeLatinToCyrillicNormalizer
  {
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

    private readonly Encoding _encoding;

    public LookalikeLatinToCyrillicNormalizer(Encoding encoding)
    {
      _encoding = encoding;
    }

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
