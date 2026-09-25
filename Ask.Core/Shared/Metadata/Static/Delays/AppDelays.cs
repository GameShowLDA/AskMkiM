namespace Ask.Core.Shared.Metadata.Static.Delays
{
  using Ask.Core.Services.Config.AppSettings;
  using Ask.Core.Shared.DTO.Settings;

  /// <summary>
  /// Предоставляет задержки, используемые приложением.
  /// </summary>
  public static class AppDelays
  {
    private static readonly object SyncRoot = new();

    /// <summary>
    /// Задержки, используемые при работе с пробойной установкой.
    /// </summary>
    public readonly static BreakdownTesterDelays BreakdownTesterDelays;

    /// <summary>
    /// Задержки, используемые при работе с модулем коммутации реле.
    /// </summary>
    public readonly static ModuleRelayControlDelays ModuleRelayControlDelays;

    /// <summary>
    /// Инициализирует статические задержки приложения.
    /// </summary>
    static AppDelays()
    {
      DelaySettings settings = new DelaySettingsFileService().Load();
      BreakdownTesterDelays = new BreakdownTesterDelays(
        settings.BreakdownTester.PostTestDelay);
      ModuleRelayControlDelays = new ModuleRelayControlDelays(
        settings.ModuleRelayControl.PreCommandDelay,
        settings.ModuleRelayControl.PostCommandDelay);
    }

    /// <summary>
    /// Возвращает текущие значения задержек оборудования.
    /// </summary>
    /// <returns>Копия текущих настроек задержек.</returns>
    public static DelaySettings GetSettings()
    {
      lock (SyncRoot)
      {
        return new DelaySettings
        {
          BreakdownTester = new BreakdownTesterDelaySettings
          {
            PostTestDelay = BreakdownTesterDelays.PostTestDelay.Delay,
          },
          ModuleRelayControl = new ModuleRelayControlDelaySettings
          {
            PreCommandDelay = ModuleRelayControlDelays.PreCommandDelay.Delay,
            PostCommandDelay = ModuleRelayControlDelays.PostCommandDelay.Delay,
          },
        };
      }
    }

    /// <summary>
    /// Сохраняет настройки задержек и применяет их в текущем процессе.
    /// </summary>
    /// <param name="settings">Новые настройки задержек оборудования.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="settings"/> равен <see langword="null"/>.
    /// </exception>
    public static void SaveAndApply(DelaySettings settings)
    {
      ArgumentNullException.ThrowIfNull(settings);
      settings.BreakdownTester ??= new BreakdownTesterDelaySettings();
      settings.ModuleRelayControl ??= new ModuleRelayControlDelaySettings();

      lock (SyncRoot)
      {
        new DelaySettingsFileService().Save(settings);
        BreakdownTesterDelays.PostTestDelay.Delay = settings.BreakdownTester.PostTestDelay;
        ModuleRelayControlDelays.PreCommandDelay.Delay = settings.ModuleRelayControl.PreCommandDelay;
        ModuleRelayControlDelays.PostCommandDelay.Delay = settings.ModuleRelayControl.PostCommandDelay;
      }
    }
  }
}
