using Ask.Core.Shared.Metadata.Atributes;

namespace Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands
{
  public enum OrganizationalComands
  {
    [CommandOrganizationalAttribute("СП")]
    /// <summary>
    /// Тип команды CP.
    /// </summary>
    CP,

    [CommandOrganizationalAttribute("ЦУ")]
    /// <summary>
    /// Тип команды CU.
    /// </summary>
    CU,

    [CommandOrganizationalAttribute("КЦ")]
    /// <summary>
    /// Тип команды KSC.
    /// </summary>
    KSC,

    [CommandOrganizationalAttribute("ОК")]
    /// <summary>
    /// Тип команды OK постоянным током.
    /// </summary>
    OK,

    [CommandOrganizationalAttribute("РМ")]
    /// <summary>
    /// Тип команды RM.
    /// </summary>
    RM,

    [CommandOrganizationalAttribute("УП")]
    /// <summary>
    /// Тип команды UP.
    /// </summary>
    UP,

    [CommandOrganizationalAttribute("ВШ")]
    /// <summary>
    /// Тип команды VSH.
    /// </summary>
    VSH,
  }
}

