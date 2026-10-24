[namespace SotiCodingQuestions.Easy;

public static class RemoveDuplicates
{
    public static List<int> RemoveAndReverse(IEnumerable<int> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        var seen = new HashSet<int>();
        var uniqueNumbers = new List<int>();

        foreach (int number in numbers)
        {
            if (seen.Add(number))
            {
                uniqueNumbers.Add(number);
            }
        }

        uniqueNumbers.Reverse();
        return uniqueNumbers;
    }
}
