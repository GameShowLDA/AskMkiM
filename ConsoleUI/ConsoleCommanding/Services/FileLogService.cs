using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.ConsoleCommanding.Services
{
  public class FileLogService
  {
    private readonly string _logPath;

    public FileLogService(string logPath = "logs.txt")
    {
      _logPath = logPath;
    }

    public void Append(string message)
    {
      try
      {
        File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
      }
      catch
      {
        // Игнорируем ошибки записи в лог
      }
    }

    public void Clear()
    {
      try
      {
        if (File.Exists(_logPath))
          File.Delete(_logPath);
      }
      catch
      {
        // Игнорируем ошибки удаления
      }
    }
  }
}
