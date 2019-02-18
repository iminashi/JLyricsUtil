using System.Collections.Generic;

namespace JLyricsUtil
{
    internal sealed class UndoConnectSymbols : IUndoableAction
    {
        private readonly int currentIndex;
        private readonly ParsedLyric current;
        private readonly ParsedLyric next;
        private readonly IList<ParsedLyric> collection;

        public UndoConnectSymbols(int currentIndex, ParsedLyric current, ParsedLyric next, IList<ParsedLyric> collection)
        {
            this.next = next;
            this.current = current;
            this.collection = collection;
            this.currentIndex = currentIndex;
        }

        public void Undo()
        {
            collection[currentIndex] = current;
            collection.Insert(currentIndex + 1, next);
        }
    }
}
