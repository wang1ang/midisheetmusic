/*
 * Copyright (c) 2007-2013 Madhav Vaidyanathan
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License version 2.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace MidiSheetMusic {

/** @class MidiOptions
 *
 * The MidiOptions class contains the available options for
 * modifying the sheet music and sound.  These options are
 * collected from the menu/dialog settings, and then are passed
 * to the SheetMusic and MidiPlayer classes.
 */
public class MidiOptions {

    // The possible values for showNoteLetters
    public const int NoteNameNone           = 0;
    public const int NoteNameLetter         = 1;
    public const int NoteNameFixedDoReMi    = 2;
    public const int NoteNameMovableDoReMi  = 3;
    public const int NoteNameFixedNumber    = 4;
    public const int NoteNameMovableNumber  = 5;

    // Sheet Music Options
    public string filename;       /** The full Midi filename */
    public string title;          /** The Midi song title */
    public bool[] tracks;         /** Which tracks to display (true = display) */
    public bool scrollVert;       /** Whether to scroll vertically or horizontally */
    public bool largeNoteSize;    /** Display large or small note sizes */
    public bool twoStaffs;        /** Combine tracks into two staffs ? */
    public int showNoteLetters;     /** Show the name (A, A#, etc) next to the notes */
    public bool showLyrics;       /** Show the lyrics under each note */
    public bool showMeasures;     /** Show the measure numbers for each staff */
    public int shifttime;         /** Shift note starttimes by the given amount */
    public int transpose;         /** Shift note key up/down by given amount */
    public int key;               /** Use the given KeySignature (notescale) */
    public TimeSignature time;    /** Use the given time signature */
    public int combineInterval;   /** Combine notes within given time interval (msec) */
    public Color[] colors;        /** The note colors to use */
    public Color shadeColor;      /** The color to use for shading. */
    public Color shade2Color;     /** The color to use for shading the left hand piano */

    // Sound options
    public bool []mute;            /** Which tracks to mute (true = mute) */
    public int tempo;              /** The tempo, in microseconds per quarter note */
    public int pauseTime;          /** Start the midi music at the given pause time */
    public int[] instruments;      /** The instruments to use per track */
    public bool useDefaultInstruments;  /** If true, don't change instruments */
    public bool playMeasuresInLoop;     /** Play the selected measures in a loop */
    public int playMeasuresInLoopStart; /** Start measure to play in loop */
    public int playMeasuresInLoopEnd;   /** End measure to play in loop */


    public MidiOptions() {
    }

    public MidiOptions(MidiFile midifile) {
        filename = midifile.FileName;
        title = Path.GetFileName(midifile.FileName);
        int numtracks = midifile.Tracks.Count;
        tracks = new bool[numtracks];
        mute =  new bool[numtracks];
        instruments = new int[numtracks];
        for (int i = 0; i < tracks.Length; i++) {
            tracks[i] = true;
            mute[i] = false;
            instruments[i] = midifile.Tracks[i].Instrument;
            if (midifile.Tracks[i].InstrumentName == "Percussion") {
                tracks[i] = false;
                mute[i] = true;
            }
        } 
        useDefaultInstruments = true;
        scrollVert = true;
        largeNoteSize = false;
        if (tracks.Length == 1) {
            twoStaffs = true;
        }
        else {
            twoStaffs = false;
        }
        showNoteLetters = NoteNameNone;
        showLyrics = true;
        showMeasures = false;
        shifttime = 0;
        transpose = 0;
        key = -1;
        time = midifile.Time;
        colors = null;
        shadeColor = Color.FromArgb(210, 205, 220);
        shade2Color = Color.FromArgb(80, 100, 250);
        combineInterval = 40;
        tempo = midifile.Time.Tempo;
        pauseTime = 0;
        playMeasuresInLoop = false; 
        playMeasuresInLoopStart = 0;
        playMeasuresInLoopEnd = midifile.EndTime() / midifile.Time.Measure;
    }

    /* Join the array into a comma separated string */
    static string Join(bool[] values) {
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < values.Length; i++) {
            if (i > 0) {
                result.Append(",");
            }
            result.Append(values[i].ToString()); 
        }
        return result.ToString();
    }

    static string Join(int[] values) {
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < values.Length; i++) {
            if (i > 0) {
                result.Append(",");
            }
            result.Append(values[i].ToString()); 
        }
        return result.ToString();
    }

    static string Join(Color[] values) {
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < values.Length; i++) {
            if (i > 0) {
                result.Append(",");
            }
            result.Append(ColorToString(values[i])); 
        }
        return result.ToString();
    }

    static string ColorToString(Color c) {
        return "" + c.R + " " + c.G + " " + c.B;
    }

    /* Convert this MidiOptions object into a INI formatted section.
     * [title]
     * filename=C:/path/to/file.mid
     * version=2.5
     * tracks=true,false,true
     * mute=true,false,true
     * instruments=24,1,52
     * useDefaultInstruments=true
     * time=3,4,720,10000    // numerator,denominator,quarter,tempo
     * scrollVert=true
     * largeNoteSize=false
     * showLyrics=true
     * twoStaffs=true
     * showNoteLetters=true
     * transpose=-2
     * key=1               // Key signature (NoteScale.N)
     * combineInterval=80
     * showMeasures=true
     * playMeasuresInLoop=false
     * playMeasuresInLoopStart=2
     * playMeasuresInLoopEnd=10
     * shadeColor=244 159 24   // R G B
     * shade2Color=244 159 24   // R G B
     * colors=244 159 24, 253 92 143, ...
     */
    public SectionINI ToINI() {
        SectionINI section = new SectionINI();
        section.Section = title;
        try {
            section.Properties["filename"] = filename;
            section.Properties["version"] = "2.6.0";
            section.Properties["tracks"] = Join(tracks);
            section.Properties["mute"] = Join(mute);
            section.Properties["instruments"] = Join(instruments);
            section.Properties["useDefaultInstruments"] = useDefaultInstruments.ToString();
            if (time != null) {
                int[] values = { time.Numerator, time.Denominator, time.Quarter, time.Tempo };
                section.Properties["time"] = Join(values);
            }
            section.Properties["scrollVert"] = scrollVert.ToString();
            section.Properties["largeNoteSize"] = largeNoteSize.ToString();
            section.Properties["showLyrics"] = showLyrics.ToString();
            section.Properties["twoStaffs"] = twoStaffs.ToString();
            section.Properties["showNoteLetters"] = showNoteLetters.ToString();
            section.Properties["transpose"] = transpose.ToString();
            section.Properties["key"] = key.ToString();
            section.Properties["combineInterval"] = combineInterval.ToString();
            section.Properties["shadeColor"] = ColorToString(shadeColor);
            section.Properties["shade2Color"] = ColorToString(shade2Color);
            if (colors != null) {
                section.Properties["colors"] = Join(colors);
            }
            section.Properties["showMeasures"] = showMeasures.ToString();
            section.Properties["playMeasuresInLoop"] = playMeasuresInLoop.ToString();
            section.Properties["playMeasuresInLoopStart"] = playMeasuresInLoopStart.ToString();
            section.Properties["playMeasuresInLoopEnd"] = playMeasuresInLoopEnd.ToString();
        }
        catch (Exception e) {
        }
        return section;
    }


    /* Initialize a MidiOptions from the section properties of an INI text file */
    public MidiOptions(SectionINI section) {
        title = section.Section;
        filename = section.GetString("filename");
        tracks = section.GetBoolArray("tracks");
        mute = section.GetBoolArray("mute");
        if (mute  != null && section.Properties["version"] == "2.5.0") {
            // MidiSheetMusic 2.5 stored the mute value incorrectly
            for (int i = 0; i < mute.Length; i++) {
                mute[i] = false;
            }
        }

        instruments = section.GetIntArray("instruments");
        int[] timesig = section.GetIntArray("time");
        if (timesig != null && timesig.Length == 4) {
            time = new TimeSignature(timesig[0], timesig[1], timesig[2], timesig[3]);
        }
        useDefaultInstruments = section.GetBool("useDefaultInstruments");
        scrollVert = section.GetBool("scrollVert");
        largeNoteSize = section.GetBool("largeNoteSize");
        showLyrics = section.GetBool("showLyrics");
        twoStaffs = section.GetBool("twoStaffs");
        showNoteLetters = section.GetInt("showNoteLetters");
        transpose = section.GetInt("transpose");
        key = section.GetInt("key");
        combineInterval = section.GetInt("combineInterval");
        showMeasures = section.GetBool("showMeasures");
        playMeasuresInLoop = section.GetBool("playMeasuresInLoop");
        playMeasuresInLoopStart = section.GetInt("playMeasuresInLoopStart");
        playMeasuresInLoopEnd = section.GetInt("playMeasuresInLoopEnd");

        Color color = section.GetColor("shadeColor");
        if (color != Color.White) {
            shadeColor = color;
        }
        color = section.GetColor("shade2Color");
        if (color != Color.White) {
            shade2Color = color;
        }
        colors = section.GetColorArray("colors");
    }

    
    /* Merge in the saved options to this MidiOptions.*/
    public void Merge(MidiOptions saved) {
        if (saved.tracks != null && saved.tracks.Length == tracks.Length) {
            for (int i = 0; i < tracks.Length; i++) {
                tracks[i] = saved.tracks[i];
            }
        }
        if (saved.mute != null && saved.mute.Length == mute.Length) {
            for (int i = 0; i < mute.Length; i++) {
                mute[i] = saved.mute[i];
            }
        }
        if (saved.instruments != null && saved.instruments.Length == instruments.Length) {
            for (int i = 0; i < instruments.Length; i++) {
                instruments[i] = saved.instruments[i];
            }
        }
        if (saved.time != null) {
            time = new TimeSignature(saved.time.Numerator, saved.time.Denominator,
                    saved.time.Quarter, saved.time.Tempo);
        }
        useDefaultInstruments = saved.useDefaultInstruments;
        scrollVert = saved.scrollVert;
        largeNoteSize = saved.largeNoteSize;
        showLyrics = saved.showLyrics;
        twoStaffs = saved.twoStaffs;
        showNoteLetters = saved.showNoteLetters;
        transpose = saved.transpose;
        key = saved.key;
        combineInterval = saved.combineInterval;
        if (saved.shadeColor != Color.White) {
            shadeColor = saved.shadeColor;
        }
        if (saved.shade2Color != Color.White) {
            shade2Color = saved.shade2Color;
        }
        if (saved.colors != null) {
            colors = saved.colors;
        }
        showMeasures = saved.showMeasures;
        playMeasuresInLoop = saved.playMeasuresInLoop;
        playMeasuresInLoopStart = saved.playMeasuresInLoopStart;
        playMeasuresInLoopEnd = saved.playMeasuresInLoopEnd;
    }
}

}


