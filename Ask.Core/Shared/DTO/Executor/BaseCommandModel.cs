using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.DTO.Executor
{
  /// <summary>
  /// Базовая модель любой команды после разбора.
  /// </summary>
  public abstract class BaseCommandModel : IError
  {
    public virtual MeasurementTypeCommand TypeCommand { get; set; }
    public List<string> SourceLines { get; set; } = new List<string>();

    public List<ErrorItem> Errors { get; set; } = new List<ErrorItem>();
    public List<WarningItem> Warnings { get; set; } = new List<WarningItem>();

    /// <summary>
    /// Номер строки, с которой начинается команда (в исходном тексте).
    /// </summary>
    public int StartLineNumber { get; set; }

    /// <summary>
    /// Номер строки, с которой начинается команда в отформатированном (трансляционном) тексте.
    /// Проставляется после форматирования.
    /// </summary>
    public int FormattedStartLineNumber { get; set; } = -1;

    /// <summary>
    /// Ключи алгоритма проверки, указанные в команде.
    /// </summary>
    public List<string> AlgorithmKey { get; set; } = new();

    /// <summary>
    /// Комментарии, указанные в команде.
    /// </summary>
    public List<string> Comment { get; set; } = new();

    public virtual IPointError PointErrors => null;

    public virtual IDislpayInfo BuildDislpayInfo => null;

    public string CommandNumber { get; set; }
    public virtual string Mnemonic { get; set; }
    public string PointsSourse { get; set; }

    /// <summary>
    /// Признак установленной точки останова для данной команды.
    /// Указывает, что выполнение анализа или обработки должно быть остановлено при достижении этой команды.
    /// </summary>
    public bool HasBreakpoint { get; set; }

    #region Методы
    public virtual T GetModel<T>(BaseCommandModel baseCommandModel) where T : class
    {
      return baseCommandModel as T;
    }

    #endregion

  }
}
