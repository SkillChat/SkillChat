namespace SkillChat.Server.ServiceInterface
{
    public static class TextHelper
    {
        public static string FallbackIfEmpty(this string preferredValue, string fallbackValue)
        {
            return string.IsNullOrEmpty(preferredValue?.Trim()) ? fallbackValue : preferredValue;
        }
    }
}