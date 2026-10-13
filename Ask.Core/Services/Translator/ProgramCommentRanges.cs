namespace Ask.Core.Services.Translator;

/// <summary>Comment offsets shared by editor coloring and command folding.</summary>
public static class ProgramCommentRanges
{
  public static List<(int start, int end)> Parse(string text)
  {
    var ranges = new List<(int start, int end)>();
    var stack = new Stack<char>();
    int start = -1;
    for (int i = 0; i < text.Length; i++)
    {
      char current = text[i];
      char next = i + 1 < text.Length ? text[i + 1] : '\0';
      if (current == '/' && next == '/')
      {
        int end = i + 2;
        while (end < text.Length && text[end] != '\r' && text[end] != '\n') end++;
        if (stack.Count == 0) ranges.Add((i, end));
        i = end - 1;
      }
      else if (current == '{' || (current == '/' && next == '*'))
      {
        if (stack.Count == 0) start = i;
        stack.Push(current);
        if (current == '/') i++;
      }
      else if (stack.Count > 0 &&
        ((current == '}' && stack.Peek() == '{') ||
         (current == '*' && next == '/' && stack.Peek() == '/')))
      {
        stack.Pop();
        if (current == '*') i++;
        if (stack.Count == 0) ranges.Add((start, i + 1));
      }
    }
    if (stack.Count > 0) ranges.Add((start, text.Length));
    return ranges;
  }

  public static string Mask(string text)
  {
    var characters = text.ToCharArray();
    foreach (var (start, end) in Parse(text))
      for (int i = start; i < end; i++)
        if (characters[i] != '\r' && characters[i] != '\n') characters[i] = ' ';
    return new string(characters);
  }
}
