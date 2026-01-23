using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Mikibot.Crawler.Http.Bilibili.Model;

namespace Mikibot.Crawler.Http.Bilibili;

public static partial class WbiSigner
{

    [GeneratedRegex(@"[!'()*]")]
    private static partial Regex GenerateEscapeRegExp();
    
    private static readonly Regex EscapeRegExp = GenerateEscapeRegExp();
    
    private static readonly int[] MixinKeyEncTab =
    [
        46, 47, 18, 2, 53, 8, 23, 32, 15, 50, 10, 31, 58, 3, 45, 35, 27, 43, 5, 49, 33, 9, 42, 19, 29, 28, 14, 39,
        12, 38, 41, 13, 37, 48, 7, 16, 24, 55, 40, 61, 26, 17, 0, 1, 60, 51, 30, 4, 22, 25, 54, 21, 56, 59, 6, 63,
        57, 62, 11, 36, 20, 34, 44, 52
    ];

    private static string GetKeyOf(string url)
    {
        return new Uri(url).Segments[^1][..^4];
    }
    
    public static string GetMixinKey(WbiInfo wbiInfo)
    {
        var orig = $"{GetKeyOf(wbiInfo.ImgUrl)}{GetKeyOf(wbiInfo.SubUrl)}";
        return MixinKeyEncTab.Aggregate("", (s, i) => s + orig[i])[..32];
    }

    private static string Escape(string raw)
    {
        return EscapeRegExp.Replace(raw, "");
    }

    private static string ToQuery(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join('&', parameters
            .OrderBy(p => p.Key)
            .Select(p => $"{p.Key}={HttpUtility.UrlEncode(Escape(p.Value))}"));
    }
    
    private static string EncodeWebComponentWithWbiSign(
        string mixinKey,
        IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var time = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}";
        var parameterList = parameters.ToList();

        var query = ToQuery(GetParameters());
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(query + mixinKey));
        var wbiSign = Convert.ToHexStringLower(hashBytes);

        return $"{ToQuery(parameterList)}&w_rid={wbiSign}&wts={time}";

        IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            foreach (var parameter in parameterList) yield return parameter;
            yield return new KeyValuePair<string, string>("wts", time);
        }
    }

    public static string SignQueryParameters(string? mixinKey, IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return mixinKey is not { Length: > 0 }
            ? throw new InvalidOperationException("Must call InitializeAsync first")
            : EncodeWebComponentWithWbiSign(mixinKey, parameters);
    }
}
