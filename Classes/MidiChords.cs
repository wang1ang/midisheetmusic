using System;
using System.Collections.Generic;
using System.Text;

namespace MidiSheetMusic
{
    using Dict = Dictionary<string, double>;
    using PairList = List<KeyValuePair<string, double>>;
    class Beat
    {
        //string[] NoteNameSharp = { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };
        //string[] NoteNameFlat = { "A", "Bb", "B", "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab" };
        public string[] NoteName = { "L1", "L2", "L3", "L4", "L5", "L6", "L7", "L8", "L9", "LA", "LB", "LC" };
        List<int> notes = new List<int>();
        List<bool> hold = new List<bool>();
        Beat l, r;
        public void AddNote(int number, int start, int end, int length)
        {
            if (start - 1 < 0 && end + 1 > length)
            {
                notes.Add(number);
                hold.Add(start < 0);
            }
            else 
            {
                if (start < length / 2)
                {
                    if (l == null)
                        l = new Beat();
                    l.AddNote(number, start, end, length / 2);
                }
                if (end > length / 2)
                {
                    if (r == null)
                        r = new Beat();
                    r.AddNote(number, start - length / 2, end - length / 2, length / 2);
                }

            }

        }
        public PairList GetChords()
        {
            Dict chord = new Dict();
            //string[] NoteName = NoteNameSharp;

            for (int i = 0; i < notes.Count; i++)
            {
                int note = notes[i];
                double decay = hold[i] ? 0.5 : 1;
                int scale;
                // Major
                scale = NoteScale.FromNumber(note); // as 1th
                Acc(chord, NoteName[scale], decay / 3);
                scale = NoteScale.FromNumber(note - 4); // as 3th
                Acc(chord, NoteName[scale], decay / 3);
                scale = NoteScale.FromNumber(note - 7); // as 5th
                Acc(chord, NoteName[scale], decay / 3);
                // Minor
                scale = NoteScale.FromNumber(note); // as 1th
                Acc(chord, NoteName[scale] + "m", decay / 3);
                scale = NoteScale.FromNumber(note - 3); // as 3th
                Acc(chord, NoteName[scale] + "m", decay / 3);
                scale = NoteScale.FromNumber(note - 7); // as 5th
                Acc(chord, NoteName[scale] + "m", decay / 3);
                // 7th
                scale = NoteScale.FromNumber(note); // as 1th
                Acc(chord, NoteName[scale] + "7", decay / 4);
                scale = NoteScale.FromNumber(note - 4); // as 3th
                Acc(chord, NoteName[scale] + "7", decay / 4);
                scale = NoteScale.FromNumber(note - 7); // as 5th
                Acc(chord, NoteName[scale] + "7", decay / 4);
                scale = NoteScale.FromNumber(note - 10); // as 7th
                Acc(chord, NoteName[scale] + "7", decay / 4);
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
            ret.Sort((a,b)=>(b.Value.CompareTo(a.Value)));
            return ret;
        }
        public static void Acc(Dict chord, string chordname, double prob)
        {
            if (!chord.ContainsKey(chordname))
            {
                chord.Add(chordname, 0);
            }
            chord[chordname] += prob;
        }
    }
    class Measure
    {
        Beat[] beats;
        public Measure(int nb)
        {
            beats = new Beat[nb];
        }
        public void AddNote(int number, int start, int end, int length)
        {
            int beatlength = length / beats.Length;
            int beat_start = Math.Max(0, start / beatlength);
            int beat_end = Math.Min(beats.Length, (end - 1) / beatlength + 1);
            for (int beat = beat_start; beat < beat_end; beat++)
            {
                if (beats[beat] == null)
                    beats[beat] = new Beat();
                beats[beat].AddNote(number, start - beat_start * beatlength, end - beat_start * beatlength, beatlength);
            }
        }
        public Dict EstimateChords(Dict global_chords)
        {
            //Dict chords = new Dict();
            string ret = "";
            foreach (Beat beat in beats)
            {
                if (beat != null)
                {
                    var chord = beat.GetChords(); // sorted
                    ret = ret + chord[0].Key + " ";
                    foreach (var c in chord)
                    {
                        Beat.Acc(global_chords, c.Key, c.Value);
                    }
                }
            }
            return global_chords;
        }
        public string GetChords(Dict global_chords)
        {
            Dict chords = new Dict();
            string ret = "";
            foreach (Beat beat in beats)
            {
                if (beat != null)
                {
                    var chord = beat.GetChords(); // sorted
                    ret = ret + chord[0].Key + " ";
                    foreach (var c in chord)
                    {
                        Beat.Acc(chords, c.Key, c.Value);
                    }
                }
            }
            var best = new KeyValuePair<string, double>("", -1);
            foreach (var c in chords)
            {
                if (c.Value > best.Value || c.Value == best.Value && global_chords[c.Key] > global_chords[best.Key])
                    best = c;
            }
            ret = ret + best.Key;
            return ret;
        }
    }
    class MidiChords
    {
        string[] NoteNameSharp = { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };
        string[] NoteNameFlat = { "A", "Bb", "B", "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab" };
        string[] NoteNameLevel = { "1", "#1", "2", "#2", "3", "4", "#4", "5", "#5", "6", "#6", "7" };
        string[] NoteName;
        public MidiChords()
        {
            NoteName = NoteNameSharp;
        }
        private TimeSignature timesig;
        private int measurelength;
        private List<Measure> measures = new List<Measure>();
        public void SetQuater(int pulse)
        {
        }
        public void SetTimeSignature(TimeSignature t)
        {
            timesig = t;
            measurelength = t.Quarter * t.Numerator * 4 / t.Denominator;
        }
        private bool flat = false;
        public void SetFlat()
        {
            NoteName = NoteNameFlat;
        }
        public void SetTempo(int tempo)
        {
        }
        public void InsertNote(MidiNote note)
        {
            int measure_start = note.StartTime / measurelength;
            int measure_end = (note.EndTime-1) / measurelength + 1;
            while (measures.Count < measure_end)
                measures.Add(null);
            for (int measure = measure_start; measure < measure_end; measure++)
            {
                if (measures[measure] == null)
                    if (timesig.Numerator % 2 == 0)
                        measures[measure] = new Measure(1);
                    else if (timesig.Numerator % 3 == 0)
                        measures[measure] = new Measure(3);
                    else if (timesig.Numerator % 5 == 0)
                        measures[measure] = new Measure(5);
                    else if (timesig.Numerator % 7 == 0)
                        measures[measure] = new Measure(7);
                    else if (timesig.Numerator % 11 == 0)
                        measures[measure] = new Measure(11);
                int start_in_measure = note.StartTime - measure * measurelength;
                int end_in_measure = note.EndTime - measure * measurelength;
                measures[measure].AddNote(note.Number, start_in_measure, end_in_measure, measurelength);
            }
        }
        public string ToString()
        {
            Dict global_chords = new Dict();
            global_chords.Add("", 0);
            foreach (var m in measures)
                m.EstimateChords(global_chords);
            StringBuilder sb = new StringBuilder();
            foreach (var m in measures)
            {
                sb.AppendLine(m.GetChords(global_chords));
            }
            sb.Replace("L1", NoteName[0]); sb.Replace("L4", NoteName[3]); sb.Replace("L7", NoteName[6]); sb.Replace("LA", NoteName[9]);
            sb.Replace("L2", NoteName[1]); sb.Replace("L5", NoteName[4]); sb.Replace("L8", NoteName[7]); sb.Replace("LB", NoteName[10]);
            sb.Replace("L3", NoteName[2]); sb.Replace("L6", NoteName[5]); sb.Replace("L9", NoteName[8]); sb.Replace("LC", NoteName[11]);

            return sb.ToString();
        }
        public static void Main(string[] arg)
        {
            if (arg.Length == 0)
            {
                Console.WriteLine("Usage: MidiFile <filename>");
                return;
            }

            MidiFile f = new MidiFile(arg[0]);
            var mainkey = GetKeySignature(f.Tracks);
            MidiChords c = new MidiChords();
            c.SetTimeSignature(f.Time);
            if (mainkey.num_flats > 0)
                c.SetFlat();
            foreach (var track in f.Tracks)
            {
                foreach (var note in track.Notes)
                {
                    c.InsertNote(note);
                }
            }
            Console.Write(f.ToString());
            Console.Write(c.ToString());
        }
        private static KeySignature GetKeySignature(List<MidiTrack> tracks)
        {
            List<int> notenums = new List<int>();
            foreach (MidiTrack track in tracks)
            {
                foreach (MidiNote note in track.Notes)
                {
                    notenums.Add(note.Number);
                }
            }
            return KeySignature.Guess(notenums);
        }

    }
}
