namespace AskMkiM.Updater.Core.Models;

/// <summary>
/// Описывает один программный файл в пакете обновления.
/// </summary>
public sealed record UpdateFileEntry(string Path, long Length, string Sha256);
