using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JLyricsUtil
{
    public sealed class RomajiLyric : INotifyPropertyChanged
    {
        private Vocal _vocal;

        private ParsedLyric _japanese;

        public Vocal Vocal
        {
            get => _vocal;
            set
            {
                if (_vocal != value)
                {
                    _vocal = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ParsedLyric Japanese
        {
            get => _japanese;

            set
            {
                if (_japanese != value)
                {
                    _japanese = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public RomajiLyric(RomajiLyric other)
        {
            Vocal = new Vocal(other.Vocal);
            if (other.Japanese != null)
                Japanese = other.Japanese;
        }

        public RomajiLyric()
        { }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
