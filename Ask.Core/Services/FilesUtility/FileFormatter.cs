using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using System.Text.RegularExpressions;

namespace Ask.Core.Services.FilesUtility
{
  public class FileFormatter
  {
    public static readonly Regex CommandHeaderRegex = new(@"^\s*\d+\s+\S+", RegexOptions.Compiled);

    public static string NormalizeProgramWhitespace(string text)
    {
      if (string.IsNullOrEmpty(text))
        return string.Empty;

      var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
      var formattedLines = new List<string>(lines.Length);
      string? blockCommentIndent = null;
      string? blockCommentCloseToken = null;
      bool hasReachedFirstCommand = false;
      bool hasReachedEndCommand = false;

      for (int i = 0; i < lines.Length; i++)
      {
        string rawLine = lines[i];
        string line = rawLine.TrimEnd(' ', '\t');
        if (!hasReachedFirstCommand)
        {
          if (CommandHeaderRegex.IsMatch(line))
          {
            hasReachedFirstCommand = true;
          }
          else
          {
            formattedLines.Add(rawLine);
            continue;
          }
        }
        else if (hasReachedEndCommand)
        {
          formattedLines.Add(rawLine);
          continue;
        }

        if (string.IsNullOrWhiteSpace(line))
        {
          formattedLines.Add(string.Empty);
          continue;
        }

        string trimmedLine = line.TrimStart(' ', '\t');

        if (blockCommentIndent != null)
        {
          if (trimmedLine.StartsWith(blockCommentCloseToken, StringComparison.Ordinal))
          {
            formattedLines.Add(blockCommentIndent + blockCommentCloseToken);
            blockCommentIndent = null;
            blockCommentCloseToken = null;
          }
          else
          {
            formattedLines.Add(blockCommentIndent + " " + trimmedLine);
          }

          continue;
        }

        string originalIndent = GetLeadingWhitespace(line);

        if (trimmedLine.StartsWith("{", StringComparison.Ordinal))
        {
          blockCommentIndent = originalIndent;
          blockCommentCloseToken = "}";
          formattedLines.Add(blockCommentIndent + "{");

          string commentBody = trimmedLine[1..].TrimStart(' ', '\t');
          if (!string.IsNullOrEmpty(commentBody))
          {
            if (commentBody == "}")
            {
              formattedLines.Add(blockCommentIndent + "}");
              blockCommentIndent = null;
              blockCommentCloseToken = null;
            }
            else if (commentBody.EndsWith("}", StringComparison.Ordinal))
            {
              string inlineBody = commentBody[..^1].TrimEnd(' ', '\t');
              if (!string.IsNullOrEmpty(inlineBody))
              {
                formattedLines.Add(blockCommentIndent + " " + inlineBody);
              }

              formattedLines.Add(blockCommentIndent + "}");
              blockCommentIndent = null;
              blockCommentCloseToken = null;
            }
            else
            {
              formattedLines.Add(blockCommentIndent + " " + commentBody);
            }
          }

          continue;
        }

        if (trimmedLine.StartsWith("/*", StringComparison.Ordinal))
        {
          blockCommentIndent = originalIndent;
          blockCommentCloseToken = "*/";
          formattedLines.Add(blockCommentIndent + "/*");

          string commentBody = trimmedLine[2..].TrimStart(' ', '\t');
          if (!string.IsNullOrEmpty(commentBody))
          {
            if (commentBody == "*/")
            {
              formattedLines.Add(blockCommentIndent + "*/");
              blockCommentIndent = null;
              blockCommentCloseToken = null;
            }
            else if (commentBody.EndsWith("*/", StringComparison.Ordinal))
            {
              string inlineBody = commentBody[..^2].TrimEnd(' ', '\t');
              if (!string.IsNullOrEmpty(inlineBody))
              {
                formattedLines.Add(blockCommentIndent + " " + inlineBody);
              }

              formattedLines.Add(blockCommentIndent + "*/");
              blockCommentIndent = null;
              blockCommentCloseToken = null;
            }
            else
            {
              formattedLines.Add(blockCommentIndent + " " + commentBody);
            }
          }

          continue;
        }

        if (CommandHeaderRegex.IsMatch(line))
        {
          formattedLines.Add(NormalizeCommandHeader(line));
        }
        else
        {
          formattedLines.Add("\t" + trimmedLine);
        }

        if (IsEndCommandLine(line))
        {
          hasReachedEndCommand = true;
        }
      }

      return string.Join(Environment.NewLine, formattedLines);
    }

    private static string NormalizeCommandHeader(string line)
    {
      string trimmedLine = line.TrimStart(' ', '\t');
      var match = Regex.Match(trimmedLine, @"^(\d+)\s+(\S+)(.*)$");
      if (!match.Success)
        return trimmedLine;

      string tail = match.Groups[3].Value;
      return $"{match.Groups[1].Value} {match.Groups[2].Value}{tail}";
    }

    private static bool IsEndCommandLine(string line)
    {
      string trimmedLine = line.TrimStart(' ', '\t');
      var match = Regex.Match(trimmedLine, @"^(\d+)\s+(\S+)(.*)$");
      return match.Success && string.Equals(match.Groups[2].Value, "КЦ", StringComparison.OrdinalIgnoreCase);
    }

    

    public static string GetLeadingWhitespace(string text)
    {
      int i = 0;
      while (i < text.Length && char.IsWhiteSpace(text[i]))
      {
        i++;
      }

      return i == 0 ? string.Empty : text[..i];
    }
  }
}
