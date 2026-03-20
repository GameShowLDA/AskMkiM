namespace Ask.Core.Shared.Metadata.Static
{
  /// <summary>
  /// Класс, содержащий пути к файлам.
  /// </summary>
  static public class FileLocations
  {
    /// <summary>
    /// Путь к файлу настроек протокола.
    /// </summary>
    static public string ProtocolConfigPath => ".\\Settings\\_protocol_config.yaml";
    static public string ParameterConfigPath => ".\\Settings\\_parameter_config.yaml";

    /// <summary>
    /// Директория для сохранения данных.
    /// </summary>
    static public string DataSaveDirectory => ".\\History";
    static public string BackupDirectory => ".\\Backup";
    static public string ArchiveDirectory => "Archives";

    /// <summary>
    /// Директория для сохранения текста из консоли.
    /// </summary>
    static public string ConsoleSaveDirectory => ".\\Saves\\Console";

    /// <summary>
    /// Путь к YAML-файлу для настроек холостого режима.
    /// </summary>
    static public string ExecutionConfigPath => ".\\Settings\\_execution_config.yaml";

    /// <summary>
    /// Путь к YAML-файлу настроек цветов приложения.
    /// </summary>
    static public string ColorConfigPath => ".\\Settings\\_color_config.yaml";

    /// <summary>
    /// Путь к JSON-файлу настроек погрешностей измерений.
    /// </summary>
    static public string MeasurementErrorConfigPath => ".\\Settings\\_measurement_error_config.yaml";
  }
}
