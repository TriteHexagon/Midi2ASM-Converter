using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using PokéMIDIGUI;
using System.Linq;
using System.Windows;

namespace PokéMIDIGUI
{
    class Program
    {
        double notetypeCheck;
        int TicksPerBeat;
        int notetype;
        string intensity;
        int octave;
        int[] LengthSum; //stores the track length in ASM units i.e. notetype * note length
        int[] allowedNotetypes;
        int timeSignature;
        string[] notesArray;
        int Tempo;
        int TrackNumber;
        string path;
        int unitaryLength;
        List<MIDINote>[] AllTrackList;
        List<MIDINote> NoteListTemp = new List<MIDINote>();
        List<string> NoiseReplaceList = new List<string>();

        //Info from the GUI
        bool NoiseTemplate;
        bool TempoTrack;
        bool PrintWarnings;
        bool AutoSync;
        bool IgnoreRests;
        bool NewNotationStyle;
        bool CapitalHex;

        string[] Envelopes;
        int[] Dutycycles;
        int Track;
        int BaseNotetype;
        int Togglenoise;

        public Program(int Track, int BaseNotetype, int[] allowedNotetypes, bool[] GUIOptions, string[] Envelopes, int Togglenoise, int[] Dutycycles) //constructor
        {
            NoiseTemplate = GUIOptions[0];
            TempoTrack = GUIOptions[1];
            PrintWarnings = GUIOptions[2];
            AutoSync = GUIOptions[3];
            IgnoreRests = GUIOptions[4];
            NewNotationStyle = GUIOptions[5];
            CapitalHex = GUIOptions[6];
            this.Envelopes = Envelopes;
            this.Dutycycles = Dutycycles;

            this.Track = Track;
            this.BaseNotetype = BaseNotetype;
            this.allowedNotetypes = allowedNotetypes;
            this.Togglenoise = Togglenoise;
            //this. = ;

            TicksPerBeat = 0;
            octave = -1;
            intensity = "0";
            LengthSum = new int[] { 0, 0, 0, 0 };

            notesArray = new string[] { "__", "C_", "C#", "D_", "D#", "E_", "F_", "F#", "G_", "G#", "A_", "A#", "B_" };

            //Gets the current directory of the application.
            path = Directory.GetCurrentDirectory();

            //opens the midi.txt file
            if (File.Exists(string.Concat(path, "\\midi.txt")) == false)
            {
                MessageBox.Show("midi.txt not found!");
                goto SkipConvertion;
            }

            Extractor();

            if (AutoSync)
            {
                ForceSync(AllTrackList);
            }

            GBPrinter(AllTrackList);

        SkipConvertion:;
        }

        public void Extractor()
        {
            int pan = 0;
            int noteLength = 0;
            int restLength = 0;
            int tempLength = 0;
            int totalLength = 0; //the sum of noteLength and restLength.
            int trackVolume = 1; //volume of the MIDI track, can change throughout the track
            int velocity = 0; // velocity of the MIDI note
            int note = -1; //stores the value of the next note; -1 is the starting note and is a "rest" that might print only in the beginning;
            string line;
            bool flagOn = false;
            int[] MIDILength = new int[] { 0, 0, 0, 0 };

            //checks if the out.asm file already exists and clears its contents
            if (File.Exists(string.Concat(path, "\\out.asm")))
            {
                FileStream fileStream = File.Open(string.Concat(path, "\\out.asm"), FileMode.Open);
                fileStream.SetLength(0);
                fileStream.Close();
            }

            System.IO.StreamReader file =
                new System.IO.StreamReader(string.Concat(path, "\\midi.txt"));

            // Reads the file line by line.
            while ((line = file.ReadLine()) != null)
            {
                //Splits the string line into multiple elements
                string[] lineString = line.Split(' ');

                if (lineString[0] == "File:")
                {
                    continue;
                }
                //finds the TicksPerBeat and the number of tracks of the MIDI file
                if (lineString[0] == "MFile")
                {
                    if (TempoTrack) {TrackNumber = Int32.Parse(lineString[2]) - 1;}
                    else {TrackNumber = Int32.Parse(lineString[2]);}                   
                    AllTrackList = new List<MIDINote>[TrackNumber]; //creates the AllTrackList with TrackNumber Length
                    TicksPerBeat = Int32.Parse(lineString[3]);
                    unitaryLength = Convert.ToInt32((double)TicksPerBeat / (48 / BaseNotetype));
                    continue;
                }

                if (lineString[0] == "MTrk")
                {
                    Track++;
                    trackVolume = 0; //resets trackvolume for the new track
                }

                if (lineString.Length < 2)
                {
                    continue;
                }

                if (lineString[1] == "TimeSig")
                {
                    string[] timeSigString = lineString[2].Split('/');
                    timeSignature = Int32.Parse(timeSigString[0]) * 16 / Int32.Parse(timeSigString[1]);
                    continue;
                }

                //finds the Tempo of the MIDI file and converts to ASM tempo
                if (lineString[1] == "Tempo")
                {
                    Tempo = Int32.Parse(lineString[2]) / 3138;
                    continue;
                }

                if (lineString[2] == "TrkEnd" & Track > 0)
                {
                    if (Int32.TryParse(lineString[0], out restLength) & restLength != 0)
                    {
                        totalLength = noteLength + restLength;
                        if (restLength > unitaryLength) //the noise channel needs the last rest
                        {
                            NoteListTemp.Add(new MIDINote(noteLength, MIDILength[Track - 1], note, pan, velocity, trackVolume));
                            NoteListTemp.Add(new MIDINote(restLength, MIDILength[Track - 1] + noteLength, note, pan, velocity, trackVolume));
                        }
                        else
                        {
                            noteLength = totalLength;
                            NoteListTemp.Add(new MIDINote(noteLength, MIDILength[Track - 1], note, pan, velocity, trackVolume));
                        }
                       MIDILength[Track - 1] += totalLength;
                    }

                    noteLength = 0;
                    restLength = 0;
                    note = -1;

                    AllTrackList[Track - 1] = new List<MIDINote>(NoteListTemp);
                    NoteListTemp.Clear();
                }

                //avoid entering the main note GBPrinter part since the lineString is shorter and the program breaks
                if (lineString.Length < 4 || Track == 0)
                {
                    continue;
                }

                //tests and changes stereopanning
                if (lineString[3] == "c=10")
                {
                    string[] tempString = lineString[4].Split('=');
                    pan = Int32.Parse(tempString[1]);
                }

                //sets new trackvolume to define the intensity of this track. Can vary multiple times during the same music.
                if (lineString[3] == "c=7")
                {
                    string[] tempString = lineString[4].Split('=');
                    trackVolume = Int32.Parse(tempString[1]);
                }

                if (Int32.TryParse(lineString[0], out tempLength)) //checks if the first part of the string is a number and saves it to restLength
                {
                    totalLength = noteLength + tempLength + restLength;
                    //register the currently "saved" note
                    switch (lineString[1])
                    {
                        case "On": //we are on a rest
                            {
                                restLength += tempLength;
                                flagOn = true;
                                if ((restLength > unitaryLength & Track != 4 & noteLength != 0) || (restLength > unitaryLength * 16 & noteLength != 0)) //the noise channel only prints rests if its bigger than 16 times the UL
                                {
                                    if (IgnoreRests)
                                    {
                                        NoteListTemp.Add(new MIDINote(noteLength+restLength, MIDILength[Track - 1], note, pan, velocity, trackVolume));
                                    }
                                    else
                                    {
                                        NoteListTemp.Add(new MIDINote(noteLength, MIDILength[Track - 1], note, pan, velocity, trackVolume));
                                        NoteListTemp.Add(new MIDINote(restLength, MIDILength[Track - 1] + noteLength, 0, pan, velocity, trackVolume));
                                    }  
                                }
                                else
                                {
                                    NoteListTemp.Add(new MIDINote(totalLength, MIDILength[Track - 1], note, pan, velocity, trackVolume));
                                }

                                MIDILength[Track - 1] += totalLength;
                                noteLength = restLength = tempLength = totalLength = 0;

                                //setup for the next note
                                string[] tempString = lineString[3].Split('=');
                                note = Int32.Parse(tempString[1]);
                                string[] tempString2 = lineString[4].Split('=');
                                velocity = Int32.Parse(tempString2[1]);

                                break;
                            }
                        case "Off":
                            {
                                flagOn = false;
                                noteLength += tempLength;
                                break;
                            }
                        default:
                            {
                                if (!flagOn & tempLength > unitaryLength)
                                {
                                    restLength += tempLength;
                                }
                                else
                                {
                                    noteLength += tempLength;
                                }
                                break;
                            }
                    }
                }
            }
            file.Close();
        }
        public void ForceSync(List<MIDINote>[] TrackList)
        {
            List<int> PivotLocation = new List<int>();
            int ErrorSum;
            int NewNoteDuration;
            //unitaryLength = Convert.ToInt32((double)TicksPerBeat / (48 / BaseNotetype));
            for (int i = 0; i < TrackList.Length; i++)
            {
                //copies the contents of TrackList[i] into NoteListTemp
                NoteListTemp.Clear();
                PivotLocation.Clear();
                NoteListTemp = new List<MIDINote>(TrackList[i]);

                //detects the pivots and proto-pivots
                for (int j = 0; j < NoteListTemp.Count; j++)
                {
                    if (NoteListTemp[j].NoteLocation % TicksPerBeat == 0)
                    {
                    }
                    else if (NoteListTemp[j].NoteLocation % TicksPerBeat == 1) //add 1 tick, add 1 to the next note
                    {
                        NoteListTemp[j].NoteLocation--;
                        NoteListTemp[j].NoteDuration++;
                        NoteListTemp[j - 1].NoteDuration--;
                    }
                    else if (NoteListTemp[j].NoteLocation % TicksPerBeat == TicksPerBeat - 1) //remove 1 tick, add 1 from the previous note
                    {
                        NoteListTemp[j - 1].NoteDuration++;
                        NoteListTemp[j].NoteLocation++;
                        NoteListTemp[j].NoteDuration--;
                    }
                    else
                    {
                        goto SkipNote;
                    }

                    PivotLocation.Add(j);

                SkipNote:;
                }

                PivotLocation.Add(NoteListTemp.Count);
                PivotLocation.Sort();

                for (int pi = 0; pi < PivotLocation.Count - 1; pi++) // pi - Location in Pivot List
                {
                    ErrorSum = 0;
                    for (int loco = PivotLocation[pi]; loco < PivotLocation[pi + 1]; loco++) // loco - location in NoteListTemp between pivots pi and pi+1
                    {
                        NewNoteDuration = Convert.ToInt32((double)NoteListTemp[loco].NoteDuration / unitaryLength) * unitaryLength;
                        ErrorSum += NewNoteDuration - NoteListTemp[loco].NoteDuration;
                        if (ErrorSum >= unitaryLength / 2)
                        {
                            NoteListTemp[loco].NoteDuration = NewNoteDuration - unitaryLength;
                            ErrorSum -= unitaryLength;
                            NoteListTemp[pi].warnings = " ; Auto-Sync says: Rounded down!";
                        }
                        else if (ErrorSum <= -unitaryLength / 2)
                        {
                            NoteListTemp[loco].NoteDuration = NewNoteDuration + unitaryLength;
                            ErrorSum += unitaryLength;
                            NoteListTemp[pi].warnings = " ; Auto-Sync says: Rounded up!";
                        }
                        else
                        {
                            NoteListTemp[loco].NoteDuration = NewNoteDuration;
                        }
                        NoteListTemp[loco].NoteLocation += NewNoteDuration - unitaryLength;
                    }
                }
            }
        }
        public void GBPrinter(List<MIDINote>[] TrackList)
        {
            int bar = 0;
            int intIntensity = 0;
            notetype = BaseNotetype;
            int noteLengthFinal = 0;
            Track = 0;
            StreamWriter sw = new StreamWriter(string.Concat(path, "\\out.asm"), true, Encoding.ASCII);

            //Header
            sw.WriteLine(";Coverted using MIDI2ASM");
            sw.WriteLine(";Code by TriteHexagon");  
            sw.WriteLine(";Version 4.0 (17-Jun-2020)");
            sw.WriteLine(";Visit github.com/TriteHexagon/Midi2ASM-Converter for up-to-date versions.");
            sw.WriteLine("");
            sw.WriteLine("; ============================================================================================================");
            sw.WriteLine("");
            sw.WriteLine("Music_Placeholder:");
            sw.WriteLine("\tmusicheader 4, 1, Music_Placeholder_Ch1");
            sw.WriteLine("\tmusicheader 1, 2, Music_Placeholder_Ch2");
            sw.WriteLine("\tmusicheader 1, 3, Music_Placeholder_Ch3");
            sw.WriteLine("\tmusicheader 1, 4, Music_Placeholder_Ch4");
            sw.WriteLine("");

            while (Track < TrackList.Length)//reuse the same variable
            {
                sw.WriteLine("Music_Placeholder_Ch{0}:", Track + 1);
                //writes the rest of the header
                switch (Track)
                {
                    case 0:
                        sw.WriteLine("\tvolume $77");
                        sw.WriteLine("\tdutycycle ${0}", Dutycycles[0]);
                        sw.Write("\tnotetype {0}", notetype);
                        sw.WriteLine(", $a{0}",Envelopes[0]);
                        if (Tempo != 0)
                        {
                            sw.WriteLine("\ttempo {0}", Tempo);
                        }
                        break;
                    case 1:
                        sw.WriteLine("\tdutycycle ${0}", Dutycycles[1]);
                        sw.Write("\tnotetype {0}", notetype);
                        sw.WriteLine(", $a{0}", Envelopes[1]);
                        break;
                    case 2:
                        sw.Write("\tnotetype {0}", notetype);
                        sw.WriteLine(", $1{0}", Envelopes[2]);
                        break;
                    case 3:
                        sw.WriteLine("\ttogglenoise {0} ; WARNING: this might sound bad.",Togglenoise);
                        sw.Write("\tnotetype {0}", notetype);
                        sw.WriteLine();
                        break;
                    default:
                        MessageBox.Show("Number of tracks exceeded the threshold. Please reduce the number of tracks and try again.");
                        break;
                }

                for (int NoteIndex = 0; NoteIndex < TrackList[Track].Count; NoteIndex++)
                {
                    bool changedNotetypeFlag = false;
                    //copies the current note to the temporary PrintingNote
                    MIDINote PrintingNote = new MIDINote(TrackList[Track][NoteIndex]);

                    BarWriter(sw, ref bar);

                    NotetypeChange(sw, PrintingNote, ref changedNotetypeFlag);
                    noteLengthFinal = Convert.ToInt32(Math.Round((double)PrintingNote.NoteDuration / unitaryLength));

                    //calculates the intensity of the note and checks if it needs to be changed

                    IntensityChange(sw, PrintingNote, ref intIntensity, changedNotetypeFlag);

                    //changes Octave
                    if (PrintingNote.octave != octave & PrintingNote.octave != -1) //doesn't change the octave for Track 3 and for rests
                    {
                        if (Track < 2) //CHANGE: make this a setting
                            if (PrintingNote.octave - 1 == 0)
                                sw.WriteLine("\toctave 1"); //prevent octave 0
                            else
                                sw.WriteLine("\toctave {0}", PrintingNote.octave - 1); //the octave appears to be always 1 lower in the ASM...
                        if (Track == 2)
                            sw.WriteLine("\toctave {0}", PrintingNote.octave); //... except in the Wave channel
                        octave = PrintingNote.octave;
                    }

                    LengthSum[Track] += noteLengthFinal * notetype;

                    //prints note
                    NotePrinter(sw, PrintingNote, noteLengthFinal);
                }
                sw.WriteLine("\tendchannel");
                sw.WriteLine("");
                sw.WriteLine("; ============================================================================================================");
                sw.WriteLine("");

                Track++;
                octave = -1;
                intensity = "";
                bar = 0;
            }

            sw.Close();
            MessageBox.Show("Conversion Successful!");

            if (NoiseTemplate)
            {
                Console.WriteLine("Noise Templates:");
                NoiseReplaceList.Sort();
                if (File.Exists(string.Concat(path, "\\noisetemplates.txt")))
                {
                    FileStream fileStream = File.Open(string.Concat(path, "\\noisetemplates.txt"), FileMode.Open);
                    fileStream.SetLength(0);
                    fileStream.Close();
                }
                StreamWriter nt = new StreamWriter(string.Concat(path, "\\noisetemplates.txt"), true, Encoding.ASCII);
                foreach (string i in NoiseReplaceList)
                {
                    nt.WriteLine(i);
                }
                nt.Close();
            }
        }

        public void BarWriter(StreamWriter sw, ref int bar)
        {
            double newBar;
            //writes comment indicating the new bar
            newBar = Math.Floor((double)LengthSum[Track] / (double)(12 * timeSignature));
            if (newBar + 1 != bar)
            {
                while (newBar + 1 > bar)
                {
                    bar++;
                }
                sw.WriteLine(";Bar {0}", bar);
            }
        }

        public void NotePrinter(StreamWriter sw, MIDINote PrintingNote, int noteLengthFinal)
        {
            if (noteLengthFinal == 0) //makes sure actual zeros are zeros
            {
                sw.Write("\t;note ");
                NotesAndNoiseArray(sw, PrintingNote);
                sw.Write(", ");
                sw.Write(0);
                if (PrintWarnings)
                {
                    sw.Write(" | ");
                    sw.Write("WARNING: Rounded down to 0");
                }
                sw.WriteLine("");
                goto End;
            }

            while (noteLengthFinal >= 16)
            {
                sw.Write("\tnote ");
                NotesAndNoiseArray(sw, PrintingNote);
                sw.Write(", ");
                sw.WriteLine(16);
                noteLengthFinal -= 16;
            }

            if (noteLengthFinal > 0)
            {
                sw.Write("\tnote ");
                NotesAndNoiseArray(sw, PrintingNote);
                sw.Write(", ");
                sw.Write(noteLengthFinal);
                if (notetypeCheck != 0 & PrintWarnings == true)
                {
                    sw.Write(" ; ");
                    sw.Write("WARNING: Rounded.");
                }
                if (PrintWarnings & AutoSync)
                {
                    sw.Write(PrintingNote.warnings);
                }
                sw.WriteLine("");
            }
        End:;
        }

        public void NotesAndNoiseArray(StreamWriter sw, MIDINote PrintingNote)
        {
            string NoiseTemplateString;
            if (Track == 3 & NoiseTemplate) //checks if the noise replace mode is true
            {
                if (PrintingNote.RawNote == 0)
                {
                    NoiseTemplateString = "__";
                }
                else
                {
                    NoiseTemplateString = "N" + octave + PrintingNote.RawNote.ToString("X");
                }
                sw.Write(NoiseTemplateString);
                if (!NoiseReplaceList.Contains(NoiseTemplateString) & NoiseTemplateString != "__")
                {
                    NoiseReplaceList.Add(NoiseTemplateString);
                }
            }
            else
            {
                sw.Write(notesArray[PrintingNote.RawNote]);
            }
        }

        public void NotetypeChange(StreamWriter sw, MIDINote PrintingNote, ref bool changedNotetypeFlag)
        {
            int newNotetype = BaseNotetype; //always tries to change the notetype to the BaseNotetype; this isn't ideal but works fine for now
            unitaryLength = Convert.ToInt32((double)TicksPerBeat * newNotetype / 48);
            //checks if there's need to change the notetype i.e. if remainder of the division by the unitary length is not 0
            notetypeCheck = Convert.ToInt32((double)(PrintingNote.NoteDuration % unitaryLength));

            for (int pos = 0; pos < allowedNotetypes.Length & notetypeCheck != 0; pos++)
            {
                newNotetype = allowedNotetypes[pos];
                unitaryLength = Convert.ToInt32((double)TicksPerBeat * newNotetype / 48);
                notetypeCheck = PrintingNote.NoteDuration % unitaryLength;
            }

            if (newNotetype != notetype)
            {
                changedNotetypeFlag = true;
                notetype = newNotetype;
                sw.Write("\tnotetype {0}", notetype);
                if (Track < 2)
                {
                    sw.WriteLine(", ${0}7", intensity);
                }
                else if (Track == 2)
                {
                    sw.WriteLine(", ${0}0", intensity);
                }
                else
                {
                    sw.WriteLine();
                }
            }

            notetypeCheck = Convert.ToInt32((double)(PrintingNote.NoteDuration % unitaryLength));
        }

        public void IntensityChange(StreamWriter sw, MIDINote PrintingNote, ref int intIntensity, bool changedNotetypeFlag)
        {
            string hexIntensity;
            intIntensity = PrintingNote.intensity / 1075;
            if (Track == 2)
            {
                if (intIntensity < 4)
                    intIntensity = 3;
                else if (intIntensity < 11)
                    intIntensity = 2;
                else
                    intIntensity = 1;
            }
            if (CapitalHex)
            {
                hexIntensity = intIntensity.ToString("X"); //converts intensity value into hexadecimal
            }
            else
            {
                hexIntensity = intIntensity.ToString("x"); //converts intensity value into hexadecimal
            }
            
            if (Track < 3 & hexIntensity != intensity) //doesn't calculate the intensity for Track 4 and if it doesn't need to be changed
            {
                intensity = hexIntensity;
                if (!changedNotetypeFlag)
                {
                    sw.Write("\tintensity $");
                    sw.Write(intensity);
                    if (Track == 0)
                        sw.WriteLine(Envelopes[0]);
                    else if (Track == 1)
                        sw.WriteLine(Envelopes[1]);
                    else if (Track == 2)
                        sw.WriteLine(Envelopes[2]); // defaults to Wave 0 in the wave channel
                }
            }
        }
    }
}
