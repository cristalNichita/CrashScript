using System.Text;

namespace CrashScript.Language.Diagnostics;

public static class DiagnosticRenderer
{
    private const int TabWidth = 4;

    public static string Render(Diagnostic diagnostic)
    {
        var builder = new StringBuilder();

        var location = diagnostic.Span.StartLocation;
        var source = diagnostic.Span.Source;

        string sourceLine = source.GetLineText(location.Line);
        string displayedLine = ExpandTabs(sourceLine);

        int rawCaretOffset = Math.Clamp(
            location.Column - 1,
            0,
            sourceLine.Length);
        
        string textBeforeCaret = sourceLine[..rawCaretOffset];
        int displayedCaretOffset = ExpandTabs(textBeforeCaret).Length;

        int highlightLength = CalculateHighlightLength(
            diagnostic,
            sourceLine,
            rawCaretOffset);

        string lineNumberText = location.Line.ToString();
        int gutterWidth = lineNumberText.Length;

        builder.AppendLine($"{diagnostic.Code}: {diagnostic.Message}");
        builder.AppendLine($"{source.FilePath}:{location.Line}:{location.Column}");
        builder.AppendLine();
        
        builder.Append(lineNumberText.PadLeft(gutterWidth));
        builder.Append(" | ");
        builder.AppendLine(displayedLine);

        builder.Append(' ', gutterWidth);
        builder.Append(" | ");
        builder.Append(' ', displayedCaretOffset);
        builder.AppendLine(new string('^', highlightLength));

        if (!string.IsNullOrWhiteSpace(diagnostic.Hint))
        {
            builder.AppendLine();
            builder.AppendLine($"Hint: {diagnostic.Hint}");
        }

        if (!string.IsNullOrWhiteSpace(diagnostic.Joke))
        {
            builder.AppendLine();
            builder.AppendLine(diagnostic.Joke);
        }
        
        return builder.ToString().TrimEnd();
    }

    private static int CalculateHighlightLength(
        Diagnostic diagnostic,
        string sourceLine,
        int caretOffset)
    {
        if (diagnostic.Span.Length == 0)
        {
            return 1;
        }

        int charactersRemaining = sourceLine.Length - caretOffset;

        if (charactersRemaining <= 0)
        {
            return 1;
        }

        return Math.Max(
            1,
            Math.Min(diagnostic.Span.Length, charactersRemaining));
    }

    private static string ExpandTabs(string text)
    {
        return text.Replace("\t", new string(' ', TabWidth));
    }
}