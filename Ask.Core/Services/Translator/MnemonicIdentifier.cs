using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Core.Services.Translator
{
  /// <summary>
  /// Универсальный идентификатор мнемоники, поддерживающий как строковые,
  /// так и типизированные команды (MeasurementTypeCommand, OrganizationalComands и др.).
  /// </summary>
  public readonly struct MnemonicIdentifier
  {
    /// <summary>
    /// Строковое значение мнемоники.
    /// </summary>
    public string Mnemonic { get; }

    /// <summary>
    /// Общий enum-объект (если команда представлена перечислением).
    /// </summary>
    public Enum? EnumValue { get; }

    /// <summary>
    /// Конструктор для строковой мнемоники.
    /// </summary>
    public MnemonicIdentifier(string mnemonic)
    {
      Mnemonic = mnemonic;
      EnumValue = null;
    }

    public static implicit operator MnemonicIdentifier(string mnemonic)
      => new MnemonicIdentifier(mnemonic);

    /// <summary>
    /// Конструктор для команд измерений (MeasurementTypeCommand).
    /// </summary>
    public MnemonicIdentifier(MeasurementTypeCommand command)
    {
      EnumValue = command;
      Mnemonic = command.ToString();
    }

    /// <summary>
    /// Конструктор для организационных команд (OrganizationalComands).
    /// </summary>
    public MnemonicIdentifier(OrganizationalComands command)
    {
      EnumValue = command;
      Mnemonic = command.ToString();
    }

    /// <summary>
    /// Возвращает строковое представление мнемоники.
    /// </summary>
    public override string ToString() => Mnemonic ?? string.Empty;

    /// <summary>
    /// Проверяет, является ли команда типом MeasurementTypeCommand.
    /// </summary>
    public bool IsMeasurementTypeCommand => EnumValue is MeasurementTypeCommand;

    /// <summary>
    /// Проверяет, является ли команда типом OrganizationalComands.
    /// </summary>
    public bool IsOrganizationalCommand => EnumValue is OrganizationalComands;

    /// <summary>
    /// Возвращает MeasurementTypeCommand, если доступен; иначе — null.
    /// </summary>
    public MeasurementTypeCommand? AsMeasurementTypeCommand =>
        EnumValue as MeasurementTypeCommand?;

    /// <summary>
    /// Возвращает OrganizationalComands, если доступен; иначе — null.
    /// </summary>
    public OrganizationalComands? AsOrganizationalCommand =>
        EnumValue as OrganizationalComands?;
  }
}
