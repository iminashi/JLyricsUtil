namespace JLyricsUtil
{
    public static class Extensions
    {
        public static bool IsCommonLatinCharacter(this char c)
            => c < 0x0100;
    }
}
