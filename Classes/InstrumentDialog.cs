/*
 * Copyright (c) 2011 Madhav Vaidyanathan
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
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MidiSheetMusic {

/** @class InstrumentDialog
 * The InstrumentDialog is used to select what instrument to use
 * for each track, when playing the music.
 */
public class InstrumentDialog {

    private ComboBox[] instrumentChoices; /** The instruments to use per track */
    private Form dialog;              /** The dialog box */

    /** Create a new InstrumentDialog.  Call the ShowDialog() method
     * to display the dialog.
     */
    public InstrumentDialog(MidiFile midifile) {

        /* Create the dialog box */
        dialog = new Form();
        Font font = dialog.Font;
        dialog.Font = new Font(font.FontFamily, font.Size * 1.4f);
        int unit = dialog.Font.Height;
        int xstart = unit * 2;
        int ystart = unit * 2;
        int labelheight = unit * 2;
        int maxwidth = 0;

        dialog.Text = Resources.Localization.Strings.chooseInstrumentsTitle;
        dialog.MaximizeBox = false;
        dialog.MinimizeBox = false;
        dialog.ShowInTaskbar = false;
        dialog.Icon = new Icon(GetType(), "Resources.Images.NotePair.ico");
        dialog.AutoScroll = true;

        List<MidiTrack> tracks = midifile.Tracks;
        instrumentChoices = new ComboBox[tracks.Count];

        /* For each midi track, create a label with the track number
         * ("Track 2"), and a ComboBox containing all the possible
         * midi instruments. Add the text "(default)" to the instrument
         * specified in the midi file.
         */
        for (int i = 0; i < tracks.Count; i++) {
            int num = i+1;
            Label label = new Label();
            label.Parent = dialog;
            label.Text = "Track " + num;
            label.TextAlign = ContentAlignment.MiddleRight;
            label.Location = new Point(xstart, ystart + i*labelheight);
            label.AutoSize = true;
            maxwidth = Math.Max(maxwidth, label.Width);
        }
        for (int i = 0; i < tracks.Count; i++) {
            instrumentChoices[i] = new ComboBox();
            instrumentChoices[i].DropDownStyle = ComboBoxStyle.DropDownList; 
            instrumentChoices[i].Parent = dialog;
            instrumentChoices[i].Location = new Point(xstart + maxwidth * 3/2, ystart + i*labelheight);
            instrumentChoices[i].Size = new Size(labelheight * 8, labelheight);

            for (int instr = 0; instr < MidiFile.Instruments.Length; instr++) {
                string name = MidiFile.Instruments[instr];
                if (tracks[i].Instrument == instr) {
                    name += " (default)";
                }
                instrumentChoices[i].Items.Add(name);
            }
            instrumentChoices[i].SelectedIndex = tracks[i].Instrument;
        }

        /* Create the "Set All To Piano" button */
        Button allPiano = new Button();
        allPiano.Parent = dialog;
        allPiano.Text = Resources.Localization.Strings.setAllPiano;
        allPiano.Location = new Point(xstart + maxwidth * 3/2, 
                                      ystart + tracks.Count * labelheight);
        allPiano.Click += new EventHandler(SetAllPiano);
        allPiano.Size = new Size(labelheight * 5, labelheight); 

        /* Create the OK and Cancel buttons */
        int ypos = ystart + (tracks.Count + 3) * labelheight;
        Button ok = new Button();
        ok.Parent = dialog;
        ok.Text = Resources.Localization.Strings.okButton;
        ok.Location = new Point(xstart, ypos);
        ok.DialogResult = DialogResult.OK;

        Button cancel = new Button();
        cancel.Parent = dialog;
        cancel.Text = Resources.Localization.Strings.cancelButton;
        cancel.Location = new Point(ok.Location.X + ok.Width + labelheight/2, ypos);
        cancel.DialogResult = DialogResult.Cancel;

        dialog.Size = new Size(instrumentChoices[0].Location.X + instrumentChoices[0].Width + 50,
                               cancel.Location.Y + cancel.Size.Height + 50);
    }

    /** Display the InstrumentDialog.
     * Return DialogResult.OK if "OK" was clicked.
     * Return DialogResult.Cancel if "Cancel" was clicked.
     */
    public DialogResult ShowDialog() {
        int[] oldInstruments = this.GetInstruments();
        DialogResult result = dialog.ShowDialog();
        if (result == DialogResult.Cancel) {
            /* If the user clicks 'Cancel', restore the old instruments */
            for (int i = 0; i < instrumentChoices.Length; i++) {
                instrumentChoices[i].SelectedIndex = oldInstruments[i];
            }
        }
        return result;
    }

    /** Set all the instrument choices to "Acoustic Grand Piano",
     *  unless the instrument is Percussion (128).
     */
    private void SetAllPiano(object sender, EventArgs args) {
        for (int i = 0; i < instrumentChoices.Length; i++) {
            if (instrumentChoices[i].SelectedIndex != 128) {
                instrumentChoices[i].SelectedIndex = 0;
            }
        }
    }

    public void Dispose() {
        dialog.Dispose();
    }

    /** Get the instruments currently selected */
    public int[] Instruments {
        get { return GetInstruments(); }
        set { SetInstruments(value); }
    }
    int[] GetInstruments() {
        int[] result = new int[ instrumentChoices.Length ];
        for (int i = 0; i < instrumentChoices.Length; i++) {
            if (instrumentChoices[i].SelectedIndex == -1) {
                instrumentChoices[i].SelectedIndex = 0;
            }
            result[i] = instrumentChoices[i].SelectedIndex;
        }
        return result;
    }

    /** Set the selected instruments */
    void SetInstruments(int[] values) {
        if (values == null || values.Length != instrumentChoices.Length) {
            return;
        }
        for (int i = 0; i < values.Length; i++) {
            instrumentChoices[i].SelectedIndex = values[i];
        }
    }


    /** Return true if all the default instruments are selected */
    public bool isDefault() {
        bool result = true;
        for (int i = 0; i < instrumentChoices.Length; i++) {
            if (instrumentChoices[i].SelectedIndex == -1) {
                instrumentChoices[i].SelectedIndex = 0;
            }
            string name = (string) instrumentChoices[i].SelectedItem;
            if (!name.Contains("default")) {
                result = false;
            }
        }
        return result;
    }

}

}

