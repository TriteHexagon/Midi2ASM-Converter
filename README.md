# Midi2ASM Converter
This is a simple tool to convert MIDI files into [pokecrystal](https://github.com/pret/pokecrystal)-based music, written in C#. As someone who's already [knee-deep into making chiptune music](https://soundcloud.com/user-930339535/sets/all-demixes) using the pokecrystal / GameBoy engine, I know how important the current tool made by FroggestSpirit is and also how frustrating it can be to arrange some songs using it. It's a good first step, but I felt I could write my own tool and fix the current problems with it, including the annoying fact that its output format is no longer supported, and add new features as well.

## Features
* Converts the notes in the MIDI file into the note commands used in pokecrystal (including rests).
* Detects velocity changes in the MIDI and translates them into intensity when needed.
* Detects changes in the notetype.
* Prints out comments with the bars so you can easily navigate the notes.
* The output file is almost ready to play so you can plop it into the game without much effort.

## What it doesn't do
* **This tool is not intended to directly convert a MIDI into GameBoy music**. The output files *will* work, but they will sound bad because of syncing issues between the tracks (more on that later).
* The intention is to save arrangers time doing the boring parts, such as fixing triplets, short notes and intensity changes, and focusing on making the music sound as good as possible.

## Instructions
* Compile the tool from the .cs file (the debug mode is on be default) or, alternatively, download this .zip file. It contains the two compiled executables (a normal and a debug version), an example midi.txt file and a readme.txt file.
* The following instructions won't go into how to put custom music into pokecrystal. If you don't know how to do it, you can follow [this guide](https://github.com/pret/pokecrystal/wiki/Add-a-new-music-song).
#### Prepare your MIDI
* The MIDI needs to be have precisely 4 or less tracks: Pulse 1 in Track 0, Pulse 2 in Track 1, Wave in Track 4 and Noise in Track 3. I recommend using AnvilStudio to delete and rearrange the tracks and MIDIEditor to switch notes between tracks if needed.
* **Make sure each track only has a single note playing at the same time**, otherwise the tool will not work correctly.
#### Convert into a .txt file
* Go to [this site](http://flashmusicgames.com/midi/mid2txt.php) and upload your MIDI track. **Make sure you select the Delta timestamp option**.
* After the MIDI file is converted into text, copy it and sve to a file named *midi.txt*.
#### Convert into ASM
* Place your midi.txt file into the same folder as the MIDI2ASM executables.
* Run the executable of your choice (more on that later). A command line will pop up with useful information if you need it.
* You should now have a out.asm file in the same folder.
* The music channels are all named *Template* so you can easily replace the name with whatever you want.
### Manually fixing notes
* I have confidence the note conversion works correctly. However, notetype changes are really tricky to do automatically. It works most of the time, but one badly translated note duration is enough to make the tracks not sync up properly.
* After the output file is written, the command line will have the total length of each track, along with the length they *all* should have. If these numbers don't match exactly, this means some rounding error made some notes shorter or longer than they should be. Even still, it's possible that the rounding errors cancel each other out and the tracks won't sync up in a specific section. I know it's a pain but that's the best I can do right now.
* In conclusion, you'll have to listen to the music and try to figure out which notes might be off.
#### Debug Mode
* The Debug Mode helps you figure out which notes need to be fixed.
* You can learn more on how to use the debug mode [here](https://github.com/TriteHexagon/Midi2ASM-Converter/blob/master/DEBUG.md).

## Known quirks
* If the noise channel actually needs rests (for example, entire bars), it will simply repeat the last note detected for that entire duration.

## Missing Features / Improvements 
* Read directly from a MIDI file. I'm not planning on doing this any time soon, seeing as it works fine at the moment and it would be a ton of work to implement.
* A mode to make the noise channel print temporary, easily repalceable notes instead of the normal notes. This is because, by experience, the notes that are present in the MIDI rarely translate to any of the existing drumkits, so I always have to manually change the notes. This mode would make that a bit easier and also prevent situations where the MIDI has different percussion in different ocatve, which naturally isn't how the noise channel works.
* Loops, seeing as some MIDIs have loop information integrated.

## Can I help?
* **YES**. I do have basic training in programming, but this is, technically, my first foray into an actual, useful programming language. As such, I guarantee that the code is awful. For that reason alone, feel free to suggest changes to make the code cleaner or fixes.
* Feel free to leave feedback wherever I might see it.

> TriteHexagon. Updated on 31/7/2019
