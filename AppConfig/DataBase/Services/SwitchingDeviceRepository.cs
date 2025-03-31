using AppConfig.DataBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Services
{
  internal class SwitchingDeviceRepository : Repository<SwitchingDeviceEntity>
  {
    public SwitchingDeviceRepository(AppDbContext context) : base(context) { }
  }
}
