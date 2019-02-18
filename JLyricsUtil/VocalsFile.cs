using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using XmlUtils;

namespace JLyricsUtil
{
    [XmlRoot("vocals")]
    public sealed class VocalsFile : XmlCountList<Vocal>
    {
    }

    [XmlRoot("vocal")]
    public sealed class Vocal : INotifyPropertyChanged
    {
        private string _lyric;
        private float _length;

        [XmlAttribute("time")]
        public string TimeSerialized
        {
            get => Time.ToString("F3", CultureInfo.InvariantCulture);
            set
            {
                Time = float.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        [XmlIgnore]
        public float Time { get; set; }

        [XmlAttribute("note")]
        public int Note { get; set; }

        [XmlAttribute("length")]
        public string LengthSerialized
        {
            get => _length.ToString("F3", CultureInfo.InvariantCulture);
            set
            {
                Length = float.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        [XmlIgnore]
        public float Length
        {
            get => _length;

            set
            {
                if (_length != value)
                {
                    _length = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [XmlAttribute("lyric")]
        public string Lyric
        {
            get => _lyric;

            set
            {
                if (_lyric != value)
                {
                    _lyric = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Vocal(Vocal other)
        {
            Time = other.Time;
            Length = other.Length;
            Note = other.Note;
            Lyric = string.Copy(other.Lyric);
        }

        public Vocal()
        { }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
