using System;

namespace JLyricsUtil
{
    internal sealed class UndoAction : IUndoableAction
    {
        private readonly Action action;

        public UndoAction(Action action)
        {
            this.action = action;
        }

        public void Undo()
        {
            action();
        }
    }
}
