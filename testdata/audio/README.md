Small audio fixtures for the audio tests, made with ffmpeg:

- `tone44k.ogg`: 0.5 s, 440 Hz sine at amplitude 0.5, mono, 44.1 kHz Vorbis (`-c:a libvorbis -q:a 2`).
- `loop48k.opus`: 0.25 s, 880 Hz sine at amplitude 0.5, mono, Opus (`-c:a libopus -b:a 32k`).

The source was `-f lavfi -i sine=frequency=<hz>:sample_rate=<rate>:duration=<s>` with `-af volume=4` (the sine filter's own amplitude is 0.125).
- `tone44k.flac`: 0.5 s, 440 Hz sine at amplitude 0.5, mono, 44.1 kHz FLAC (`-ac 1 -c:a flac`).
- `tone44k.mp3`: the same tone as MP3 (`-ac 1 -c:a libmp3lame -q:a 4`). MP3 adds encoder delay and padding, so its length is a few frames off 0.5 s.
- `track48k.flac`: 3 s, 330 Hz sine at amplitude 0.5, stereo, 48 kHz FLAC (`-ac 2 -c:a flac`), for streaming position tests.
