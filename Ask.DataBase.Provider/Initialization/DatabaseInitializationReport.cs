namespace Ask.DataBase.Provider.Initialization;

/// <summary>
/// Отчёт о выполнении инициализации базы данных.
/// Содержит итоговую информацию о пути к базе, пересоздании файла,
/// применённых миграциях и созданных данных по умолчанию.
/// </summary>
public sealed class DatabaseInitializationReport
{
  /// <summary>
  /// Полный путь к используемому файлу базы данных.
  /// </summary>
  public string DatabasePath { get; internal set; } = string.Empty;

  /// <summary>
  /// Признак того, что файл базы данных существовал до запуска инициализации.
  /// </summary>
  public bool DatabaseAlreadyExisted { get; internal set; }

  /// <summary>
  /// Признак того, что в ходе инициализации был создан новый файл базы данных.
  /// </summary>
  public bool DatabaseCreated { get; internal set; }

  /// <summary>
  /// Признак того, что повреждённая база была перемещена и создана заново.
  /// </summary>
  public bool DatabaseRecreated { get; internal set; }

  /// <summary>
  /// Количество применённых миграций EF Core.
  /// </summary>
  public int AppliedMigrations { get; internal set; }

  /// <summary>
  /// Признак того, что схема была создана через <c>EnsureCreated</c>.
  /// </summary>
  public bool UsedEnsureCreated { get; internal set; }

  /// <summary>
  /// Количество добавленных горячих клавиш по умолчанию.
  /// </summary>
  public int SeededHotkeys { get; internal set; }

  /// <summary>
  /// Количество созданных строк настроек по умолчанию.
  /// </summary>
  public int CreatedDefaultSettingsRows { get; internal set; }

  /// <summary>
  /// Список всех сообщений, записанных во время инициализации.
  /// </summary>
  public List<string> Messages { get; } = new();
}
