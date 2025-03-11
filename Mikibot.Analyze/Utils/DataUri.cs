using System.Diagnostics.CodeAnalysis;

namespace Mikibot.Analyze.Utils;

public static class DataUri
{
    
    public static string Build(string mimeType, string base64Data)
    {
        return $"data:{mimeType};base64,{base64Data}";
    }

    public static bool TryGetData(string dataUri, [NotNullWhen(true)]out string? data)
    {
        const string sig = "base64,";
        var segmentPosition = dataUri.LastIndexOf(sig, StringComparison.Ordinal);

        if (segmentPosition > 0)
        {
            data = dataUri[(segmentPosition + sig.Length)..];
            return true;
        }

        data = null;
        return false;
    }


    public static string? GetData(string dataUri)
    {
        TryGetData(dataUri, out var data);
        return data;
    }
}