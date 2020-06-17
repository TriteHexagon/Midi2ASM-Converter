using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokéMIDIGUI
{
    class MIDINote
    {
        public int NoteLocation { get; set; }
        public int NoteDuration { get; set; }
        public int RawNote { get; set; }
        public int octave { get; set; }
        public int intensity { get; set; }
        public int pan { get; set; }
        public string warnings { get; set; }

        public MIDINote(int NoteDuration, int NoteLocation, int RawNote, int RawPan, int velocity, int TrackVolume)
        {
            this.NoteDuration = NoteDuration;
            this.NoteLocation = NoteLocation;
            pan = RawPan;
            octave = Convert.ToInt32(Math.Floor((double)(RawNote / 12 - 1)));
            if (RawNote == 0)
            {
                this.RawNote = 0;
            }
            else
            {
                this.RawNote = RawNote % 12 + 1;
            }
            intensity = TrackVolume * velocity; // divide by 1075 to get the GB intensity
        }

        public MIDINote(MIDINote Copy)
        {
            NoteDuration = Copy.NoteDuration;
            NoteLocation = Copy.NoteLocation;
            RawNote = Copy.RawNote;
            octave = Copy.octave;
            intensity = Copy.intensity;
            pan = Copy.pan;
            warnings = Copy.warnings;
        }

        public void ShowNotes()
        {
            Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", NoteLocation, NoteDuration, RawNote, octave, intensity, pan);
        }
    }
}
