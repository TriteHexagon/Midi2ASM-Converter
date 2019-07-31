using System;
using System.Text;
using System.IO;

namespace MIDI_Converter
{
    class Program
    {
        int Track;
        int bar; 
        double newBar; 
        int unitaryLength; 
        double notetypeCheck;
        int TicksPerBeat;
        int notetype;
        int newNotetype;
        int[] allowedNotetypes;
        int pos;
        int trackVolume;
        int note;
        int noteLength;
        int restLength;
        int tempLength;
        int noteLengthFinal;
        int velocity;
        int intIntensity;
        string newIntensity;
        string intensity;
        int octave;
        int newOctave;
        int notePosition;
        int[] LengthSum; //stores the track length in ASM units i.e. notetype * note length
        int[] TickSum; //stores the tick length; useful for debugging
        string[] notesArray;
        bool debug;

        public Program()
        {
            Track = 0;
            bar = 0;
            newBar = 0;
            unitaryLength = 0;
            TicksPerBeat = 0;
            notetype = 12;
            newNotetype = 12;
            trackVolume = 0;
            note = -1; //-1 is the starting note and is a "rest" that prints only in the beginning
            pos = 0;
            noteLength = 0;
            restLength = 0;
            noteLengthFinal = 0;
            velocity = 0;
            newIntensity = "0";
            intensity = "0";
            octave = -1;
            LengthSum = new int[] { 0, 0, 0, 0 };
            TickSum = new int[] { 0, 0, 0, 0 };
            notesArray = new string[] {"__", "C_", "C#", "D_", "D#", "E_", "F_", "F#", "G_", "G#", "A_", "A#", "B_"};
            allowedNotetypes = new int[] {6, 8, 12};
            debug = true;
        }

        public void BarWriter(StreamWriter sw)
        {
            //writes comment indicating the new bar
            newBar = Math.Floor((double)LengthSum[Track - 1] / (double)(12 * 16));
            if (newBar+1 != bar)
            {
                while (newBar + 1 > bar)
                {
                    bar++;
                }
                sw.WriteLine(";Bar {0}", bar);
            }
        }
            
        public void NotetypeChange(StreamWriter sw)
        {
            newNotetype = 12; //resets notetype every time

            unitaryLength = Convert.ToInt32((double)TicksPerBeat * newNotetype / 48);
            //checks if there's need to change the notetype i.e. if remainder of the division by the unitary length is not 0
            notetypeCheck = Convert.ToInt32((double)(tempLength % unitaryLength));

            pos = 0;
            while (notetypeCheck != 0 & pos < allowedNotetypes.Length)
            {
                newNotetype=allowedNotetypes[pos];
                unitaryLength = Convert.ToInt32((double)TicksPerBeat * newNotetype / 48);
                notetypeCheck = tempLength % unitaryLength;
                pos++;
            }

            if (newNotetype != notetype)
            {
                notetype = newNotetype;
                sw.Write("\tnotetype {0}", notetype);
                if (Track <= 2)
                {
                    sw.WriteLine(", ${0}7", intensity);
                }
                else if (Track == 3)
                {
                    sw.WriteLine(", $10");
                }
                else
                {
                    sw.WriteLine();
                }
            }
        }

        public void NotePrinter(StreamWriter sw)
        {
            while (noteLengthFinal > 16)
            {
                sw.Write("\tnote ");
                sw.Write(notesArray[notePosition]);
                sw.Write(", ");
                sw.WriteLine(16);
                noteLengthFinal -= 16;
            }

            if (noteLengthFinal > 0)
            {
                sw.Write("\tnote ");
                sw.Write(notesArray[notePosition]);
                sw.Write(", ");
                sw.Write(noteLengthFinal);
                if (notetypeCheck != 0 & debug==true)
                {
                    sw.Write(" ;");
                    sw.Write(tempLength);
                    sw.Write(" WARNING: Rounded");
                }
                sw.WriteLine("");
            }
        }

        public void RestWriter(StreamWriter sw)
        {
            //writes the Bar again
            BarWriter(sw);
            tempLength = restLength;
            NotetypeChange(sw);
            noteLengthFinal = Convert.ToInt32(Math.Round((double)restLength / unitaryLength));
            //writes the rest
            LengthSum[Track - 1] += noteLengthFinal*notetype;
            TickSum[Track - 1] += restLength;
            notePosition = 0;
            NotePrinter(sw);
        }

        public void NoteWriter(StreamWriter sw)
        {
            BarWriter(sw);

            tempLength = noteLength;
            NotetypeChange(sw);
            noteLengthFinal = Convert.ToInt32(Math.Round((double)noteLength / unitaryLength));

            //calculates the intensity of the note and checks if it needs to be changed
            intIntensity = Convert.ToInt32(Math.Round((double)(trackVolume * velocity) / 1075));
            newIntensity = intIntensity.ToString("X"); //converts intensity value into hexadecimal

            if (newIntensity != intensity & Track <3) //doesn't calculate the intensity for Tracks 3 and 4
            {
                sw.Write("\tintensity $");
                sw.Write(newIntensity);
                sw.WriteLine("7");
                intensity = newIntensity;
            }

            //changes Octave
            newOctave = Convert.ToInt32(Math.Floor((double)(note / 12 - 1)));
            if (newOctave != octave & Track!=4) //doesn't change the octave for Track 4
            {
                sw.WriteLine("\toctave {0}", newOctave-1); //the octave appears to be always 1 lower than in the GB
                octave = newOctave;
            }
            //calculates note length
            notePosition = note % 12 + 1;

            LengthSum[Track - 1] += noteLengthFinal*notetype;
            TickSum[Track - 1] += noteLength;

            //prints note
            NotePrinter(sw);
        }

        public void exec()
        {
            int counter = 0;
            int Tempo = 0;
            int pan = 0;
            string panHex = "ff";
            string line;
            
            //Gets the current directory of the application.
            string path = Directory.GetCurrentDirectory();

            //checks if the out.asm file already exists and clears its contents
            if (File.Exists(string.Concat(path, "\\out.asm")))
            {
                FileStream fileStream = File.Open(string.Concat(path, "\\out.asm"), FileMode.Open);
                fileStream.SetLength(0);
                fileStream.Close();
            }

            //opens the midi.txt file
            if (File.Exists(string.Concat(path, "\\midi.txt"))==false)
            {
                Console.WriteLine("midi.txt not found!");
                goto End;
            }
            System.IO.StreamReader file =
                new System.IO.StreamReader(string.Concat(path, "\\midi.txt"));
            StreamWriter sw = new StreamWriter((string.Concat(path, "\\out.asm")), true, Encoding.ASCII);

            //Header
            sw.WriteLine(";Coverted using MIDI2ASM");
            sw.WriteLine(";Coded by TriteHexagon.");
            sw.WriteLine(";Version 1.0. (31/7/2019)");
            sw.WriteLine(";https://github.com/TriteHexagon/Midi2ASM-Converter");
            sw.WriteLine("");
            sw.WriteLine("; ============================================================================================================");
            sw.WriteLine("");
            sw.WriteLine("Music_Template:");
            sw.WriteLine("\tmusicheader 4, 1, Music_Template_Ch1");
            sw.WriteLine("\tmusicheader 1, 2, Music_Template_Ch2");
            sw.WriteLine("\tmusicheader 1, 3, Music_Template_Ch3");
            sw.WriteLine("\tmusicheader 1, 4, Music_Template_Ch4");
            sw.WriteLine("");

            if(debug)
            {
                Console.WriteLine("DEBUG MODE");
            }
            

            // Reads the file line by line.
            while ((line = file.ReadLine()) != null)
            {
                //Splits the string line into multiple elements
                string[] lineString = line.Split(' ');

                if (lineString.Length == 1)
                {
                    if (lineString[0] == "MTrk")
                    {
                        Track++;
                        bar = 0;
                        octave = -1; //resets octave for the new track
                        trackVolume = 0; //resets trackvolume for the new track
                        notetype = 12;
                        newNotetype = 12;
                        sw.WriteLine("Music_Template_Ch{0}:", Track);
                        //writes the rest of the header
                        switch (Track)
                        {
                            case 1:
                                sw.WriteLine("\tvolume $77");
                                sw.WriteLine("\tdutycycle $2");
                                sw.Write("\tnotetype {0}", notetype);
                                sw.WriteLine(", $A7");
                                break;
                            case 2:
                                sw.WriteLine("\tdutycycle $1");
                                sw.Write("\tnotetype {0}", notetype);
                                sw.WriteLine(", $A7");
                                break;
                            case 3:
                                sw.Write("\tnotetype {0}", notetype);
                                sw.WriteLine(", $10");
                                break;
                            case 4:
                                sw.WriteLine("\ttogglenoise 1 ; WARNING: this will sound bad. Change.");
                                sw.Write("\tnotetype {0}", notetype);
                                sw.WriteLine();
                                break;
                            default:
                                Console.WriteLine("Houston, we have a problem.");
                                break;
                        }
                    }
                    continue;
                }

                //finds the TicksPerBeat of the MIDI file
                if (lineString[0] == "MFile")
                {
                    TicksPerBeat = Int32.Parse(lineString[3]);
                    unitaryLength = Convert.ToInt32((double)TicksPerBeat / (48 / newNotetype));
                }

                //finds the Tempo of the MIDI file and converts to ASM tempo
                if (lineString[1] == "Tempo")
                {
                    Tempo = Int32.Parse(lineString[2]) / 3138;
                    sw.WriteLine("\ttempo {0}", Tempo);
                }

                if (lineString.Length < 4)
                {
                    if (lineString[2] == "TrkEnd")
                    {
                        restLength = Int32.Parse(lineString[0]);
                        if (restLength > unitaryLength) //the noise channel needs the last rest
                        {
                            NoteWriter(sw);
                            RestWriter(sw);
                        }
                        else
                        {
                            noteLength += restLength;
                            NoteWriter(sw);
                        }
                        noteLength = 0;
                        restLength = 0;
                        note = -1;
                        notePosition = 0;
                        sw.WriteLine("\tendchannel");
                        sw.WriteLine("");
                        sw.WriteLine("; ============================================================================================================");
                        sw.WriteLine("");
                    }
                    continue;
                }

                //tests and changes stereopanning
                if (lineString[3] == "c=10")
                {
                    string[] tempString = lineString[4].Split('=');
                    pan = Int32.Parse(tempString[1]);
                    if (pan < 43)
                    {
                        panHex = "f0";
                    }
                    if (pan > 85)
                    {
                        panHex = "f";
                    }
                    else
                    {
                        panHex = "ff";
                    }
                    sw.WriteLine(string.Concat("\tstereopanning $", panHex));
                }

                //sets new trackvolume to define the intensity of this track
                if (lineString[3] == "c=7")
                {
                    string[] tempString = lineString[4].Split('=');
                    trackVolume = Int32.Parse(tempString[1]);
                }

                if (Int32.TryParse(lineString[0], out restLength)) //checks if the first part of the string is a number and saves it to restLength
                {
                    if (lineString[1] != "On")
                    {
                        noteLength += restLength;
                        continue;
                    }

                    if (lineString[1] == "On")
                    {
                        if (note==-1) //prints the first rest in case it exists i.e. if the note is -1. The NotePrinter guarantees it doesn't print a 0. Also works on Track 4.
                        {
                            restLength += noteLength;
                            RestWriter(sw);
                        }
                        else if ((restLength > unitaryLength & Track!=4)) //the noise channel doesn't need rests
                        {
                            NoteWriter(sw);
                            RestWriter(sw);
                        }
                        else
                        {
                            noteLength += restLength;
                            NoteWriter(sw);
                        }
                        noteLength = 0;
                        restLength = 0;

                        string[] tempString = lineString[3].Split('=');
                        note = Int32.Parse(tempString[1]);
                        string[] tempString2 = lineString[4].Split('=');
                        velocity = Int32.Parse(tempString2[1]);
                    }
                }
                counter++;
            }

            file.Close();

            sw.Close();
            Console.WriteLine();
            Console.WriteLine("CONVERSION SUCCESSFUL!");
            Console.WriteLine("There were {0} lines.", counter);
            Console.WriteLine();

            //prints Track length to the console
            Track = 0;
            do
            {
                Console.Write("Track {0}'s ", Track);
                Console.WriteLine("total length is {0}.", LengthSum[Track]);
                Track++;
            } while (Track < 4);

            Console.WriteLine();
            Console.WriteLine("Expected track length is {0}.", (double)TickSum[0]*48/TicksPerBeat);
            
            if (debug)
            {
                Console.WriteLine();
                Console.WriteLine("Total ticks is {0}.", TickSum[0]);
                Console.WriteLine("Ticks Per Beat is {0}.", TicksPerBeat);
            }
            Console.WriteLine();
            End:
            Console.WriteLine("Press any key to exit...");
            // Suspend the screen.  
            Console.ReadKey();
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.exec();
        }
    }
}