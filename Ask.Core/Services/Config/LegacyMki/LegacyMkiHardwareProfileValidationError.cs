namespace Ask.Core.Services.Config.LegacyMki;

/// <summary>
/// Описывает одну ошибку в legacy-конфигурации аппаратуры АСК.
/// </summary>
/// <param name="Path">Путь к параметру конфигурации.</param>
/// <param name="Message">Понятное пользователю описание ошибки.</param>
public sealed record LegacyMkiHardwareProfileValidationError(string Path, string Message);
