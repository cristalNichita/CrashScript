namespace CrashScript.Language.Source;

/// <summary>
///     Represents the complete source code of a CrashScript file.
/// </summary>
public sealed class SourceText
{
    private readonly int[] _lineStarts;

    public SourceText(string filePath, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(text);

        FilePath = Path.GetFullPath(filePath);
        Text = text;
        _lineStarts = FindLineStarts(text);
    }

    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public string Text { get; }

    public int Length => Text.Length;

    public int LineCount => _lineStarts.Length;

    public char this[int index] => Text[index];

    /// <summary>
    ///     Converts an absolute character offset into a one-based line and column.
    /// </summary>
    public SourceLocation GetLocation(int offset)
    {
        if (offset < 0 || offset > Length)
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "Offset must be inside the source text.");

        var lineIndex = Array.BinarySearch(_lineStarts, offset);

        if (lineIndex < 0) lineIndex = ~lineIndex - 1;

        var lineStart = _lineStarts[lineIndex];

        return new SourceLocation(
            offset,
            lineIndex + 1,
            offset - lineStart + 1);
    }

    /// <summary>
    ///     Returns a line without its trailing CR or LF characters.
    ///     The line number is one-based.
    /// </summary>
    public string GetLineText(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > LineCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineNumber),
                lineNumber,
                "Line number is outside the source text.");
        }

        var lineIndex = lineNumber - 1;
        var start = _lineStarts[lineIndex];

        var end = lineIndex + 1 < _lineStarts.Length
            ? _lineStarts[lineIndex + 1]
            : Text.Length;

        while (end > start &&
               (Text[end - 1] == '\r' || Text[end - 1] == '\n'))
        {
            end--;
        }

        return Text.Substring(start, end - start);
    }

    private static int[] FindLineStarts(string text)
    {
        var starts = new List<int>
        {
            0
        };

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                starts.Add(index + 1);
            }
        }

        return starts.ToArray();
    }
}