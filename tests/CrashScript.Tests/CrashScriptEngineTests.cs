using CrashScript.Language;
using CrashScript.Language.Source;

namespace CrashScript.Tests;

public sealed class CrashScriptEngineTests
{
    [Fact]
    public void LoadSourceFile_ReadsCrashScriptFile()
    {
        const string sourceCode = "log(\"Hello from CrashScript\");";

        string filePath = CreateTemporaryFile(
            CrashScriptEngine.FileExtension,
            sourceCode);

        try
        {
            var engine = new CrashScriptEngine();

            SourceText source = engine.LoadSourceFile(filePath);

            Assert.Equal(Path.GetFullPath(filePath), source.FilePath);
            Assert.Equal(Path.GetFileName(filePath), source.FileName);
            Assert.Equal(sourceCode, source.Text);
            Assert.Equal(sourceCode.Length, source.Length);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void LoadSourceFile_ThrowsWhenFileDoesNotExist()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{CrashScriptEngine.FileExtension}");

        var engine = new CrashScriptEngine();

        Assert.Throws<FileNotFoundException>(
            () => engine.LoadSourceFile(filePath));
    }

    [Fact]
    public void LoadSourceFile_RejectsNonCrashExtension()
    {
        string filePath = CreateTemporaryFile(
            ".txt",
            "log(\"Wrong extension\");");

        try
        {
            var engine = new CrashScriptEngine();

            InvalidDataException exception =
                Assert.Throws<InvalidDataException>(() => engine.LoadSourceFile(filePath));

            Assert.Contains(
                CrashScriptEngine.FileExtension,
                exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTemporaryFile(
        string extension,
        string contents)
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}{extension}");
        
        File.WriteAllText(filePath, contents);
        
        return filePath;
    }
}