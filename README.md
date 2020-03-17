# Midi2ASM Converter
This is a simple tool to convert MIDI files into [pokecrystal](https://github.com/pret/pokecrystal)-based music, written in C#. As someone who's already [knee-deep into making chiptune music](https://soundcloud.com/user-930339535/sets/all-demixes) using the pokecrystal / GameBoy engine, I know how important the current tool made by FroggestSpirit is and also how frustrating it can be to arrange some songs using it. It was a good first step, but I felt I could write my own tool and fix the current problems with it, including the annoying fact that its output format is no longer supported, and add new features as well.

## Features
* Converts the notes in the MIDI file into the note commands used in pokecrystal (including rests) in a .asm file.
* Detects velocity changes in the MIDI and translates them into intensity.
* Detects stereo.
* Detects changes in the notetype.
* Prints out comments with the music's bars so you can easily navigate the notes.
* The output file is ready to play so you can plop it into the game without much effort.
* An option to make the noise channels' notes entirely of easily replaceable placeholder names.
* Warnings in the finished .asm file that let you now the notes that might not be in sync.

## What it doesn't do
* **This tool is not intended to directly convert a MIDI into GameBoy music**. The output files *will* work, but they might sound bad because of potential syncing issues between the tracks.
* The intention is to save arrangers time doing the boring parts, such as fixing triplets, short notes and intensity changes.

## Settings
* These can be changed in the config file that comes with the .zip file.
* **TempoTrack**: If this is *true*, then your MIDI files need to have 5 different tracks; the first Track (track 0) only serves to grab the tempo of the song and will be ignored otherwise. If this option is *false*, then you MIDI file needs to have 4 tracks exactly.
* **NoiseReplace**: If this is *true*, the noise channel's notes will be have placeholder notes instead of the usual. These are labelled X1 through XC (following the hexadecimal numbers). If this is *false* then the notes will be written like the other channels.
* **TimeSignature**: This only affects where the Bar comments are printed. The default is 16, for the common time (4/4). If your song has another time signature, this should be changed so you can easily navigate the code. The formula to calculate the value is TS = x * 16/y, where the time signature is x/y. 8 should work for a time signature of 2/4 and 24 for a time signature of 6/4. Unfortunately this formula breaks for songs with unusual time signatures (anything other than a divisor of 16 for the y).
* If there's no config file or otherwise the data is corrupted, then the default is TempoTrack true, NoiseReplace false and timeSignature 16.

## Instructions
* Download the .zip file in the releases. It contains the executable, a config.txt file, an example midi.txt file and this Readme.
* Alternatively you can compile the tool from the .cs file.
* The following instructions won't go into how to put custom music into pokecrystal. If you don't know how to do it, you can follow [this guide](https://github.com/pret/pokecrystal/wiki/Add-a-new-music-song).
#### Prepare your MIDI
* The MIDI needs to be have precisely 5 or 4 tracks, depending on whether TempoTrack is true or false in the config file. If you're using the TempoTrack true, then Track 0 of your MIDI will be ignored. The order of the other tracks is Pulse 1, Pulse 2, Wave and Noise. I recommend using AnvilStudio to delete and rearrange the tracks and MIDIEditor to switch notes between tracks if needed.
* AnvilStudio also has a feature to split tracks with multiple channels into separate tracks; you can use this in case you have all your notes in the same track.
* **It is recommended** that you only have one note playing at the same time in each track, but the tool still works if there are concurrent notes playing. If the notes start at the same time, only one of them will be considered but exactly which one may be a bit random. If one starts as the previous is still playing, the first one will end and the new one will play again. This latter feature is particularly useful for the noise channel, so feel free to keep all the notes there and see where it gets you.
If two songs are playing at the same time, the output will include junk information from the other notes, like octaves and intensities, which while ignored by the game, will increase the length of the code unnecessarily.
#### Convert into a .txt file
* Go to [this site](http://flashmusicgames.com/midi/mid2txt.php) and upload your MIDI track. **Make sure you select the Delta timestamp option**.
* After the MIDI file is converted into text, copy it and save to a file named *midi.txt*.
#### Convert into ASM
* Change the settings in the config.txt file to your desired choices.
* Place your midi.txt file into the same folder as the MIDI2ASM executable and run it.
* A command line will pop up with useful information if you need it. If everything goes correctly, just follow the instructions.
* You should now have a *out.asm* file in the same folder.
* The music channels are all named *Placeholder* so you can easily replace the name with whatever you want.
### Manually fixing notes
* I have 100% confidence the note conversion works correctly. However, notetype changes are really tricky to do automatically. It works most of the time, but one badly translated note duration is enough to make the tracks not sync up properly.
* After the output file is written, the command line will have the total length of each track, along with the length they *all* should have. If these numbers don't match exactly, this means some rounding error made some notes shorter or longer than they should be. Even still, it's possible that the rounding errors cancel each other out and the tracks won't sync up in a specific section.
* In conclusion, you'll have to listen to the music and try to figure out which notes might be off.

## Missing Features / Improvements 
* Read directly from a MIDI file. I'm not planning on doing this any time soon, seeing as it works fine at the moment and it would be a ton of work to implement.
* Loops, seeing as some MIDIs have loop information integrated.

## Can I help?
* YES! This is, technically, my first foray into an actual programming language. As such, I guarantee that the code is awful. For that reason alone, feel free to suggest changes to make the code cleaner or suggest fixes.
* Feel free to leave feedback wherever I might see it.

> TriteHexagon. Updated on 23-Sep-2019
