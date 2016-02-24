/*
 * Copyright (c) 2007-2011 Madhav Vaidyanathan
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
using System.Drawing;

using NUnit.Framework;
using MidiSheetMusic;


/* Test cases for the MidiFileReader class */
[TestFixture]
public class MidiFileReaderTest {

    /* The filename for storing the temporary midi file */
    const string testfile = "test.mid";

    /* Create a variable-length encoded integer from the given bytes.
     * A varlen integer ends when a byte less than 0x80 (128).
     */
    static int varlen(byte b1, byte b2, byte b3, byte b4) {
        int result = ((b1 & 0x7F) << 21) |
                     ((b2 & 0x7F) << 14) | 
                     ((b3 & 0x7F) << 7)  |
                     (b4 & 0x7F);
        return result;
    }

    /* Write the given data to the test file test.mid */
    static void WriteTestFile(byte[] data) {
        FileStream fileout = File.Open(testfile, FileMode.Create,
                                   FileAccess.Write);
        fileout.Write(data, 0, data.Length);
        fileout.Close();
    }

    [Test]
    /* Test that MidiFileReader.ReadByte() returns the correct
     * byte, and that the file offset is incremented by 1.
     */
    public void TestByte() {
        byte[] data = new byte[] { 10, 20, 30, 40, 50 };
        WriteTestFile(data);
        MidiFileReader reader = new MidiFileReader(testfile);

        int offset = 0;
        foreach (byte b in data) {
            Assert.AreEqual(reader.GetOffset(), offset);
            Assert.AreEqual(reader.Peek(), b);
            Assert.AreEqual(reader.ReadByte(), b);
            offset++;
        }
        File.Delete(testfile);
    }

    /* Test that MidiFileReader.ReadShort() returns the correct
     * unsigned short, and that the file offset is incremented by 2.
     */
    [Test]
    public void TestShort() {
        ushort[] nums = new ushort[] { 200, 3000, 10000, 40000 };
        byte[] data = new byte[nums.Length * 2];
        int index = 0; 
        for (int i = 0; i < nums.Length; i++) {
            data[index]   = (byte)( (nums[i] >> 8) & 0xFF );
            data[index+1] = (byte)( nums[i] & 0xFF );
            index += 2;
        }
        WriteTestFile(data);
        MidiFileReader reader = new MidiFileReader(testfile);

        int offset = 0;
        foreach (ushort u in nums) {
            Assert.AreEqual(reader.GetOffset(), offset);
            Assert.AreEqual(reader.ReadShort(), u);
            offset += 2;
        }
        File.Delete(testfile);
    }
 
    /* Test that MidiFileReader.ReadInt() returns the correct
     * int, and that the file offset is incremented by 4.
     */
    [Test]
    public void TestInt() {
        int[] nums = new int[] { 200, 10000, 80000, 999888777 };
        byte[] data = new byte[nums.Length * 4];
        int index = 0; 
        for (int i = 0; i < nums.Length; i++) {
            data[index]   = (byte)( (nums[i] >> 24) & 0xFF );
            data[index+1] = (byte)( (nums[i] >> 16) & 0xFF );
            data[index+2] = (byte)( (nums[i] >> 8) & 0xFF );
            data[index+3] = (byte)(  nums[i] & 0xFF );
            index += 4;
        }
        WriteTestFile(data);
        MidiFileReader reader = new MidiFileReader(testfile);

        int offset = 0;
        foreach (int x in nums) {
            Assert.AreEqual(reader.GetOffset(), offset);
            Assert.AreEqual(reader.ReadInt(), x);
            offset += 4;
        }
        File.Delete(testfile);
    }

    /* Test that MidiFileReader.ReadVarlen() correctly parses variable
     * length integers.  A variable length int ends when the byte is
     * less than 0x80 (128). 
     */
    [Test]
    public void TestVarlen() {
        byte[] data = new byte[12];

        data[0] = 0x40;

        data[1] = 0x90; 
        data[2] = 0x30;

        data[3] = 0x81;
        data[4] = 0xA5;
        data[5] = 0x10;

        data[6] = 0x81;
        data[7] = 0x84;
        data[8] = 0xBF;
        data[9] = 0x05;

        WriteTestFile(data);
        MidiFileReader reader = new MidiFileReader(testfile);

        int len = varlen(0, 0, 0, data[0]);
        Assert.AreEqual(reader.GetOffset(), 0);
        Assert.AreEqual(reader.ReadVarlen(), len);
        Assert.AreEqual(reader.GetOffset(), 1);

        len = varlen(0, 0, data[1], data[2]);
        Assert.AreEqual(reader.ReadVarlen(), len);
        Assert.AreEqual(reader.GetOffset(), 3);

        len = varlen(0, data[3], data[4], data[5]);
        Assert.AreEqual(reader.ReadVarlen(), len);
        Assert.AreEqual(reader.GetOffset(), 6);

        len = varlen(data[6], data[7], data[8], data[9]);
        Assert.AreEqual(reader.ReadVarlen(), len);
        Assert.AreEqual(reader.GetOffset(), 10);
        
        File.Delete(testfile);
    }

    /* Test that MidiFileReader.ReadASCII() returns the correct
     * ascii chars, and that the file offset is incremented by the
     * length of the chars.
     */
    [Test]
    public void TestAscii() {
        byte[] data = new byte[] { 65, 66, 67, 68, 69, 70 };
        WriteTestFile(data);
        MidiFileReader reader = new MidiFileReader(testfile);
        Assert.AreEqual(reader.GetOffset(), 0);
        Assert.AreEqual(reader.ReadAscii(3), "ABC");
        Assert.AreEqual(reader.GetOffset(), 3);
        Assert.AreEqual(reader.ReadAscii(3), "DEF");
        Assert.AreEqual(reader.GetOffset(), 6);
        File.Delete(testfile);
    }

    /* Test that MidiFileReader.Skip() skips the correct amount
     * of bytes, and that the file offset is incremented by the
     * number of bytes skipped.
     */
    [Test]
    public void TestSkip() {
        byte[] data = new byte[] { 65, 66, 67, 68, 69, 70, 71 };
        WriteTestFile(data);
        MidiFileReader reader = new MidiFileReader(testfile);
        Assert.AreEqual(reader.GetOffset(), 0);
        reader.Skip(3);
        Assert.AreEqual(reader.GetOffset(), 3);
        Assert.AreEqual(reader.ReadByte(), 68);
        reader.Skip(2);
        Assert.AreEqual(reader.GetOffset(), 6);
        Assert.AreEqual(reader.ReadByte(), 71);
        Assert.AreEqual(reader.GetOffset(), 7);
        File.Delete(testfile);
    }
}

/* The test cases for the MidiFile class */
[TestFixture]
public class MidiFileTest {

    const string testfile = "test.mid";

    /* The list of Midi Events */
    const byte EventNoteOff         = 0x80;
    const byte EventNoteOn          = 0x90;
    const byte EventKeyPressure     = 0xA0;
    const byte EventControlChange   = 0xB0;
    const byte EventProgramChange   = 0xC0;
    const byte EventChannelPressure = 0xD0;
    const byte EventPitchBend       = 0xE0;
    const byte SysexEvent1          = 0xF0;
    const byte SysexEvent2          = 0xF7;
    const byte MetaEvent            = 0xFF;

    /* The list of Meta Events */
    const byte MetaEventSequence    = 0x0;
    const byte MetaEventKeySignature = 0x59;
    const byte MetaEventTempo        = 0x51;


    static void WriteTestFile(byte[] data) {
        FileStream fileout = File.Open(testfile, FileMode.Create,
                                   FileAccess.Write);
        fileout.Write(data, 0, data.Length);
        fileout.Close();
    }

    /* Create a Midi File with 3 sequential notes, where each
     * note starts after the previous one ends (timewise).
     *
     * Parse the MidiFile. Verify the following:
     * - The time signature
     * - The number of tracks
     * - The midi note numbers, start time, and duration
     */
    [Test]
    public void TestSequentialNotes() {
        const byte notenum = 60;
        const byte quarternote = 240;
        const byte numtracks = 1;
        const byte velocity = 80;

        byte[] data = new byte[] {
            77, 84, 104, 100,        /* MThd ascii header */
            0, 0, 0, 6,              /* length of header in bytes */
            0, 1,                    /* one or more simultaneous tracks */
            0, numtracks, 
            0, quarternote,  
            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 24,             /* Length of track, in bytes */

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum,   velocity,
            60, EventNoteOff, notenum,   0,
            0,  EventNoteOn,  notenum+1, velocity,
            30, EventNoteOff, notenum+1, 0,
            0,  EventNoteOn,  notenum+2, velocity,
            90, EventNoteOff, notenum+2, 0
        };

        WriteTestFile(data);
        MidiFile midifile = new MidiFile(testfile);
        File.Delete(testfile);

        Assert.AreEqual(midifile.Tracks.Count, 1);
        Assert.AreEqual(midifile.Time.Numerator, 4);
        Assert.AreEqual(midifile.Time.Denominator, 4);
        Assert.AreEqual(midifile.Time.Quarter, quarternote);
        Assert.AreEqual(midifile.Time.Measure, quarternote * 4);

        MidiTrack track = midifile.Tracks[0];
        List<MidiNote> notes = track.Notes;
        Assert.AreEqual(notes.Count, 3);

        Assert.AreEqual(notes[0].StartTime, 0);
        Assert.AreEqual(notes[0].Number, notenum);
        Assert.AreEqual(notes[0].Duration, 60);

        Assert.AreEqual(notes[1].StartTime, 60);
        Assert.AreEqual(notes[1].Number, notenum+1);
        Assert.AreEqual(notes[1].Duration, 30);

        Assert.AreEqual(notes[2].StartTime, 90);
        Assert.AreEqual(notes[2].Number, notenum+2);
        Assert.AreEqual(notes[2].Duration, 90);

    }

    /* Create a Midi File with 3 notes that overlap timewise,
     * where a note starts before the previous note ends.
     *
     * Parse the MidiFile. Verify the following:
     * - The time signature
     * - The number of tracks
     * - The midi note numbers, start time, and duration
     */
    [Test]
    public void TestOverlappingNotes() {
        const byte notenum = 60;
        const byte quarternote = 240;
        const byte numtracks = 1;
        const byte velocity = 80;

        byte[] data = new byte[] {
            77, 84, 104, 100,        /* MThd ascii header  */
            0, 0, 0, 6,              /* length of header in bytes */
            0, 1,                    /* one or more simultaneous tracks */
            0, numtracks, 
            0, quarternote,  
            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 24,             /* Length of track, in bytes */

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum,   velocity,
            30, EventNoteOn,  notenum+1, velocity,
            30, EventNoteOn,  notenum+2, velocity,
            30, EventNoteOff, notenum+1, 0,
            30, EventNoteOff, notenum,   0,
            30, EventNoteOff, notenum+2, 0
        };

        WriteTestFile(data);
        MidiFile midifile = new MidiFile(testfile);
        File.Delete(testfile);

        Assert.AreEqual(midifile.Tracks.Count, 1);
        Assert.AreEqual(midifile.Time.Numerator, 4);
        Assert.AreEqual(midifile.Time.Denominator, 4);
        Assert.AreEqual(midifile.Time.Quarter, quarternote);
        Assert.AreEqual(midifile.Time.Measure, quarternote * 4);

        MidiTrack track = midifile.Tracks[0];

        List<MidiNote> notes = track.Notes;
        Assert.AreEqual(notes.Count, 3);

        Assert.AreEqual(notes[0].StartTime, 0);
        Assert.AreEqual(notes[0].Number, notenum);
        Assert.AreEqual(notes[0].Duration, 120);

        Assert.AreEqual(notes[1].StartTime, 30);
        Assert.AreEqual(notes[1].Number, notenum+1);
        Assert.AreEqual(notes[1].Duration, 60);

        Assert.AreEqual(notes[2].StartTime, 60);
        Assert.AreEqual(notes[2].Number, notenum+2);
        Assert.AreEqual(notes[2].Duration, 90);
    }

    /* Create a Midi File with 3 notes, where the event code
     * (EventNoteOn, EventNoteOff) is missing for notes 2 and 3.
     *
     * Parse the MidiFile. Verify the following:
     * - The time signature
     * - The number of tracks
     * - The midi note numbers, start time, and duration
     */
    [Test]
    public void TestMissingEventCode() {
        const byte notenum = 60;
        const byte quarternote = 240;
        const byte numtracks = 1;
        const byte velocity = 80;

        byte[] data = new byte[] {
            77, 84, 104, 100,        /* MThd ascii header */
            0, 0, 0, 6,              /* length of header in bytes */
            0, 1,                    /* one or more simultaneous tracks */
            0, numtracks, 
            0, quarternote,  
            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 20,             /* Length of track, in bytes */

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum,   velocity,
            30,               notenum+1, velocity,
            30,               notenum+2, velocity,
            30, EventNoteOff, notenum+1, 0,
            30,               notenum,   0,
            30,               notenum+2, 0
        };

        WriteTestFile(data);
        MidiFile midifile = new MidiFile(testfile);
        File.Delete(testfile);

        Assert.AreEqual(midifile.Tracks.Count, 1);
        Assert.AreEqual(midifile.Time.Numerator, 4);
        Assert.AreEqual(midifile.Time.Denominator, 4);
        Assert.AreEqual(midifile.Time.Quarter, quarternote);
        Assert.AreEqual(midifile.Time.Measure, quarternote * 4);

        MidiTrack track = midifile.Tracks[0];

        List<MidiNote> notes = track.Notes;
        Assert.AreEqual(notes.Count, 3);

        Assert.AreEqual(notes[0].StartTime, 0);
        Assert.AreEqual(notes[0].Number, notenum);
        Assert.AreEqual(notes[0].Duration, 120);

        Assert.AreEqual(notes[1].StartTime, 30);
        Assert.AreEqual(notes[1].Number, notenum+1);
        Assert.AreEqual(notes[1].Duration, 60);

        Assert.AreEqual(notes[2].StartTime, 60);
        Assert.AreEqual(notes[2].Number, notenum+2);
        Assert.AreEqual(notes[2].Duration, 90);
    }


    /* Create a Midi File with 3 notes, and many extra events
     * (KeyPressure, ControlChange, ProgramChange, PitchBend).
     *
     * Parse the MidiFile. Verify the following:
     * - The time signature
     * - The number of tracks
     * - The midi note numbers, start time, and duration
     */
    [Test]
    public void TestVariousEvents() {
        const byte notenum = 60;
        const byte quarternote = 240;
        const byte numtracks = 1;
        const byte velocity = 80;

        byte[] data = new byte[] {
            77, 84, 104, 100,        /* MThd ascii header */
            0, 0, 0, 6,              /* length of header in bytes */
            0, 1,                    /* one or more simultaneous tracks */
            0, numtracks, 
            0, quarternote,  
            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 39,             /* Length of track, in bytes */

            /*  time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum,   velocity,
            60, EventNoteOff, notenum,   0,
            0,  EventKeyPressure, notenum, 10,
            0,  EventControlChange, 10, 10,
            0,  EventNoteOn,  notenum+1, velocity,
            30, EventNoteOff, notenum+1, 0,
            0,  EventProgramChange, 10,
            0,  EventPitchBend, 0, 0,
            0,  EventNoteOn,  notenum+2, velocity,
            90, EventNoteOff, notenum+2, 0
        };

        WriteTestFile(data);
        MidiFile midifile = new MidiFile(testfile);
        File.Delete(testfile);

        Assert.AreEqual(midifile.Tracks.Count, 1);
        Assert.AreEqual(midifile.Time.Numerator, 4);
        Assert.AreEqual(midifile.Time.Denominator, 4);
        Assert.AreEqual(midifile.Time.Quarter, quarternote);
        Assert.AreEqual(midifile.Time.Measure, quarternote * 4);

        MidiTrack track = midifile.Tracks[0];
        List<MidiNote> notes = track.Notes;
        Assert.AreEqual(notes.Count, 3);

        Assert.AreEqual(notes[0].StartTime, 0);
        Assert.AreEqual(notes[0].Number, notenum);
        Assert.AreEqual(notes[0].Duration, 60);

        Assert.AreEqual(notes[1].StartTime, 60);
        Assert.AreEqual(notes[1].Number, notenum+1);
        Assert.AreEqual(notes[1].Duration, 30);

        Assert.AreEqual(notes[2].StartTime, 90);
        Assert.AreEqual(notes[2].Number, notenum+2);
        Assert.AreEqual(notes[2].Duration, 90);

    }

    /* Create a Midi File with 3 notes, and some meta-events
     * (Sequence, Key Signature)
     *
     * Parse the MidiFile. Verify the following:
     * - The time signature
     * - The number of tracks
     * - The midi note numbers, start time, and duration
     */
    [Test]
    public void TestMetaEvents() {
        const byte notenum = 60;
        const byte quarternote = 240;
        const byte numtracks = 1;
        const byte velocity = 80;

        byte[] data = new byte[] {
            77, 84, 104, 100,        /* MThd ascii header  */
            0, 0, 0, 6,              /* length of header in bytes */
            0, 1,                    /* one or more simultaneous tracks */
            0, numtracks, 
            0, quarternote,  
            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 36,             /* Length of track, in bytes */

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum,   velocity,
            60, EventNoteOff, notenum,   0,
            0,  MetaEvent, MetaEventSequence, 2, 0, 6,
            0,  EventNoteOn,  notenum+1, velocity,
            30, EventNoteOff, notenum+1, 0,
            0,  MetaEvent, MetaEventKeySignature, 2, 3, 0,
            0,  EventNoteOn,  notenum+2, velocity,
            90, EventNoteOff, notenum+2, 0
        };

        WriteTestFile(data);
        MidiFile midifile = new MidiFile(testfile);
        File.Delete(testfile);

        Assert.AreEqual(midifile.Tracks.Count, 1);
        Assert.AreEqual(midifile.Time.Numerator, 4);
        Assert.AreEqual(midifile.Time.Denominator, 4);
        Assert.AreEqual(midifile.Time.Quarter, quarternote);
        Assert.AreEqual(midifile.Time.Measure, quarternote * 4);

        MidiTrack track = midifile.Tracks[0];
        List<MidiNote> notes = track.Notes;
        Assert.AreEqual(notes.Count, 3);

        Assert.AreEqual(notes[0].StartTime, 0);
        Assert.AreEqual(notes[0].Number, notenum);
        Assert.AreEqual(notes[0].Duration, 60);

        Assert.AreEqual(notes[1].StartTime, 60);
        Assert.AreEqual(notes[1].Number, notenum+1);
        Assert.AreEqual(notes[1].Duration, 30);

        Assert.AreEqual(notes[2].StartTime, 90);
        Assert.AreEqual(notes[2].Number, notenum+2);
        Assert.AreEqual(notes[2].Duration, 90);

    }


    /* Create a Midi File with 3 tracks, and 3 notes per track.
     *
     * Parse the MidiFile. Verify the following:
     * - The time signature
     * - The number of tracks
     * - The midi note numbers, start time, and duration
     */
    [Test]
    public void TestMultipleTracks() {
        const byte notenum = 60;
        const byte quarternote = 240;
        const byte numtracks = 3;
        const byte velocity = 80;

        byte[] data = new byte[] {
            77, 84, 104, 100,        /* MThd ascii header */
            0, 0, 0, 6,              /* length of header in bytes */
            0, 1,                    /* one or more simultaneous tracks */ 
            0, numtracks, 
            0, quarternote, 
 
            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 24,             /* Length of track, in bytes */

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum,   velocity,
            60, EventNoteOff, notenum,   0,
            0,  EventNoteOn,  notenum+1, velocity,
            30, EventNoteOff, notenum+1, 0,
            0,  EventNoteOn,  notenum+2, velocity,
            90, EventNoteOff, notenum+2, 0,

            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 24,             /* Length of track, in bytes */

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum+1, velocity,
            60, EventNoteOff, notenum+1, 0,
            0,  EventNoteOn,  notenum+2, velocity,
            30, EventNoteOff, notenum+2, 0,
            0,  EventNoteOn,  notenum+3, velocity,
            90, EventNoteOff, notenum+3, 0,

            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 24,             /* Length of track, in bytes */

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum+2, velocity,
            60, EventNoteOff, notenum+2, 0,
            0,  EventNoteOn,  notenum+3, velocity,
            30, EventNoteOff, notenum+3, 0,
            0,  EventNoteOn,  notenum+4, velocity,
            90, EventNoteOff, notenum+4, 0,

        };

        WriteTestFile(data);
        MidiFile midifile = new MidiFile(testfile);
        File.Delete(testfile);

        Assert.AreEqual(midifile.Tracks.Count, numtracks);
        Assert.AreEqual(midifile.Time.Numerator, 4);
        Assert.AreEqual(midifile.Time.Denominator, 4);
        Assert.AreEqual(midifile.Time.Quarter, quarternote);
        Assert.AreEqual(midifile.Time.Measure, quarternote * 4);


        for (int tracknum = 0; tracknum < numtracks; tracknum++) {
            MidiTrack track = midifile.Tracks[tracknum];
            List<MidiNote> notes = track.Notes;
            Assert.AreEqual(notes.Count, 3);

            Assert.AreEqual(notes[0].StartTime, 0);
            Assert.AreEqual(notes[0].Number, notenum + tracknum);
            Assert.AreEqual(notes[0].Duration, 60);

            Assert.AreEqual(notes[1].StartTime, 60);
            Assert.AreEqual(notes[1].Number, notenum + tracknum + 1);
            Assert.AreEqual(notes[1].Duration, 30);

            Assert.AreEqual(notes[2].StartTime, 90);
            Assert.AreEqual(notes[2].Number, notenum + tracknum + 2);
            Assert.AreEqual(notes[2].Duration, 90);
        }
    }



    /* Create a Midi File that is truncated, where the
     * track length is 30 bytes, but only 24 bytes of
     * track data are there.
     *
     * Verify that the MidiFile is still parsed successfully.
     */
    [Test]
    public void TestTruncatedFile() {
        const byte notenum = 60;
        const byte quarternote = 240;
        const byte numtracks = 1;
        const byte velocity = 80;

        byte[] data = new byte[] {
            77, 84, 104, 100,        /* MThd ascii header */
            0, 0, 0, 6,              /* length of header in bytes */
            0, 1,                    /* one or more simultaneous tracks */
            0, numtracks, 
            0, quarternote,  
            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 30,             /* Length of track, in bytes. Should be 24. */

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum,   velocity,
            60, EventNoteOff, notenum,   0,
            0,  EventNoteOn,  notenum+1, velocity,
            30, EventNoteOff, notenum+1, 0,
            0,  EventNoteOn,  notenum+2, velocity,
            90, EventNoteOff, notenum+2, 0
        };

        WriteTestFile(data);
        bool got_exception = false;
        try {
            MidiFile midifile = new MidiFile(testfile);
        }
        catch (MidiFileException e) {
            got_exception = true;
        }
        File.Delete(testfile);
        Assert.AreEqual(got_exception, false);
    }

    /* Create a single track with:
     * - note numbers between 70 and 80
     * - note numbers between 65 and 75
     * - note numbers between 50 and 60
     * - note numbers between 55 and 65
     *
     * Then call SplitTracks().  Verify that
     * - Track 0 has numbers between 65-75, 70-80
     * - Track 1 has numbers between 50-60, 55-65
     */
    [Test]
    public void TestSplitTrack() {
        MidiTrack track = new MidiTrack(1);
        int start, number;

        /* Create notes between 70 and 80 */
        for (int i = 0; i < 100; i++) {
            start = i * 10;
            number = 70 + (i % 10);
            MidiNote note = new MidiNote(start, 0, number, 10);
            track.AddNote(note);
        }

        /* Create notes between 65 and 75 */
        for (int i = 0; i < 100; i++) {
            start = i * 10 + 1;
            number = 65 + (i % 10);
            MidiNote note = new MidiNote(start, 0, number, 10);
            track.AddNote(note);
        }

        /* Create notes between 50 and 60 */
        for (int i = 0; i < 100; i++) {
            start = i * 10;
            number = 50 + (i % 10);
            MidiNote note = new MidiNote(start, 0, number, 10);
            track.AddNote(note);
        }

        /* Create notes between 55 and 65 */
        for (int i = 0; i < 100; i++) {
            start = i * 10 + 1;
            number = 55 + (i % 10);
            MidiNote note = new MidiNote(start, 0, number, 10);
            track.AddNote(note);
        }

        track.Notes.Sort( track.Notes[0] );
        List<MidiTrack> tracks = MidiFile.SplitTrack(track, 40);

        Assert.AreEqual(tracks[0].Notes.Count, 200);
        Assert.AreEqual(tracks[1].Notes.Count, 200);

        for (int i = 0; i < 100; i++) {
            MidiNote note1 = tracks[0].Notes[i*2];
            MidiNote note2 = tracks[0].Notes[i*2 + 1];
            Assert.AreEqual(note1.StartTime, i*10);
            Assert.AreEqual(note2.StartTime, i*10 + 1);
            Assert.AreEqual(note1.Number, 70 + (i % 10));
            Assert.AreEqual(note2.Number, 65 + (i % 10));
        }
        for (int i = 0; i < 100; i++) {
            MidiNote note1 = tracks[1].Notes[i*2];
            MidiNote note2 = tracks[1].Notes[i*2 + 1];
            Assert.AreEqual(note1.StartTime, i*10);
            Assert.AreEqual(note2.StartTime, i*10 + 1);
            Assert.AreEqual(note1.Number, 50 + (i % 10));
            Assert.AreEqual(note2.Number, 55 + (i % 10));
        }
    }

    /* Create 3 tracks with the following notes:
     * - Start times 1, 3, 5 ... 99
     * - Start times 2, 4, 6 .... 100
     * - Start times 10, 20, .... 100
     * Combine all the tracks to a single track.
     * In the single track, verify that:
     * - The notes are sorted by start time
     * - There are no duplicate notes (same start time and number).
     */
    [Test]
    public void TestCombineToSingleTrack() {
        List<MidiTrack> tracks = new List<MidiTrack>();
        int start, number;

        tracks.Add(new MidiTrack(1));
        for (int i = 1; i <= 99; i += 2) {
            start = i;
            number = 30 + (i % 10);
            MidiNote note = new MidiNote(start, 0, number, 10);
            tracks[0].AddNote(note);
        }
        tracks.Add(new MidiTrack(2));
        for (int i = 0; i <= 100; i += 2) {
            start = i;
            number = 50 + (i % 10);
            MidiNote note = new MidiNote(start, 0, number, 10);
            tracks[1].AddNote(note);
        }
        tracks.Add(new MidiTrack(3));
        for (int i = 0; i <= 100; i += 10) {
            start = i;
            number = 50 + (i % 10);
            MidiNote note = new MidiNote(start, 0, number, 20);
            tracks[2].AddNote(note);
        }

        MidiTrack track = MidiFile.CombineToSingleTrack(tracks);
        Assert.AreEqual(track.Notes.Count, 101);
        for (int i = 0; i <= 100; i++) {
            MidiNote note = track.Notes[i];
            Assert.AreEqual(note.StartTime, i);
            if (i % 2 == 0) {
                Assert.AreEqual(note.Number, 50 + (i % 10));
            }
            else {
                Assert.AreEqual(note.Number, 30 + (i % 10));
            }
            if (i % 10 == 0) {
                Assert.AreEqual(note.Duration, 20);
            }
            else {
                Assert.AreEqual(note.Duration, 10);
            }
        }
    }

    /* Create a set of notes with the following start times.
     * 0, 2, 3, 10, 15, 20, 22, 35, 36, 62.
     *
     * After rounding the start times, the start times will be:
     * 0, 0, 0,  0,  0, 20, 20, 20, 36, 62.
     */
    [Test]
    public void TestRoundStartTimes() {
        const byte notenum = 20;

        List<MidiTrack> tracks = new List<MidiTrack>();
        MidiTrack track1 = new MidiTrack(0);
        track1.AddNote(new MidiNote(0, 0, notenum, 60));
        track1.AddNote(new MidiNote(3, 0, notenum+1, 60));
        track1.AddNote(new MidiNote(15, 0, notenum+2, 60));
        track1.AddNote(new MidiNote(22, 0, notenum+3, 60));
        track1.AddNote(new MidiNote(62, 0, notenum+4, 60));

        MidiTrack track2 = new MidiTrack(1);
        track2.AddNote(new MidiNote(2, 0, notenum+10, 60));
        track2.AddNote(new MidiNote(10, 0, notenum+11, 60));
        track2.AddNote(new MidiNote(20, 0, notenum+12, 60));
        track2.AddNote(new MidiNote(35, 0, notenum+13, 60));
        track2.AddNote(new MidiNote(36, 0, notenum+14, 60));

        tracks.Add(track1);
        tracks.Add(track2);

        int quarter = 130;
        int tempo = 500000;
        TimeSignature time = new TimeSignature(4, 4, quarter, tempo);

        /* quarternote * 60,000 / 500,000 = 15 pulses
         * So notes within 15 pulses should be grouped together. 
         * 0, 2, 3, 10, 15 are grouped to starttime 0
         * 20, 22, 35      are grouped to starttime 20
         * 36              is still 36
         * 62              is still 62
         */
        MidiFile.RoundStartTimes(tracks, 60, time);
        List<MidiNote> notes1 = tracks[0].Notes;
        List<MidiNote> notes2 = tracks[1].Notes;
        Assert.AreEqual(notes1.Count, 5);
        Assert.AreEqual(notes2.Count, 5);

        Assert.AreEqual(notes1[0].Number, notenum);
        Assert.AreEqual(notes1[1].Number, notenum+1);
        Assert.AreEqual(notes1[2].Number, notenum+2);
        Assert.AreEqual(notes1[3].Number, notenum+3);
        Assert.AreEqual(notes1[4].Number, notenum+4);

        Assert.AreEqual(notes2[0].Number, notenum+10);
        Assert.AreEqual(notes2[1].Number, notenum+11);
        Assert.AreEqual(notes2[2].Number, notenum+12);
        Assert.AreEqual(notes2[3].Number, notenum+13);
        Assert.AreEqual(notes2[4].Number, notenum+14);


        Assert.AreEqual(notes1[0].StartTime, 0);
        Assert.AreEqual(notes1[1].StartTime, 0);
        Assert.AreEqual(notes1[2].StartTime, 0);
        Assert.AreEqual(notes1[3].StartTime, 20);
        Assert.AreEqual(notes1[3].StartTime, 20);
        Assert.AreEqual(notes1[4].StartTime, 62);

        Assert.AreEqual(notes2[0].StartTime, 0);
        Assert.AreEqual(notes2[1].StartTime, 0);
        Assert.AreEqual(notes2[2].StartTime, 20);
        Assert.AreEqual(notes2[3].StartTime, 20);
        Assert.AreEqual(notes2[4].StartTime, 36);
    }

    /* Create a list of notes with start times:
     * 0, 50, 90, 101, 123
     * and duration 1 pulse.
     * Verify that RoundDurations() rounds the
     * durations to the correct value.
     */
    [Test]
    public void TestRoundDurations() {
        MidiTrack track = new MidiTrack(1);
        MidiNote note = new MidiNote(0, 0, 55, 45);
        track.AddNote(note);
        int[] starttimes = new int[] { 50, 90, 101, 123 };
        foreach (int start in starttimes) {
            note = new MidiNote(start, 0, 55, 1);
            track.AddNote(note);
        }
        List<MidiTrack> tracks = new List<MidiTrack>();
        tracks.Add(track);
        int quarternote = 40;
        MidiFile.RoundDurations(tracks, quarternote);
        Assert.AreEqual(track.Notes[0].Duration, 45);
        Assert.AreEqual(track.Notes[1].Duration, 40);
        Assert.AreEqual(track.Notes[2].Duration, 10);
        Assert.AreEqual(track.Notes[3].Duration, 20);
        Assert.AreEqual(track.Notes[4].Duration, 1);
    }


    /* Create the midi file used by the TestChangeSound() methods */
    public MidiFile CreateTestChangeSoundMidiFile() {
        const byte notenum = 60;
        const byte quarternote = 240;
        const byte numtracks = 3;
        const byte velocity = 80;

        byte[] data = new byte[] {
            77, 84, 104, 100,        /* MThd ascii header */
            0, 0, 0, 6,              /* length of header in bytes */
            0, 1,                    /* one or more simultaneous tracks */ 
            0, numtracks, 
            0, quarternote, 
 
            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 34,             /* Length of track, in bytes */

            /* tempo event, len=3, tempo = 0x0032ff */
            0,  MetaEvent,    MetaEventTempo, 3, 0x0, 0x32, 0xff,
            /* instrument = 4 (Electric Piano 1) */
            0,  EventProgramChange, 4,

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum,   velocity,
            60, EventNoteOff, notenum,   0,
            0,  EventNoteOn,  notenum+1, velocity,
            30, EventNoteOff, notenum+1, 0,
            0,  EventNoteOn,  notenum+2, velocity,
            90, EventNoteOff, notenum+2, 0,

            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 34,             /* Length of track, in bytes */

            /* tempo event, len=3, tempo = 0xa0b0cc */
            0,  MetaEvent,    MetaEventTempo, 3, 0xa0, 0xb0, 0xcc,
            /* instrument = 5 (Electric Piano 2) */
            0,  EventProgramChange, 5,

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum+10, velocity,
            60, EventNoteOff, notenum+10, 0,
            0,  EventNoteOn,  notenum+11, velocity,
            30, EventNoteOff, notenum+11, 0,
            0,  EventNoteOn,  notenum+12, velocity,
            90, EventNoteOff, notenum+12, 0,

            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 34,             /* Length of track, in bytes */

            /* tempo event, len=3, tempo = 0x121244 */
            0,  MetaEvent,    MetaEventTempo, 3, 0x12, 0x12, 0x44,
            /* instrument = 0 (Acoustic Grand Piano) */
            0,  EventProgramChange, 0,

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,  notenum+20, velocity,
            60, EventNoteOff, notenum+20, 0,
            0,  EventNoteOn,  notenum+21, velocity,
            30, EventNoteOff, notenum+21, 0,
            0,  EventNoteOn,  notenum+22, velocity,
            90, EventNoteOff, notenum+22, 0,

        };

        WriteTestFile(data);

        /* Verify the original Midi File */
        MidiFile midifile = new MidiFile(testfile);
        Assert.AreEqual(midifile.Tracks.Count, 3);
        Assert.AreEqual(midifile.Tracks[0].Instrument, 4);
        Assert.AreEqual(midifile.Tracks[1].Instrument, 5);
        Assert.AreEqual(midifile.Tracks[2].Instrument, 0);
        for (int tracknum = 0; tracknum < 3; tracknum++) {
            MidiTrack track = midifile.Tracks[tracknum];
            List<MidiNote> notes = track.Notes;
            Assert.AreEqual(notes.Count, 3);

            Assert.AreEqual(notes[0].StartTime, 0);
            Assert.AreEqual(notes[0].Number, notenum + 10*tracknum);
            Assert.AreEqual(notes[0].Duration, 60);

            Assert.AreEqual(notes[1].StartTime, 60);
            Assert.AreEqual(notes[1].Number, notenum + 10*tracknum + 1);
            Assert.AreEqual(notes[1].Duration, 30);

            Assert.AreEqual(notes[2].StartTime, 90);
            Assert.AreEqual(notes[2].Number, notenum + 10*tracknum + 2);
            Assert.AreEqual(notes[2].Duration, 90);
        }
        return midifile;
    }


    /* Test changing the tempo using the ChangeSound() method.
     * Create a MidiFile and parse it.
     * Call ChangeSound() with tempo = 0x405060.
     * Parse the new MidiFile, and verify the TimeSignature tempo is 0x405060
     */
    [Test]
    public void TestChangeSoundTempo() {
        MidiFile midifile = CreateTestChangeSoundMidiFile();

        MidiOptions options = new MidiOptions(midifile);
        options.tempo = 0x405060;
        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);
        Assert.AreEqual(newmidi.Tracks.Count, 3);
        Assert.AreEqual(newmidi.Time.Tempo, 0x405060);

        File.Delete(testfile);
    }

    /* Test transposing the notes with the ChangeSound() method.
     * Create a Midi File with 3 tracks, and 3 notes per track. Parse the MidiFile.
     * Call ChangeSound() with transpose = 10.
     * Parse the new MidiFile, and verify the MidiNote numbers are now 10 notes higher.
     */
    [Test]
    public void TestChangeSoundTranspose() {
        const byte notenum = 60;

        MidiFile midifile = CreateTestChangeSoundMidiFile();

        MidiOptions options = new MidiOptions(midifile);
        options.transpose = 10;
        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);
        Assert.AreEqual(newmidi.Tracks.Count, 3);

        for (int tracknum = 0; tracknum < 3; tracknum++) {
            MidiTrack track = newmidi.Tracks[tracknum];
            List<MidiNote> notes = track.Notes;
            Assert.AreEqual(notes.Count, 3);

            Assert.AreEqual(notes[0].StartTime, 0);
            Assert.AreEqual(notes[0].Number, notenum + 10*tracknum + 10);
            Assert.AreEqual(notes[0].Duration, 60);

            Assert.AreEqual(notes[1].StartTime, 60);
            Assert.AreEqual(notes[1].Number, notenum + 10*tracknum + 11);
            Assert.AreEqual(notes[1].Duration, 30);

            Assert.AreEqual(notes[2].StartTime, 90);
            Assert.AreEqual(notes[2].Number, notenum + 10*tracknum + 12);
            Assert.AreEqual(notes[2].Duration, 90);
        }

        File.Delete(testfile);
    }

    /* Test changing the instruments with the ChangeSound() method.
     * Create a Midi File with 3 tracks. Parse the MidiFile.
     * Call ChangeSound() with instruments [40,41,42].
     * Parse the new MidiFile, and verify the instruments are now Violin, Viola, and Cello.
     */
    [Test]
    public void TestChangeSoundInstruments() {
        MidiFile midifile = CreateTestChangeSoundMidiFile();

        MidiOptions options = new MidiOptions(midifile);
        options.useDefaultInstruments = false;
        for (int i = 0; i < 3; i++) {
            options.instruments[i] = 40 + i;
        }
        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);
        Assert.AreEqual(newmidi.Tracks.Count, 3);
        Assert.AreEqual(newmidi.Tracks[0].Instrument, 40);
        Assert.AreEqual(newmidi.Tracks[1].Instrument, 41);
        Assert.AreEqual(newmidi.Tracks[2].Instrument, 42);

        File.Delete(testfile);

    }

    /* Test changing the tracks to include with the ChangeSound() method.
     * Create a Midi File with 3 tracks. Parse the MidiFile. Parse the MidiFile.
     * Call ChangeSound() with tracks = [ false, true, false];
     * Parse the new MidiFile, and verify that only the second track is included.
     */
    [Test]
    public void TestChangeSoundTrack() {
        const byte notenum = 60;

        MidiFile midifile = CreateTestChangeSoundMidiFile();

        MidiOptions options = new MidiOptions(midifile);
        options.useDefaultInstruments = false;

        options.tracks[0] = false;
        options.tracks[1] = true;
        options.tracks[2] = false;
        options.instruments[0] = 40;
        options.instruments[1] = 41;
        options.instruments[2] = 42;
        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);
        Assert.AreEqual(newmidi.Tracks.Count, 1);
        Assert.AreEqual(newmidi.Tracks[0].Instrument, 41);
        for (int i = 0; i < 3; i++) {
            MidiNote note = newmidi.Tracks[0].Notes[i];
            Assert.AreEqual(note.Number, notenum + 10 + i);
        }

        File.Delete(testfile);
    }

    /* Test chaning the pauseTime with the ChangeSound() method.
     * Create a Midi File with 3 tracks, and 3 notes per track. Parse the MidiFile.
     * Call ChangeSound() with pauseTime = 50.
     * Parse the new MidiFile, and verify the first note is gone, and the 2nd/3rd
     * notes have their start time 50 pulses earlier.
     */
    [Test]
    public void TestChangeSoundPauseTime() {
        const byte notenum = 60;

        MidiFile midifile = CreateTestChangeSoundMidiFile();

        MidiOptions options = new MidiOptions(midifile);
        options.pauseTime = 50;
        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);
        Assert.AreEqual(newmidi.Tracks[0].Instrument, 4);
        Assert.AreEqual(newmidi.Tracks[1].Instrument, 5);
        Assert.AreEqual(newmidi.Tracks[2].Instrument, 0);

        for (int tracknum = 0; tracknum < 3; tracknum++) {
            MidiTrack track = newmidi.Tracks[tracknum];
            List<MidiNote> notes = track.Notes;
            Assert.AreEqual(notes.Count, 2);

            Assert.AreEqual(notes[0].StartTime, 60 - options.pauseTime);
            Assert.AreEqual(notes[0].Number, notenum + 10*tracknum + 1);
            Assert.AreEqual(notes[0].Duration, 30);

            Assert.AreEqual(notes[1].StartTime, 90 - options.pauseTime);
            Assert.AreEqual(notes[1].Number, notenum + 10*tracknum + 2);
            Assert.AreEqual(notes[1].Duration, 90);
        }
        File.Delete(testfile);
    }


    /* Create the MidiFile for the TestChangeSoundPerChannel tests.
     * It has only one track, but multiple channels.
     */
    MidiFile CreateTestChangeSoundPerChannelMidiFile() {
        const byte notenum = 60;
        const byte quarternote = 240;
        const byte numtracks = 1;
        const byte velocity = 80;

        byte[] data = new byte[] {
            77, 84, 104, 100,        /* MThd ascii header */
            0, 0, 0, 6,              /* length of header in bytes */
            0, 1,                    /* one or more simultaneous tracks */ 
            0, numtracks, 
            0, quarternote, 
 
            77, 84, 114, 107,        /* MTrk ascii header */
            0, 0, 0, 81,             /* Length of track, in bytes */

            /* Instruments are
             * channel 0 = 0 (Acoustic Piano)
             * channel 1 = 4 (Electric Piano 1)
             * channel 2 = 5 (Electric Piano 2)
             */
            0,  EventProgramChange,   0,
            0,  EventProgramChange+1, 4,
            0,  EventProgramChange+2, 5,

            /* time_interval, NoteEvent, note number, velocity */
            0,  EventNoteOn,    notenum,    velocity,
            0,  EventNoteOn+1,  notenum+10, velocity,
            0,  EventNoteOn+2,  notenum+20, velocity,
            30, EventNoteOff,   notenum,    0,
            0,  EventNoteOff+1, notenum+10,   0,
            0,  EventNoteOff+2, notenum+20,   0,

            30, EventNoteOn,    notenum+1,  velocity,
            0,  EventNoteOn+1,  notenum+11, velocity,
            0,  EventNoteOn+2,  notenum+21, velocity,
            30, EventNoteOff,   notenum+1,    0,
            0,  EventNoteOff+1, notenum+11,   0,
            0,  EventNoteOff+2, notenum+21,   0,

            30, EventNoteOn,    notenum+2,  velocity,
            0,  EventNoteOn+1,  notenum+12, velocity,
            0,  EventNoteOn+2,  notenum+22, velocity,
            30, EventNoteOff,   notenum+2,    0,
            0,  EventNoteOff+1, notenum+12,   0,
            0,  EventNoteOff+2, notenum+22,   0,

        };

        /* Verify that the original midi has 3 tracks, one per channel */
        WriteTestFile(data);
        MidiFile midifile = new MidiFile(testfile);
        Assert.AreEqual(midifile.Tracks.Count, 3);
        Assert.AreEqual(midifile.Tracks[0].Instrument, 0);
        Assert.AreEqual(midifile.Tracks[1].Instrument, 4);
        Assert.AreEqual(midifile.Tracks[2].Instrument, 5);
        for (int tracknum = 0; tracknum < 3; tracknum++) {
            MidiTrack track = midifile.Tracks[tracknum];
            Assert.AreEqual(track.Notes.Count, 3);
            for (int n = 0; n < track.Notes.Count; n++) {
                MidiNote m = track.Notes[n];
                Assert.AreEqual(m.Number, notenum + 10*tracknum + n);
            }
        }
        return midifile;
    }


    /* Test changing the tempo with the ChangeSoundPerChannel() method.
     * Create a MidiFile with 1 track, and multiple channels.  Parse the MidiFile.
     * Call ChangeSoundPerChannel() with tempo = 0x405060;
     * Parse the new MidiFile, and verify the TimeSignature tempo = 0x405060.
     */
    [Test]
    public void TestChangeSoundPerChannelTempo() {
        MidiFile midifile = CreateTestChangeSoundPerChannelMidiFile();
        MidiOptions options = new MidiOptions(midifile);
        options.tempo = 0x405060;
        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);

        Assert.AreEqual(newmidi.Time.Tempo, 0x405060);
        File.Delete(testfile);
    }

    /* Test transposing the notes with the ChangeSoundPerChannel() method.
     * Create a MidiFile with 1 track, and multiple channels.  Parse the MidiFile.
     * Call ChangeSoundPerChannel() with transpose = 10.
     * Parse the new MidiFile, and verify the notes are transposed 10 values.
     */
    [Test]
    public void TestChangeSoundPerChannelTranspose() {
        const byte notenum = 60;

        MidiFile midifile = CreateTestChangeSoundPerChannelMidiFile();
        MidiOptions options = new MidiOptions(midifile);
        options.transpose = 10;
        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);

        Assert.AreEqual(newmidi.Tracks.Count, 3);
        for (int tracknum = 0; tracknum < 3; tracknum++) {
            MidiTrack track = newmidi.Tracks[tracknum];
            for (int i = 0; i < 3; i++) {
               MidiNote note = track.Notes[i];
               Assert.AreEqual(note.Number, notenum + tracknum*10 + i + 10);
            }
        }

        File.Delete(testfile);
    }


    /* Test changing the instruments with the ChangeSoundPerChannel() method.
     * Create a MidiFile with 1 track, and multiple channels.  Parse the MidiFile.
     * Call ChangeSoundPerChannel() with instruments = [40, 41, 42].
     * Parse the new MidiFile, and verify the new instruments are used.
     */

    [Test]
    public void TestChangeSoundPerChannelInstruments() {
        MidiFile midifile = CreateTestChangeSoundPerChannelMidiFile();
        MidiOptions options = new MidiOptions(midifile);
        options.useDefaultInstruments = false;
        for (int tracknum = 0; tracknum < 3; tracknum++) {
            options.instruments[tracknum] = 40 + tracknum;
        }
        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);

        Assert.AreEqual(newmidi.Tracks.Count, 3);
        Assert.AreEqual(newmidi.Tracks[0].Instrument, 40);
        Assert.AreEqual(newmidi.Tracks[1].Instrument, 41);
        Assert.AreEqual(newmidi.Tracks[2].Instrument, 42);

        File.Delete(testfile);
    }

    /* Test changing the tracks to include with the ChangeSoundPerChannel() method.
     * Create a MidiFile with 1 track, and multiple channels.  Parse the MidiFile.
     * Call ChangeSoundPerChannel() with tracks = [false, true, false]
     * Parse the new MidiFile, and verify that only the 2nd track is included.
     */
    [Test]
    public void TestChangeSoundPerChannelTracks() {
        const byte notenum = 60;

        MidiFile midifile = CreateTestChangeSoundPerChannelMidiFile();
        MidiOptions options = new MidiOptions(midifile);
        options.tracks[0] = false;
        options.tracks[1] = true;
        options.tracks[2] = false;

        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);

        Assert.AreEqual(newmidi.Tracks.Count, 1);

        MidiTrack track = newmidi.Tracks[0];
        Assert.AreEqual(track.Notes.Count, 3);
        for (int i = 0; i < track.Notes.Count; i++) {
            MidiNote note = track.Notes[i];
            Assert.AreEqual(note.Number, notenum + 10 + i);
        }

        File.Delete(testfile);
    }

    /* Test changing the pauseTime with the ChangeSoundPerChannel() method.
     * Create a MidiFile with 1 track, and multiple channels.  Parse the MidiFile.
     * The original start times for each note are 0, 60, 120.
     * Call ChangeSoundPerChannel() with pauseTime = 50.
     * Parse the new MidiFile, and verify the first note is gone, and that the
     * start time of the last two notes are 50 pulses less.
     */
    [Test]
    public void TestChangeSoundPerChannelPauseTime() {
        const byte notenum = 60;

        MidiFile midifile = CreateTestChangeSoundPerChannelMidiFile();
        MidiOptions options = new MidiOptions(midifile);
        options.pauseTime = 50;
        bool ret = midifile.ChangeSound(testfile, options);
        Assert.AreEqual(ret, true);
        MidiFile newmidi = new MidiFile(testfile);

        Assert.AreEqual(newmidi.Tracks.Count, 3);
        for (int tracknum = 0; tracknum < 3; tracknum++) {
            MidiTrack track = newmidi.Tracks[tracknum];
            Assert.AreEqual(track.Notes.Count, 2);
            for (int i = 0; i < 2; i++) {
                MidiNote note = track.Notes[i];
                Assert.AreEqual(note.Number, notenum + 10*tracknum + 1 + i);
                Assert.AreEqual(note.StartTime, 60 * (i+1) - 50);
            }
        }
        File.Delete(testfile);
    }


}


/* Test cases for the KeySignature class */
[TestFixture]
public class KeySignatureTest {

    /* Test that the key signatures return the correct accidentals.
     * C major (0 sharps, 0 flats) should return 0 accidentals.
     * G major through F# major should return F#, C#, G#, D#, A#, E#
     * F major through D-flat major should return B-flat, E-flat, A-flat, D-flat, G-flat
     */
    [Test]
    public void TestGetSymbols() {
        KeySignature k;
        AccidSymbol[] symbols1, symbols2;

        k = new KeySignature(0, 0);
        symbols1 = k.GetSymbols(Clef.Treble);
        symbols2 = k.GetSymbols(Clef.Bass);
        Assert.AreEqual(symbols1.Length, 0);
        Assert.AreEqual(symbols2.Length, 0);

        int[] sharps = new int[] {
            WhiteNote.F, WhiteNote.C, WhiteNote.G, WhiteNote.D,
            WhiteNote.A, WhiteNote.E
        };

        for (int sharp = 1; sharp < 7; sharp++) {
            k = new KeySignature(sharp, 0);
            symbols1 = k.GetSymbols(Clef.Treble);
            symbols2 = k.GetSymbols(Clef.Bass);
            for (int i = 0; i < sharp; i++) {
                Assert.AreEqual(symbols1[i].Note.Letter, sharps[i]);
                Assert.AreEqual(symbols2[i].Note.Letter, sharps[i]);
            }
        }

        int[] flats = new int[] {
            WhiteNote.B, WhiteNote.E, WhiteNote.A, WhiteNote.D,
            WhiteNote.G
        }; 

        for (int flat = 1; flat < 6; flat++) {
            k = new KeySignature(0, flat);
            symbols1 = k.GetSymbols(Clef.Treble);
            symbols2 = k.GetSymbols(Clef.Bass);
            for (int i = 0; i < flat; i++) {
                Assert.AreEqual(symbols1[i].Note.Letter, flats[i]);
                Assert.AreEqual(symbols2[i].Note.Letter, flats[i]);
            }
        }
    }


    /* For each key signature, loop through all the notes, from 1 to 128.
     * Verify that the key signature returns the correct accidental
     * (sharp, flat, natural, none) for the given note.
     */
    [Test]
    public void TestGetAccidental() {

        int measure = 1;
        KeySignature k;
        Accid[] expected = new Accid[12];
        for (int i = 0; i < 12; i++) {
            expected[i] = Accid.None;
        }
        expected[NoteScale.Bflat]  = Accid.Flat;
        expected[NoteScale.Csharp] = Accid.Sharp;
        expected[NoteScale.Dsharp] = Accid.Sharp;
        expected[NoteScale.Fsharp] = Accid.Sharp;
        expected[NoteScale.Gsharp] = Accid.Sharp;

        /* Test C Major */
        k = new KeySignature(0, 0);
        measure = 1;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        /* Test G major, F# */
        k = new KeySignature(1, 0);
        measure = 1;
        expected[NoteScale.Fsharp] = Accid.None;
        expected[NoteScale.F] = Accid.Natural;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        /* Test D major, F#, C# */
        k = new KeySignature(2, 0);
        measure = 1;
        expected[NoteScale.Csharp] = Accid.None;
        expected[NoteScale.C] = Accid.Natural;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        /* Test A major, F#, C#, G# */
        k = new KeySignature(3, 0);
        measure = 1;
        expected[NoteScale.Gsharp] = Accid.None;
        expected[NoteScale.G] = Accid.Natural;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        /* Test E major, F#, C#, G#, D# */
        k = new KeySignature(4, 0);
        measure = 1;
        expected[NoteScale.Dsharp] = Accid.None;
        expected[NoteScale.D] = Accid.Natural;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        /* Test B major, F#, C#, G#, D#, A# */
        k = new KeySignature(5, 0);
        measure = 1;
        expected[NoteScale.Asharp] = Accid.None;
        expected[NoteScale.A] = Accid.Natural;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        for (int i = 0; i < 12; i++) {
            expected[i] = Accid.None;
        }
        expected[NoteScale.Aflat]  = Accid.Flat;
        expected[NoteScale.Bflat]  = Accid.Flat;
        expected[NoteScale.Csharp] = Accid.Sharp;
        expected[NoteScale.Eflat]  = Accid.Flat;
        expected[NoteScale.Fsharp] = Accid.Sharp;

        /* Test F major, Bflat */
        k = new KeySignature(0, 1);
        measure = 1;
        expected[NoteScale.Bflat] = Accid.None;
        expected[NoteScale.B] = Accid.Natural;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        /* Test Bflat major, Bflat, Eflat */
        k = new KeySignature(0, 2);
        measure = 1;
        expected[NoteScale.Eflat] = Accid.None;
        expected[NoteScale.E] = Accid.Natural;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        /* Test Eflat major, Bflat, Eflat, Afat */
        k = new KeySignature(0, 3);
        measure = 1;
        expected[NoteScale.Aflat] = Accid.None;
        expected[NoteScale.A] = Accid.Natural;
        expected[NoteScale.Dflat] = Accid.Flat;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        /* Test Aflat major, Bflat, Eflat, Aflat, Dflat */
        k = new KeySignature(0, 4);
        measure = 1;
        expected[NoteScale.Dflat] = Accid.None;
        expected[NoteScale.D] = Accid.Natural;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }

        /* Test Dflat major, Bflat, Eflat, Aflat, Dflat, Gflat */
        k = new KeySignature(0, 5);
        measure = 1;
        expected[NoteScale.Gflat] = Accid.None;
        expected[NoteScale.G] = Accid.Natural;
        for (int note = 1; note < 128; note++) {
            int notescale = NoteScale.FromNumber(note);
            Assert.AreEqual(expected[notescale], 
                          k.GetAccidental(note, measure));
            measure++;
        }
    }


    /* Test that GetAccidental() and GetWhiteNote() return the correct values.
     * - The WhiteNote should be one below for flats, and one above for sharps.
     * - The accidental should only be returned the first time the note is passed.
     *   On the second time, GetAccidental() should return none.
     * - When a sharp/flat accidental is returned, calling GetAccidental() on
     *   the white key just below/above should now return a natural accidental.
     */
    [Test]
    public void TestGetAccidentalSameMeasure() {
        KeySignature k;

        /* G Major, F# */
        k = new KeySignature(1, 0);

        int note = NoteScale.ToNumber(NoteScale.C, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.C);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.C);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.Fsharp, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.F, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Natural);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.Fsharp, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Sharp);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.Bflat, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Flat);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.A, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.A);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.B, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Natural);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.Bflat, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Flat);

        

        /* F Major, Bflat */
        k = new KeySignature(0, 1);

        note = NoteScale.ToNumber(NoteScale.G, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.G);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.G);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.Bflat, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.B, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Natural);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.Bflat, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Flat);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.B);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.Fsharp, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Sharp);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.G, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.G);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.F, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Natural);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.None);

        note = NoteScale.ToNumber(NoteScale.Fsharp, 1);
        Assert.AreEqual(k.GetWhiteNote(note).Letter, WhiteNote.F);
        Assert.AreEqual(k.GetAccidental(note, 1), Accid.Sharp);

    }


    /* Create an array of note numbers (from 1 to 128), and verify that
     * the correct KeySignature is guessed.
     */
    [Test]
    public void TestGuess() {
        List<int> notes = new List<int>();

        /* C major */
        int octave = 0;
        for (int i = 0; i < 100; i++) {
            notes.Add( NoteScale.ToNumber(NoteScale.A, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.B, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.C, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.D, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.E, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.F, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.G, octave));
            octave = (octave + 1) % 7;
        }
        for (int i = 0; i < 10; i++) {
            notes.Add( NoteScale.ToNumber(NoteScale.Fsharp, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.Dsharp, octave));
        }
        Assert.AreEqual(KeySignature.Guess(notes).ToString(), "C major, A minor");

        /* A Major, F#, C#, G# */
        notes.Clear();
        octave = 0;
        for (int i = 0; i < 100; i++) {
            notes.Add( NoteScale.ToNumber(NoteScale.A, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.B, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.Csharp, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.D, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.E, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.Fsharp, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.Gsharp, octave));
            octave = (octave + 1) % 7;
        }
        for (int i = 0; i < 10; i++) {
            notes.Add( NoteScale.ToNumber(NoteScale.F, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.Dsharp, octave));
        }
        Assert.AreEqual(KeySignature.Guess(notes).ToString(), "A major, F# minor");

        /* Eflat Major, Bflat, Eflat, Aflat */
        notes.Clear();
        octave = 0;
        for (int i = 0; i < 100; i++) {
            notes.Add( NoteScale.ToNumber(NoteScale.Aflat, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.Bflat, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.C, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.D, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.Eflat, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.F, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.G, octave));
            octave = (octave + 1) % 7;
        }
        for (int i = 0; i < 10; i++) {
            notes.Add( NoteScale.ToNumber(NoteScale.Dflat, octave));
            notes.Add( NoteScale.ToNumber(NoteScale.B, octave));
        }
        Assert.AreEqual(KeySignature.Guess(notes).ToString(), "E-flat major, C minor");
    }
}


/* The TestSymbol is used for the SymbolWidths test cases */
public class TestSymbol : MusicSymbol {
    int starttime;
    int width;

    public TestSymbol(int starttime, int width) {
        this.starttime = starttime;
        this.width = width;
    }

    public override int StartTime { 
        get { return starttime; } 
    }
    public override int MinWidth {
        get { return width; } 
    }
    public override int Width {
        get { return width; }
        set { width = value; }
    }
    public override int AboveStaff {
        get { return 0; } 
    }
    public override int BelowStaff {
        get { return 0; } 
    }
    public override void Draw(Graphics g, Pen pen, int ytop) {}
}


/* Test cases for the SymbolWidths class */
[TestFixture]
public class SymbolWidthsTest {

    /* Given multiple tracks of symbols, test that the SymbolWidths.StartTimes
     * returns all the unique start times of all the symbols, in sorted order.
     */
    [Test]
    public void TestStartTimes() {
        List<MusicSymbol>[] tracks = new List<MusicSymbol>[3];
        for (int i = 0; i < 3; i++) {
            List<MusicSymbol> symbols = new List<MusicSymbol>();
            for (int j = 0; j < 5; j++) {
                symbols.Add(new TestSymbol(i*10 + j, 10));
            }
            tracks[i] = symbols;
        }
        SymbolWidths s = new SymbolWidths(tracks, null);
        int[] starttimes = s.StartTimes;
        int index = 0;
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 5; j++) {
                Assert.AreEqual(starttimes[index], i*10 + j);
                index++;
            }
        }
    }


    /* Create 3 tracks with 1 symbol each. The widths of each symbol
     * are 0, 4, and 8 respectively.  Verify that GetExtraWidth()
     * returns the correct value.
     *
     * Add 1 symbol to each track, with the same start time as the
     * previous symbols, so that the total widths for each track is
     * 0, 5, and 10 respectively.  Verify that GetExtraWidth()
     * returns the correct value.
     *
     * Create a symbol with width 6, but only add it to the first track.
     * Verify that GetExtraWidth() returns the correct value.
     */
    [Test]
    public void TestGetExtraWidth() {
        List<MusicSymbol>[] tracks = new List<MusicSymbol>[3];
        for (int i = 0; i < 3; i++) {
            List<MusicSymbol> symbols = new List<MusicSymbol>();
            symbols.Add(new TestSymbol(100, i*4));
            tracks[i] = symbols;
        }
        SymbolWidths s = new SymbolWidths(tracks, null);
        int extra = s.GetExtraWidth(0, 100);
        Assert.AreEqual(extra, 8); 
        extra = s.GetExtraWidth(1, 100);
        Assert.AreEqual(extra, 4); 
        extra = s.GetExtraWidth(2, 100);
        Assert.AreEqual(extra, 0); 

        tracks[0].Add(new TestSymbol(200, 6));
        s = new SymbolWidths(tracks, null);
        extra = s.GetExtraWidth(0, 200);
        Assert.AreEqual(extra, 0); 
        extra = s.GetExtraWidth(1, 200);
        Assert.AreEqual(extra, 6); 
        extra = s.GetExtraWidth(2, 200);
        Assert.AreEqual(extra, 6); 
    }

}


/* Test cases for the ClefMeasures class */
[TestFixture]
public class ClefMeasuresTest {
    static int middleC = 60;
    static int G3 = 55;
    static int F4 = 65;

    /* Create a list of notes all above middle C.
     * Verify that all the clefs are treble clefs.
     */
    [Test]
    public void TestAllTreble() {
        List<MidiNote> notes = new List<MidiNote>();
        for (int i = 0; i < 100; i++) {
            int num = middleC + (i % 5);
            MidiNote note = new MidiNote(i*10, 0, num, 5);
            notes.Add(note);
        }
        ClefMeasures clefs = new ClefMeasures(notes, 40);
        for (int i = 0; i < 100; i++) {
            Clef clef = clefs.GetClef(10 * i);
            Assert.AreEqual(clef, Clef.Treble);
        }
    }

    /* Create a list of notes all below middle C.
     * Verify that all the clefs are bass clefs.
     */
    [Test]
    public void TestAllBass() {
        List<MidiNote> notes = new List<MidiNote>();
        for (int i = 0; i < 100; i++) {
            int num = middleC - (i % 5);
            MidiNote note = new MidiNote(i*10, 0, num, 5);
            notes.Add(note);
        }
        ClefMeasures clefs = new ClefMeasures(notes, 40);
        for (int i = 0; i < 100; i++) {
            Clef clef = clefs.GetClef(10 * i);
            Assert.AreEqual(clef, Clef.Bass);
        }
    }

    /* Create a list of notes where the average note is above middle-C.
     * Verify that
     * - notes above F4 are treble clef
     * - notes below G3 are bass clef
     * - notes in between G3 and F4 are treble clef.
     */
    [Test]
    public void TestMainClefTreble() {
        List<MidiNote> notes = new List<MidiNote>();
        for (int i = 0; i < 100; i++) {
            int num = F4 + (i % 20);
            MidiNote note = new MidiNote(i*10, 0, num, 5);
            notes.Add(note);
        }
        for (int i = 100; i < 200; i++) {
            int num = G3 - (i % 2);
            MidiNote note = new MidiNote(i*10, 0, num, 5);
            notes.Add(note);
        }
        for (int i = 200; i < 300; i++) {
            int num = middleC - (i % 2);
            MidiNote note = new MidiNote(i*10, 0, num, 5);
            notes.Add(note);
        }
        ClefMeasures clefs = new ClefMeasures(notes, 50);
        for (int i = 0; i < 100; i++) {
            Clef clef = clefs.GetClef(i*10);
            Assert.AreEqual(clef, Clef.Treble);
        }
        for (int i = 100; i < 200; i++) {
            Clef clef = clefs.GetClef(i*10);
            Assert.AreEqual(clef, Clef.Bass);
        }
        for (int i = 200; i < 300; i++) {
            Clef clef = clefs.GetClef(i*10);
            /* Even though the average note is below middle C,
             * the main clef is treble.
             */
            Assert.AreEqual(clef, Clef.Treble);
        }
    }

    /* Create a list of notes where the average note is below middle-C.
     * Verify that
     * - notes above F4 are treble clef
     * - notes below G3 are bass clef
     * - notes in between G3 and F4 are bass clef.
     */
    [Test]
    public void TestMainClefBass() {
        List<MidiNote> notes = new List<MidiNote>();
        for (int i = 0; i < 100; i++) {
            int num = F4 + (i % 2);
            MidiNote note = new MidiNote(i*10, 0, num, 5);
            notes.Add(note);
        }
        for (int i = 100; i < 200; i++) {
            int num = G3 - (i % 20);
            MidiNote note = new MidiNote(i*10, 0, num, 5);
            notes.Add(note);
        }
        for (int i = 200; i < 300; i++) {
            int num = middleC + (i % 2);
            MidiNote note = new MidiNote(i*10, 0, num, 5);
            notes.Add(note);
        }
        ClefMeasures clefs = new ClefMeasures(notes, 50);
        for (int i = 0; i < 100; i++) {
            Clef clef = clefs.GetClef(i*10);
            Assert.AreEqual(clef, Clef.Treble);
        }
        for (int i = 100; i < 200; i++) {
            Clef clef = clefs.GetClef(i*10);
            Assert.AreEqual(clef, Clef.Bass);
        }
        for (int i = 200; i < 300; i++) {
            Clef clef = clefs.GetClef(i*10);
            /* Even though the average note is above middle C,
             * the main clef is bass.
             */
            Assert.AreEqual(clef, Clef.Bass);
        }
    }

}

/* Test cases for the ChordSymbol class */
[TestFixture]
public class ChordSymbolTest {

    /* Test a chord with
     * - 2 notes at bottom of treble clef.
     * - No accidentals
     * - Quarter duration
     * - Stem facing up.
     */
    [Test]
    public void TestStemUpTreble() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num1 = WhiteNote.BottomTreble.Number();
        int num2 = num1 + 2;
        MidiNote note1 = new MidiNote(0, 0, num1, quarter);
        MidiNote note2 = new MidiNote(0, 0, num2, quarter);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        notes.Add(note2);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Treble, null);
        Assert.AreEqual(chord.ToString(), 
                        "ChordSymbol clef=Treble start=0 end=400 width=16 hastwostems=False Note whitenote=F4 duration=Quarter leftside=True Note whitenote=G4 duration=Quarter leftside=False Stem duration=Quarter direction=1 top=G4 bottom=F4 end=F5 overlap=True side=2 width_to_pair=0 receiver_in_pair=False ");

    }

    /* Test a chord with
     * - 2 notes at top of treble clef.
     * - No accidentals
     * - Quarter duration
     * - Stem facing down.
     */
    [Test]
    public void TestStemDownTreble() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num2 = WhiteNote.TopTreble.Number();
        int num1 = num2 - 2;
        MidiNote note1 = new MidiNote(0, 0, num1, quarter);
        MidiNote note2 = new MidiNote(0, 0, num2, quarter);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        notes.Add(note2);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Treble, null);
        Assert.AreEqual(chord.ToString(), 
                        "ChordSymbol clef=Treble start=0 end=400 width=16 hastwostems=False Note whitenote=D5 duration=Quarter leftside=True Note whitenote=E5 duration=Quarter leftside=False Stem duration=Quarter direction=2 top=E5 bottom=D5 end=E4 overlap=True side=2 width_to_pair=0 receiver_in_pair=False ");

    }

    /* Test a chord with
     * - 2 notes at bottom of bass clef.
     * - No accidentals
     * - Quarter duration
     * - Stem facing up.
     */
    [Test]
    public void TestStemUpBass() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num1 = WhiteNote.BottomBass.Number();
        int num2 = num1 + 2;
        MidiNote note1 = new MidiNote(0, 0, num1, quarter);
        MidiNote note2 = new MidiNote(0, 0, num2, quarter);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        notes.Add(note2);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Bass, null);
        Assert.AreEqual(chord.ToString(),
                        "ChordSymbol clef=Bass start=0 end=400 width=16 hastwostems=False Note whitenote=A3 duration=Quarter leftside=True Note whitenote=B3 duration=Quarter leftside=False Stem duration=Quarter direction=1 top=B3 bottom=A3 end=A4 overlap=True side=2 width_to_pair=0 receiver_in_pair=False ");

    }

    /* Test a chord with
     * - 2 notes at top of treble clef.
     * - No accidentals
     * - Quarter duration
     * - Stem facing down.
     */
    [Test]
    public void TestStemDownBass() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num2 = WhiteNote.TopBass.Number();
        int num1 = num2 - 2;
        MidiNote note1 = new MidiNote(0, 0, num1, quarter);
        MidiNote note2 = new MidiNote(0, 0, num2, quarter);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        notes.Add(note2);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Bass, null);
        Assert.AreEqual(chord.ToString(),
                        "ChordSymbol clef=Bass start=0 end=400 width=16 hastwostems=False Note whitenote=F3 duration=Quarter leftside=True Note whitenote=G3 duration=Quarter leftside=False Stem duration=Quarter direction=2 top=G3 bottom=F3 end=G2 overlap=True side=2 width_to_pair=0 receiver_in_pair=False ");

    }

    /* Test a chord with
     * - 1 notes at bottom of treble clef.
     * - No accidentals
     * - Sixteenth duration
     * - Stem facing up.
     * Test that GetAboveWidth returns 1 note above the staff.
     */
    [Test]
    public void TestSixteenthDuration() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num1 = WhiteNote.BottomTreble.Number();
        MidiNote note1 = new MidiNote(0, 0, num1, quarter/4);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Treble, null);
        Assert.AreEqual(chord.ToString(),
                        "ChordSymbol clef=Treble start=0 end=100 width=16 hastwostems=False Note whitenote=F4 duration=Sixteenth leftside=True Stem duration=Sixteenth direction=1 top=F4 bottom=F4 end=G5 overlap=False side=2 width_to_pair=0 receiver_in_pair=False ");
        Assert.AreEqual(chord.AboveStaff, SheetMusic.NoteHeight);
    }

    /* Test a chord with
     * - 1 notes at bottom of treble clef.
     * - No accidentals
     * - whole duration
     * - no stem
     */
    [Test]
    public void TestWholeDuration() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num1 = WhiteNote.BottomTreble.Number();
        MidiNote note1 = new MidiNote(0, 0, num1, quarter*4);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Treble, null);
        Assert.AreEqual(chord.ToString(),
                        "ChordSymbol clef=Treble start=0 end=1600 width=16 hastwostems=False Note whitenote=F4 duration=Whole leftside=True ");

    }

    /* Test a chord with
     * - 2 notes at bottom of treble clef
     * - The notes overlap when drawn.
     * - No accidentals
     * - Quarter duration
     * - Stem facing up.
     */
    [Test]
    public void TestNotesOverlap() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num1 = WhiteNote.BottomTreble.Number();
        int num2 = num1 + 1;
        MidiNote note1 = new MidiNote(0, 0, num1, quarter);
        MidiNote note2 = new MidiNote(0, 0, num2, quarter);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        notes.Add(note2);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Treble, null);
        Assert.AreEqual(chord.ToString(),
                        "ChordSymbol clef=Treble start=0 end=400 width=25 hastwostems=False AccidSymbol accid=Sharp whitenote=F4 clef=Treble width=9 Note whitenote=F4 duration=Quarter leftside=True Note whitenote=F4 duration=Quarter leftside=True Stem duration=Quarter direction=1 top=F4 bottom=F4 end=E5 overlap=False side=2 width_to_pair=0 receiver_in_pair=False ");

    }

    /* Test a chord with
     * - 2 notes at top of treble clef
     * - The notes overlap when drawn.
     * - No accidentals
     * - Quarter duration
     * - Stem facing down.
     * - Stem is on the right side of the first note.
     */
    [Test]
    public void TestNotesOverlapStemDown() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num1 = WhiteNote.TopTreble.Number();
        int num2 = num1 + 1;
        MidiNote note1 = new MidiNote(0, 0, num1, quarter);
        MidiNote note2 = new MidiNote(0, 0, num2, quarter);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        notes.Add(note2);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Treble, null);
        Assert.AreEqual(chord.ToString(),
                        "ChordSymbol clef=Treble start=0 end=400 width=16 hastwostems=False Note whitenote=E5 duration=Quarter leftside=True Note whitenote=F5 duration=Quarter leftside=False Stem duration=Quarter direction=2 top=F5 bottom=E5 end=F4 overlap=True side=2 width_to_pair=0 receiver_in_pair=False ");

    }


    /* Test a chord with
     * - 2 notes at bottom of treble clef
     * - The notes have different durations (quarter, eighth)
     * - No accidentals
     * - Two stems: one facing up, one facing down.
     */
    [Test]
    public void TestTwoStems() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num1 = WhiteNote.BottomTreble.Number();
        int num2 = num1 + 2;
        MidiNote note1 = new MidiNote(0, 0, num1, quarter);
        MidiNote note2 = new MidiNote(0, 0, num2, quarter/2);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        notes.Add(note2);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Treble, null);
        Assert.AreEqual(chord.ToString(),
                        "ChordSymbol clef=Treble start=0 end=400 width=16 hastwostems=True Note whitenote=F4 duration=Quarter leftside=True Note whitenote=G4 duration=Eighth leftside=False Stem duration=Quarter direction=2 top=F4 bottom=F4 end=G3 overlap=False side=1 width_to_pair=0 receiver_in_pair=False Stem duration=Eighth direction=1 top=G4 bottom=G4 end=F5 overlap=True side=2 width_to_pair=0 receiver_in_pair=False ");

    }


    /* Test a chord with
     * - 2 notes at bottom of treble clef.
     * - Both notes have sharp accidentals.
     * - Quarter duration
     * - Stem facing up.
     * Test that Width returns extra space for accidentals.
     */
    [Test]
    public void TestAccidentals() {
        SheetMusic.SetNoteSize(false);
        KeySignature key = new KeySignature(0, 0);
        int quarter = 400;
        TimeSignature time = new TimeSignature(4, 4, quarter, 60000);
        int num1 = WhiteNote.BottomTreble.Number() + 1;
        int num2 = num1 + 2;
        MidiNote note1 = new MidiNote(0, 0, num1, quarter);
        MidiNote note2 = new MidiNote(0, 0, num2, quarter);
        List<MidiNote> notes = new List<MidiNote>(2);
        notes.Add(note1);
        notes.Add(note2);
        ChordSymbol chord = new ChordSymbol(notes, key, time, Clef.Treble, null);
        Assert.AreEqual(chord.ToString(), 
                       "ChordSymbol clef=Treble start=0 end=400 width=34 hastwostems=False AccidSymbol accid=Sharp whitenote=F4 clef=Treble width=9 AccidSymbol accid=Sharp whitenote=G4 clef=Treble width=9 Note whitenote=F4 duration=Quarter leftside=True Note whitenote=G4 duration=Quarter leftside=False Stem duration=Quarter direction=1 top=G4 bottom=F4 end=F5 overlap=True side=2 width_to_pair=0 receiver_in_pair=False ");

        int notewidth = 2*SheetMusic.NoteHeight + SheetMusic.NoteHeight*3/4;
        int accidwidth = 3*SheetMusic.NoteHeight;
        Assert.AreEqual(chord.MinWidth, notewidth + accidwidth);
    }

}

