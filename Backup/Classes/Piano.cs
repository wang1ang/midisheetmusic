/*
 * Copyright (c) 2009-2012 Madhav Vaidyanathan
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
using System.Drawing.Printing;
using System.Windows.Forms;

namespace MidiSheetMusic {


/** @class Piano
 *
 * The Piano Control is the panel at the top that displays the
 * piano, and highlights the piano notes during playback.
 * The main methods are:
 *
 * SetMidiFile() - Set the Midi file to use for shading.  The Midi file
 *                 is needed to determine which notes to shade.
 *
 * ShadeNotes() - Shade notes on the piano that occur at a given pulse time.
 *
 */
public class Piano : Control {
    public const int KeysPerOctave = 7;
    public const int MaxOctave = 7;

    private static int WhiteKeyWidth;  /** Width of a single white key */
    private static int WhiteKeyHeight; /** Height of a single white key */
    private static int BlackKeyWidth;  /** Width of a single black key */
    private static int BlackKeyHeight; /** Height of a single black key */
    private static int margin;         /** The top/left margin to the piano */
    private static int BlackBorder;    /** The width of the black border around the keys */

    private static int[] blackKeyOffsets;   /** The x pixles of the black keys */

    /* The gray1Pens for drawing black/gray lines */
    private Pen gray1Pen, gray2Pen, gray3Pen;

    /* The brushes for filling the keys */
    private Brush gray1Brush, gray2Brush, shadeBrush, shade2Brush;

    private bool useTwoColors;              /** If true, use two colors for highlighting */
    private List<MidiNote> notes;           /** The Midi notes for shading */
    private int maxShadeDuration;           /** The maximum duration we'll shade a note for */
    private int showNoteLetters;            /** Display the letter for each piano note */
    private Graphics graphics;              /** The graphics for shading the notes */

    /** Create a new Piano. */
    public Piano() {
        int screenwidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        if (screenwidth >= 3200) {
            /* Linux/Mono is reporting width of 4 screens */
            screenwidth = screenwidth / 4;
        }
        screenwidth = screenwidth * 95/100;
        WhiteKeyWidth = (int)(screenwidth / (2.0 + KeysPerOctave * MaxOctave));
        if (WhiteKeyWidth % 2 != 0) {
            WhiteKeyWidth--;
        }
        margin = 0;
        BlackBorder = WhiteKeyWidth/2;
        WhiteKeyHeight = WhiteKeyWidth * 5;
        BlackKeyWidth = WhiteKeyWidth / 2;
        BlackKeyHeight = WhiteKeyHeight * 5 / 9; 

        Width = margin*2 + BlackBorder*2 + WhiteKeyWidth * KeysPerOctave * MaxOctave;
        Height = margin*2 + BlackBorder*3 + WhiteKeyHeight;
        if (blackKeyOffsets == null) {
            blackKeyOffsets = new int[] { 
                WhiteKeyWidth - BlackKeyWidth/2 - 1,
                WhiteKeyWidth + BlackKeyWidth/2 - 1,
                2*WhiteKeyWidth - BlackKeyWidth/2,
                2*WhiteKeyWidth + BlackKeyWidth/2,
                4*WhiteKeyWidth - BlackKeyWidth/2 - 1,
                4*WhiteKeyWidth + BlackKeyWidth/2 - 1,
                5*WhiteKeyWidth - BlackKeyWidth/2,
                5*WhiteKeyWidth + BlackKeyWidth/2,
                6*WhiteKeyWidth - BlackKeyWidth/2,
                6*WhiteKeyWidth + BlackKeyWidth/2
           };
        }
        Color gray1 = Color.FromArgb(16, 16, 16);
        Color gray2 = Color.FromArgb(90, 90, 90);
        Color gray3 = Color.FromArgb(200, 200, 200);
        Color shade1 = Color.FromArgb(210, 205, 220);
        Color shade2 = Color.FromArgb(150, 200, 220);

        gray1Pen = new Pen(gray1, 1);
        gray2Pen = new Pen(gray2, 1);
        gray3Pen = new Pen(gray3, 1);

        gray1Brush = new SolidBrush(gray1);
        gray2Brush = new SolidBrush(gray2);
        shadeBrush = new SolidBrush(shade1);
        shade2Brush = new SolidBrush(shade2);
        showNoteLetters = MidiOptions.NoteNameNone;
        BackColor = Color.LightGray;
    }

    /** Set the MidiFile to use.
     *  Save the list of midi notes. Each midi note includes the note Number 
     *  and StartTime (in pulses), so we know which notes to shade given the
     *  current pulse time.
     */ 
    public void SetMidiFile(MidiFile midifile, MidiOptions options) {
        if (midifile == null) {
            notes = null;
            useTwoColors = false;
            return;
        }

        List<MidiTrack> tracks = midifile.ChangeMidiNotes(options);
        MidiTrack track = MidiFile.CombineToSingleTrack(tracks);
        notes = track.Notes;

        maxShadeDuration = midifile.Time.Quarter * 2;

        /* We want to know which track the note came from.
         * Use the 'channel' field to store the track.
         */
        for (int tracknum = 0; tracknum < tracks.Count; tracknum++) {
            foreach (MidiNote note in tracks[tracknum].Notes) {
                note.Channel = tracknum;
            }
        }

        /* When we have exactly two tracks, we assume this is a piano song,
         * and we use different colors for highlighting the left hand and
         * right hand notes.
         */
        useTwoColors = false;
        if (tracks.Count == 2) {
            useTwoColors = true;
        }

        showNoteLetters = options.showNoteLetters;
        this.Invalidate();
    }

    /** Set the colors to use for shading */
    public void SetShadeColors(Color c1, Color c2) {
        shadeBrush.Dispose();
        shade2Brush.Dispose();
        shadeBrush = new SolidBrush(c1);
        shade2Brush = new SolidBrush(c2);
    }

    /** Draw the outline of a 12-note (7 white note) piano octave */
    private void DrawOctaveOutline(Graphics g) {
        int right = WhiteKeyWidth * KeysPerOctave;

        // Draw the bounding rectangle, from C to B
        g.DrawLine(gray1Pen, 0, 0, 0, WhiteKeyHeight);
        g.DrawLine(gray1Pen, right, 0, right, WhiteKeyHeight);
        // g.DrawLine(gray1Pen, 0, 0, right, 0);
        g.DrawLine(gray1Pen, 0, WhiteKeyHeight, right, WhiteKeyHeight);
        g.DrawLine(gray3Pen, right-1, 0, right-1, WhiteKeyHeight);
        g.DrawLine(gray3Pen, 1, 0, 1, WhiteKeyHeight);

        // Draw the line between E and F
        g.DrawLine(gray1Pen, 3*WhiteKeyWidth, 0, 3*WhiteKeyWidth, WhiteKeyHeight);
        g.DrawLine(gray3Pen, 3*WhiteKeyWidth - 1, 0, 3*WhiteKeyWidth - 1, WhiteKeyHeight);
        g.DrawLine(gray3Pen, 3*WhiteKeyWidth + 1, 0, 3*WhiteKeyWidth + 1, WhiteKeyHeight);

        // Draw the sides/bottom of the black keys
        for (int i =0; i < 10; i += 2) {
            int x1 = blackKeyOffsets[i];
            int x2 = blackKeyOffsets[i+1];

            g.DrawLine(gray1Pen, x1, 0, x1, BlackKeyHeight); 
            g.DrawLine(gray1Pen, x2, 0, x2, BlackKeyHeight); 
            g.DrawLine(gray1Pen, x1, BlackKeyHeight, x2, BlackKeyHeight);
            g.DrawLine(gray2Pen, x1-1, 0, x1-1, BlackKeyHeight+1); 
            g.DrawLine(gray2Pen, x2+1, 0, x2+1, BlackKeyHeight+1); 
            g.DrawLine(gray2Pen, x1-1, BlackKeyHeight+1, x2+1, BlackKeyHeight+1);
            g.DrawLine(gray3Pen, x1-2, 0, x1-2, BlackKeyHeight+2); 
            g.DrawLine(gray3Pen, x2+2, 0, x2+2, BlackKeyHeight+2); 
            g.DrawLine(gray3Pen, x1-2, BlackKeyHeight+2, x2+2, BlackKeyHeight+2);
        }

        // Draw the bottom-half of the white keys
        for (int i = 1; i < KeysPerOctave; i++) {
            if (i == 3) {
                continue;  // we draw the line between E and F above
            }
            g.DrawLine(gray1Pen, i*WhiteKeyWidth, BlackKeyHeight, i*WhiteKeyWidth, WhiteKeyHeight);
            Pen pen1 = gray2Pen;
            Pen pen2 = gray3Pen;
            g.DrawLine(pen1, i*WhiteKeyWidth - 1, BlackKeyHeight+1, i*WhiteKeyWidth - 1, WhiteKeyHeight);
            g.DrawLine(pen2, i*WhiteKeyWidth + 1, BlackKeyHeight+1, i*WhiteKeyWidth + 1, WhiteKeyHeight);
        }

    }

    /** Draw an outline of the piano for 7 octaves */
    private void DrawOutline(Graphics g) {
        for (int octave = 0; octave < MaxOctave; octave++) {
            g.TranslateTransform(octave * WhiteKeyWidth * KeysPerOctave, 0);
            DrawOctaveOutline(g);
            g.TranslateTransform(-(octave * WhiteKeyWidth * KeysPerOctave), 0);
        }
    }
 
    /* Draw the Black keys */
    private void DrawBlackKeys(Graphics g) {
        for (int octave = 0; octave < MaxOctave; octave++) {
            g.TranslateTransform(octave * WhiteKeyWidth * KeysPerOctave, 0);
            for (int i = 0; i < 10; i += 2) {
                int x1 = blackKeyOffsets[i];
                int x2 = blackKeyOffsets[i+1];
                g.FillRectangle(gray1Brush, x1, 0, BlackKeyWidth, BlackKeyHeight);
                g.FillRectangle(gray2Brush, x1+1, BlackKeyHeight - BlackKeyHeight/8, 
                                BlackKeyWidth-2, BlackKeyHeight/8);
            }
            g.TranslateTransform(-(octave * WhiteKeyWidth * KeysPerOctave), 0);
        }
    }

    /* Draw the black border area surrounding the piano keys.
     * Also, draw gray outlines at the bottom of the white keys.
     */
    private void DrawBlackBorder(Graphics g) {
        int PianoWidth = WhiteKeyWidth * KeysPerOctave * MaxOctave;
        g.FillRectangle(gray1Brush, margin, margin, PianoWidth + BlackBorder*2, BlackBorder-2);
        g.FillRectangle(gray1Brush, margin, margin, BlackBorder, WhiteKeyHeight + BlackBorder * 3);
        g.FillRectangle(gray1Brush, margin, margin + BlackBorder + WhiteKeyHeight, 
                                    BlackBorder*2 + PianoWidth, BlackBorder*2);
        g.FillRectangle(gray1Brush, margin + BlackBorder + PianoWidth, margin, 
                                    BlackBorder, WhiteKeyHeight + BlackBorder*3);

        g.DrawLine(gray2Pen, margin + BlackBorder, margin + BlackBorder -1, 
                             margin + BlackBorder + PianoWidth, margin + BlackBorder -1);
        
        g.TranslateTransform(margin + BlackBorder, margin + BlackBorder); 

        // Draw the gray bottoms of the white keys  
        for (int i = 0; i < KeysPerOctave * MaxOctave; i++) {
            g.FillRectangle(gray2Brush, i*WhiteKeyWidth+1, WhiteKeyHeight+2,
                             WhiteKeyWidth-2, BlackBorder/2);
        }
        g.TranslateTransform(-(margin + BlackBorder), -(margin + BlackBorder)); 
    }

    /** Draw the note letters underneath each white note */
    private void DrawNoteLetters(Graphics g) {
        string[] letters = { "C", "D", "E", "F", "G", "A", "B" };
        string[] numbers = { "1", "3", "5", "6", "8", "10", "12" };
        string[] names;
        if (showNoteLetters == MidiOptions.NoteNameLetter) {
            names = letters;
        }
        else if (showNoteLetters == MidiOptions.NoteNameFixedNumber) {
            names = numbers;
        }
        else {
            return;
        }
        g.TranslateTransform(margin + BlackBorder, margin + BlackBorder); 
        for (int octave = 0; octave < MaxOctave; octave++) {
            for (int i = 0; i < KeysPerOctave; i++) {
                g.DrawString(names[i], SheetMusic.LetterFont, Brushes.White,
                             (octave*KeysPerOctave + i) * WhiteKeyWidth + WhiteKeyWidth/3,
                             WhiteKeyHeight + BlackBorder * 3/4);
            }
        }
        g.TranslateTransform(-(margin + BlackBorder), -(margin + BlackBorder)); 
    }

    /** Draw the Piano. */
    protected override void OnPaint(PaintEventArgs e) {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        g.TranslateTransform(margin + BlackBorder, margin + BlackBorder); 
        g.FillRectangle(Brushes.White, 0, 0, 
                        WhiteKeyWidth * KeysPerOctave * MaxOctave, WhiteKeyHeight);
        DrawBlackKeys(g);
        DrawOutline(g);
        g.TranslateTransform(-(margin + BlackBorder), -(margin + BlackBorder));
        DrawBlackBorder(g);
        if (showNoteLetters != MidiOptions.NoteNameNone) {
            DrawNoteLetters(g);
        }
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    }

    /* Shade the given note with the given brush.
     * We only draw notes from notenumber 24 to 96.
     * (Middle-C is 60).
     */
    private void ShadeOneNote(Graphics g, int notenumber, Brush brush) {
        int octave = notenumber / 12;
        int notescale = notenumber % 12;

        octave -= 2;
        if (octave < 0 || octave >= MaxOctave)
            return;

        g.TranslateTransform(octave * WhiteKeyWidth * KeysPerOctave, 0);
        int x1, x2, x3;

        int bottomHalfHeight = WhiteKeyHeight - (BlackKeyHeight+3);

        /* notescale goes from 0 to 11, from C to B. */
        switch (notescale) {
        case 0: /* C */
            x1 = 2;
            x2 = blackKeyOffsets[0] - 2;
            g.FillRectangle(brush, x1, 0, x2 - x1, BlackKeyHeight+3);
            g.FillRectangle(brush, x1, BlackKeyHeight+3, WhiteKeyWidth-3, bottomHalfHeight);
            break;
        case 1: /* C# */
            x1 = blackKeyOffsets[0]; 
            x2 = blackKeyOffsets[1];
            g.FillRectangle(brush, x1, 0, x2 - x1, BlackKeyHeight);
            if (brush == gray1Brush) {
                g.FillRectangle(gray2Brush, x1+1, 
                                BlackKeyHeight - BlackKeyHeight/8, 
                                BlackKeyWidth-2, BlackKeyHeight/8);
            }
            break;
        case 2: /* D */
            x1 = WhiteKeyWidth + 2;
            x2 = blackKeyOffsets[1] + 3;
            x3 = blackKeyOffsets[2] - 2; 
            g.FillRectangle(brush, x2, 0, x3 - x2, BlackKeyHeight+3);
            g.FillRectangle(brush, x1, BlackKeyHeight+3, WhiteKeyWidth-3, bottomHalfHeight);
            break;
        case 3: /* D# */
            x1 = blackKeyOffsets[2]; 
            x2 = blackKeyOffsets[3];
            g.FillRectangle(brush, x1, 0, BlackKeyWidth, BlackKeyHeight);
            if (brush == gray1Brush) {
                g.FillRectangle(gray2Brush, x1+1, 
                                BlackKeyHeight - BlackKeyHeight/8, 
                                BlackKeyWidth-2, BlackKeyHeight/8);
            }
            break;
        case 4: /* E */
            x1 = WhiteKeyWidth * 2 + 2;
            x2 = blackKeyOffsets[3] + 3; 
            x3 = WhiteKeyWidth * 3 - 1;
            g.FillRectangle(brush, x2, 0, x3 - x2, BlackKeyHeight+3);
            g.FillRectangle(brush, x1, BlackKeyHeight+3, WhiteKeyWidth-3, bottomHalfHeight);
            break;
        case 5: /* F */
            x1 = WhiteKeyWidth * 3 + 2;
            x2 = blackKeyOffsets[4] - 2; 
            x3 = WhiteKeyWidth * 4 - 2;
            g.FillRectangle(brush, x1, 0, x2 - x1, BlackKeyHeight+3);
            g.FillRectangle(brush, x1, BlackKeyHeight+3, WhiteKeyWidth-3, bottomHalfHeight);
            break;
        case 6: /* F# */
            x1 = blackKeyOffsets[4]; 
            x2 = blackKeyOffsets[5];
            g.FillRectangle(brush, x1, 0, BlackKeyWidth, BlackKeyHeight);
            if (brush == gray1Brush) {
                g.FillRectangle(gray2Brush, x1+1, 
                                BlackKeyHeight - BlackKeyHeight/8, 
                                BlackKeyWidth-2, BlackKeyHeight/8);
            }
            break;
        case 7: /* G */
            x1 = WhiteKeyWidth * 4 + 2;
            x2 = blackKeyOffsets[5] + 3; 
            x3 = blackKeyOffsets[6] - 2; 
            g.FillRectangle(brush, x2, 0, x3 - x2, BlackKeyHeight+3);
            g.FillRectangle(brush, x1, BlackKeyHeight+3, WhiteKeyWidth-3, bottomHalfHeight);
            break;
        case 8: /* G# */
            x1 = blackKeyOffsets[6]; 
            x2 = blackKeyOffsets[7];
            g.FillRectangle(brush, x1, 0, BlackKeyWidth, BlackKeyHeight);
            if (brush == gray1Brush) {
                g.FillRectangle(gray2Brush, x1+1, 
                                BlackKeyHeight - BlackKeyHeight/8, 
                                BlackKeyWidth-2, BlackKeyHeight/8);
            }
            break;
        case 9: /* A */
            x1 = WhiteKeyWidth * 5 + 2;
            x2 = blackKeyOffsets[7] + 3; 
            x3 = blackKeyOffsets[8] - 2; 
            g.FillRectangle(brush, x2, 0, x3 - x2, BlackKeyHeight+3);
            g.FillRectangle(brush, x1, BlackKeyHeight+3, WhiteKeyWidth-3, bottomHalfHeight);
            break;
        case 10: /* A# */
            x1 = blackKeyOffsets[8]; 
            x2 = blackKeyOffsets[9];
            g.FillRectangle(brush, x1, 0, BlackKeyWidth, BlackKeyHeight);
            if (brush == gray1Brush) {
                g.FillRectangle(gray2Brush, x1+1, 
                                BlackKeyHeight - BlackKeyHeight/8, 
                                BlackKeyWidth-2, BlackKeyHeight/8);
            }
            break;
        case 11: /* B */
            x1 = WhiteKeyWidth * 6 + 2;
            x2 = blackKeyOffsets[9] + 3; 
            x3 = WhiteKeyWidth * KeysPerOctave - 1;
            g.FillRectangle(brush, x2, 0, x3 - x2, BlackKeyHeight+3);
            g.FillRectangle(brush, x1, BlackKeyHeight+3, WhiteKeyWidth-3, bottomHalfHeight);
            break;
        default:
            break;
        }
        g.TranslateTransform(-(octave * WhiteKeyWidth * KeysPerOctave), 0);
    }

    /** Find the MidiNote with the startTime closest to the given time.
     *  Return the index of the note.  Use a binary search method.
     */
    private int FindClosestStartTime(int pulseTime) {
        int left = 0;
        int right = notes.Count-1;

        while (right - left > 1) {
            int i = (right + left)/2;
            if (notes[left].StartTime == pulseTime)
                break;
            else if (notes[i].StartTime <= pulseTime)
                left = i;
            else
                right = i;
        }
        while (left >= 1 && (notes[left-1].StartTime == notes[left].StartTime)) {
            left--;
        }
        return left;
    }

    /** Return the next StartTime that occurs after the MidiNote
     *  at offset i, that is also in the same track/channel.
     */
    private int NextStartTimeSameTrack(int i) {
        int start = notes[i].StartTime;
        int end = notes[i].EndTime;
        int track = notes[i].Channel;

        while (i < notes.Count) {
            if (notes[i].Channel != track) {
                i++;
                continue;
            }
            if (notes[i].StartTime > start) {
                return notes[i].StartTime;
            }
            end = Math.Max(end, notes[i].EndTime);
            i++;
        }
        return end;
    }


    /** Return the next StartTime that occurs after the MidiNote
     *  at offset i.  If all the subsequent notes have the same
     *  StartTime, then return the largest EndTime.
     */
    private int NextStartTime(int i) {
        int start = notes[i].StartTime;
        int end = notes[i].EndTime;

        while (i < notes.Count) {
            if (notes[i].StartTime > start) {
                return notes[i].StartTime;
            }
            end = Math.Max(end, notes[i].EndTime);
            i++;
        }
        return end;
    }

    /** Find the Midi notes that occur in the current time.
     *  Shade those notes on the piano displayed.
     *  Un-shade the those notes played in the previous time.
     */
    public void ShadeNotes(int currentPulseTime, int prevPulseTime) {
        if (notes == null || notes.Count == 0) {
            return;
        }
        if (graphics == null) {
            graphics = CreateGraphics();
        }
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        graphics.TranslateTransform(margin + BlackBorder, margin + BlackBorder);

        /* Loop through the Midi notes.
         * Unshade notes where StartTime <= prevPulseTime < next StartTime
         * Shade notes where StartTime <= currentPulseTime < next StartTime
         */
        int lastShadedIndex = FindClosestStartTime(prevPulseTime - maxShadeDuration * 2);
        for (int i = lastShadedIndex; i < notes.Count; i++) {
            int start = notes[i].StartTime;
            int end = notes[i].EndTime;
            int notenumber = notes[i].Number;
            int nextStart = NextStartTime(i);
            int nextStartTrack = NextStartTimeSameTrack(i);
            end = Math.Max(end, nextStartTrack);
            end = Math.Min(end, start + maxShadeDuration-1);
                
            /* If we've past the previous and current times, we're done. */
            if ((start > prevPulseTime) && (start > currentPulseTime)) {
                break;
            }

            /* If shaded notes are the same, we're done */
            if ((start <= currentPulseTime) && (currentPulseTime < nextStart) &&
                (currentPulseTime < end) && 
                (start <= prevPulseTime) && (prevPulseTime < nextStart) &&
                (prevPulseTime < end)) {
                break;
            }

            /* If the note is in the current time, shade it */
            if ((start <= currentPulseTime) && (currentPulseTime < end)) {
                if (useTwoColors) {
                    if (notes[i].Channel == 1) {
                        ShadeOneNote(graphics, notenumber, shade2Brush);
                    }
                    else {
                        ShadeOneNote(graphics, notenumber, shadeBrush);
                    }
                }
                else {
                    ShadeOneNote(graphics, notenumber, shadeBrush);
                }
            }

            /* If the note is in the previous time, un-shade it, draw it white. */
            else if ((start <= prevPulseTime) && (prevPulseTime < end)) {
                int num = notenumber % 12;
                if (num == 1 || num == 3 || num == 6 || num == 8 || num == 10) {
                    ShadeOneNote(graphics, notenumber, gray1Brush);
                }
                else {
                    ShadeOneNote(graphics, notenumber, Brushes.White);
                }
            }
        }
        graphics.TranslateTransform(-(margin + BlackBorder), -(margin + BlackBorder));
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    }
}

}
