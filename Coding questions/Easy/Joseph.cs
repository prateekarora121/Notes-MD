namespace SotiCodingQuestions.Easy;

public static class Joseph
{
    public static int ToggleBits(int number)
    {
        if (number is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Number must be between 1 and 100.");
        }

        int bitCount = 0;
        int remaining = number;

        while (remaining > 0)
        {
            bitCount++;
            remaining >>= 1;
        }

        return ((1 << bitCount) - 1) ^ number;
    }
}
