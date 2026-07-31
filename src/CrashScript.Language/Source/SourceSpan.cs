namespace CrashScript.Language.Source;

/// <summary>
///     Represents a continuous fragment of source code.
/// </summary>
public readonly record struct SourceSpan
{
    public SourceSpan(SourceText source, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (start < 0 || start > source.Length)
            throw new ArgumentOutOfRangeException(
                nameof(start),
                start,
                "Span start must be inside the source text.");

        if (length < 0 || start + length > source.Length)
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                "Span length must not extend beyond the source text.");

        Source = source;
        Start = start;
        Length = length;
    }

    public SourceText Source { get; }
    public int Start { get; }
    public int Length { get; }

    public int End => Start + Length;

    public SourceLocation StartLocation => Source.GetLocation(Start);
    public SourceLocation EndLocation => Source.GetLocation(End);

    public string Text
    {
        get
        {
            if (Length == 0) return string.Empty;

            return Source.Text.Substring(Start, Length);
        }
    }

    public static SourceSpan Empty(SourceText source, int position)
    {
        return new SourceSpan(source, position, 0);
    }
}