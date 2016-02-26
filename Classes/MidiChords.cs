using System;
using System.Collections.Generic;
using System.Text;

namespace MidiSheetMusic
{
    using Dict = Dictionary<string, double>;
    using PairList = List<KeyValuePair<string, double>>;
    class Beat
    {
        string[] NoteNameSharp = { "A", "A#", "B", "C", "C#", "D", "D#", "E", "E#", "F", "G", "G#" };
        string[] NoteNameFlat = { "A", "Bb", "B", "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab" };
        int[] notes;
        bool[] hold;
        Beat l, r;
        public PairList GetChords()
        {
            Dict chord = new Dict();
            string[] NoteName = NoteNameSharp;

            for (int i = 0; i < notes.Length; i++)
            {
                int note = notes[i];
                double decay = hold[i] ? 0.5 : 1;
                int scale;
                // Major
                scale = NoteScale.FromNumber(note);
                Acc(chord, NoteName[scale], decay / 3);
                scale = NoteScale.FromNumber(note - 3);
                Acc(chord, NoteName[scale], decay / 3);
                scale = NoteScale.FromNumber(note - 7);
                Acc(chord, NoteName[scale], decay / 3);
                // Minor
                scale = NoteScale.FromNumber(note);
                Acc(chord, NoteName[scale] + "m", decay / 3);
                scale = NoteScale.FromNumber(note - 4);
                Acc(chord, NoteName[scale] + "m", decay / 3);
                scale = NoteScale.FromNumber(note - 7);
                Acc(chord, NoteName[scale] + "m", decay / 3);
                // 7th
                scale = NoteScale.FromNumber(note);
                Acc(chord, NoteName[scale] + "7", decay / 4);
                scale = NoteScale.FromNumber(note - 3);
                Acc(chord, NoteName[scale] + "7", decay / 4);
                scale = NoteScale.FromNumber(note - 6);
                Acc(chord, NoteName[scale] + "m", decay / 4);
                scale = NoteScale.FromNumber(note - 10);
                Acc(chord, NoteName[scale] + "m", decay / 4);
            }
            if (l != null)
            {
                PairList chord_l = l.GetChords();
                foreach (var pair in chord_l)
                {
                    Acc(chord, pair.Key, pair.Value);
                }
            }
            if (r != null)
            {
                PairList chord_r = r.GetChords();
                foreach (var pair in chord_r)
                {
                    Acc(chord, pair.Key, pair.Value / 2);
                }
            }
            List<KeyValuePair<string, double>> ret = new List<KeyValuePair<string, double>>();
            foreach (var c in chord)
            {
                ret.Add(c);
            }
            return ret;
        }
        public void Acc(Dict chord, string chordname, double prob)
        {
            if (!chord.ContainsKey(chordname))
            {
                chord.Add(chordname, 0);
            }
            chord[chordname] += prob;
        }
    }
    class MidiChords
    {
    }
}
