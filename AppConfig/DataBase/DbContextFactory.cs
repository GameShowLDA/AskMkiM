using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using AppConfig.DataBase;

namespace AppConfig.DataBase
{
  public class DbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
  {
    public AppDbContext CreateDbContext(string[] args)
    {
      string basePath =  FileLocations.ConfigFilePath;

      DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={basePath}");

      return new AppDbContext(optionsBuilder.Options);
    }
  }
}
