using System;

namespace JLyricsUtil
{
    public sealed class ParsedLyric
    {
        public string Content { get; set; }
        public bool WasCombined { get; set; }
        public string OriginalUnsplit { get; set; }

        public ParsedLyric(string content)
        {
            Content = content;
        }

        public bool FirstCharacterIsLatin()
            => Content[0] < 0x0100;

        public int IndexOf(string str, int startIndex)
            => Content.IndexOf(str, startIndex, StringComparison.OrdinalIgnoreCase);

        //public int IndexOf(string str)
        //    => IndexOf(str, StringComparison.OrdinalIgnoreCase);

        //public int IndexOf(string str, StringComparison comparisonType)
        //    => Content.IndexOf(str, 0, comparisonType);

        public override string ToString()
            => Content;

        public static ParsedLyric operator +(ParsedLyric left, ParsedLyric right)
        {
            return new ParsedLyric(left.Content + right.Content)
            {
                WasCombined = true
            };
        }

        public char FirstCharacter()
            => Content[0];
    }
}
