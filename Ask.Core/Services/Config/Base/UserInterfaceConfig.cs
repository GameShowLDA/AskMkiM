using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;

namespace Ask.Core.Services.Config.Base
{
  public static class UserInterfaceConfig
  {
    private static UserInterfaceDto UserInterfaceModel = new UserInterfaceDto
    {
      Language = "ru"
    };

    public static Action<UserInterfaceDto>? SaveUserInterfaceEvent;
    public static Func<UserInterfaceDto, Task>? SaveUserInterfaceAsyncEvent;

    #region Set.
    /// <summary>
    /// Устанавливает язык интерйефса программы.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static void SetLanguage(string enable)
    {
      var lang = LanguageSettings.NormalizeLanguageCode(enable);
      UserInterfaceModel.Language = lang;
      LanguageSettings.SetLanguageAsync(lang);
    }

    /// <summary>
    /// Устанавливает тему оформления интерфейса программы.
    /// </summary>
    /// <param name="theme">Название темы интерфейса.</param>
    public static void SetTheme(ThemeMode theme) => UserInterfaceModel.Theme = theme;

    public static void SetSyntaxHighlighting(bool enable) => UserInterfaceModel.UseSyntaxHighlighting = enable;

    public static void SetSyntaxErrorUnderlining(bool enable) => UserInterfaceModel.UseSyntaxErrorUnderlining = enable;

    public static void SetStyleErrorUnderlining(bool enable) => UserInterfaceModel.UseStyleErrorUnderlining = enable;

    public static void SetDiagnosticUnderliningMode(DiagnosticUnderliningMode mode)
    {
      SetSyntaxErrorUnderlining(mode.ShowsErrors());
      SetStyleErrorUnderlining(mode.ShowsWarnings());
    }

    public static void SetCommandBodyBackgroundHighlighting(bool enable) => UserInterfaceModel.UseCommandBodyBackgroundHighlighting = enable;

    public static void SetChainPointBodyBackgroundHighlighting(bool enable) => UserInterfaceModel.UseChainPointBodyBackgroundHighlighting = enable;

    public static void SetTopMenuIcons(bool enable) => UserInterfaceModel.UseTopMenuIcons = enable;

    public static void SetCommandAutoCollapse(bool enable) => UserInterfaceModel.UseCommandAutoCollapse = enable;

    public static Task SetUserInterfaceModel(UserInterfaceDto user)
    {
      SetLanguage(user.Language);
      SetTheme(user.Theme);
      UserInterfaceModel.AutoLockMinutes = NormalizeAutoLockMinutes(user.AutoLockMinutes);
      CopyRoleAutoLockMinutes(user, UserInterfaceModel);
      SetSyntaxHighlighting(user.UseSyntaxHighlighting);
      SetDiagnosticUnderliningMode(DiagnosticUnderliningModeExtensions.FromVisibility(
        user.UseSyntaxErrorUnderlining, user.UseStyleErrorUnderlining));
      SetCommandBodyBackgroundHighlighting(user.UseCommandBodyBackgroundHighlighting);
      SetChainPointBodyBackgroundHighlighting(user.UseChainPointBodyBackgroundHighlighting);
      SetTopMenuIcons(user.UseTopMenuIcons);
      SetCommandAutoCollapse(user.UseCommandAutoCollapse);
      EventAggregator.Publish(new EditorEvents.DiagnosticUnderliningChanged());

      return Task.CompletedTask;
    }

    #endregion

    #region Get.

    /// <summary>
    /// Возвращает язык интерфейса программы.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static Task<string> GetLanguage() => Task.FromResult(UserInterfaceModel.Language);
    public static Task<ThemeMode> GetTheme() => Task.FromResult(UserInterfaceModel.Theme);
    public static int GetAutoLockMinutes() => NormalizeAutoLockMinutes(RoleAuthorizationConfig.CurrentRole switch
    {
      RoleType.Administrator => UserInterfaceModel.AdministratorAutoLockMinutes ?? 5,
      RoleType.Developer => UserInterfaceModel.DeveloperAutoLockMinutes ?? 5,
      RoleType.Adjuster => UserInterfaceModel.AdjusterAutoLockMinutes ?? 0,
      RoleType.Root => UserInterfaceModel.RootAutoLockMinutes ?? 0,
      _ => 0
    });
    private static int NormalizeAutoLockMinutes(int value) => value is 1 or 2 or 5 or 10 ? value : 0;

    private static void CopyRoleAutoLockMinutes(UserInterfaceDto source, UserInterfaceDto target)
    {
      target.AdministratorAutoLockMinutes = source.AdministratorAutoLockMinutes;
      target.DeveloperAutoLockMinutes = source.DeveloperAutoLockMinutes;
      target.AdjusterAutoLockMinutes = source.AdjusterAutoLockMinutes;
      target.RootAutoLockMinutes = source.RootAutoLockMinutes;
    }

    public static bool GetSyntaxHighlighting() => UserInterfaceModel.UseSyntaxHighlighting;
    public static bool GetSyntaxErrorUnderlining() => UserInterfaceModel.UseSyntaxErrorUnderlining;
    public static bool GetStyleErrorUnderlining() => UserInterfaceModel.UseStyleErrorUnderlining;
    public static bool GetCommandBodyBackgroundHighlighting() => UserInterfaceModel.UseCommandBodyBackgroundHighlighting;
    public static bool GetChainPointBodyBackgroundHighlighting() => UserInterfaceModel.UseChainPointBodyBackgroundHighlighting;
    public static bool GetTopMenuIcons() => UserInterfaceModel.UseTopMenuIcons;
    public static bool GetCommandAutoCollapse() => UserInterfaceModel.UseCommandAutoCollapse;

    public static Task<UserInterfaceDto> GetParameterModel()
    {
      UserInterfaceDto parametrModel = new UserInterfaceDto
      {
        Language = UserInterfaceModel.Language,
        Theme = UserInterfaceModel.Theme,
        AutoLockMinutes = GetAutoLockMinutes(),
        UseSyntaxHighlighting = UserInterfaceModel.UseSyntaxHighlighting,
        UseSyntaxErrorUnderlining = UserInterfaceModel.UseSyntaxErrorUnderlining,
        UseStyleErrorUnderlining = UserInterfaceModel.UseStyleErrorUnderlining,
        UseCommandBodyBackgroundHighlighting = UserInterfaceModel.UseCommandBodyBackgroundHighlighting,
        UseChainPointBodyBackgroundHighlighting = UserInterfaceModel.UseChainPointBodyBackgroundHighlighting,
        UseTopMenuIcons = UserInterfaceModel.UseTopMenuIcons,
        UseCommandAutoCollapse = UserInterfaceModel.UseCommandAutoCollapse
      };
      CopyRoleAutoLockMinutes(UserInterfaceModel, parametrModel);
      return Task.FromResult(parametrModel);
    }

    public static async Task SaveProtocolModel(UserInterfaceDto parametrModel)
    {
      SetLanguage(parametrModel.Language);
      SetTheme(parametrModel.Theme);
      parametrModel.AutoLockMinutes = NormalizeAutoLockMinutes(parametrModel.AutoLockMinutes);
      switch (RoleAuthorizationConfig.CurrentRole)
      {
        case RoleType.Administrator:
          UserInterfaceModel.AdministratorAutoLockMinutes = parametrModel.AutoLockMinutes;
          break;
        case RoleType.Developer:
          UserInterfaceModel.DeveloperAutoLockMinutes = parametrModel.AutoLockMinutes;
          break;
        case RoleType.Adjuster:
          UserInterfaceModel.AdjusterAutoLockMinutes = parametrModel.AutoLockMinutes;
          break;
        case RoleType.Root:
          UserInterfaceModel.RootAutoLockMinutes = parametrModel.AutoLockMinutes;
          break;
      }
      CopyRoleAutoLockMinutes(UserInterfaceModel, parametrModel);
      SetSyntaxHighlighting(parametrModel.UseSyntaxHighlighting);
      var diagnosticMode = DiagnosticUnderliningModeExtensions.FromVisibility(
        parametrModel.UseSyntaxErrorUnderlining, parametrModel.UseStyleErrorUnderlining);
      parametrModel.UseSyntaxErrorUnderlining = diagnosticMode.ShowsErrors();
      parametrModel.UseStyleErrorUnderlining = diagnosticMode.ShowsWarnings();
      SetDiagnosticUnderliningMode(diagnosticMode);
      SetCommandBodyBackgroundHighlighting(parametrModel.UseCommandBodyBackgroundHighlighting);
      SetChainPointBodyBackgroundHighlighting(parametrModel.UseChainPointBodyBackgroundHighlighting);
      SetTopMenuIcons(parametrModel.UseTopMenuIcons);
      SetCommandAutoCollapse(parametrModel.UseCommandAutoCollapse);

      await InvokeSaveUserInterfaceAsync(parametrModel);
      SaveUserInterfaceEvent?.Invoke(parametrModel);
      EventAggregator.Publish(new EditorEvents.DiagnosticUnderliningChanged());

      LanguageSettings.SetLanguageAsync(UserInterfaceModel.Language);
      ThemeSettings.SetThemeAsync(UserInterfaceModel.Theme);

      ThemeEventAdapter.RaiseSyntaxHighlighting(parametrModel.UseSyntaxHighlighting);
      ThemeEventAdapter.RaiseChangeTheme(parametrModel.Theme);
    }

    #endregion

    private static async Task InvokeSaveUserInterfaceAsync(UserInterfaceDto parametrModel)
    {
      if (SaveUserInterfaceAsyncEvent == null)
      {
        return;
      }

      foreach (Func<UserInterfaceDto, Task> handler in SaveUserInterfaceAsyncEvent.GetInvocationList())
      {
        await handler(parametrModel);
      }
    }
  }
}
