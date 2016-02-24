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

/** @class SampleSongDialog
 * The SampleSongDialog is used to select one of the 50+ sample
 * midi songs that ship with MidiSheetMusic.
 *
 * The method ShowDialog() returns DialogResult.OK/Cancel.
 * The method GetSong() returns the selected song.
 */
public class SampleSongDialog : Form {

    private ListView songView;    /** The list of songs **/
    private Button ok;            /** The ok button */
    private Button cancel;        /** The cancel button */

    /** Create a new SampleSongDialog.  Call the ShowDialog() method
     * to display the dialog.
     */
    public SampleSongDialog() {

        /* Create the dialog box */
        Text = Strings.sampleSongTitle;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Icon = new Icon(GetType(), "Resources.Images.NotePair.ico");
        AutoScroll = true;
        Font font = this.Font;
        this.Font = new Font(font.FontFamily, font.Size * 1.4f);
        int labelheight = Font.Height * 2;
        Size = new Size(labelheight * 22, labelheight * 8);

        Bitmap noteImage = new Bitmap(typeof(SampleSongDialog), "Resources.Images.SmallNotePair.png");
        ImageList imagelist = new ImageList();
        imagelist.Images.Add(noteImage);

        songView = new ListView();
        songView.Location = new Point(labelheight/2, labelheight/2);
        songView.Size = new Size(ClientSize.Width - labelheight*4,
                                 ClientSize.Height - labelheight);
        songView.Sorting = SortOrder.Ascending;
        songView.MultiSelect = false;
        songView.HideSelection = false;
        songView.View = View.List;
        songView.SmallImageList = imagelist;
        songView.LargeImageList = imagelist;

        string[] songs = new string[] {
            "Bach__Invention_No._13",
            "Bach__Minuet_in_G_major",
            "Bach__Musette_in_D_major",
            "Bach__Prelude_in_C_major",
            "Beethoven__Fur_Elise",
            "Beethoven__Minuet_in_G_major",
            "Beethoven__Moonlight_Sonata",
            "Beethoven__Sonata_Pathetique_2nd_Mov",
            "Bizet__Habanera_from_Carmen",
            "Borodin__Polovstian_Dance",
            "Brahms__Hungarian_Dance_No._5",
            "Brahms__Waltz_No._15_in_A-flat_major",
            "Brahms__Waltz_No._9_in_D_minor",
            "Chopin__Minute_Waltz_Op._64_No._1_in_D-flat_major",
            "Chopin__Nocturne_Op._9_No._1_in_B-flat_minor",
            "Chopin__Nocturne_Op._9_No._2_in_E-flat_major",
            "Chopin__Nocturne_in_C_minor",
            "Chopin__Prelude_Op._28_No._20_in_C_minor",
            "Chopin__Prelude_Op._28_No._4_in_E_minor",
            "Chopin__Prelude_Op._28_No._6_in_B_minor",
            "Chopin__Prelude_Op._28_No._7_in_A_major",
            "Chopin__Waltz_Op._64_No._2_in_Csharp_minor",
            "Clementi__Sonatina_Op._36_No._1",
            "Easy_Songs__Brahms_Lullaby",
            "Easy_Songs__Greensleeves",
            "Easy_Songs__Jingle_Bells",
            "Easy_Songs__Silent_Night",
            "Easy_Songs__Twinkle_Twinkle_Little_Star",
            "Field__Nocturne_in_B-flat_major",
            "Grieg__Canon_Op._38_No._8",
            "Grieg__Peer_Gynt_Morning",
            "Handel__Sarabande_in_D_minor",
            "Liadov__Prelude_Op._11_in_B_minor",
            "MacDowelll__To_a_Wild_Rose",
            "Massenet__Elegy_in_E_minor",
            "Mendelssohn__Venetian_Boat_Song_Op._19b_No._6",
            "Mendelssohn__Wedding_March",
            "Mozart__Aria_from_Don_Giovanni",
            "Mozart__Eine_Kleine_Nachtmusik",
            "Mozart__Fantasy_No._3_in_D_minor",
            "Mozart__Minuet_from_Don_Juan",
            "Mozart__Rondo_Alla_Turca",
            "Mozart__Sonata_K.545_in_C_major",
            "Offenbach__Barcarolle_from_The_Tales_of_Hoffmann",
            "Pachelbel__Canon_in_D_major",
            "Prokofiev__Peter_and_the_Wolf",
            "Puccini__O_Mio_Babbino_Caro",
            "Rebikov__Valse_Melancolique_Op._2_No._3",
            "Saint-Saens__The_Swan",
            "Satie__Gnossienne_No._1",
            "Satie__Gymnopedie_No._1",
            "Schubert__Impromptu_Op._90_No._4_in_A-flat_major",
            "Schubert__Moment_Musicaux_No._1_in_C_major",
            "Schubert__Moment_Musicaux_No._3_in_F_minor",
            "Schubert__Serenade_in_D_minor",
            "Schumann__Scenes_From_Childhood_Op._15_No._12",
            "Schumann__The_Happy_Farmer",
            "Strauss__The_Blue_Danube_Waltz",
            "Tchaikovsky__Album_for_the_Young_-_Old_French_Song",
            "Tchaikovsky__Album_for_the_Young_-_Polka",
            "Tchaikovsky__Album_for_the_Young_-_Waltz",
            "Tchaikovsky__Nutcracker_-_Dance_of_the_Reed_Flutes",
            "Tchaikovsky__Nutcracker_-_Dance_of_the_Sugar_Plum_Fairies",
            "Tchaikovsky__Nutcracker_-_March_of_the_Toy_Soldiers",
            "Tchaikovsky__Nutcracker_-_Waltz_of_the_Flowers",
            "Tchaikovsky__Swan_Lake",
            "Verdi__La_Donna_e_Mobile"
        };

        foreach (string name in songs) {
            /* Change "Bach__Minuet_in_G" to "Bach: Minuet in G" */
            string song = name;
            song = song.Replace("__", ": ");
            song = song.Replace("_", " ");
            ListViewItem item = new ListViewItem(song);
            item.ImageIndex = 0;
            songView.Items.Add(item);
        }

        songView.Parent = this;

        /* Create the OK and Cancel buttons */
        ok = new Button();
        ok.Parent = this;
        ok.Text = Strings.okButton;
        ok.Location = new Point(ClientSize.Width - labelheight*3, labelheight);
        ok.DialogResult = DialogResult.OK;

        cancel = new Button();
        cancel.Parent = this;
        cancel.Text = Strings.cancelButton;
        cancel.Location = new Point(ClientSize.Width - labelheight*3, 
                                    labelheight*2 + labelheight/4);
        cancel.DialogResult = DialogResult.Cancel;

    }

    /** When the dialog is resized, adjust the size of the songView.
     *  Change the position of the ok/cancel buttons relative to the
     *  right side of the dialog.
     */
    protected override void OnResize(EventArgs e) {
        int labelheight = Font.Height * 2;
        base.OnResize(e);
        if (songView != null) {
            songView.Size = new Size(ClientSize.Width - labelheight*4,
                                     ClientSize.Height - labelheight);
        }
        if (ok != null) {
            ok.Location = new Point(ClientSize.Width - labelheight*3, labelheight);
        }
        if (cancel != null) {
            cancel.Location = new Point(ClientSize.Width - labelheight*3, 
                                        labelheight*2 + labelheight/4);
        }
    }

    /** Get the currently selected song */
    public string GetSong() {
        string song = null;
        foreach (ListViewItem item in songView.Items) {
            if (item.Selected) {
                song = item.Text;
                break;
            }
        }
        if (song == null) {
            song = songView.Items[0].Text;
        }
        song = song.Replace(": ", "__");
        song = song.Replace(" ", "_");
        return song;
    }
}

}

