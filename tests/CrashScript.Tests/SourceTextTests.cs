using CrashScript.Language.Source;

namespace CrashScript.Tests;

public sealed class SourceTextTests
{
    [Fact]
    public void GetLocation_ReturnsOneBasedLineAndColumn()
    {
        var source = new SourceText(
            "test.crash",
            "first\nsecond\nthird");

        int offset = source.Text.IndexOf(
            "second",
            StringComparison.Ordinal);

        SourceLocation location = source.GetLocation(offset);

        Assert.Equal(2, location.Line);
        Assert.Equal(1, location.Column);
        Assert.Equal(offset, location.Offset);
    }

    [Fact]
    public void GetLineText_RemovesLineEnding()
    {
        var source = new SourceText(
            "test.crash",
            "first\r\nsecond\nthird");

        Assert.Equal("first", source.GetLineText(1));
        Assert.Equal("second", source.GetLineText(2));
        Assert.Equal("third", source.GetLineText(3));
    }

    [Fact]
    public void SourceSpan_ReturnsOriginalText()
    {
        var source = new SourceText(
            "test.crash",
            "memory score: int = 10;");

        int start = source.Text.IndexOf(
            "score",
            StringComparison.Ordinal);

        var span = new SourceSpan(
            source,
            start,
            "score".Length);

        Assert.Equal("score", span.Text);
        Assert.Equal(1, span.StartLocation.Line);
        Assert.Equal(8, span.StartLocation.Column);
    }
}