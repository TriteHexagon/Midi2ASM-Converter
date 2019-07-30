# Midi2ASM Converter
A simple tool to convert MIDI files into pokecrystal-based music files, written in C#. The idea was to improve upon FroggestSpirit's similar tool: convert the notes and other commands into the current command format, fix errors and add features.

## Features
* Converts the notes in the MIDI file into the note commands in pokecrystal (including rests).
* Detects velocity changes in the MIDI and translates that into intensity.
* Detects changes in the notetype.

## How to Use
* Compile the tool from the .cs file or download the .zip. It contains the compiled executable, an example midi.txt file and a readme.txt file.
* The following instructions assume you already know how to put a custom music into pokecrystal (and likely have used FroggestSpirt's tool to do so). If you don't, you can follow [this guide](https://github.com/pret/pokecrystal/wiki/Add-a-new-music-song)
### Prepare your MIDI
* The MIDI needs to be have precisely 4 or less tracks: Pulse 1 in Track 0, Pulse 2 in Track 1, Wave in Track 4 and Noise in Track 3. I recommend using AnvilStudio to delete and rearrange the tracks and MIDIEditor to switch notes between tracks if needed.
* **Make sure each track only has a single note playing at the same time**, otherwise the tool will not work correctly.
### Convert into a .txt file
* Go to [this site](http://flashmusicgames.com/midi/mid2txt.php) and upload your MIDI track. **Make sure you select the Delta timestamp option**. After the site converts your MIDI file into text, copy it and save into a file named midi.txt.
### Convert into ASM
* Place your midi.txt file into the same folder as the MIDI2ASM executable.
* Run the executable. A command line will pop up so be prepared.
* If everything goes well, you should now have a out.asm file in the same folder. The music channels are name *Template* so you cna easily replace them.
### Manually fixing problems
* I have confidence the notes work correctly. However, notetype changes are incredibly tricky to do and MIDI is too precise. This means that most likely your tracks won't sync properly with each other. The command line that opens up contains the length of each track, along with the length they *all* should have. If they don't match exactly, this means some rounding error made some notes shorter or longer than it should be.
*

## Missing Features / Improvements / Known quirks
