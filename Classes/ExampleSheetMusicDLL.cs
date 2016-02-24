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

/** An example of using just the SheetMusic.dll library.
 *  To compile run:
 *   # csc /target:exe ExampleSheetMusicDLL.cs /reference:SheetMusic.dll
 *   # ExampleSheetMusicDLL.exe sample.mid
 */
using System;
using System.Windows.Forms;
using System.Drawing;
using MidiSheetMusic;

public class ExampleSheetMusicDLL {

    [STAThread]
    public static void Main(string[] argv) {
        if (argv.Length < 1) {
            Console.WriteLine("Usage: ExampleSheetMusicDLL filename.mid");
            return;
        }
        string filename = argv[0];
        Form form = new Form();
        SheetMusic sheet = new SheetMusic(filename, null);
        sheet.Parent = form;
        form.Size = new Size(600, 400);
        form.AutoScroll = true;
        Application.Run(form);
    }
}


