using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.DataBase.Engine.Provider
{
  public interface IDeviceRepository
  {
    Task<object?> GetByIdAsync(int id);
  }
}
