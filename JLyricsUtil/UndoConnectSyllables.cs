using System.Collections.Generic;

namespace JLyricsUtil
{
    internal sealed class UndoConnectSyllables : IUndoableAction
    {
        private readonly RomajiLyric next;
        private readonly RomajiLyric current;
        private readonly IList<RomajiLyric> collection;
        private readonly int currentIndex;

        public UndoConnectSyllables(int currentIndex, RomajiLyric current, RomajiLyric next, IList<RomajiLyric> collection)
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
