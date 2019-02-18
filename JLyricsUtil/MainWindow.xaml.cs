using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XmlUtils;

namespace JLyricsUtil
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<LyricLine> LyricLines = new ObservableCollection<LyricLine>();
        private readonly Stack<IUndoableAction> UndoStack = new Stack<IUndoableAction>();

        private readonly Regex openingBracketRegex = new Regex("[(〈《«“〔「｢『【 ]", RegexOptions.Compiled);
        private readonly Regex combiningKanaRegex = new Regex("[ゃゅょャュョァィゥェォ]", RegexOptions.Compiled); // TODO: Add small hiragana?

        private List<ParsedLyric> SymbolsAndWords = new List<ParsedLyric>();

        private ListBox SelectedListBox;

        public MainWindow()
        {
            InitializeComponent();

            itemsCtrl.ItemsSource = LyricLines;
        }

        private void ParseCharacters()
        {
            if (tbJapanese.Text.Length == 0)
                return;

            var oldSymbolList = SymbolsAndWords;
            int oldSymbolIndex = -1;
            SymbolsAndWords = new List<ParsedLyric>();

            if (oldSymbolList.Count > 0)
            {
                oldSymbolIndex = 0;
                var undo = new UndoAction(() => SymbolsAndWords = oldSymbolList);
                UndoStack.Push(undo);
            }

            var buffer = new StringBuilder();
            bool isOpeningQuote = true;

            string textToParse = tbJapanese.Text;

            for (int i = 0; i < textToParse.Length; i++)
            {
                char currentChar = textToParse[i];

                if (char.IsWhiteSpace(currentChar))
                    continue;

                if (i < textToParse.Length - 1)
                {
                    char nextChar = textToParse[i + 1];

                    // If an opening bracket, add to buffer and move to next character
                    if (openingBracketRegex.IsMatch(currentChar.ToString()))
                    {
                        buffer.Append(currentChar);
                        continue;
                    }
                    else if (currentChar == '"' && isOpeningQuote)
                    {
                        buffer.Append(currentChar);
                        isOpeningQuote = false;
                        continue;
                    }
                    // If a common latin character
                    else if (currentChar.IsCommonLatinCharacter())
                    {
                        if (currentChar == '"' && !isOpeningQuote)
                            isOpeningQuote = true;

                        buffer.Append(currentChar);

                        bool oldSymbolPresent = oldSymbolIndex != -1 && oldSymbolIndex < oldSymbolList.Count;
                        bool oldSymbolWasCombined = oldSymbolPresent && oldSymbolList[oldSymbolIndex].WasCombined;
                        bool oldSymbolFirstCharSame = oldSymbolWasCombined && oldSymbolList[oldSymbolIndex].FirstCharacter() == currentChar;

                        // Keep adding to the buffer unless the next character is whitespace or not a latin character
                        // Also stop adding if the current parsed lyric in the old list was combined,
                        // And its first character is the same as this one
                        if (!char.IsWhiteSpace(nextChar)
                            && nextChar.IsCommonLatinCharacter()
                            && !oldSymbolFirstCharSame)
                        {
                            continue;
                        }
                    }
                    // Combine kana combining forms
                    else if (combiningKanaRegex.IsMatch(nextChar.ToString()))
                    {
                        buffer.Append(currentChar);
                        buffer.Append(nextChar);
                        i++;
                    }
                    else if (nextChar == '"' && isOpeningQuote)
                    {
                        // Do nothing
                    }
                    // Add all following punctuation to the current buffer, except for opening brackets
                    else if (char.IsPunctuation(nextChar) && !openingBracketRegex.IsMatch(nextChar.ToString()))
                    {
                        buffer.Append(currentChar);
                        while (char.IsPunctuation(nextChar))
                        {
                            // This is a closing double quote so the next one will be an opening quote
                            if (nextChar == '"')
                                isOpeningQuote = true;

                            buffer.Append(nextChar);
                            i++;

                            if (i + 1 == textToParse.Length)
                                break;
                            nextChar = textToParse[i + 1];
                        }
                    }
                    else if (buffer.Length != 0)
                    {
                        buffer.Append(currentChar);
                    }
                }
                // Final character in the text to parse
                else if (buffer.Length != 0)
                {
                    buffer.Append(currentChar);
                }

                string symbolToAdd = buffer.Length > 0 ? buffer.ToString() : currentChar.ToString();
                buffer.Clear();

                if (oldSymbolIndex == -1 || oldSymbolIndex >= oldSymbolList.Count)
                {
                    SymbolsAndWords.Add(new ParsedLyric(symbolToAdd));
                }
                else // Try to find the symbol in the old list
                {
                    var oldSymbol = oldSymbolList[oldSymbolIndex];

                    if (oldSymbol.Content == symbolToAdd)
                    {
                        SymbolsAndWords.Add(oldSymbol);

                        oldSymbolIndex++;
                    }
                    // Preserve combined symbols
                    else if (oldSymbol.WasCombined)
                    {
                        int textIndex = i;

                        // If the symbol starts with a bracket or quote, the text index needs to be moved back
                        if (openingBracketRegex.IsMatch(symbolToAdd) || symbolToAdd[0] == '"')
                        {
                            int k = 0;
                            while (k < symbolToAdd.Length - 1 && char.IsPunctuation(symbolToAdd[k]))
                            {
                                textIndex--;
                                k++;
                            }
                        }

                        // If combining kana are present, index needs to be moved back
                        textIndex -= combiningKanaRegex.Matches(symbolToAdd).Count;

                        int symbolIndex = 0;

                        // Check if the combination is a match with the text
                        while (textToParse[textIndex] == oldSymbol.Content[symbolIndex])
                        {
                            if (++symbolIndex >= oldSymbol.Content.Length)
                                break;
                            if (++textIndex >= textToParse.Length)
                                break;

                            // Ignore any whitespace (line breaks) in the text
                            while (char.IsWhiteSpace(textToParse[textIndex]) && textIndex < textToParse.Length)
                                textIndex++;
                        }

                        if (symbolIndex == oldSymbol.Content.Length)
                        {
                            // Whole combination was matched
                            SymbolsAndWords.Add(oldSymbol);
                            i = textIndex;
                        }

                        oldSymbolIndex++;
                    }
                    else
                    {
                        SymbolsAndWords.Add(new ParsedLyric(symbolToAdd));

                        // Move old symbol index forward if we are at a word that was split into syllables
                        if (oldSymbol.OriginalUnsplit != null)
                        {
                            while (symbolToAdd.Equals(oldSymbol.OriginalUnsplit, StringComparison.OrdinalIgnoreCase))
                            {
                                if (++oldSymbolIndex >= oldSymbolList.Count)
                                    break;

                                oldSymbol = oldSymbolList[oldSymbolIndex];
                            }
                        }
                        else
                        {
                            oldSymbolIndex++;
                        }
                    }
                }
            }
        }

        private bool IsMatch(ParsedLyric word, int lineIndex, int romajiLyricIndex)
        {
            string lyric = LyricLines[lineIndex][romajiLyricIndex].Vocal.Lyric;
            string fullWord = string.Empty;

            // Check if first letter matches
            if (!word.Content.Substring(0, 1).Equals(lyric.Substring(0, 1), StringComparison.OrdinalIgnoreCase))
                return false;

            do
            {
                lyric = LyricLines[lineIndex][romajiLyricIndex].Vocal.Lyric;

                if (lyric.EndsWith("-") || lyric.EndsWith("+"))
                {
                    fullWord += lyric.Substring(0, lyric.Length - 1);
                }
                else
                {
                    fullWord += lyric;
                }

                ++romajiLyricIndex;
            }
            while (lyric.EndsWith("-") && romajiLyricIndex < LyricLines[lineIndex].Count);

            return fullWord.Equals(word.Content, StringComparison.OrdinalIgnoreCase);
        }

        private void MatchRomajiToSymbols()
        {
            if (SymbolsAndWords.Count == 0)
                ParseCharacters();

            int romajiLyricIndex = 0;
            int lineIndex = 0;

            for (int i = 0; i < SymbolsAndWords.Count; i++)
            {
                if (lineIndex == LyricLines.Count)
                    break;

                RomajiLyric romaji = LyricLines[lineIndex][romajiLyricIndex];

                string lyric = romaji.Vocal.Lyric;

                bool prevWasDifferentWord = romajiLyricIndex == 0 || !LyricLines[lineIndex][romajiLyricIndex - 1].Vocal.Lyric.EndsWith("-");
                bool isFirstSyllableOfAWord = prevWasDifferentWord && lyric.EndsWith("-");

                ParsedLyric parsedLyric = SymbolsAndWords[i];

                // Break any parsed words into syllables as they appear in the XML lyrics
                if (isFirstSyllableOfAWord
                    && parsedLyric.FirstCharacterIsLatin()
                    && lyric != parsedLyric.Content
                    && IsMatch(parsedLyric, lineIndex, romajiLyricIndex))
                {
                    // Replace current parsed word with first syllable
                    string unsplitWord = parsedLyric.Content;
                    parsedLyric.Content = lyric;
                    parsedLyric.OriginalUnsplit = unsplitWord;

                    int indexIncrement = 1;
                    string nextLyric;

                    // Insert rest of the syllables of a polysyllabic word into the list
                    do
                    {
                        nextLyric = LyricLines[lineIndex][romajiLyricIndex + indexIncrement].Vocal.Lyric;
                        SymbolsAndWords.Insert(i + indexIncrement, new ParsedLyric(nextLyric) { OriginalUnsplit = unsplitWord });
                        ++indexIncrement;
                    }
                    while (nextLyric.EndsWith("-"));
                }

                romaji.Japanese = parsedLyric;

                // Move to next line
                if (++romajiLyricIndex >= LyricLines[lineIndex].Count)
                {
                    romajiLyricIndex = 0;
                    lineIndex++;
                }
            }

            // Remove any previously matched Japanese from remaining syllables
            while (lineIndex < LyricLines.Count)
            {
                while (romajiLyricIndex < LyricLines[lineIndex].Count)
                {
                    LyricLines[lineIndex][romajiLyricIndex].Japanese = null;
                    romajiLyricIndex++;
                }
                romajiLyricIndex = 0;
                lineIndex++;
            }
        }

        private void ConnectSyllableWithNext_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedListBox?.SelectedItem == null)
                return;

            int selectedIndex = SelectedListBox.SelectedIndex;
            RomajiLyric selected = (RomajiLyric)SelectedListBox.SelectedItem;
            LyricLine selectedLine = (LyricLine)SelectedListBox.ItemsSource;

            if (selected == null || selectedIndex + 1 >= SelectedListBox.Items.Count)
                return;

            Vocal selectedVocal = selected.Vocal;
            RomajiLyric next = (RomajiLyric)SelectedListBox.Items[selectedIndex + 1];

            var undoAction = new UndoConnectSyllables(selectedIndex, new RomajiLyric(selected), next, selectedLine);
            UndoStack.Push(undoAction);

            if (selectedVocal.Lyric.EndsWith("-"))
            {
                selectedVocal.Lyric = selectedVocal.Lyric.Substring(0, selectedVocal.Lyric.Length - 1);
            }

            selectedVocal.Lyric += next.Vocal.Lyric;
            selectedVocal.Length = (float)Math.Round(selectedVocal.Length + next.Vocal.Length, 3, MidpointRounding.AwayFromZero);

            selectedLine.Remove(next);

            MatchRomajiToSymbols();
        }

        private void CombineSymbolsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedListBox?.SelectedItem == null)
                return;

            RomajiLyric selected = (RomajiLyric)SelectedListBox.SelectedItem;
            int index = SymbolsAndWords.FindIndex(s => ReferenceEquals(s, selected.Japanese));
            if (index == -1 || index + 1 >= SymbolsAndWords.Count)
                return;

            var undo = new UndoConnectSymbols(index, SymbolsAndWords[index], SymbolsAndWords[index + 1], SymbolsAndWords);
            UndoStack.Push(undo);

            ParsedLyric combination = selected.Japanese + SymbolsAndWords[index + 1];
            SymbolsAndWords.RemoveAt(index + 1);

            SymbolsAndWords[index] = combination;

            MatchRomajiToSymbols();
        }

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParseCharacters();

                MatchRomajiToSymbols();
            }
            catch (Exception ex)
            {
                MessageBox.Show(":(\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearUndo()
        {
            UndoStack.Clear();
        }

        /*private void FixButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "XML Files|*.xml"
            };
            if (dialog.ShowDialog() == true)
            {
                var vocalsFile = XmlHelper.Deserialize<VocalsFile>(dialog.FileName);

                foreach(Vocal voc in vocalsFile)
                {
                    voc.Time = (float)Math.Round(voc.Time + 7.412f, 3);
                }

                XmlHelper.Serialize(dialog.FileName, vocalsFile);
            }
        }*/

        private void ListBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var oldSelectedListBox = SelectedListBox;

            SelectedListBox = (ListBox)sender;

            if (oldSelectedListBox != null && oldSelectedListBox != SelectedListBox)
                oldSelectedListBox.SelectedIndex = -1;

            e.Handled = true;
        }

        private void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = UndoStack.Count > 0;

        private void UndoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UndoStack.Pop().Undo();
            MatchRomajiToSymbols();
        }

        private void itemsCtrl_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                return;

            switch (e.Key)
            {
                case Key.S:
                    ConnectSyllableWithNext_Click(null, null);

                    e.Handled = true;
                    break;

                case Key.D:
                    CombineSymbolsMenuItem_Click(null, null);

                    e.Handled = true;
                    break;
            }
        }

        private void CleanParseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearUndo();
                SymbolsAndWords.Clear();

                ParseCharacters();
                MatchRomajiToSymbols();
            }
            catch (Exception ex)
            {
                MessageBox.Show(":(\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = true;

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "XML Files|*.xml"
            };

            try
            {
                if (dialog.ShowDialog() == true)
                {
                    itemsCtrl.ItemsSource = null;
                    LyricLines.Clear();
                    SymbolsAndWords.Clear();
                    ClearUndo();

                    LyricLine line = new LyricLine();
                    var vocals = XmlHelper.Deserialize<VocalsFile>(dialog.FileName);

                    foreach (Vocal voc in vocals)
                    {
                        line.Add(new RomajiLyric { Vocal = voc });
                        if (voc.Lyric.EndsWith("+"))
                        {
                            LyricLines.Add(line);
                            line = new LyricLine();
                        }
                    }

                    itemsCtrl.ItemsSource = LyricLines;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening file: " + Environment.NewLine + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = LyricLines.Count > 0;

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "XML Files|*.xml",
                AddExtension = true,
                FileName = "PART JVOCALS_RS2.xml"
            };

            try
            {
                if (dialog.ShowDialog() == true)
                {
                    VocalsFile vocals = new VocalsFile();
                    foreach (LyricLine line in LyricLines)
                    {
                        foreach (RomajiLyric lyric in line)
                        {
                            Vocal currentVocal = lyric.Vocal;
                            string romaji = lyric.Vocal.Lyric;
                            string japanese = lyric.Japanese?.Content;

                            // Use the original file contents for any unmatched syllables
                            if (string.IsNullOrEmpty(japanese))
                            {
                                japanese = romaji;
                            }
                            // Connect all Japanese characters, but skip words already in romaji
                            else if (!romaji.Equals(japanese, StringComparison.OrdinalIgnoreCase))
                            {
                                if (romaji.EndsWith("+"))
                                    japanese += "+";
                                else
                                    japanese += "-";
                            }

                            vocals.Add(new Vocal
                            {
                                Length = currentVocal.Length,
                                Lyric = japanese,
                                Note = currentVocal.Note,
                                Time = currentVocal.Time
                            });
                        }
                    }

                    XmlHelper.Serialize(dialog.FileName, vocals);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Saving file failed: " + Environment.NewLine + ex.Message,
                    "Error Saving File",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
