"""Procedural, royalty-free audio for DungeonUnity. Run: python3 Tools/make_audio.py
Everything is synthesised from oscillators and noise; no samples are used."""
import numpy as np, os
from scipy.io import wavfile
from scipy.signal import butter, lfilter
SR = 44100; OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio")
rng = np.random.default_rng(128)
def t(sec): return np.arange(int(SR * sec)) / SR
def env(n, a=0.002, d=0.2, curve=4.0):
    x = np.arange(n) / SR; e = np.minimum(1, x / max(a, 1e-4)) * np.exp(-curve * np.maximum(0, x - a) / max(d, 1e-4)); return e
def lp(x, f, o=2): b, a = butter(o, f / (SR / 2), "low"); return lfilter(b, a, x)
def hp(x, f, o=2): b, a = butter(o, f / (SR / 2), "high"); return lfilter(b, a, x)
def bp(x, lo, hi, o=2): b, a = butter(o, [lo / (SR / 2), hi / (SR / 2)], "band"); return lfilter(b, a, x)
def norm(x, peak=0.89): m = np.max(np.abs(x)) or 1; return x / m * peak
def save(name, x, stereo=False):
    x = norm(x); data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    if stereo and data.ndim == 1: data = np.stack([data, data], 1)
    wavfile.write(os.path.join(OUT, name + ".wav"), SR, data); print(name, round(len(x) / SR, 2), "s")
def kick(len_s=0.45):
    x = t(len_s); f = 45 + 110 * np.exp(-x * 28); ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph) * env(len(x), 0.001, 0.25, 3) + 0.3 * lp(rng.standard_normal(len(x)), 3000) * env(len(x), 0.0005, 0.01, 6)
def hat(len_s=0.08, open_=False):
    n = rng.standard_normal(int(SR * len_s)); return hp(n, 7000) * env(len(n), 0.0005, 0.25 if open_ else 0.03, 5)
def clap():
    n = rng.standard_normal(int(SR * 0.25)); e = sum(env(len(n), 0.001 + i * 0.01, 0.05, 6) for i in range(3)) / 3
    return bp(n, 900, 2500) * e
def place(buf, snd, at):
    i = int(at * SR); j = min(len(buf), i + len(snd)); buf[i:j] += snd[: j - i]
BPM = 128; BEAT = 60 / BPM; BARS = 16; LEN = BARS * 4 * BEAT  # 30s
def techno():
    buf = np.zeros(int(SR * LEN) + SR)
    k, h, ho, c = kick(), hat(), hat(0.3, True), clap()
    notes = [36, 36, 39, 36, 43, 36, 41, 39]  # C minor-ish midi
    for b in range(BARS * 4):
        at = b * BEAT; bar = b // 4
        place(buf, k * 0.9, at)
        place(buf, h * 0.25, at + BEAT / 2)
        if bar >= 2: place(buf, h * 0.12, at + BEAT / 4); place(buf, h * 0.12, at + 3 * BEAT / 4)
        if b % 4 in (1, 3) and bar >= 4: place(buf, c * 0.35, at)
        if b % 8 == 7: place(buf, ho * 0.2, at + BEAT / 2)
        # rolling bass on 16ths, skip downbeat
        for s in range(1, 4):
            m = notes[(b + s) % len(notes)]; f = 440 * 2 ** ((m - 69) / 12)
            x = t(BEAT / 4); saw = 2 * ((x * f) % 1) - 1
            cutoff = 300 + 1500 * (0.5 + 0.5 * np.sin(2 * np.pi * b / 32))
            place(buf, lp(saw, cutoff) * env(len(x), 0.002, 0.08, 3) * 0.35, at + s * BEAT / 4)
    # acid lead from bar 8
    for b in range(32, BARS * 4, 2):
        f = 440 * 2 ** ((60 + [0, 3, 7, 10][b // 2 % 4] - 69) / 12); x = t(BEAT * 0.9)
        sq = np.sign(np.sin(2 * np.pi * f * x)); place(buf, lp(sq, 2200) * env(len(x), 0.01, 0.3, 2) * 0.12, b * BEAT)
    # pad
    x = t(LEN); pad = sum(np.sin(2 * np.pi * f * x + np.sin(x * 0.3 + i)) for i, f in enumerate([130.8, 155.6, 196.0]))
    buf[: len(x)] += lp(pad, 900) * 0.04
    out = buf[: int(SR * LEN)]
    fade = int(SR * 0.01); out[:fade] *= np.linspace(0, 1, fade); out[-fade:] *= np.linspace(1, 0, fade)  # seamless loop click guard
    return out
def loop(snd_fn, bars=2):
    buf = np.zeros(int(SR * bars * 4 * BEAT)); snd = snd_fn()
    for b in range(bars * 4):
        if snd_fn is kick: place(buf, snd, b * BEAT)
        else:
            for s in range(4): place(buf, snd * (1 if s == 2 else 0.4), b * BEAT + s * BEAT / 4)
    return buf
def zap():
    x = t(0.35); f = 2500 * np.exp(-x * 9) + 200; ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sign(np.sin(ph)) * 0.5 * env(len(x), 0.001, 0.25, 3) + np.sin(ph * 1.5) * 0.3 * env(len(x), 0.001, 0.2, 3)
def footstep(i):
    n = rng.standard_normal(int(SR * 0.18)); thud = np.sin(2 * np.pi * (70 + 15 * i) * t(0.18)) * env(len(n), 0.001, 0.05, 4)
    return lp(n, 1200 + 300 * i) * env(len(n), 0.001, 0.04, 5) * 0.6 + thud
def creak():
    x = t(1.8); f = 180 + 60 * np.sin(2 * np.pi * 0.7 * x) + 20 * rng.standard_normal(len(x)).cumsum() / SR * 30
    ph = 2 * np.pi * np.cumsum(f) / SR; s = np.sign(np.sin(ph)) * (0.5 + 0.5 * np.sin(2 * np.pi * 23 * x)) ** 4
    return bp(s, 300, 3000) * np.minimum(1, x / 0.1) * np.minimum(1, (1.8 - x) / 0.3)
def pickup():
    x = t(0.5); out = np.zeros(len(x))
    for i, f in enumerate([660, 880, 1320]):
        st = int(i * 0.08 * SR); y = t(0.5 - i * 0.08); out[st:st + len(y)] += np.sin(2 * np.pi * f * y) * env(len(y), 0.002, 0.15, 3)
    return out
def click(): x = t(0.05); return np.sin(2 * np.pi * 1800 * x) * env(len(x), 0.0005, 0.01, 6)
def hover(): x = t(0.08); return np.sin(2 * np.pi * (900 + 600 * x / 0.08) * x) * env(len(x), 0.002, 0.03, 4) * 0.5
def crowd():
    x = t(20); n = rng.standard_normal(len(x)); murmur = bp(n, 250, 1800) * (0.6 + 0.4 * lp(np.abs(rng.standard_normal(len(x))), 3))
    out = murmur * 0.5 + lp(rng.standard_normal(len(x)), 120) * 0.6
    for _ in range(14):  # occasional cheers
        at = rng.uniform(0, 18); c = bp(rng.standard_normal(int(SR * 1.2)), 600, 3500) * np.hanning(int(SR * 1.2)); place(out, c * 0.8, at)
    f = int(SR * 0.5); out[:f] *= np.linspace(0, 1, f); out[-f:] *= np.linspace(1, 0, f); return out
def roomtone():
    x = t(15); hum = np.sin(2 * np.pi * 50 * x) * 0.2 + np.sin(2 * np.pi * 100 * x) * 0.08
    out = lp(rng.standard_normal(len(x)), 400) * 0.5 + hum; f = int(SR * 0.3); out[:f] *= np.linspace(0, 1, f); out[-f:] *= np.linspace(1, 0, f); return out
def flush():
    x = t(4); n = rng.standard_normal(len(x)); sweep = np.array([bp(n[i:i + SR // 4], 300 + 1200 * (1 - i / len(n)), 3000 + 1500 * (1 - i / len(n))) for i in range(0, len(n), SR // 4)], dtype=object)
    s = np.concatenate(list(sweep))[: len(x)]; e = np.minimum(1, x / 0.2) * np.exp(-np.maximum(0, x - 1.5) * 1.2)
    gurgle = np.sin(2 * np.pi * (120 + 40 * np.sin(2 * np.pi * 6 * x)) * x) * 0.3 * e
    return s * e + gurgle
def bassdrop():
    x = t(3.5); f = 30 + 220 * np.exp(-x * 1.6); ph = 2 * np.pi * np.cumsum(f) / SR
    sub = np.sin(ph) * np.minimum(1, x / 0.02) * np.exp(-x * 0.6); dist = np.tanh(sub * 3) * 0.6
    riser = hp(rng.standard_normal(len(x)), 2000) * np.exp(-x * 4) * 0.3
    return dist + riser
if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    save("Music_Techno128_Loop", techno(), stereo=True)
    save("Loop_Kick128", loop(kick)); save("Loop_Hats128", loop(lambda: hat()))
    save("SFX_LaserZap", zap())
    for i in range(4): save(f"SFX_Footstep_{i+1}", footstep(i))
    save("SFX_DoorCreak", creak()); save("SFX_PillPickup", pickup())
    save("UI_Click", click()); save("UI_Hover", hover())
    save("Amb_Crowd", crowd(), stereo=True); save("Amb_RoomTone", roomtone())
    save("SFX_ToiletFlush", flush()); save("SFX_BassDrop", bassdrop())
