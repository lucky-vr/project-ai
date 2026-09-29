"""Regenerate the village's original, procedurally synthesized WAV cues."""

import math
import random
import struct
import wave
from pathlib import Path

SAMPLE_RATE = 22050
HERE = Path(__file__).resolve().parent


def bell(frequency, elapsed, duration):
    if elapsed < 0 or elapsed > duration:
        return 0.0
    attack = min(1.0, elapsed / 0.012)
    decay = math.exp(-4.2 * elapsed / duration)
    phase = 2 * math.pi * frequency * elapsed
    return attack * decay * (
        math.sin(phase) + 0.35 * math.sin(2.02 * phase) + 0.12 * math.sin(3.93 * phase)
    )


def write_wav(name, samples):
    peak = max(max(abs(x) for x in samples), 1.0)
    with wave.open(str(HERE / name), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(SAMPLE_RATE)
        out.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, x / peak)) * 32767)) for x in samples))


def cue(name, length, notes, volume):
    samples = []
    for i in range(int(length * SAMPLE_RATE)):
        t = i / SAMPLE_RATE
        value = sum(gain * bell(freq, t - start, duration) for start, duration, freq, gain in notes)
        samples.append(value * volume)
    write_wav(name, samples)


def ambience():
    random.seed(280926)
    duration = 24
    samples = []
    low_noise = 0.0
    birds = [(2.5, 0.4, 900), (6.8, 0.32, 1150), (11.4, 0.48, 980),
             (16.1, 0.36, 1260), (20.8, 0.4, 1020)]
    for i in range(duration * SAMPLE_RATE):
        t = i / SAMPLE_RATE
        low_noise = low_noise * 0.995 + random.uniform(-1, 1) * 0.005
        breeze = low_noise * 0.12 * (0.7 + 0.3 * math.sin(2 * math.pi * t / 11))
        pad = (math.sin(2 * math.pi * 130.81 * t) +
               0.6 * math.sin(2 * math.pi * 196.00 * t) +
               0.35 * math.sin(2 * math.pi * 164.81 * t)) * 0.012
        chirp = 0.0
        for start, length, base in birds:
            dt = t - start
            if 0 < dt < length:
                envelope = math.sin(math.pi * dt / length) ** 2
                phase = 2 * math.pi * (base * dt + 310 * dt * dt / length)
                chirp += math.sin(phase) * envelope * 0.026
        seam = min(1.0, t / 1.5, (duration - t) / 1.5)
        samples.append((breeze + pad + chirp) * seam)
    write_wav("village_ambience.wav", samples)


if __name__ == "__main__":
    cue("action_success.wav", .62, [(0, .38, 659.25, 1), (.10, .42, 880, .65)], .25)
    cue("place_complete.wav", 1.45, [(0, .6, 523.25, .85), (.16, .7, 659.25, .8),
                                       (.32, .85, 783.99, .7), (.50, .95, 1046.5, .5)], .24)
    cue("village_complete.wav", 2.8, [(0, 1.1, 523.25, .9), (.18, 1.2, 659.25, .8),
                                        (.36, 1.35, 783.99, .75), (.7, 1.8, 1046.5, .7),
                                        (1.08, 1.5, 1318.5, .45)], .26)
    ambience()
