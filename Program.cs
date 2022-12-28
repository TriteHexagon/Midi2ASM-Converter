using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Melanchall.DryWetMidi.Core;
using System.Diagnostics;

namespace MIDI2ASMGUI
{
    class Program
    {
        double notetypeCheck;
        int TimeDivision;
        int notetype;
        string StringIntensity;
        int octave;
        int[] LengthSum; //stores the track length in ASM units i.e. notetype * note length
        int[] allowedNotetypes;
        int timeSignature;
        string[] notesArray;
        int Tempo;
        int NumberOfTracks = 0; //compatibility with old Extractor
        string directoryPath;
        string[] pathSplit;
        string midiFileName;
        string midiFileExtension;
        string ASMFileName = "out"; //used for the name of the output file
        string NotationStyle;
        int unitaryLength;
        int[] unitaryLengthArray;
        List<MIDINote>[] AllTrackList;
        List<MIDINote> NoteListTemp = new List<MIDINote>();
        List<string> NoiseReplaceList = new List<string>();

        //Info from the GUI
        bool NoiseTemplate;
        bool TempoTrack;
        bool PrintWarnings;
        bool AutoSync;
        bool IgnoreRests;
        bool CapitalHex;
        bool IgnoreIntensity;
        bool RawMIDI;
        bool DebugFlag;
        bool ForceTimeSignature;

        string[] Envelopes;
        int[] Dutycycles;
        int[] TimeSignatureOverride;
        int TrackNumber = 0;
        int BaseNotetypeLocation;
        int Togglenoise;

        public Program(int BaseNotetypeLocation, int[] allowedNotetypes, bool[] GUIOptions, string[] Envelopes, int Togglenoise, int[] Dutycycles, int[] TimeSignatureOverride, string filePath, string NotationStyle) //constructor
        {
            RawMIDI = false; //should come from the GUI!
            DebugFlag = false;

            NoiseTemplate = GUIOptions[0];
            TempoTrack = GUIOptions[1];
            PrintWarnings = GUIOptions[2];
            AutoSync = GUIOptions[3];
            IgnoreRests = GUIOptions[4];
            CapitalHex = GUIOptions[5];
            IgnoreIntensity = GUIOptions[7];
            ForceTimeSignature = false;
            this.Envelopes = Envelopes;
            this.Dutycycles = Dutycycles;
            this.TimeSignatureOverride = TimeSignatureOverride;
            if (TimeSignatureOverride[0] != 0 & TimeSignatureOverride[1] != 0)
            {
                ForceTimeSignature = true;
            }
            this.BaseNotetypeLocation = BaseNotetypeLocation;
            this.allowedNotetypes = allowedNotetypes;
            this.Togglenoise = Togglenoise;
            this.NotationStyle = NotationStyle;

            //takes care of the filename and extension
            pathSplit = filePath.Split('\\');
            directoryPath = filePath.Substring(0, filePath.Length - pathSplit[pathSplit.Length-1].Length);
            pathSplit = pathSplit[pathSplit.Length - 1].Split('.');
            midiFileName = pathSplit[0];
            midiFileExtension = pathSplit[1];
            if(!GUIOptions[6])
            {
                ASMFileName = midiFileName;
            }

            unitaryLengthArray = new int[allowedNotetypes.Length];
            TimeDivision = 0;
            octave = -1;
            if (CapitalHex)
                StringIntensity = "A";
            else 
                StringIntensity = "a";
            LengthSum = new int[] { 0, 0, 0, 0 };

            notesArray = new string[] { "__", "C_", "C#", "D_", "D#", "E_", "F_", "F#", "G_", "G#", "A_", "A#", "B_" };

            //Gets the current directory of the application.
            //path = Directory.GetCurrentDirectory();

            ////opens the midi.txt file
            //if (File.Exists(midiFilePath) == false)
            //{
            //    MessageBox.Show("midi.mid not found!");
            //    goto SkipConvertion;
            //}

            //checks if the out.asm file already exists and clears its contents
            if (File.Exists(string.Concat(directoryPath, ASMFileName, ".asm")))
            {
                FileStream fileStream = File.Open(string.Concat(directoryPath, ASMFileName, ".asm"), FileMode.Open);
                fileStream.SetLength(0);
                fileStream.Close();
            }

            Extractor();

            if (AutoSync)
            {
                ForceSync(AllTrackList);
            }

            GBPrinter(AllTrackList);

            if (NotationStyle != "PCLegacy")
            {
                GBConverter(directoryPath, ASMFileName, NotationStyle);
            }
            MessageBox.Show("Conversion Successful!");
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
            bool flagNote = false;
            int[] MIDILength = new int[] { 0, 0, 0, 0 };

            var midifile = MidiFile.Read(directoryPath + midiFileName + "." + midiFileExtension);
            var trackChuncks = midifile.GetTrackChunks(); // contains the information for all tracks

            if (TempoTrack) 
            { 
                NumberOfTracks = trackChuncks.Count() - 1;
                TrackNumber = -1;
            }
            else 
            { 
                NumberOfTracks = trackChuncks.Count(); 
            }

            //finds the TimeDivision and the number of tracks of the MIDI file
            string[] tempDivision = midifile.TimeDivision.ToString().Split(' ');
            TimeDivision = Int32.Parse(tempDivision[0]);
            for (int k = 0; k < allowedNotetypes.Length; k++)
            {
                unitaryLengthArray[k] = Convert.ToInt32((double)TimeDivision / (48 / allowedNotetypes[k]));
            }
            unitaryLength = unitaryLengthArray[BaseNotetypeLocation];

            AllTrackList = new List<MIDINote>[NumberOfTracks]; //creates the AllTrackList with NumberOfTracks Length
            // Reads each track in sequence

            foreach (var track in trackChuncks)
            {
                TrackNumber++;
                //if (TrackNumber <= 0)
                   // goto SkippedTrack;
                trackVolume = 1; //resets trackvolume for the new track

                //loops through events
                foreach (MidiEvent Level2 in track.Events)
                {
                    if (RawMIDI)
                    {
                        //get the Delta Time of the event
                        Trace.Write(Level2.DeltaTime);
                        Trace.Write(" - ");
                        Trace.WriteLine(Level2);
                    }
                    tempLength = (int)Level2.DeltaTime;
                    totalLength = noteLength + tempLength + restLength;

                    switch (Level2.EventType.ToString())
                    {
                        case "ControlChange":
                            {
                                ControlChangeEvent TempEvent = (ControlChangeEvent)Level2;
                                //tests and changes stereopanning
                                if (TempEvent.ControlNumber == 10)
                                {
                                    pan = TempEvent.ControlValue;
                                }
                                //sets new trackvolume to define the intensity of this track. Can vary multiple times during the same music.
                                if (TempEvent.ControlNumber == 7)
                                {
                                    trackVolume = TempEvent.ControlValue;
                                }
                                break;
                            }
                        //only relevant for the first track
                        case "TimeSignature":
                            {
                                if (!ForceTimeSignature)
                                {
                                    TimeSignatureEvent TempEvent = (TimeSignatureEvent)Level2;
                                    timeSignature = TempEvent.Numerator * 16 / TempEvent.Denominator;
                                    Trace.WriteLine("time signature numerator " + TempEvent.Numerator);
                                    Trace.WriteLine("time signature denominator " + TempEvent.Denominator);
                                }
                                else
                                {
                                    timeSignature = TimeSignatureOverride[0] * 16 / TimeSignatureOverride[1];
                                }
                                break;
                            }
                        case "SetTempo":
                            {
                                SetTempoEvent TempEvent = (SetTempoEvent)Level2;
                                Tempo = Convert.ToInt32(TempEvent.MicrosecondsPerQuarterNote) / 3138; ;
                                break;
                            }
                        case "NoteOn":
                            {
                                //register the currently "saved" note
                                // we are on a rest

                                NoteEvent TempEvent = (NoteEvent)Level2;
                                flagNote = true;
                                restLength += tempLength;
                                if ((restLength >= unitaryLength & TrackNumber != 4) || (restLength >= unitaryLength * 16)) //the noise channel only prints rests if its bigger than 16 times the UL
                                {
                                    if (IgnoreRests)
                                    {
                                        NoteListTemp.Add(new MIDINote(noteLength + restLength, MIDILength[TrackNumber - 1], note, pan, velocity, trackVolume));
                                    }
                                    else if (noteLength==0)
                                    {
                                        NoteListTemp.Add(new MIDINote(restLength, MIDILength[TrackNumber - 1], note, pan, velocity, trackVolume));

                                    }
                                    else
                                    {
                                        NoteListTemp.Add(new MIDINote(noteLength, MIDILength[TrackNumber - 1], note, pan, velocity, trackVolume));
                                        NoteListTemp.Add(new MIDINote(restLength, MIDILength[TrackNumber - 1] + noteLength, 0, pan, velocity, trackVolume));

                                    }
                                }
                                else
                                {
                                    NoteListTemp.Add(new MIDINote(totalLength, MIDILength[TrackNumber - 1], note, pan, velocity, trackVolume));
                                }
                              

                                MIDILength[TrackNumber - 1] += totalLength;
                                noteLength = restLength = tempLength = totalLength = 0;

                                //setup for the next note
                                note = TempEvent.NoteNumber;
                                velocity = TempEvent.Velocity;
                                break;
                            }
                        case "NoteOff":
                            {
                                //NoteEvent TempEvent = (NoteEvent)Level2;
                                flagNote = true;
                                noteLength += tempLength;
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                    if (!flagNote & tempLength > unitaryLength)
                    {
                        restLength += tempLength;
                    }
                    else if (!flagNote)
                    {
                        noteLength += tempLength;
                    }
                    flagNote = false;
                }


                //last note print
                if (TrackNumber > 0)
                {
                    NoteListTemp.Add(new MIDINote(totalLength, MIDILength[TrackNumber - 1], note, pan, velocity, trackVolume));
                }

                //debug
                if (DebugFlag)
                {
                    foreach (MIDINote MIDINote in NoteListTemp)
                    {
                        MIDINote.ShowNotes();
                    }
                }

                //We've reached the end of the track
                noteLength = restLength = totalLength = 0;
                note = -1;


                if(TrackNumber>0)
                {
                    AllTrackList[TrackNumber - 1] = new List<MIDINote>(NoteListTemp);
                    //for(int i=0;i<NoteListTemp.Count;i++)
                    //{
                    //    Console.WriteLine(NoteListTemp.ElementAt(i));
                    //}
                    NoteListTemp.Clear();
                }
            //SkippedTrack:;
            }
        }

        public void ForceSync(List<MIDINote>[] TrackList)
        {
            List<int> PivotLocation = new List<int>();
            int ErrorSum;
            int NewNoteDuration;
            //unitaryLength = Convert.ToInt32((double)TimeDivision / (48 / allowedNotetypes[BaseNotetypeLocation]));
            for (int i = 0; i < TrackList.Length; i++)
            {
                //copies the contents of TrackList[i] into NoteListTemp
                NoteListTemp.Clear();
                PivotLocation.Clear();
                NoteListTemp = new List<MIDINote>(TrackList[i]);

                //detects the pivots and proto-pivots
                for (int j = 0; j < NoteListTemp.Count; j++)
                {
                    if (NoteListTemp[j].NoteLocation % TimeDivision == 0)
                    {
                        //do nothing if it's a pivot
                    }
                    //protopivots - add 1 tick, add 1 to the next note
                    else if (NoteListTemp[j].NoteLocation % TimeDivision == 1) 
                    {
                        NoteListTemp[j].NoteLocation--;
                        NoteListTemp[j].NoteDuration++;
                        NoteListTemp[j - 1].NoteDuration--;
                    }
                    else if (NoteListTemp[j].NoteLocation % TimeDivision == TimeDivision - 1) //remove 1 tick, add 1 from the previous note
                    {
                        NoteListTemp[j - 1].NoteDuration++;
                        NoteListTemp[j].NoteLocation++;
                        NoteListTemp[j].NoteDuration--;
                    }
                    else
                    {
                        goto SkipNote;
                    }

                    //adds the location of the pivot to the pivot list
                    PivotLocation.Add(j);

                SkipNote:;
                }

                //adds the last note as a pivot
                PivotLocation.Add(NoteListTemp.Count);
                PivotLocation.Sort();

                for (int pi = 0; pi < PivotLocation.Count - 1; pi++) // pi - Location in Pivot List
                {
                    ErrorSum = 0;
                    for (int loco = PivotLocation[pi]; loco < PivotLocation[pi + 1]; loco++)
                    // loco - location in NoteListTemp between pivots pi and pi+1
                    {
                        double[] TestNoteDurationArray = new double[unitaryLengthArray.Length]; //clears old array

                        for (int t = 0; t < TestNoteDurationArray.Length; t++)
                        {
                            TestNoteDurationArray[t] = (double)NoteListTemp[loco].NoteDuration / unitaryLengthArray[t];
                        }

                        unitaryLength = unitaryLengthArray[BaseNotetypeLocation];
                        if (TestNoteDurationArray[BaseNotetypeLocation] % 1 != 0)
                        //checks if the NoteDuration cannot fit into the base notetype perfectly (modulo is not 0)
                        {
                            for (int t = 0; t < TestNoteDurationArray.Length; t++)
                            {
                                //if any of the other notetypes fits exactly, change the unitary length for this note
                                //if not, default to the base notetype (we changed it before)
                                if (TestNoteDurationArray[t] % 1 == 0)
                                {
                                    unitaryLength = unitaryLengthArray[t];
                                    break;
                                }
                            }
                        }

                        //gets the new note duration by rounding the previous note to the nearest multiplier of the UL
                        //the UL could have been changed before
                        NewNoteDuration = Convert.ToInt32((double)NoteListTemp[loco].NoteDuration / unitaryLength) * unitaryLength;
                        //gets the error between the new duration and the old duration
                        ErrorSum += NewNoteDuration - NoteListTemp[loco].NoteDuration;
                        if (ErrorSum >= unitaryLength / 2)
                        {
                            NoteListTemp[loco].NoteDuration = NewNoteDuration - unitaryLength;
                            ErrorSum -= unitaryLength;
                            NoteListTemp[pi].warnings = " ; WARNING: Auto-Sync says: Rounded down!";
                        }
                        else if (ErrorSum <= -unitaryLength / 2)
                        {
                            NoteListTemp[loco].NoteDuration = NewNoteDuration + unitaryLength;
                            ErrorSum += unitaryLength;
                            NoteListTemp[pi].warnings = " ; WARNING: Auto-Sync says: Rounded up!";
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
            int intIntensity = 10;
            StringIntensity = "a";
            notetype = allowedNotetypes[BaseNotetypeLocation];
            int noteLengthFinal;
            TrackNumber = 0; //tracknumber starts at 0 here because of indexes
            StreamWriter sw = new StreamWriter(string.Concat(directoryPath, ASMFileName, ".asm"), true, Encoding.ASCII);

            //Header
            sw.WriteLine(";Coverted using MIDI2ASM");
            sw.WriteLine(";Code by TriteHexagon");  
            sw.WriteLine(";Version 5.2.0 (16-Nov-2022)");
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

            while (TrackNumber < TrackList.Length)//reuse the same variable
            {
                sw.WriteLine("Music_Placeholder_Ch{0}:", TrackNumber + 1);
                //writes the rest of the header
                switch (TrackNumber)
                {
                    case 0:
                        sw.WriteLine("\tvolume $77");
                        sw.WriteLine("\tdutycycle ${0}", Dutycycles[0]);
                        sw.Write("\tnotetype {0}", notetype);
                        sw.WriteLine(", ${0}{1}", StringIntensity, Envelopes[0]);
                        if (Tempo != 0)
                        {
                            sw.WriteLine("\ttempo {0}", Tempo);
                        }
                        break;
                    case 1:
                        sw.WriteLine("\tdutycycle ${0}", Dutycycles[1]);
                        sw.Write("\tnotetype {0}", notetype);
                        sw.WriteLine(", ${0}{1}", StringIntensity, Envelopes[1]);
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
                        MessageBox.Show("Number of tracks exceeded the max. Reduce the number of tracks and try again.");
                        break;
                }

                //removes the beginning rests
                for(int i=0;i<TrackList.Length;i++)
                {
                    if(TrackList[i].Count()>0)
                    {
                        if (TrackList[i].ElementAt(0).NoteDuration == 0 & TrackList[i].ElementAt(0).RawNote == 0)
                        {
                            TrackList[i].RemoveAt(0);
                        }
                    }
                }   

                for (int NoteIndex = 0; NoteIndex < TrackList[TrackNumber].Count; NoteIndex++)
                {
                    bool changedNotetypeFlag = false;
                    //copies the current note to the temporary PrintingNote
                    MIDINote PrintingNote = new MIDINote(TrackList[TrackNumber][NoteIndex]);

                    BarWriter(sw, ref bar);

                    NotetypeChange(sw, PrintingNote, ref changedNotetypeFlag);
                    noteLengthFinal = Convert.ToInt32(Math.Round((double)PrintingNote.NoteDuration / unitaryLength));

                    //changes Octave
                    if (PrintingNote.octave != octave & PrintingNote.RawNote!=0 & TrackNumber != 3) //doesn't change the octave for TrackNumber 3 (Noise), for rests and if the previous octave is the same
                    {
                        if (TrackNumber == 2)
                        {
                            sw.Write("\toctave {0}", PrintingNote.octave); //... except in the Wave channel
                            if (PrintWarnings & PrintingNote.octave == 0)
                                sw.WriteLine(" ;WARNING: Octave 0 isn't supported, won't work correctly");
                            else
                                sw.WriteLine();
                        }
                        else
                        {
                            sw.Write("\toctave {0}", PrintingNote.octave - 1); //the octave appears to be always 1 lower in the ASM...
                            if (PrintWarnings & PrintingNote.octave - 1 == 0)
                                sw.WriteLine(" ;WARNING: Octave 0 isn't supported, won't work correctly");
                            else
                                sw.WriteLine();
                        }  
                        octave = PrintingNote.octave;
                    }

                    //calculates the intensity of the note and checks if it needs to be changed
                    if (!IgnoreIntensity)
                    {
                        IntensityChange(sw, PrintingNote, ref intIntensity, changedNotetypeFlag);
                    }
                    
                    LengthSum[TrackNumber] += noteLengthFinal * notetype;

                    //prints note
                    NotePrinter(sw, PrintingNote, noteLengthFinal);
                }
                sw.WriteLine("\tendchannel");
                sw.WriteLine("");
                sw.WriteLine("; ============================================================================================================");
                sw.WriteLine("");

                TrackNumber++;
                octave = -1;
                StringIntensity = "a";
                bar = 0;
            }

            sw.Close();

            if (NoiseTemplate)
            {
                NoiseReplaceList.Sort();
                if (File.Exists(string.Concat(directoryPath, ASMFileName, "-nt.txt")))
                {
                    FileStream fileStream = File.Open(string.Concat(directoryPath, ASMFileName, "-nt.txt"), FileMode.Open);
                    fileStream.SetLength(0);
                    fileStream.Close();
                }
                StreamWriter nt = new StreamWriter(string.Concat(directoryPath, ASMFileName, "-nt.txt"), true, Encoding.ASCII);
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
            newBar = Math.Floor((double)LengthSum[TrackNumber] / (double)(12 * timeSignature));
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
            if (TrackNumber == 3 & NoiseTemplate) //checks if the noise replace mode is true and if we're in Track 3 (noise)
            {
                if (PrintingNote.RawNote == 0)
                {
                    NoiseTemplateString = "__";
                }
                else
                {
                    NoiseTemplateString = "N" + PrintingNote.octave + PrintingNote.RawNote.ToString("X");
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
            int newNotetype = allowedNotetypes[BaseNotetypeLocation];
            unitaryLength = Convert.ToInt32((double)TimeDivision * newNotetype / 48);
            //checks if there's need to change the notetype i.e. if remainder of the division by the unitary length is not 0
            notetypeCheck = Convert.ToInt32((double)(PrintingNote.NoteDuration % unitaryLength));

            for (int pos = 0; pos < allowedNotetypes.Length & notetypeCheck != 0; pos++)
            {
                newNotetype = allowedNotetypes[pos];
                unitaryLength = Convert.ToInt32((double)TimeDivision * newNotetype / 48);
                notetypeCheck = PrintingNote.NoteDuration % unitaryLength;
            }

            if (newNotetype != notetype)
            {
                changedNotetypeFlag = true;
                notetype = newNotetype;
                sw.Write("\tnotetype {0}", notetype);
                if (TrackNumber < 2)
                {
                    sw.WriteLine(", ${0}7", StringIntensity);
                }
                else if (TrackNumber == 2) //wave channel
                {
                    sw.WriteLine(", ${0}0", StringIntensity);
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
            string hexIntensity ;
            intIntensity = PrintingNote.intensity;

            if (intIntensity == 0) //failsafe to prevent intensity 0 and repeated intensities
                intIntensity = 10;

            if (TrackNumber == 2)
            {
                if (intIntensity < 4)
                    intIntensity = 3;
                else if (intIntensity < 11)
                    intIntensity = 2;
                else
                    intIntensity = 1;
            }
            if (CapitalHex)
                hexIntensity = intIntensity.ToString("X"); //converts intensity value into hexadecimal
            else
                hexIntensity = intIntensity.ToString("x"); //converts intensity value into hexadecimal

            if (TrackNumber < 3 & hexIntensity != StringIntensity) //doesn't calculate the intensity for TrackNumber 4 and if it doesn't need to be changed
            {
                StringIntensity = hexIntensity;
                if (StringIntensity == "0")
                    StringIntensity = "a";
                if (!changedNotetypeFlag)
                {
                    sw.Write("\tintensity $");
                    sw.Write(StringIntensity);
                    if (TrackNumber == 0)
                        sw.WriteLine(Envelopes[0]);
                    else if (TrackNumber == 1)
                        sw.WriteLine(Envelopes[1]);
                    else if (TrackNumber == 2)
                        sw.WriteLine(Envelopes[2]); // defaults to Wave 0 in the wave channel
                }
            }
        }

        public void GBConverter(string directory, string ASMFileName, string NotationStyle)
        {
            bool NoiseTrack = false;
            bool WroteChannelCount = false;
            string line;
            string[] lineSplit;
            string[] subLineSplit;
            int notetype = 12;
            char[] CharArray;
            string path = directory + ASMFileName + ".asm";
            string altPath = directory + ASMFileName + "-" + NotationStyle + ".asm";
            List<string> ChannelList = new List<string>();

            if (File.Exists(altPath))
            {
                FileStream fileStream = File.Open(altPath, FileMode.Open);
                fileStream.SetLength(0);
                fileStream.Close();
            }
            StreamReader fileReader = new System.IO.StreamReader(path);
            StreamWriter tempFileWriter = new StreamWriter(altPath, true, Encoding.ASCII);

            while ((line = fileReader.ReadLine()) != null)
            {
                //take care of the headers first
                lineSplit = line.Split('_');
                if (lineSplit[lineSplit.Length-1]== "Ch4:")
                {
                    NoiseTrack = true;
                }
                if (NotationStyle == "PRPY" & line== "Music_Placeholder:")
                {
                    goto Skip;
                }
                if (lineSplit[0]=="Music" & line != "Music_Placeholder:")
                    //we are in a header and not the main header
                {
                    if (NotationStyle == "PRPY")
                    {
                        tempFileWriter.WriteLine(line + ":");
                        goto Skip;
                    }
                    bool HeaderInList = ChannelList.Contains(line.TrimEnd(':'));
                    if (!HeaderInList)
                    {
                        tempFileWriter.WriteLine("." + line);
                        goto Skip;
                    }
                }

                lineSplit = line.Split(' ');        
                switch (lineSplit[0])
                {
                    case "\tmusicheader":
                        {
                            if (NotationStyle == "PRPY")
                            {
                                break;
                            }
                            else if (!WroteChannelCount)
                            {
                                tempFileWriter.WriteLine("\tchannel_count " + lineSplit[1].TrimEnd(','));
                                WroteChannelCount = true;
                            }
                            tempFileWriter.WriteLine("\tchannel " + lineSplit[2] + " " + lineSplit[3]);
                            ChannelList.Add(lineSplit[3]);
                            break;
                        }
                    case "\tvolume":
                        {
                            CharArray = lineSplit[1].ToCharArray();
                            tempFileWriter.Write("\tvolume ");
                            tempFileWriter.WriteLine(CharArray[1].ToString() + ", " + CharArray[2].ToString());
                            break;
                        }
                    case "\tintensity":
                        {
                            CharArray = lineSplit[1].ToCharArray();
                            int envelopeMax = Int32.Parse(CharArray[1].ToString(), System.Globalization.NumberStyles.HexNumber);
                            int fade = Int32.Parse(CharArray[2].ToString(), System.Globalization.NumberStyles.HexNumber);
                            if (fade > 7)
                            {
                                fade = -fade + 8;
                            }

                            if (NotationStyle == "PRPY")
                            {
                                tempFileWriter.WriteLine("\tnote_type " + notetype + ", " + envelopeMax + ", " + fade);
                                break;
                            }
                            else
                            {
                                tempFileWriter.WriteLine("\tvolume_envelope "+ envelopeMax + ", " + fade);
                                break;
                            }     
                        }
                    case "\tdutycycle":
                        {
                            CharArray = lineSplit[1].ToCharArray();
                            tempFileWriter.Write("\tduty_cycle ");
                            if (CharArray[0] == '$')
                                //in case it's in the format $X
                                tempFileWriter.WriteLine(Int32.Parse(CharArray[1].ToString(), System.Globalization.NumberStyles.HexNumber));
                            else
                                //in case it's decimal
                                tempFileWriter.WriteLine(CharArray[0]);                     
                            break;
                        }
                    case "\tnotetype":
                        {
                            subLineSplit = lineSplit[1].Split(',');
                            //takes care of the first part
                            CharArray = subLineSplit[0].ToCharArray();
                            if (CharArray[0]=='$')
                            {
                                //in case it's in the format $X
                                notetype = Int32.Parse(CharArray[1].ToString(), System.Globalization.NumberStyles.HexNumber);
                            }
                            else
                            {
                                //in case it's decimal
                                notetype = Int32.Parse(subLineSplit[0]);
                            }
                            //case for the noise track
                            if (NoiseTrack)
                            {
                                tempFileWriter.Write("\tdrum_speed ");
                                tempFileWriter.WriteLine(notetype);
                                break;
                            }
                            else
                            {
                                CharArray = lineSplit[2].ToCharArray();
                                int envelopeMax = Int32.Parse(CharArray[1].ToString(), System.Globalization.NumberStyles.HexNumber);
                                int fade = Int32.Parse(CharArray[2].ToString(), System.Globalization.NumberStyles.HexNumber);
                                if (fade > 7)
                                {
                                    fade = -fade + 8;
                                }
                                tempFileWriter.WriteLine("\tnote_type " + notetype + ", " + envelopeMax + ", " + fade);
                                break;
                            }
                        }
                    case "\tnote":
                        {
                            subLineSplit = lineSplit[1].Split(','); //equivalent to removing the last ,

                            if (subLineSplit[0] == "__")
                            {
                                tempFileWriter.WriteLine("\trest " + lineSplit[2]);
                            }
                            else
                            {
                                if (NoiseTrack)
                                {
                                    int drum_note = Array.IndexOf(notesArray, subLineSplit[0]);
                                    tempFileWriter.WriteLine("\tdrum_note " + drum_note + ", " + lineSplit[2]);
                                }
                                else
                                {
                                    tempFileWriter.WriteLine(line);
                                }
                            }
                            break;
                        }
                    case "\ttogglenoise":
                        {
                            if (NotationStyle == "PRPY" & PrintWarnings)
                            {
                                tempFileWriter.WriteLine("\t ;WARNING: Drum Notes not yet supported for pokered/pokeyellow. This will definitely sound bad.");
                                break;
                            }
                            else
                            {
                                CharArray = lineSplit[1].ToCharArray();
                                tempFileWriter.Write("\ttoggle_noise ");
                                if (CharArray[0] == '$')
                                    //in case it's in the format $X
                                    tempFileWriter.WriteLine(Int32.Parse(CharArray[1].ToString(), System.Globalization.NumberStyles.HexNumber));
                                else
                                    //in case it's decimal
                                    tempFileWriter.WriteLine(CharArray[0]);
                                break;
                            } 
                        }
                    case "\tstereopanning":
                        {
                            CharArray = lineSplit[1].ToCharArray();
                            bool left_panning = false;
                            bool right_panning = false;
                            if (CharArray[1]=='f' || CharArray[1]=='F')
                            {
                                left_panning = true;
                            }
                            if (CharArray[2] == 'f' || CharArray[2] == 'F')
                            {
                                right_panning = true;
                            }
                            tempFileWriter.WriteLine("\tstereo_panning " + left_panning + ", " + right_panning);
                            break;
                        }
                    case "\tvibrato":
                        {
                            subLineSplit = lineSplit[1].Split(',');
                            //takes care of the first part
                            CharArray = subLineSplit[0].ToCharArray();
                            int vibrato_delay;
                            if (CharArray[0] == '$')
                            {
                                //in case it's in the format $X
                                vibrato_delay = Int32.Parse(CharArray[1].ToString(), System.Globalization.NumberStyles.HexNumber);
                            }
                            else
                            {
                                //in case it's decimal
                                vibrato_delay = Int32.Parse(subLineSplit[0]);
                            }
                            CharArray = lineSplit[2].ToCharArray();
                            int vibrato_intensity = Int32.Parse(CharArray[1].ToString(), System.Globalization.NumberStyles.HexNumber);
                            int vibrato_type = Int32.Parse(CharArray[2].ToString(), System.Globalization.NumberStyles.HexNumber);
                            tempFileWriter.WriteLine("\tvibrato " + vibrato_delay + ", " + vibrato_intensity + ", " + vibrato_type);
                            break;
                        }
                    case "\ttone":
                        {
                            CharArray = lineSplit[1].ToCharArray();
                            tempFileWriter.Write("\tpitch_offset ");
                            string assemble_tone = lineSplit[1];
                            if (CharArray[0] == '$')
                            {
                                //in case it's in the format $X
                                assemble_tone = lineSplit[1].TrimStart('$');
                                tempFileWriter.WriteLine(Int32.Parse(assemble_tone, System.Globalization.NumberStyles.HexNumber));
                            }
                            tempFileWriter.WriteLine("\tpitch_offset " + Int32.Parse(assemble_tone, System.Globalization.NumberStyles.HexNumber));
                            break;
                        }
                    case "\tpitchoffset":
                        {
                            tempFileWriter.WriteLine("\ttranspose " + lineSplit[1].TrimEnd(',') + ", " + Array.IndexOf(notesArray, lineSplit[2]));
                            break;
                        }
                    case "\tendchannel":
                        {
                            tempFileWriter.WriteLine("\tsound_ret");
                            break;
                        }
                    case "\tcallchannel":
                        {
                            tempFileWriter.WriteLine("\tsound_call ." + lineSplit[1]);
                            break;
                        }
                    case "\tloopchannel":
                        {
                            tempFileWriter.WriteLine("\tsound_loop " + lineSplit[1] + ", " + lineSplit[2]);
                            break;
                        }
                    default:
                        {
                            tempFileWriter.WriteLine(line);
                            break;
                        }
                }
            Skip:;
            }
            fileReader.Close();
            tempFileWriter.Close();
            File.Delete(path);
        }
    }
}
