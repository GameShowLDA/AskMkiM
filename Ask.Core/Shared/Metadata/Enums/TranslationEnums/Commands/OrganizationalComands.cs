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

    [CommandOrganizationalAttribute("ПТ")]
    /// <summary>
    /// Тип команды PT.
    /// </summary>
    PT,

    [CommandOrganizationalAttribute("ОТ")]
    /// <summary>
    /// Тип команды OT.
    /// </summary>
    OT,
    [CommandOrganizationalAttribute("ВШ")]
    /// <summary>
    /// Тип команды VSH.
    /// </summary>
    VSH,

    [CommandOrganizationalAttribute("СК")]
    /// <summary>
    /// Тип команды CK.
    /// </summary>
    CK,

    [CommandOrganizationalAttribute("ОС")]
    /// <summary>
    /// Тип команды ОС.
    /// </summary>
    OC,
  }
}
