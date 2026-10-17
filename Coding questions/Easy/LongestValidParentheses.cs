namespace SotiCodingQuestions.Easy;

public static class LongestValidParentheses
{
    public static int FindLength(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var indices = new Stack<int>();
        indices.Push(-1);
        int longest = 0;

        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '(')
            {
                indices.Push(i);
            }
            else if (value[i] == ')')
            {
                indices.Pop();
                if (indices.Count == 0)
                {
                    indices.Push(i);
                }
                else
                {
                    longest = Math.Max(longest, i - indices.Peek());
                }
            }
            else
            {
                throw new ArgumentException("Input must contain only '(' and ')'.", nameof(value));
            }
        }

        return longest;
    }
}
