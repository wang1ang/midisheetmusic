/*
 * Copyright (c) 2007-2012 Madhav Vaidyanathan
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

/** @class ConfigINI
 *
 * The ConfigINI class represents an INI configuration file.
 * The file format is:
 *
 * [section name]
 * name1=value1
 * name2=value2
 *
 * [section2 name]
 * name1=value1
 * name2=value2
 *
 */

public class SectionINI {
    public string Section;    /* The section title */
    public Dictionary<string,string> Properties;  /* The section name/value properties */

    public SectionINI() {
        Properties = new Dictionary<string,string>();
    }

    static Color ParseColor(string value) {
        try {
            string[] rgb = value.Split(new Char[] {' '} );
            if (rgb != null && rgb.Length == 3) {
                Color color = Color.FromArgb( Int32.Parse(rgb[0]), Int32.Parse(rgb[1]), Int32.Parse(rgb[2]) );
                return color;
            }
            return Color.White;
        }
        catch (Exception e) {
            return Color.White;
        }
    }

    public string GetString(string key) {
        if (Properties.ContainsKey(key)) {
            return Properties[key];
        }
        else {
            return null;
        }
    }

    public bool GetBool(string key) {
        try {
            string value = Properties[key];
            bool boolvalue = Boolean.Parse(value);
            return boolvalue;
        }
        catch (Exception e) {
            return false;
        }
    }

    public int GetInt(string key) {
        try {
            string value = Properties[key];
            int intvalue = Int32.Parse(value);
            return intvalue;
        }
        catch (Exception e) {
            return 0;
        }
    }

    public Color GetColor(string key) {
        try {
            string value = Properties[key];
            return ParseColor(value);
        }
        catch (Exception e) {
            return Color.White;
        }
    }

    public string[] GetArray(string key) {
        try {
            string value = Properties[key];
            return value.Split(new Char[] {','} );
        }
        catch (Exception e) {
            return null;
        }
    }

    public bool[] GetBoolArray(string key) {
        string[] strarray = GetArray(key);
        if (strarray == null) {
            return null;
        }
        bool[] result = new bool[strarray.Length];
        for (int i = 0; i < result.Length; i++) {
            try {
                result[i] = Boolean.Parse(strarray[i]);
            }
            catch (Exception e) {}
        }
        return result;
    }

    public int[] GetIntArray(string key) {
        string[] strarray = GetArray(key);
        if (strarray == null) {
            return null;
        }
        int[] result = new int[strarray.Length];
        for (int i = 0; i < result.Length; i++) {
            try {
                result[i] = Int32.Parse(strarray[i]);
            }
            catch (Exception e) {}
        }
        return result;
    }

    public Color[] GetColorArray(string key) {
        string[] strarray = GetArray(key);
        if (strarray == null) {
            return null;
        }
        Color[] result = new Color[strarray.Length];
        for (int i = 0; i < result.Length; i++) {
            Color c = ParseColor(strarray[i]);
            if (c == Color.White) {
                return null;
            }
            result[i] = c; 
        }
        return result;
    }
}


public class ConfigINI {

    private string filename;
    private List<SectionINI> sections;

    public ConfigINI(string filename) {
        this.filename = filename;
        Load();
    }

    /* Load and parse the INI file into a dictionary mapping
     * section titles to the name/value pairs under the section.
     */
    void Load() {
        sections = new List<SectionINI>();
        try {
            StreamReader stream = new StreamReader(filename);
            string line = null;
            SectionINI section = null;
            while ( (line = stream.ReadLine()) != null) {
                if (line == "" || line.StartsWith(";")) {
                    continue;
                }
                else if (line.StartsWith("[")) {
                    string title = line.Replace("[", "").Replace("]", "");
                    section = new SectionINI();
                    section.Section = title;
                    sections.Add(section);
                }
                else {
                    string[] pair = line.Split(new Char[] {'='} );
                    if (pair != null && pair.Length == 2 && section != null) {
                        string key = pair[0]; string value = pair[1];
                        section.Properties[key] = value;
                    }
                }
            }
            stream.Close();
        }
        catch (Exception e) {
        }
    }

    /* Save the most recent 20 sections */
    public void Save() {
        int maxsections = 20;
        try {
            StreamWriter stream = new StreamWriter(filename, false);
            for (int i = 0; i < sections.Count; i++) {
                if (i >= maxsections) {
                    break;
                }
                SectionINI section = sections[i];
                stream.WriteLine("[" + section.Section + "]"); 
                foreach (string key in section.Properties.Keys) {
                    stream.WriteLine(key + "=" + section.Properties[key]);
                }
                stream.WriteLine();
            }
            stream.Flush();
            stream.Close();
        }
        catch (Exception e) {
        }
    }

    /* Retrieve the section with the given title.
     * If found, move that section to the front of the list.
     */
    public SectionINI GetSection(string title) {
        SectionINI match = null;
        foreach (SectionINI section in sections) {
            if (section.Section == title) {
                match = section; break;
            }
        }
        if (match != null) {
            sections.Remove(match);
            sections.Insert(0, match);
        }
        return match;
    }


    /* Return the first section */
    public SectionINI FirstSection() {
        if (sections.Count > 0) {
            return sections[0];
        }
        else {
            return null;
        }
    }

    /* Add the given section to the Config INI, and save it to disk */
    public void AddSection(SectionINI section) {
        for (int i = 0; i < sections.Count; i++) {
            if (sections[i].Section == section.Section) {
                sections.RemoveAt(i);
                i--;
            }
        }
        sections.Insert(0, section);
    }

    /* Return a list of the first 10 filenames */
    public List<string> GetRecentFilenames() {
        List<string> result = new List<string>();
        int total = 0;
        foreach (SectionINI section in sections) {
            if (section.GetString("filename") != null) {
                result.Add(section.GetString("filename"));
                total++;
            }
            if (total >= 10) {
                break;
            }
        }
        return result;
    }
}

}
