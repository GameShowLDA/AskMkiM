using Ask.Core.Shared.DTO.Executor.MeasurementError;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBaseConfiguration.Configurations.Measurement
{
  /// <summary>Конфигурация таблицы MeasurementErrorRanges.</summary>
  public class MeasurementErrorRangeEntityConfiguration : IEntityTypeConfiguration<MeasurementErrorRangeEntity>
  {
    public void Configure(EntityTypeBuilder<MeasurementErrorRangeEntity> builder)
    {
      builder.HasKey(x => x.Id);

      builder.Property(x => x.MinValue).IsRequired();
      builder.Property(x => x.NumericError).IsRequired();
      builder.Property(x => x.PercentageError).IsRequired();

      // Уникальность пары (EntityId, Min, Max) — чтобы не было дублей одного и того же диапазона.
      builder.HasIndex(x => new { x.MeasurementErrorEntityId, x.MinValue, x.MaxValue })
             .IsUnique();
    }
  }
}
