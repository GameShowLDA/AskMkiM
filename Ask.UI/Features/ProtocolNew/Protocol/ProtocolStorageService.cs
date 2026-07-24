using Ask.Core.Services.Protocols;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using System.IO;

namespace Ask.UI.Features.ProtocolNew.Protocol
{
  /// <summary>
  /// Хранит состояние протоколов текущего выполнения и делегирует их сохранение
  /// универсальному механизму истории протоколов.
  /// </summary>
  internal sealed class ProtocolStorageService
  {
    /// <summary>Путь к последнему сохранённому протоколу выполнения.</summary>
    private string? _lastExecutionProtocolPath;

    /// <summary>Путь к последнему сохранённому итоговому протоколу.</summary>
    private string? _lastInspectionProtocolPath;

    /// <summary>Текст текущего итогового протокола.</summary>
    public string InspectionProtocolText { get; private set; } = string.Empty;

    /// <summary>
    /// Заменяет хранимый текст итогового протокола.
    /// </summary>
    /// <param name="protocolText">Новый текст итогового протокола.</param>
    public void SetInspectionProtocol(string? protocolText)
    {
      InspectionProtocolText = protocolText ?? string.Empty;
    }

    /// <summary>Очищает текст итогового протокола перед новым выполнением.</summary>
    public void ClearInspectionProtocol()
    {
      InspectionProtocolText = string.Empty;
      _lastInspectionProtocolPath = null;
    }

    /// <summary>
    /// Сохраняет снимок записей протокола выполнения в формате ASKTRACE.
    /// </summary>
    /// <param name="name">Имя сохраняемого протокола.</param>
    /// <param name="messages">Снимок записей протокола выполнения.</param>
    public async Task SaveExecutionProtocolAsync(
      string name,
      IReadOnlyList<ShowMessageModel> messages)
    {
      _lastExecutionProtocolPath = await ExecutionProtocolHistoryService.SaveAsync(name, messages);
    }

    /// <summary>
    /// Сохраняет текущий итоговый протокол рядом с последним файлом ASKTRACE.
    /// </summary>
    /// <param name="name">Имя сохраняемого протокола.</param>
    /// <param name="checkType">Тип завершённой проверки.</param>
    public async Task SaveInspectionProtocolAsync(string name, CheckType checkType)
    {
      if (string.IsNullOrWhiteSpace(InspectionProtocolText))
      {
        return;
      }

      _lastInspectionProtocolPath = await ExecutionProtocolHistoryService.SaveInspectionAsync(
        name,
        InspectionProtocolText,
        checkType,
        _lastExecutionProtocolPath);
    }

    /// <summary>
    /// Возвращает последний сохранённый в текущей сессии ASKTRACE-файл,
    /// а при его отсутствии — последний протокол из каталога History.
    /// </summary>
    /// <returns>Абсолютный путь к протоколу либо <see langword="null"/>.</returns>
    public string? ResolveLatestExecutionProtocolPath()
    {
      if (!string.IsNullOrWhiteSpace(_lastExecutionProtocolPath)
          && File.Exists(_lastExecutionProtocolPath))
      {
        return Path.GetFullPath(_lastExecutionProtocolPath);
      }

      return ExecutionProtocolHistoryService.ResolveLatestProtocolPath();
    }

    /// <summary>
    /// Возвращает путь к последнему сохранённому итоговому протоколу текущего выполнения.
    /// </summary>
    /// <returns>Абсолютный путь к итоговому протоколу либо <see langword="null"/>.</returns>
    public string? ResolveLatestInspectionProtocolPath()
    {
      return !string.IsNullOrWhiteSpace(_lastInspectionProtocolPath)
             && File.Exists(_lastInspectionProtocolPath)
        ? Path.GetFullPath(_lastInspectionProtocolPath)
        : null;
    }

    /// <summary>Возвращает абсолютный путь к общему каталогу истории протоколов.</summary>
    public string GetHistoryDirectory()
    {
      return ExecutionProtocolHistoryService.GetHistoryDirectory();
    }
  }
}
