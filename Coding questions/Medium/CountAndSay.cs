using System.Text;

namespace SotiCodingQuestions.Medium;

public static class CountAndSay
{
    public static string GetTerm(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n);

        string term = "1";

        for (int step = 1; step < n; step++)
        {
            var nextTerm = new StringBuilder();
            int index = 0;

            while (index < term.Length)
            {
                int runEnd = index + 1;
                while (runEnd < term.Length && term[runEnd] == term[index])
                {
                    runEnd++;
                }

                nextTerm.Append(runEnd - index);
                nextTerm.Append(term[index]);
                index = runEnd;
            }

            term = nextTerm.ToString();
        }

        return term;
    }
}
