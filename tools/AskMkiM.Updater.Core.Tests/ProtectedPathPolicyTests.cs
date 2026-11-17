using AskMkiM.Updater.Core;

namespace AskMkiM.Updater.Core.Tests;

public sealed class ProtectedPathPolicyTests
{
  [Theory]
  [InlineData("Resources/app.db")]
  [InlineData("Resources\\role-auth.json")]
  [InlineData("Settings/delaySettings.yaml")]
  [InlineData("History/2026-11-11/protocol.asktrace")]
  [InlineData("logs/all.log")]
  [InlineData("UpdateBackups/backup/file.dll")]
  public void IsProtected_ReturnsTrueForMutableData(string path)
  {
    Assert.True(ProtectedPathPolicy.IsProtected(path));
  }

  [Theory]
  [InlineData("AskMkiM.exe")]
  [InlineData("Ask.Engine.dll")]
  [InlineData("Resources/logo.png")]
  [InlineData("AppHelp/ru/index.html")]
  public void IsProtected_ReturnsFalseForProgramFiles(string path)
  {
    Assert.False(ProtectedPathPolicy.IsProtected(path));
  }

  [Theory]
  [InlineData("../AskMkiM.exe")]
  [InlineData("AppHelp/../../app.db")]
  [InlineData("C:\\AskMkiM\\AskMkiM.exe")]
  public void NormalizeRelativePath_RejectsPathTraversal(string path)
  {
    Assert.Throws<InvalidDataException>(() => PathSafety.NormalizeRelativePath(path));
  }
}
