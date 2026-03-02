using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace UI.Services.Archive
{
  internal static class ArchiveManifestService
  {
    public const string ManifestEntryName = "__apkw_manifest.json";

    public static IReadOnlyList<string> ValidateArchive(ZipArchive archive)
    {
      if (archive == null)
      {
        throw new ArgumentNullException(nameof(archive));
      }

      var notifications = new List<string>();
      var manifest = ReadManifest(archive);
      var archiveFiles = archive.Entries
        .Where(IsArchiveFileEntry)
        .ToDictionary(entry => NormalizeEntryName(entry.FullName), entry => entry, StringComparer.OrdinalIgnoreCase);

      var manifestFiles = manifest.Files
        .Where(file => !string.IsNullOrWhiteSpace(file.Name))
        .GroupBy(file => NormalizeEntryName(file.Name), StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

      foreach (var archiveFile in archiveFiles)
      {
        if (!manifestFiles.TryGetValue(archiveFile.Key, out var manifestFile))
        {
          notifications.Add($"Checksum record is missing for file '{archiveFile.Key}'.");
          continue;
        }

        var actualChecksum = ComputeChecksum(archiveFile.Value);
        if (!actualChecksum.Equals(NormalizeChecksum(manifestFile.Checksum), StringComparison.OrdinalIgnoreCase))
        {
          notifications.Add(
            $"Checksum mismatch for file '{archiveFile.Key}'. " +
            $"Expected '{manifestFile.Checksum}', actual '{actualChecksum}'.");
        }
      }

      foreach (var manifestFile in manifestFiles.Values)
      {
        var normalizedName = NormalizeEntryName(manifestFile.Name);
        if (!archiveFiles.ContainsKey(normalizedName))
        {
          notifications.Add($"File '{normalizedName}' is listed in manifest but is missing from archive.");
        }
      }

      return notifications;
    }

    public static List<ArchiveManifestFileRecord> BuildManifestRecords(ZipArchive archive)
    {
      if (archive == null)
      {
        throw new ArgumentNullException(nameof(archive));
      }

      return archive.Entries
        .Where(IsArchiveFileEntry)
        .Select(entry => new ArchiveManifestFileRecord
        {
          Name = NormalizeEntryName(entry.FullName),
          Checksum = ComputeChecksum(entry), 
        })
        .OrderBy(record => record.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    public static void WriteManifest(ZipArchive archive, IEnumerable<ArchiveManifestFileRecord> fileRecords)
    {
      if (archive == null)
      {
        throw new ArgumentNullException(nameof(archive));
      }

      var records = (fileRecords ?? Enumerable.Empty<ArchiveManifestFileRecord>())
        .Where(record => record != null && !string.IsNullOrWhiteSpace(record.Name))
        .Select(record => new ArchiveManifestFileRecord
        {
          Name = NormalizeEntryName(record.Name),
          Checksum = NormalizeChecksum(record.Checksum),
          CreationDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("o")
        })
        .OrderBy(record => record.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

      var manifest = new ArchiveManifest
      {
        Files = records
      };

      var existingManifestEntry = archive.GetEntry(ManifestEntryName);
      existingManifestEntry?.Delete();

      var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
      var serializer = new DataContractJsonSerializer(typeof(ArchiveManifest));

      using (var manifestStream = manifestEntry.Open())
      {
        serializer.WriteObject(manifestStream, manifest);
      }
    }

    public static void RemoveFileRecord(ZipArchive archive, string archiveEntryName)
    {
      if (archive == null)
      {
        throw new ArgumentNullException(nameof(archive));
      }

      if (string.IsNullOrWhiteSpace(archiveEntryName))
      {
        throw new ArgumentException("Archive entry name is required.", nameof(archiveEntryName));
      }

      var normalizedEntryName = NormalizeEntryName(archiveEntryName);
      var manifest = ReadManifest(archive);

      manifest.Files = manifest.Files
        .Where(record =>
          !NormalizeEntryName(record.Name).Equals(normalizedEntryName, StringComparison.OrdinalIgnoreCase))
        .ToList();

      WriteManifest(archive, manifest.Files);
    }

    public static bool IsArchiveFileEntry(ZipArchiveEntry entry)
    {
      return entry != null &&
             !string.IsNullOrEmpty(entry.Name) &&
             !entry.FullName.Equals(ManifestEntryName, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeEntryName(string entryName)
    {
      if (entryName == null)
      {
        return string.Empty;
      }

      var normalizedName = entryName.Replace('\\', '/');
      while (normalizedName.StartsWith("/", StringComparison.Ordinal))
      {
        normalizedName = normalizedName.Substring(1);
      }

      return normalizedName.Trim();
    }

    private static ArchiveManifest ReadManifest(ZipArchive archive)
    {
      var manifestEntry = archive.GetEntry(ManifestEntryName);
      if (manifestEntry == null)
      {
        return new ArchiveManifest
        {
          Files = new List<ArchiveManifestFileRecord>()
        };
      }

      var serializer = new DataContractJsonSerializer(typeof(ArchiveManifest));
      using (var stream = manifestEntry.Open())
      {
        var manifest = serializer.ReadObject(stream) as ArchiveManifest;
        if (manifest?.Files == null)
        {
          return new ArchiveManifest
          {
            Files = new List<ArchiveManifestFileRecord>()
          };
        }

        manifest.Files = manifest.Files
          .Where(file => file != null && !string.IsNullOrWhiteSpace(file.Name))
          .Select(file => new ArchiveManifestFileRecord
          {
            Name = NormalizeEntryName(file.Name),
            Checksum = NormalizeChecksum(file.Checksum),
            CreationDate = file.CreationDate
          })
          .ToList();

        return manifest;
      }
    }

    private static string ComputeChecksum(ZipArchiveEntry entry)
    {
      using (var stream = entry.Open())
      {
        return ComputeChecksum(stream);
      }
    }

    private static string ComputeChecksum(Stream stream)
    {
      using (var sha256 = SHA256.Create())
      {
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
      }
    }

    private static string NormalizeChecksum(string checksum)
    {
      return (checksum ?? string.Empty).Trim().ToLowerInvariant();
    }
  }

  [DataContract]
  internal sealed class ArchiveManifest
  {
    [DataMember(Name = "files")]
    public List<ArchiveManifestFileRecord> Files { get; set; } = new List<ArchiveManifestFileRecord>();
  }

  [DataContract]
  internal sealed class ArchiveManifestFileRecord
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "checksum")]
    public string Checksum { get; set; }

    [DataMember(Name = "creationdate")]
    public string CreationDate { get; set; }
  }
}
