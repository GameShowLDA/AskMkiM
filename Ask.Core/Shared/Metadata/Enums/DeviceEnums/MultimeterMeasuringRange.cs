using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums
{
  //НАПОМИНАЛКА ДЛЯ РАЗРАБА!
  //Плевать что измеряем, на проверку вкл./выкл. AUTO всегда в ответ вернётся 1/0.

  //Gemini мне тут сказал, что у Keysight 34465A нет авто режима для ёмкости (оно напиздело).
  //И что команда AUTO ON сработает на сопротивлении, напряжении, но не на ёмкости (мне кажется что оно пиздит мне).
  //Обновление 1: оно реально напиздело.

  public enum MultimeterRange
  {
    [Display(Name = "AUTO ON", Description = "1")]
    Auto,

    [Display(Name = "0.1", Description = "+1.00000000E-01")]
    mV_100,

    [Display(Name = "1", Description = "+1.00000000E+00")]
    V_1,

    [Display(Name = "10", Description = "+1.00000000E+01")]
    V_10,

    [Display(Name = "100", Description = "+1.00000000E+02")]
    V_100,

    [Display(Name = "750", Description = "+1.00000000E+02")]
    V_750,

    [Display(Name = "1000", Description = "+1.00000000E+03")]
    V_1000,

    Ohm_100,
    kOhm_1,
    kOhm_10,
    kOhm_100,
    MOhm_1,
    MOhm_10,
    MOhm_100,
    GOhm_1,

    nF_1,
    nF_10,
    nF_100,
    uF_1,
    uF_10,
    uF_100
  }

  public enum VoltageRange
  {
    [Display(Name = ":AUTO ON", Description = "1")]
    Auto,

    [Display(Name = " 0.1", Description = "+1.00000000E-01")]
    mV_100,

    [Display(Name = " 1", Description = "+1.00000000E+00")]
    V_1,

    [Display(Name = " 10", Description = "+1.00000000E+01")]
    V_10,

    [Display(Name = " 100", Description = "+1.00000000E+02")]
    V_100,

    [Display(Name = " 750", Description = "+1.00000000E+03")]
    V_750,

    [Display(Name = " 1000", Description = "+1.00000000E+03")]
    V_1000
  }

  public enum ResistanceRange
  {
    Auto,
    Ohm_100,
    kOhm_1,
    kOhm_10,
    kOhm_100,
    MOhm_1,
    MOhm_10,
    MOhm_100,
    GOhm_1
  }

  public enum CapacitanceRange
  {
    Auto,
    nF_1,
    nF_10,
    nF_100,
    uF_1,
    uF_10,
    uF_100
  }
}