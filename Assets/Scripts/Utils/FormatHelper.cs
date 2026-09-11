public static class FormatHelper
{
    public static string FormatGold(int gold)
    {
        if (gold >= 1000000)
        {
            float val = gold / 1000000f;
            return val % 1 == 0 ? $"{val:F0}M" : $"{val:F1}M";
        }
        if (gold >= 1000)
        {
            float val = gold / 1000f;
            return val % 1 == 0 ? $"{val:F0}k" : $"{val:F1}k";
        }
        return gold.ToString();
    }
}
