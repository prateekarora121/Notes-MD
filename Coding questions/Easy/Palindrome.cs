namespace SotiCodingQuestions.Easy;

public static class Palindrome
{
    public static bool IsPalindrome(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        int left = 0;
        int right = value.Length - 1;

        while (left < right)
        {
            if (value[left] != value[right])
            {
                return false;
            }

            left++;
            right--;
        }

        return true;
    }
}
