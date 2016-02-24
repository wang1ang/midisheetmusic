/*
 * Copyright (c) 2011-2012 Madhav Vaidyanathan
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

/** @class PlayMeasuresInLoop
 * This class displays the dialog used for the "Play Measures in Loop" feature.
 * It displays:
 * - A checkbox to enable this feature
 * - Two numeric spinboxes, to select the start and end measures.
 *
 * When the user clicks OK:
  * - IsEnabled() returns true if the "play in loop" feature is enabled.
  * - GetStartMeasure() returns the start measure of the loop
  * - GetEndMeasure() returns the end measure of the loop
 */
public class PlayMeasuresDialog {

    private Form dialog;                 /** The dialog box */
    private NumericUpDown startMeasure;  /** The starting measure */
    private NumericUpDown endMeasure;    /** The ending measure */
    private CheckBox enable;             /** Whether to enable or not */

    /** Create a new PlayMeasuresDialog. Call the ShowDialog() method
     *  to display the dialog.
     */
    public PlayMeasuresDialog(MidiFile midifile) {
        int lastStart = midifile.EndTime();
        int lastMeasure = 1 + lastStart / midifile.Time.Measure;

        /* Create the dialog box */
        dialog = new Form();
        dialog.Text = Strings.playMeasuresTitle;
        dialog.MaximizeBox = false;
        dialog.MinimizeBox = false;
        dialog.ShowInTaskbar = false;
        dialog.Icon = new Icon(GetType(), "Resources.Images.NotePair.ico");
        dialog.AutoScroll = true;

        Font font = dialog.Font;
        dialog.Font = new Font(font.FontFamily, font.Size * 1.4f);
        int labelheight = dialog.Font.Height * 2;
        int xpos = labelheight/2;
        int ypos = labelheight/2;

        enable = new CheckBox();
        enable.Parent = dialog;
        enable.Text = Strings.playMeasuresTitle;
        enable.Checked = false;
        enable.Location = new Point(xpos, ypos);
        enable.Size = new Size(labelheight*9, labelheight);

        ypos += labelheight * 3/2;

        Label label = new Label();
        label.Parent = dialog;
        label.Text = Strings.startMeasure;
        label.Location = new Point(xpos, ypos);
        label.Size = new Size(labelheight * 3, labelheight);

        xpos += labelheight * 4;

        startMeasure = new NumericUpDown();
        startMeasure.Parent = dialog;
        startMeasure.Minimum = 1;
        startMeasure.Maximum = lastMeasure;
        startMeasure.Value = 1;
        startMeasure.Location = new Point(xpos, ypos);
        startMeasure.Size = new Size(labelheight*2, labelheight);

        xpos = labelheight/2;
        ypos += labelheight * 3/2;

        label = new Label();
        label.Parent = dialog;
        label.Text = Strings.endMeasure;
        label.Location = new Point(xpos, ypos);
        label.Size = new Size(labelheight * 3, labelheight);

        xpos += labelheight * 4;
        endMeasure = new NumericUpDown();
        endMeasure.Parent = dialog;
        endMeasure.Minimum = 1;
        endMeasure.Maximum = lastMeasure;
        endMeasure.Value = lastMeasure;
        endMeasure.Location = new Point(xpos, ypos);
        endMeasure.Size = new Size(labelheight*2, labelheight);

        /* Create the OK button */
        xpos = labelheight/2;
        ypos += labelheight * 3/2;
        Button ok = new Button();
        ok.Parent = dialog;
        ok.Text = Strings.okButton;
        ok.Location = new Point(xpos, ypos);
        ok.DialogResult = DialogResult.OK;

        dialog.Size = new Size(labelheight * 10,
                               labelheight * 8);

    }


    /** Display the InstrumentDialog. 
     *  This always returns DialogResult.OK.
     */
    public DialogResult ShowDialog() {
        return dialog.ShowDialog();
    }


    /** Get/set the enabled value */
    public bool Enabled {
        get { return enable.Checked; }
        set { enable.Checked = value; }
    }

    /** Get/Set the start measure.
     *  Internally we count measures starting from 0, 
     *  so decrement the value by 1.
     */
    public int StartMeasure {
        get { return (int)startMeasure.Value - 1; }
        set { startMeasure.Value = value + 1; }
    }

    /** Get/Set the end measure.
     *  Internally we count measures starting from 0, 
     *  so decrement the value by 1.
     */
    public int EndMeasure {
        get { return (int)endMeasure.Value - 1; }
        set { endMeasure.Value = value + 1; } 
    }
    
    public void Dispose() {
        dialog.Dispose();
    }

}

}

