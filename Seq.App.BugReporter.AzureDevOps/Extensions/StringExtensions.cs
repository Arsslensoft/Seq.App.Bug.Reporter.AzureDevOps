using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Seq.App.BugReporter.AzureDevOps.Extensions;

internal static class StringExtensions
{
    /// <summary>
    ///     Parses a string of key:value pairs into a dictionary.
    /// </summary>
    /// <param name="valueString">The input string.</param>
    /// <returns>The parsed key value pairs</returns>
    internal static Dictionary<string, string> ParseKeyValueArray(this string valueString)
    {
        var values = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(valueString)) return values;

        foreach (var val in valueString.Split(',').ToArray())
        {
            var temp = val.Split(':').ToArray();
            if (temp.GetUpperBound(0) == 1) values.Add(temp[0], temp[1]);
        }

        return values;
    }

    /// <summary>
    ///     Truncates a string to a maximum length.
    /// </summary>
    /// <param name="inputString">The input string</param>
    /// <param name="maxLength">The maximum length</param>
    /// <returns>The truncated string</returns>
    internal static string Truncate(this string inputString, int maxLength)
    {
        return string.IsNullOrEmpty(inputString) ? inputString :
            inputString.Length > maxLength ? inputString.Remove(maxLength) : inputString;
    }

    /// <summary>
    ///     Truncates a string to a maximum length, adding an ellipsis if required.
    /// </summary>
    /// <param name="inputString">The input string</param>
    /// <param name="maxLength">The maximum length</param>
    /// <returns>The truncated string</returns>
    internal static string TruncateWithEllipsis(this string inputString, int maxLength)
    {
        return string.IsNullOrEmpty(inputString) || inputString.Length <= maxLength
            ? inputString
            : Truncate(inputString, Math.Max(maxLength, 3) - 3) + "…";
    }


    internal static string GetStringHash(this string str)
    {
        var crypt = new SHA256Managed();
        var hash = string.Empty;
        var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(str));
        foreach (var theByte in crypto) hash += theByte.ToString("x2");
        return hash;
    }
}