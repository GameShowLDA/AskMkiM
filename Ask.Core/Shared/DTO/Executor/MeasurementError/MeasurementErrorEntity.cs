using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Core.Shared.DTO.Executor.MeasurementError
{
  /// <summary>
  /// Тип команды (режим метрологии) и связанные с ним диапазоны погрешностей.
  /// </summary>
  public class MeasurementErrorEntity
  {
    /// <summary>Первичный ключ.</summary>
    public int Id { get; set; }

    /// <summary>Тип команды, для которой задаются погрешности.</summary>
    public MeasurementTypeCommand Type { get; set; }

    /// <summary>Коллекция диапазонов погрешностей (один-ко-многим).</summary>
    public List<MeasurementErrorRangeEntity> Ranges { get; set; } = new();

    /// <summary>Пустой конструктор для EF.</summary>
    public MeasurementErrorEntity() { }

    /// <summary>Удобный конструктор с указанием типа команды.</summary>
    public MeasurementErrorEntity(MeasurementTypeCommand type) => Type = type;
  }
}
