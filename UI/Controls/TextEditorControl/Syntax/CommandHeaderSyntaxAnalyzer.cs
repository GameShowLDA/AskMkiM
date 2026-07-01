using System;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Controls.TextEditorControl.Syntax
{
  public sealed class CommandHeaderSyntaxAnalyzer
  {
    private static readonly Regex ValidHeaderRegex = new(
      @"^\s*(?<number>\d+)\s+(?<mnemonic>[А-Яа-яA-Za-z0-9]+)\b",
      RegexOptions.Compiled);

    private static readonly Regex NoSpaceBetweenNumberAndMnemonicRegex = new(
      @"^\s*(?<number>\d+)(?<mnemonic>[А-Яа-яA-Za-z]+)\b",
      RegexOptions.Compiled);

    private static readonly Regex NumberOnlyRegex = new(
      @"^\s*(?<number>\d+)\s*$",
      RegexOptions.Compiled);

    private static readonly Regex StartsWithMnemonicRegex = new(
      @"^\s*(?<mnemonic>[А-Яа-яA-Za-z]+)\b",
      RegexOptions.Compiled);

    private readonly HashSet<string> _knownMnemonics;

    public CommandHeaderSyntaxAnalyzer(IEnumerable<string> knownMnemonics)
    {
      _knownMnemonics = knownMnemonics
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<TextSyntaxDiagnostic> AnalyzeLine(
      DocumentLine line,
      string lineText,
      bool hasPreviousCommand,
      out CommandHeaderInfo? header)
    {
      header = null;

      var diagnostics = new List<TextSyntaxDiagnostic>();

      if (string.IsNullOrWhiteSpace(lineText))
        return diagnostics;

      // Если строка не начинается с команды, но до неё уже была команда,
      // пока считаем её строкой-продолжением.
      // Это важно для многострочных команд, например ЦУ.
      if (!char.IsDigit(lineText.TrimStart()[0]) && hasPreviousCommand)
        return diagnostics;

      // 12ЦУ Текст
      var noSpaceMatch = NoSpaceBetweenNumberAndMnemonicRegex.Match(lineText);
      if (noSpaceMatch.Success)
      {
        diagnostics.Add(new TextSyntaxDiagnostic
        {
          Code = "CMD001",
          Severity = TextSyntaxSeverity.Error,
          Message = "Между номером команды и мнемоникой должен быть пробел.",
          StartOffset = line.Offset + noSpaceMatch.Groups["number"].Index,
          Length = noSpaceMatch.Groups["number"].Length + noSpaceMatch.Groups["mnemonic"].Length,
          LineNumber = line.LineNumber,
          ColumnNumber = noSpaceMatch.Groups["number"].Index + 1
        });

        return diagnostics;
      }

      // 12
      var numberOnlyMatch = NumberOnlyRegex.Match(lineText);
      if (numberOnlyMatch.Success)
      {
        diagnostics.Add(new TextSyntaxDiagnostic
        {
          Code = "CMD002",
          Severity = TextSyntaxSeverity.Error,
          Message = "После номера команды должна быть указана мнемоника.",
          StartOffset = line.Offset + numberOnlyMatch.Groups["number"].Index,
          Length = numberOnlyMatch.Groups["number"].Length,
          LineNumber = line.LineNumber,
          ColumnNumber = numberOnlyMatch.Groups["number"].Index + 1
        });

        return diagnostics;
      }

      // ЦУ Текст
      if (!char.IsDigit(lineText.TrimStart()[0]))
      {
        var mnemonicMatch = StartsWithMnemonicRegex.Match(lineText);

        diagnostics.Add(new TextSyntaxDiagnostic
        {
          Code = "CMD003",
          Severity = TextSyntaxSeverity.Error,
          Message = mnemonicMatch.Success
            ? "Команда должна начинаться с номера."
            : "Строка не является корректной командой.",
          StartOffset = line.Offset,
          Length = Math.Max(1, lineText.Length),
          LineNumber = line.LineNumber,
          ColumnNumber = 1
        });

        return diagnostics;
      }

      var headerMatch = ValidHeaderRegex.Match(lineText);

      if (!headerMatch.Success)
      {
        diagnostics.Add(new TextSyntaxDiagnostic
        {
          Code = "CMD004",
          Severity = TextSyntaxSeverity.Error,
          Message = "Неверный заголовок команды. Ожидается формат: <номер> <мнемоника>.",
          StartOffset = line.Offset,
          Length = Math.Max(1, lineText.Length),
          LineNumber = line.LineNumber,
          ColumnNumber = 1
        });

        return diagnostics;
      }

      string number = headerMatch.Groups["number"].Value;
      string mnemonic = headerMatch.Groups["mnemonic"].Value;

      header = new CommandHeaderInfo
      {
        CommandNumber = number,
        Mnemonic = mnemonic,
        LineNumber = line.LineNumber,
        LineOffset = line.Offset,
        NumberStartOffset = line.Offset + headerMatch.Groups["number"].Index,
        MnemonicStartOffset = line.Offset + headerMatch.Groups["mnemonic"].Index,
        MnemonicLength = mnemonic.Length,
        SourceLine = lineText
      };

      AnalyzeMnemonic(header, diagnostics);

      return diagnostics;
    }

    private void AnalyzeMnemonic(
      CommandHeaderInfo header,
      List<TextSyntaxDiagnostic> diagnostics)
    {
      string mnemonic = header.Mnemonic;

      if (ContainsMixedLatinAndCyrillic(mnemonic))
      {
        diagnostics.Add(new TextSyntaxDiagnostic
        {
          Code = "CMD005",
          Severity = TextSyntaxSeverity.Warning,
          Message = "В мнемонике смешаны латинские и кириллические символы.",
          StartOffset = header.MnemonicStartOffset,
          Length = header.MnemonicLength,
          LineNumber = header.LineNumber,
          ColumnNumber = header.MnemonicStartOffset - header.LineOffset + 1
        });
      }

      if (_knownMnemonics.Contains(mnemonic))
        return;

      var possibleMnemonics = _knownMnemonics
        .Where(x => x.StartsWith(mnemonic, StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => x)
        .Take(5)
        .ToList();

      string message = possibleMnemonics.Count > 0
        ? $"Неполное имя команды: {mnemonic}. Возможные варианты: {string.Join(", ", possibleMnemonics)}."
        : $"Неизвестная команда: {mnemonic}.";

      diagnostics.Add(new TextSyntaxDiagnostic
      {
        Code = "CMD006",
        Severity = TextSyntaxSeverity.Error,
        Message = message,
        StartOffset = header.MnemonicStartOffset,
        Length = header.MnemonicLength,
        LineNumber = header.LineNumber,
        ColumnNumber = header.MnemonicStartOffset - header.LineOffset + 1
      });
    }

    private static bool ContainsMixedLatinAndCyrillic(string text)
    {
      bool hasLatin = false;
      bool hasCyrillic = false;

      foreach (char ch in text)
      {
        if (IsLatin(ch))
          hasLatin = true;

        if (IsCyrillic(ch))
          hasCyrillic = true;
      }

      return hasLatin && hasCyrillic;
    }

    private static bool IsLatin(char ch)
    {
      return ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static bool IsCyrillic(char ch)
    {
      return ch is >= 'А' and <= 'я' or 'Ё' or 'ё';
    }
  }
}
