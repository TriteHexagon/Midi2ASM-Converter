# Debug Mode

* The debug mode allows you to more easily find and fix the faulty notes in your music.
* In the final out.asm file, it prints out the note duration in ticks and a warning as a comment in front of the note, in case the program had to round that note's duration. These are likely the notes giving you problems, so you can use this version to easily find the faulty notes.
* To do this, you need a value printed in the command line called **Ticks per Beat (TpB)**, which is how many "ticks" fit into a beat in the MIDI.
* You can convert this number into what I call the **Unitary Length (UL)** by using the following formula: *UL = TpB * Notetype / 48*. This is how much you have to divide the note duration in ticks to obtain the note duration in the .asm files.
* As the notetype is usually 12, UL is usually *TpB/4*.
* Ticks are much more precise than the UL, which is where the rounding errors come from. The program attempts to fit the note into notetype 8 and 6 exactly, but if it fails it will reverts to 12, round the duration to the nearest integer and prints out the rounding warning.
* Most of the time it isn't a problem, but you'll have to use your judgement if the program didn't do this correctly.
