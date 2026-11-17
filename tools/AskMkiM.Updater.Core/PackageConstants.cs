namespace AskMkiM.Updater.Core;

internal static class PackageConstants
{
  internal const string ManifestEntryName = ".askmkim-update/manifest.json";
  internal const string InstalledManifestName = ".askmkim-installed-manifest.json";
  internal const string TrailerMagic = "ASKMKIMUPDATEV1!";
  internal const int TrailerSize = 24;
}
