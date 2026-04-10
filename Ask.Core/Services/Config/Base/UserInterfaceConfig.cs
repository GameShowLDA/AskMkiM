using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.DTO.Settings;
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

    public static void SetCommandBodyBackgroundHighlighting(bool enable) => UserInterfaceModel.UseCommandBodyBackgroundHighlighting = enable;

    public static void SetChainPointBodyBackgroundHighlighting(bool enable) => UserInterfaceModel.UseChainPointBodyBackgroundHighlighting = enable;

    public static void SetTopMenuIcons(bool enable) => UserInterfaceModel.UseTopMenuIcons = enable;

    public static void SetCommandAutoCollapse(bool enable) => UserInterfaceModel.UseCommandAutoCollapse = enable;


    public static Task SetUserInterfaceModel(UserInterfaceDto user)
    {
      SetLanguage(user.Language);
      SetTheme(user.Theme);
      SetSyntaxHighlighting(user.UseSyntaxHighlighting);
      SetCommandBodyBackgroundHighlighting(user.UseCommandBodyBackgroundHighlighting);
      SetChainPointBodyBackgroundHighlighting(user.UseChainPointBodyBackgroundHighlighting);
      SetTopMenuIcons(user.UseTopMenuIcons);
      SetCommandAutoCollapse(user.UseCommandAutoCollapse);

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
    public static bool GetSyntaxHighlighting() => UserInterfaceModel.UseSyntaxHighlighting;
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
        UseSyntaxHighlighting = UserInterfaceModel.UseSyntaxHighlighting,
        UseCommandBodyBackgroundHighlighting = UserInterfaceModel.UseCommandBodyBackgroundHighlighting,
        UseChainPointBodyBackgroundHighlighting = UserInterfaceModel.UseChainPointBodyBackgroundHighlighting,
        UseTopMenuIcons = UserInterfaceModel.UseTopMenuIcons,
        UseCommandAutoCollapse = UserInterfaceModel.UseCommandAutoCollapse
      };
      return Task.FromResult(parametrModel);
    }

    public static async Task SaveProtocolModel(UserInterfaceDto parametrModel)
    {
      SetLanguage(parametrModel.Language);
      SetTheme(parametrModel.Theme);
      SetSyntaxHighlighting(parametrModel.UseSyntaxHighlighting);
      SetCommandBodyBackgroundHighlighting(parametrModel.UseCommandBodyBackgroundHighlighting);
      SetChainPointBodyBackgroundHighlighting(parametrModel.UseChainPointBodyBackgroundHighlighting);
      SetTopMenuIcons(parametrModel.UseTopMenuIcons);
      SetCommandAutoCollapse(parametrModel.UseCommandAutoCollapse);

      await InvokeSaveUserInterfaceAsync(parametrModel);
      SaveUserInterfaceEvent?.Invoke(parametrModel);

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
