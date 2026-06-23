# Vehicle event sounds

Place vehicle-specific sound effect files in this folder.

Sound assets are not included with this project. Prepare your own short audio files only after confirming that you have the rights to use them. If no audio files are present, the simulator still runs normally and simply stays silent.

This folder is mainly for short event sound effects. Continuous engine loop playback is also supported. The simulator first looks for multiple RPM-range loop files and crossfades between the available layers. If fewer than two layer files are available, it falls back to the existing single `engine-loop.wav` file and changes pitch from the current simulator RPM.

## Expected files

```text
Assets/Sounds/LexusLfa/
  ignition-on.wav
  ignition-off.wav
  engine-start.wav
  engine-stop.wav
  shift-up.wav
  shift-down.wav
  reverse-engage.wav
  reverse-disengage.wav
  parking-brake-applied.wav
  parking-brake-released.wav
  engine-loop.wav
  idle-loop.wav
  low-loop.wav
  mid-loop.wav
  high-loop.wav

Assets/Sounds/LexusLc500/
  ignition-on.wav
  ignition-off.wav
  engine-start.wav
  engine-stop.wav
  shift-up.wav
  shift-down.wav
  reverse-engage.wav
  reverse-disengage.wav
  parking-brake-applied.wav
  parking-brake-released.wav
  engine-loop.wav
  idle-loop.wav
  low-loop.wav
  mid-loop.wav
  high-loop.wav

Assets/Sounds/E13 Note e-POWER/
  Startup.wav    : system startup sound
  Seatbelt.wav   : seatbelt warning after startup
  signal.wav     : legacy local turn signal source
  Reverse.wav    : legacy local reverse warning source
  SystemStop.wav : system shutdown sound

Assets/Sounds/NissanNoteE13/
  turn-signal-loop.wav : one-shot turn signal / hazard click source
  reverse-loop.wav     : reverse warning loop
  forward-approach-loop.wav : low-speed forward vehicle-approaching loop
```

## Events

- Ignition ON / OFF
- Engine start / stop
- Successful shift up / shift down
- Reverse gear engage / disengage
- Parking brake apply / release
- Engine running loop fallback: `engine-loop.wav`
- Optional RPM layers: `idle-loop.wav`, `low-loop.wav`, `mid-loop.wav`, `high-loop.wav`
- Nissan Note e-POWER E13 startup, seatbelt warning, turn signal, reverse warning, low-speed forward approach warning, and shutdown sounds

The JSON files in `Data/Vehicles` define which relative sound path belongs to each event.

For Nissan Note e-POWER E13, the turn signal file is triggered once per blink cycle instead of being looped directly. The visual turn indicators use the same configured period, so the click timing and lamp timing stay aligned.

For Nissan Note e-POWER E13, `forward-approach-loop.wav` is used only while READY and moving forward at low speed in D or B. It fades in from very low speed, fades out at higher speed, and does not play in R because reverse uses `reverse-loop.wav`.

## Engine loop audio

`engine-loop.wav` is the fallback engine loop. It is used when no RPM layer files exist, or when only one layer file exists.

If at least two of the following files exist for the selected vehicle, the simulator uses multi-layer crossfade playback instead:

```text
idle-loop.wav : sound near idle rpm
low-loop.wav  : low-rpm sound
mid-loop.wav  : mid-rpm sound
high-loop.wav : high-rpm sound
```

Engine loop audio is played only while the simulated engine is running. Ignition self-check does not play this audio, because the engine is still off during that animation.

Recommended format:

```text
Format: WAV
Length: 1 to 5 seconds
Content: naturally loopable engine sound for the intended rpm range
```

You must provide audio files yourself and confirm that you have permission to use them. If the files are missing, the application continues to run silently. For convincing multi-layer playback, prepare loop files from the same vehicle and similar recording conditions.

## Prototype material note

For local RPM-linked engine sound testing, the project may use temporary CC0 material from OpenGameArt, such as `racing car engine sound loops` by `domasx2`, and `Car engine start 01` by `looneybits`. These materials are not Lexus LFA or Lexus LC500 recordings, and they must be treated only as placeholders for verifying the audio implementation.

Audio files are intentionally excluded from Git by `.gitignore`. Before publishing builds or pushing audio assets anywhere, re-check the source license and whether bundling the converted audio with the application is permitted.

The Nissan Note e-POWER E13 local turn signal, reverse warning, and forward approach warning files are prototype test assets. Do not describe them as genuine Nissan recordings unless their source and license have been separately verified.
