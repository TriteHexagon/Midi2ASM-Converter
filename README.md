# Midi2ASM Converter
This is a simple tool to convert MIDI files into pokecrystal-based music files, written in C#. As someone who's already knee-deep into making chiptune music using the pokecrystal / GameBoy engine, I know how important the current tool made by FroggestSpirit is and also how frustrating it can be to arrange some songs using it. It's a good first step, but I felt it was time I fixed some of the problems with it, including the fact that its output format is no longer supported. And so I decided to learn C# and make my own tool from scratch. The idea was to do the same thing as FroggestSpirit's MIDI converter does, but add and improve the note conversion process, as well as add new features.

## Features
* Converts the notes in the MIDI file into the note commands used in pokecrystal (including rests).
* Detects velocity changes in the MIDI and translates them into intensity when needed.
* Detects changes in the notetype.
* The output file is ready to play so you can plop it into the game without much added effort.

## What it doesn't do
* This tool is not intended to directly convert a MIDI into GameBoy music. The output files *will* work, but they will sound kinda bad and bland. The intention is to save arrangers time in doing the boring parts, such as fixing triplets, short notes and intensity changes, and focus on making the music sound as good as possible.

## How to Use
* Compile the tool from the .cs file or, alternatively, download this .zip file. It contains the compiled executable, an example midi.txt file and a readme.txt file.
* The following instructions assume you already know how to put a custom music into pokecrystal. If you don't, you can follow [this guide](https://github.com/pret/pokecrystal/wiki/Add-a-new-music-song).
#### Prepare your MIDI
* The MIDI needs to be have precisely 4 or less tracks: Pulse 1 in Track 0, Pulse 2 in Track 1, Wave in Track 4 and Noise in Track 3. I recommend using AnvilStudio to delete and rearrange the tracks and MIDIEditor to switch notes between tracks if needed.
* **Make sure each track only has a single note playing at the same time**, otherwise the tool will not work correctly.
#### Convert into a .txt file
* Go to [this site](http://flashmusicgames.com/midi/mid2txt.php) and upload your MIDI track. **Make sure you select the Delta timestamp option**.
* After the MIDI file is converted into text, copy it and save into a file named *midi.txt*.
#### Convert into ASM
* Place your midi.txt file into the same folder as the MIDI2ASM executable.
* Run the executable. A command line will pop up so be prepared.
* If everything goes well, you should now have a out.asm file in the same folder.
* The music channels are all named *Template* so you can easily replace the name with whatever you want.
#### Manually fixing notes
* I have confidence the notes work correctly. However, notetype changes are really tricky to do automatically. It works most of the time, but one badly translated note is enough to make the tracks not sync up properly.
* After the output file is wirtten. the command line that opens up contains the length of each track, along with the length they *all* should have. If these numbers don't match exactly, this means some rounding error made some notes shorter or longer than it should be. Even still, it's possible that the rounding errors cancel each other out and the tracks won't sync up in a specific section. I know it's a pain but that's the best I can do right now.

## Known quirks
* If the noise channel actually needs rests (for example, in entire bars), it will simply repeat the last note for that entire duration.
* 


## Missing Features / Improvements 
* Read directly from a MIDI file.

## How it works
*

## Can I help?
* **YES**. I do have basic training in programming, but this is, technically, my first foray into an actual, useful language. As such, I gurantee that the code is probably awful. For that reason alone, feel free to suggest changes that make the code cleaner or fixes potential quirks with it.

* Other than that, the logic behind the notetypes is still tripping me up. If someone finds a fix for it, I'd greatly appreciate it.
