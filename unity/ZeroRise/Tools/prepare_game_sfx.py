"""Offline edits of documented CC0 recordings. Run from the Unity project.

Sources are cached in QA/sfx-sources. No generated waveform or downloaded
executable is used; numpy plus Python's PCM WAV reader are sufficient.
"""
from pathlib import Path
import json
import wave
import numpy as np

SOURCE = Path('QA/sfx-sources')
OUT = Path('Assets/ZeroRise/Art/Audio/GameSfx')
OUT.mkdir(parents=True, exist_ok=True)


def read(name):
    with wave.open(str(SOURCE / (name + '.wav')), 'rb') as w:
        rate, channels, width = w.getframerate(), w.getnchannels(), w.getsampwidth()
        raw = w.readframes(w.getnframes())
    if width == 3:
        b = np.frombuffer(raw, np.uint8).reshape(-1, 3).astype(np.int32)
        v = b[:, 0] | (b[:, 1] << 8) | (b[:, 2] << 16)
        v = ((v ^ 0x800000) - 0x800000) / 8388608.0
    elif width == 2:
        v = np.frombuffer(raw, '<i2') / 32768.0
    else:
        raise ValueError('Unsupported PCM width: ' + str(width))
    v = v.reshape(-1, channels).mean(axis=1)
    if rate != 48000:
        v = np.interp(np.arange(round(len(v) * 48000 / rate)) / 48000,
                      np.arange(len(v)) / rate, v)
    return v


cache = {}
manifest = []


def edit(source, name, start, end, low=7000, high=45, loop=0, rms=.17, fade=.006, bell=None, require_full=False):
    if source not in cache:
        cache[source] = read(source)
    x = cache[source][round(start*48000):round(end*48000)].copy()
    if require_full:
        assert len(x) == round(end*48000)-round(start*48000), (name, 'source ends before crop')
    assert len(x) > 960, name
    x -= x.mean()
    f = np.fft.rfftfreq(len(x), 1/48000)
    curve = 1 / np.sqrt(1 + (f/low)**8)
    if high:
        curve *= 1 - 1 / np.sqrt(1 + (f/high)**8)
    if bell:
        center, width, db = bell
        curve *= 10 ** (db * np.exp(-.5 * ((f-center)/width)**2) / 20)
    x = np.fft.irfft(np.fft.rfft(x)*curve, n=len(x))
    if loop:
        n = min(round(loop*48000), len(x)//4)
        t = np.linspace(0, np.pi/2, n)
        x = np.concatenate((x[n:-n], x[-n:]*np.cos(t) + x[:n]*np.sin(t)))
    else:
        n = min(round(fade*48000), len(x)//4)
        envelope = np.sin(np.linspace(0, np.pi/2, n))**2
        x[:n] *= envelope
        x[-n:] *= envelope[::-1]
    x *= min(rms/max(1e-8, np.sqrt(np.mean(x*x))), .82/max(1e-8, abs(x).max()))
    with wave.open(str(OUT/(name+'.wav')), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(48000)
        w.writeframes(np.round(x*32767).astype('<i2').tobytes())
    manifest.append(dict(clip=name, recording=source, start=start, end=end,
                         seconds=len(x)/48000, loopOverlap=loop,
                         lowpass=low, highpass=high, bell=bell, fade=0 if loop else fade,
                         peak=float(abs(x).max()), rms=float(np.sqrt(np.mean(x*x)))))


def footfalls(source, prefix, starts, duration, target):
    """Keep reviewed single contacts, rather than the eight loudest raw peaks.

    Candidate windows were compared after the playback bandpass. Short crops
    retain the contact and material decay without the next scuff/footfall.
    Conservative RMS targets avoid amplifying the soft tails to a sharp tick.
    """
    assert len(starts) == 8
    for i, start in enumerate(starts):
        edit(source, prefix+str(i), start, start+duration,
             low=5800, high=90, rms=target, fade=.012, require_full=True)


# Independent engine and road layers keep tyre noise out of a stationary car.
edit('EngineStationary', 'EngineIdle', 6, 12, low=1250, high=55, loop=.35, rms=.15)
edit('EngineStationary', 'EngineLoad', 12, 18, low=1800, high=55, loop=.40, rms=.16, bell=(1348,320,-6))
# The old 900 Hz highpass still contained a strong 1.2 kHz engine/cabin
# resonance. The road layer now keeps the higher, broadband contact texture.
edit('CarDrive', 'RoadTyres', 43, 49, low=7500, high=2100, loop=.45, rms=.075)
edit('CarDrive', 'EngineStart', .02, 1.65, low=6000, rms=.16)
edit('CarDrive', 'EngineStop', 67.25, 70.4, low=3500, rms=.12, fade=.05)
# Keep the latch/open and body slam separate so they follow the actual hinge.
edit('CarDoor', 'DoorOpen', .0, 1.35, low=9000, rms=.15)
edit('CarDoor', 'DoorClose', 3.17, 3.82, low=9000, rms=.17)
edit('CarDoor', 'CabinClick', .0, .23, low=4500, rms=.10)
edit('CarHorn', 'Horn', .02, 1.55, low=8000, rms=.20)
edit('TyreSkid', 'TyreSlip', .12, 1.95, low=8000, high=200, loop=.10, rms=.18)
for i, start in enumerate([.25, 2.8, 5.0]):
    edit('Cloth', 'Clothing'+str(i), start, start+.65, low=6500, high=150, rms=.10)
for i, start in enumerate([2.5, 7.0, 13.0]):
    edit('Drain', 'DrainDrop'+str(i), start, start+2.2, low=7000, rms=.12)
edit('Fence', 'FenceCreak', .1, 1.7, low=5000, rms=.12)
for i, start in enumerate([5, 21, 39]):
    edit('Sparrows', 'Sparrow'+str(i), start, start+3.1, low=10000, high=500, rms=.10, fade=.12)
edit('WindTree', 'LeavesAir', 42, 86, low=7000, high=170, loop=1.5, rms=.12)
footfalls('FootConcrete', 'ShoeConcrete',
          [1.89, 4.34, 13.90, 14.47, 16.86, 19.31, 20.48, 23.93], .22, .050)
footfalls('FootGravel', 'ShoeGravel',
          [3.92, 5.13, 6.35, 7.39, 14.71, 18.41, 20.37, 24.16], .25, .045)
footfalls('FootGrass', 'ShoeGrass',
          [5.08, 6.87, 7.85, 8.83, 10.51, 14.31, 17.75, 23.27], .25, .045)
# All three complete windows come from the actual smooth branch swish. The
# original is only 2.5465 seconds: its quiet end and isolated twig clicks must
# not become an over-amplified or silently truncated contact variant.
for i, start in enumerate([.40, .65, .85]):
    edit('BushLeaves', 'BushRustle'+str(i), start, start+.55,
         low=6500, high=240, rms=.065, fade=.055, require_full=True)
(OUT/'RecordingEdits.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
print('Edited', len(manifest), 'recorded clips; 48 kHz mono, peaks <= 0.82.')
