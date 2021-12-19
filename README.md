# Midi2ASM Converter
This is a simple tool to convert MIDI files to [pokecrystal](https://github.com/pret/pokecrystal)-based and [pokered](https://github.com/pret/pokered)-based music, written in C#. As someone who's already [knee-deep into making chiptune music](https://soundcloud.com/user-930339535/sets/all-demixes) using the pokecrystal, I know how important the current tool made by FroggestSpirit is and also how frustrating it can be to arrange some songs using it. It was a good first step, but I felt I could write my own tool and fix the current problems with it, including the annoying fact that its output format is no longer supported, and add new features as well.

It uses [DryWetMidi](https://github.com/melanchall/drywetmidi) for the MIDI-processing logic.

**NOTE:** This program is still in beta and I'm not a professional programmer. **Weird bugs might happen!** Please leave a issue report so I can help you fix them. There's a list of known bugs below which I either can't fix or don't know how to.

## Features
* Converts the notes in the MIDI file into the note commands used in pokecrystal in an .asm file.
* Support for legacy and new pokecrystal commands, and pokered/pokeyellow commands.
* Detects velocity changes in the MIDI and translates them into intensity.
* Detects stereo.
* Detects changes in the notetype.
* Prints out comments with the music's bars so you can easily navigate the notes.
* The output file is ready to play so you can plop it into the game directly.
* An option to make the noise channels' notes entirely of easily replaceable placeholder names.

## What it doesn't do
* **This tool is not intended to directly convert a MIDI into GameBoy music**. The output files *will* work, but they won't sound very good for various reasons.
* The intention is to save arrangers time doing the boring parts, such as fixing triplets, short notes and intensity changes.

## The GUI
Everything has a tooltip so it should be self-explanatory enough, but I'll go over some of the most complex options.

* **Auto-Sync**: This option forces tracks to sync to the chosen notetype by maintaining a "ledger" of all the roundings being done to the note lengths; when the rounding exceeds a certain threshold, the next note is forced to round down or up to fit with the "grid". This thus **guarantees all tracks will sync globally**. However, since some notes are forcibly cut short or prolonged, there might be occasional notes that have the wrong lengths or get their length set to 0 (especially if they are very short). The code requires a certain notetype to be chosen as the "base" for which all notes are rounded to (this is the checked notetype on the list).

If you're unsure which notetype is the best, for 95% of the cases 12 is the ideal option, but there can be certain songs or parts that require other notetypes. You can try using 6 and 3 for parts with very short notes, and 8 or 4 for sections with triplets. Smaller notetypes might lead to a more accurate translation, but it will also lead to longer code. A major downside to using Auto-Sync is that it will "eat up" notes that don't fit with the "grid" (triplets and short notes most of the time). You can allow other notetypes (more on that on the next section) which detects these notes, but this may lead to *too many* notes using these notetypes, and thus longer code.
Nevertheless, **this is the recommended option** unless you know what you're doing.
* **Allowed Notetypes**: This is the list of notetypes that are accepted by the program. *Checked* means it's considered a base notetype and the program always tries to round notes to fit this notetype. A *square* means the notetype is allowed but not as the base; essentially, only when notes fall exactly to the expected length does the program change the notetype.
* **Ignore First Track**: If this is *checked*, then your MIDI files need to have 5 different tracks; the first Track (track 0) only serves to grab the tempo of the song and will be ignored otherwise. If this option is *unchecked*, then you MIDI file needs to have 4 tracks exactly.
* **Noise Templates**: If this is *checked*, the noise channel's notes will be have placeholder notes instead of the usual; for this reason these files will not work directly. These notes are labelled in the format N#H, where # is a number (an octave) and H is an hexadecimal number (from 0 to F). This option also creates a text file in the same directory with the various templates used in the file. If this is *unchecked* then the notes will be written like the other channels.

## Instructions
* Download one of the .exe file in the releases. x64 is for 64-bit machines, x86 is for 32-bit machines. If you don't know what that means, the x86 version should work anywhere.
* The following instructions won't go into how to put custom music into pokecrystal / pokered. If you don't know how to do it, you can follow [this guide](https://github.com/pret/pokecrystal/wiki/Add-a-new-music-song).
#### Prepare your MIDI
* The MIDI needs to be have precisely 5 or 4 tracks, depending on whether you decide to ignore first track or not. The order of the other tracks is Pulse 1, Pulse 2, Wave and Noise. I recommend using AnvilStudio to delete and rearrange the tracks and MIDIEditor to switch notes between tracks if needed.
* AnvilStudio also has a feature to split tracks with multiple channels into separate tracks; you can use this in case you have all your notes in the same track.
* **It is recommended** that you only have one note playing at the same time in each track, but the tool still works if there are concurrent notes playing. If the notes start at the same time, only one of them will be considered but exactly which one may be a bit random. If one starts as the previous is still playing, the first one will end and the new one will play again. This latter feature is particularly useful for the noise channel, so feel free to keep all the notes there and see where it gets you.
If two songs are playing at the same time, the output will include junk information from the other notes, like octaves and intensities, which while ignored by the game, will increase the length of the code unnecessarily.
#### Convert into ASM
* Open the executable and change your options in the GUI. If a message pops up, follow its instructions.
* Select your MIDI file using the Select File button.
* If the conversion was successful, a message will pop-up saying the conversion was successful.
* You should now have a *out.asm* file in the same folder (or with the same name as your MIDI file if you disabled that option).
* If you selected the Noise Templates option, there should be a *out-nt.txt* file in the same folder containing all noise notes detected and the templates for easy replacement.
* The music channels are all named *Placeholder* so you can easily replace the name with whatever you want.

## Missing Features / Improvements 
* Expand the program to include global manipulation of the finished code, such as increase overall intensity for a certain channel.
* Loops, seeing as some MIDIs have loop information integrated.

## Known Bugs & Issues
* When using the 5 track option, if the first track (the tempo track) isn't totally clear of any notes, the program WILL crash. I've tried to fix it but I don't know what the issue is - I'll get back to it someday. If you clear the first track of all notes, it should work.

## Can I help?
* Feel free to leave feedback wherever I might see it.

> TriteHexagon. Updated on 2021-12-19
