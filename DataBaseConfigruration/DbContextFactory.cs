using DataBaseConfiguration.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataBaseConfiguration
{
  public class DbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
  {
    public AppDbContext CreateDbContext(string[] args)
    {
      string basePath = DataBaseConfig.ConfigFilePath;

      DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={basePath}");

      return new AppDbContext(optionsBuilder.Options);
    }
  }
}
