"""Reproducible offline edits of the CC0 weather recordings in QA/weather-sources.

Run from the Unity project using Python + numpy. Originals stay outside Assets;
the game never downloads audio or synthesizes a noise substitute for rainfall.
MP3 preview inputs are decoded once to WAV before running this script; see README.
"""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

SOURCE = Path('QA/weather-sources')
OUT = Path('Assets/ZeroRise/Art/Audio/WeatherSfx')
OUT.mkdir(parents=True, exist_ok=True)
manifest = []


def read(name):
    path = SOURCE / (name + '.wav')
    with wave.open(str(path), 'rb') as w:
        rate, channels, width = w.getframerate(), w.getnchannels(), w.getsampwidth()
        raw = w.readframes(w.getnframes())
    if width == 3:
        b = np.frombuffer(raw, np.uint8).reshape(-1, 3).astype(np.int32)
        v = b[:, 0] | (b[:, 1] << 8) | (b[:, 2] << 16)
        v = ((v ^ 0x800000) - 0x800000) / 8388608.0
    elif width == 2:
        v = np.frombuffer(raw, '<i2') / 32768.0
    else:
        raise ValueError((path, 'Unsupported PCM width', width))
    v = v.reshape(-1, channels).mean(axis=1)
    if rate != 48000:
        v = np.interp(np.arange(round(len(v)*48000/rate))/48000,
                      np.arange(len(v))/rate, v)
    return v


def edit(source, name, start, end, high, low, target, overlap=0, fade=.08):
    x = read(source)[round(start*48000):round(end*48000)].copy()
    assert len(x) == round((end-start)*48000), (source, 'Incomplete crop')
    x -= x.mean()
    f = np.fft.rfftfreq(len(x), 1/48000)
    curve = (1 - 1/np.sqrt(1+(f/high)**8)) / np.sqrt(1+(f/low)**8)
    x = np.fft.irfft(np.fft.rfft(x)*curve, n=len(x))
    if overlap:
        n = round(overlap*48000)
        t = np.linspace(0, np.pi/2, n)
        x = np.concatenate((x[n:-n], x[-n:]*np.cos(t) + x[:n]*np.sin(t)))
    else:
        n = round(fade*48000)
        envelope = np.sin(np.linspace(0, np.pi/2, n))**2
        x[:n] *= envelope
        x[-n:] *= envelope[::-1]
    x *= min(target/max(1e-8, np.sqrt(np.mean(x*x))), .72/max(1e-8, abs(x).max()))
    pcm = np.round(x*32767).astype('<i2').tobytes()
    with wave.open(str(OUT/(name+'.wav')), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(48000)
        w.writeframes(pcm)
    manifest.append(dict(clip=name, source=source, start=start, end=end,
                         seconds=len(x)/48000, highpass=high, lowpass=low,
                         rms=float(np.sqrt(np.mean(x*x))), peak=float(abs(x).max()),
                         loopOverlap=overlap, fade=0 if overlap else fade,
                         pcmSha256=hashlib.sha256(pcm).hexdigest()))


# The close-mic concrete take has hard clipped individual drops. Use the
# steadier late terrace section for the broad urban rain bed instead.
edit('RainTerrace', 'RainRoad', 118, 151, 160, 7500, .055, overlap=1.5)
edit('RainRoof', 'RainRoof', 10, 38, 130, 4800, .060, overlap=1.5)
edit('RainLeaves', 'RainLeaves', 5, 36, 190, 7200, .055, overlap=1.5)
edit('Thunder', 'Thunder', 0, 5, 35, 3200, .10, fade=.20)
# Retain only the broadband water/tyre texture of an actual wet-street pass.
# Position, gain and movement come from the real game vehicle, not a canned pass.
edit('WetTyres', 'WetTyres', 7.2, 10.4, 1900, 7600, .050, overlap=.35)
(OUT/'RecordingEdits.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
print(json.dumps(manifest, indent=2))
