namespace SotiCodingQuestions.Easy;

public static class MinSquares
{
    public static int FindMinimumCount(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);

        var minimumCounts = new int[number + 1];

        for (int current = 1; current <= number; current++)
        {
            minimumCounts[current] = int.MaxValue;

            for (int root = 1; root * root <= current; root++)
            {
                minimumCounts[current] = Math.Min(
                    minimumCounts[current],
                    minimumCounts[current - root * root] + 1);
            }
        }

        return minimumCounts[number];
    }
}
