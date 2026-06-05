using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  public interface IHasRackStructure
  {
    public Dictionary<BusStructureEnum.Type, List<int?>> BusStructure { get; set; }
  }
}
