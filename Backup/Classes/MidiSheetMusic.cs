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
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;

namespace MidiSheetMusic {

/** @class MidiSheetMusic
 * MidiSheetMusic is simply the class containing the Main() method 
 */
public class MidiSheetMusic {


    /** Check if the Timidity MIDI player is installed */
    private static void CheckTimidity() {
        FileInfo info = new FileInfo("/usr/bin/timidity");
        if (!info.Exists) {
            string message = "The Timidity MIDI player is not installed.\n";
            message += "Therefore, the MIDI audio sound will not work.\n";
            message += "To install Timidity on Ubuntu Linux, run the command:\n";
            message += "# sudo apt-get install timidity\n";

            MessageBox.Show(message, "MIDI Audio is disabled",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /** The Main function for this application. */
    [STAThread]
    public static void Main(string[] argv) {
        Application.EnableVisualStyles();
        SheetMusicWindow form = new SheetMusicWindow();
        if (Type.GetType("Mono.Runtime") != null) {
            CheckTimidity();
        }
        if (argv.Length == 1) {
            form.OpenMidiFile(argv[0]);
        }
        Application.Run(form);
    }
}
}

