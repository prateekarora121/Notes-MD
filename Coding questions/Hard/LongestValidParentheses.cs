namespace SotiCodingQuestions.Hard;

public static class LongestValidParentheses
{
    public static int FindLength(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        int leftCount = 0;
        int rightCount = 0;
        int longest = 0;

        foreach (char character in value)
        {
            if (character == '(')
            {
                leftCount++;
            }
            else if (character == ')')
            {
                rightCount++;
            }
            else
            {
                throw new ArgumentException("Input must contain only '(' and ')'.", nameof(value));
            }

            if (leftCount == rightCount)
            {
                longest = Math.Max(longest, 2 * rightCount);
            }
            else if (rightCount > leftCount)
            {
                leftCount = 0;
                rightCount = 0;
            }
        }

        leftCount = 0;
        rightCount = 0;

        for (int index = value.Length - 1; index >= 0; index--)
        {
            if (value[index] == '(')
            {
                leftCount++;
            }
            else if (value[index] == ')')
            {
                rightCount++;
            }

            if (leftCount == rightCount)
            {
                longest = Math.Max(longest, 2 * leftCount);
            }
            else if (leftCount > rightCount)
            {
                leftCount = 0;
                rightCount = 0;
            }
        }

        return longest;
    }
}
