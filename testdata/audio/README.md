Small audio fixtures for the audio tests, made with ffmpeg:

- `tone44k.ogg`: 0.5 s, 440 Hz sine at amplitude 0.5, mono, 44.1 kHz Vorbis (`-c:a libvorbis -q:a 2`).
- `loop48k.opus`: 0.25 s, 880 Hz sine at amplitude 0.5, mono, Opus (`-c:a libopus -b:a 32k`).

The source was `-f lavfi -i sine=frequency=<hz>:sample_rate=<rate>:duration=<s>` with `-af volume=4` (the sine filter's own amplitude is 0.125).
