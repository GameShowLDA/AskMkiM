using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TestArchive
{
  internal class Program
  {
    static void Main(string[] args)
    {
      Console.OutputEncoding = Encoding.UTF8;
      using (var archiveManager = new ArchiveManager())
      {
        var isRunning = true;
        while (isRunning)
        {
          PrintMenu(archiveManager.OpenedArchivePath);
          Console.Write("Enter command number: ");
          var command = Console.ReadLine()?.Trim();
          Console.WriteLine();

          try
          {
            switch (command)
            {
              case "1":
                CreateArchive(archiveManager);
                break;
              case "2":
                OpenArchive(archiveManager);
                break;
              case "3":
                PrintFiles(archiveManager);
                break;
              case "4":
                ReadFileText(archiveManager);
                break;
              case "5":
                AddFile(archiveManager);
                break;
              case "6":
                DeleteFile(archiveManager);
                break;
              case "7":
                DeleteArchive(archiveManager);
                break;
              case "8":
                PrintIntegrityNotifications(archiveManager.IntegrityNotifications);
                break;
              case "9":
                archiveManager.CloseArchive();
                Console.WriteLine("Archive is closed.");
                break;
              case "0":
                isRunning = false;
                break;
              default:
                Console.WriteLine("Unknown command.");
                break;
            }
          }
          catch (Exception ex)
          {
            Console.WriteLine($"Error: {ex.Message}");
          }

          if (isRunning)
          {
            Console.WriteLine();
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
            Console.WriteLine();
          }
        }
      }
    }

    private static void PrintMenu(string openedArchivePath)
    {
      TryClearConsole();
      Console.WriteLine("=== APKW Archive Test Menu ===");
      Console.WriteLine($"Opened archive: {openedArchivePath ?? "(not opened)"}");
      Console.WriteLine();
      Console.WriteLine("1 - Create archive");
      Console.WriteLine("2 - Open archive");
      Console.WriteLine("3 - Show file list");
      Console.WriteLine("4 - Open file and read text (UTF-8)");
      Console.WriteLine("5 - Add file to archive");
      Console.WriteLine("6 - Delete file from archive");
      Console.WriteLine("7 - Delete archive");
      Console.WriteLine("8 - Show checksum notifications");
      Console.WriteLine("9 - Close opened archive");
      Console.WriteLine("0 - Exit");
      Console.WriteLine();
    }

    private static void TryClearConsole()
    {
      try
      {
        if (!Console.IsOutputRedirected)
        {
          Console.Clear();
        }
      }
      catch (IOException)
      {
      }
    }

    private static void CreateArchive(ArchiveManager archiveManager)
    {
      Console.Write("Archive name (without extension): ");
      var archiveName = Console.ReadLine();

      var archivePath = archiveManager.CreateArchive(archiveName);
      Console.WriteLine($"Archive created: {archivePath}");
    }

    private static void OpenArchive(ArchiveManager archiveManager)
    {
      Console.Write("Path to .apkw archive: ");
      var archivePath = Console.ReadLine();
      archiveManager.OpenArchive(archivePath);
      Console.WriteLine($"Archive opened: {archiveManager.OpenedArchivePath}");

      PrintIntegrityNotifications(archiveManager.IntegrityNotifications);
    }

    private static void PrintFiles(ArchiveManager archiveManager)
    {
      var files = archiveManager.GetFileList();
      if (files.Count == 0)
      {
        Console.WriteLine("No files in archive.");
        return;
      }

      Console.WriteLine("Files in archive:");
      PrintNumberedList(files);
    }

    private static void ReadFileText(ArchiveManager archiveManager)
    {
      Console.Write("Archive entry name: ");
      var archiveEntryName = Console.ReadLine();

      var text = archiveManager.GetFileText(archiveEntryName);
      Console.WriteLine("----- File start -----");
      Console.WriteLine(text);
      Console.WriteLine("------ File end ------");
    }

    private static void AddFile(ArchiveManager archiveManager)
    {
      Console.Write("Path to file on disk: ");
      var filePath = Console.ReadLine();
      archiveManager.AddFileToOpenedArchive(filePath);

      Console.WriteLine("File added.");
      PrintIntegrityNotifications(archiveManager.IntegrityNotifications);
    }

    private static void DeleteFile(ArchiveManager archiveManager)
    {
      Console.Write("Archive entry name to delete: ");
      var archiveEntryName = Console.ReadLine();
      archiveManager.DeleteFileFromOpenedArchive(archiveEntryName);
      Console.WriteLine("File deleted.");
      PrintIntegrityNotifications(archiveManager.IntegrityNotifications);
    }

    private static void DeleteArchive(ArchiveManager archiveManager)
    {
      Console.Write("Archive path (empty = delete opened archive): ");
      var archivePath = Console.ReadLine();

      archiveManager.DeleteArchive(string.IsNullOrWhiteSpace(archivePath) ? null : archivePath);
      Console.WriteLine("Archive deleted.");
    }

    private static void PrintIntegrityNotifications(IReadOnlyList<string> notifications)
    {
      if (notifications == null || notifications.Count == 0)
      {
        Console.WriteLine("Checksums: no errors found.");
        return;
      }

      Console.WriteLine("Integrity issues found:");
      PrintNumberedList(notifications);
    }

    private static void PrintNumberedList(IReadOnlyList<string> values)
    {
      for (var index = 0; index < values.Count; index++)
      {
        Console.WriteLine($"{index + 1}. {values[index]}");
      }
    }
  }
}
