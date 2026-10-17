namespace SotiCodingQuestions.Medium;

public static class RestoreIpAddresses
{
    public static IList<string> Find(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);

        if (digits.Any(character => character is < '0' or > '9'))
        {
            throw new ArgumentException("Input must contain only digits.", nameof(digits));
        }

        var results = new List<string>();
        if (digits.Length is < 4 or > 12)
        {
            return results;
        }

        var segments = new List<string>(4);
        AddAddresses(digits, 0, segments, results);
        return results;
    }

    private static void AddAddresses(
        string digits,
        int start,
        List<string> segments,
        List<string> results)
    {
        int remainingDigits = digits.Length - start;
        int remainingSegments = 4 - segments.Count;

        if (remainingDigits < remainingSegments || remainingDigits > remainingSegments * 3)
        {
            return;
        }
//"25525511135"
        if (segments.Count == 4)
        {
            results.Add(string.Join('.', segments));
            return;
        }

        for (int length = 1; length <= 3 && start + length <= digits.Length; length++)
        {
            if (length > 1 && digits[start] == '0')
            {
                break;
            }

            string segment = digits.Substring(start, length);
            if (int.Parse(segment) > 255)
            {
                continue;
            }

            segments.Add(segment);
            AddAddresses(digits, start + length, segments, results);
            segments.RemoveAt(segments.Count - 1);
        }
    }
}
