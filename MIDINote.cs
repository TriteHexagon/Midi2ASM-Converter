using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MIDI2ASMGUI
{
    class MIDINote
    {
        public int NoteLocation { get; set; } //cumulative position of the note in the MIDI
        public int NoteDuration { get; set; } //how long the note lasts in ticks
        public int RawNote { get; set; } //number of the note as recorded in the midi
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

            if (TrackVolume == 1)
                intensity = (int)(TrackVolume * velocity / 8.3); // divide by 1075 to get the GB intensity
            else 
                intensity = (TrackVolume * velocity) / 1075; // divide by 1075 to get the GB intensity
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
            string line = "duration: " + NoteDuration + " | location: " + NoteLocation.ToString() + " | rawnote: " + RawNote + " | octave: " + octave + " | intensity: " + intensity + " | pan: " + pan;
            Trace.WriteLine(line);
        }
    }
}
