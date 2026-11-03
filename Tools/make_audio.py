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
def chill():
    """Slow ambient pad with a soft 64 bpm pulse, 16 s seamless loop."""
    L = 16.0; x = t(L); out = np.zeros(len(x))
    chords = [[220.0, 261.6, 329.6], [196.0, 246.9, 293.7], [174.6, 220.0, 261.6], [196.0, 246.9, 329.6]]
    seg = len(x) // 4
    for i, ch in enumerate(chords):
        y = t(seg / SR); e = np.sin(np.pi * np.arange(seg) / seg) ** 0.5
        tone = sum(np.sin(2 * np.pi * f * y + 0.5 * np.sin(2 * np.pi * 0.2 * y + j)) for j, f in enumerate(ch))
        out[i * seg:(i + 1) * seg] += lp(tone, 1200) * e * 0.3
    for b in range(int(L * 64 / 60)):
        place(out, lp(kick(0.6), 200) * 0.25, b * 60 / 64)
    out += lp(rng.standard_normal(len(x)), 3000) * 0.01 * (0.5 + 0.5 * np.sin(2 * np.pi * x / L))
    f = int(SR * 0.02); out[:f] *= np.linspace(0, 1, f); out[-f:] *= np.linspace(1, 0, f); return out
def house():
    """Deep-house groove at 128 bpm for the VIP room, 8 bars."""
    bars = 8; buf = np.zeros(int(SR * bars * 4 * BEAT) + SR); k, h, c = kick(), hat(0.12, True), clap()
    chord = [[57, 60, 64, 67], [55, 59, 62, 65], [53, 57, 60, 64], [55, 59, 62, 67]]
    for b in range(bars * 4):
        at = b * BEAT; place(buf, k * 0.7, at); place(buf, h * 0.2, at + BEAT / 2)
        if b % 2 == 1: place(buf, c * 0.25, at)
        if b % 4 in (0, 2):
            x = t(BEAT * 0.45); notes = chord[(b // 4) % 4]
            stab = sum(np.sign(np.sin(2 * np.pi * 440 * 2 ** ((m - 69) / 12) * x)) for m in notes)
            place(buf, lp(stab, 1400) * env(len(x), 0.005, 0.2, 3) * 0.05, at + BEAT * 0.5)
        m = chord[(b // 4) % 4][0] - 24; f = 440 * 2 ** ((m - 69) / 12); x = t(BEAT * 0.4)
        place(buf, np.sin(2 * np.pi * f * x) * env(len(x), 0.005, 0.25, 2) * 0.35, at + BEAT * 0.5)
    out = buf[: int(SR * bars * 4 * BEAT)]; f = int(SR * 0.01); out[:f] *= np.linspace(0, 1, f); out[-f:] *= np.linspace(1, 0, f); return out
def muffled(x): return lp(x, 350, 4) * 1.6
def corridor_hum():
    x = t(12); buzz = np.sign(np.sin(2 * np.pi * 60 * x)) * 0.05 + np.sin(2 * np.pi * 120 * x) * 0.1
    flicker = (rng.random(len(x) // 2205 + 1) > 0.97).repeat(2205)[: len(x)] * 0.15
    out = lp(buzz, 1500) * (1 + flicker) + lp(rng.standard_normal(len(x)), 200) * 0.3
    f = int(SR * 0.3); out[:f] *= np.linspace(0, 1, f); out[-f:] *= np.linspace(1, 0, f); return out
def drone():
    x = t(20); out = sum(np.sin(2 * np.pi * f * x + np.sin(2 * np.pi * 0.05 * x * (i + 1))) * a for i, (f, a) in enumerate([(55, 0.4), (82.4, 0.25), (110, 0.15), (164.8, 0.08)]))
    out = out + bp(rng.standard_normal(len(x)), 2000, 6000) * 0.01
    f = int(SR * 0.5); out[:f] *= np.linspace(0, 1, f); out[-f:] *= np.linspace(1, 0, f); return out

# ---------------- v3: richer stereo tracks, sidechain, reverb, saturation ----------------
def mtof(m): return 440 * 2 ** ((m - 69) / 12)
def sat(x, d=2.0): return np.tanh(x * d) / np.tanh(d)
def reverb(x, mix=0.25, decay=0.5):
    out = x.copy()
    for dl, g in [(0.029, 0.7), (0.037, 0.65), (0.041, 0.6), (0.053, 0.55), (0.067, 0.5), (0.083, 0.45)]:
        d = int(dl * SR); y = np.zeros_like(x); y[d:] = x[:-d] * g * decay
        for k in range(1, 4): dd = d * (k + 1); y[dd:] += x[:-dd] * (g * decay) ** (k + 1)
        out += y * mix / 3
    return lp(out, 7000)
def delay(x, sec, fb=0.4, mix=0.3):
    d = int(sec * SR); out = x.copy(); y = x.copy()
    for _ in range(5): y = np.concatenate([np.zeros(d), y[:-d]]) * fb; out += y * mix / fb
    return out
def saw(f, x): return 2 * ((x * f) % 1) - 1
def supersaw(f, x, n=5, det=0.012):
    return sum(saw(f * (1 + det * (i - n // 2)), x + i * 0.13) for i in range(n)) / n
def acid303(m, sec, cut, res=0.85, accent=1.0):
    """Saw through a resonant swept low pass (state variable) for the squelch."""
    x = t(sec); src = saw(mtof(m), x); out = np.zeros(len(x)); lo = bd = 0.0
    e = env(len(x), 0.002, sec * 0.6, 3)
    for i in range(len(x)):
        fc = cut * (0.25 + 0.75 * e[i] * accent); fq = 2 * np.sin(np.pi * min(fc, SR / 6) / SR); q = 1 - res
        hi = src[i] - lo - q * bd; bd += fq * hi; lo += fq * bd; out[i] = lo
    return sat(out * e, 2.5)
def pump(n, beat_s, depth=0.7):
    ph = (np.arange(n) / SR) % beat_s / beat_s; return 1 - depth * np.exp(-ph * 9)
def snare():
    n = int(SR * 0.22); x = t(0.22)
    return bp(rng.standard_normal(n), 1500, 8000) * env(n, 0.001, 0.08, 5) * 0.7 + np.sin(2 * np.pi * 190 * x) * env(n, 0.001, 0.05, 5)
def metal_hit():
    n = int(SR * 0.4); x = t(0.4)
    tone = sum(np.sin(2 * np.pi * f * x) for f in [311, 467, 733, 1187, 1690]) / 5
    return sat((tone + bp(rng.standard_normal(n), 2000, 9000) * 0.5) * env(n, 0.001, 0.12, 4), 3)
def stereoize(l, r): return np.stack([l, r], 1)
def finish(L, R, bars, bpm):
    n = int(SR * bars * 4 * 60 / bpm); L, R = L[:n], R[:n]
    f = int(SR * 0.008)
    for ch in (L, R): ch[:f] *= np.linspace(0, 1, f); ch[-f:] *= np.linspace(1, 0, f)
    return stereoize(sat(L, 1.3), sat(R, 1.3))
class Mix:
    def __init__(s, bars, bpm): s.bpm = bpm; s.B = 60 / bpm; s.n = int(SR * (bars * 4 * s.B + 2)); s.L = np.zeros(s.n); s.R = np.zeros(s.n); s.bars = bars; s.bus = np.zeros(s.n); s.busR = np.zeros(s.n)
    def add(s, snd, at, vol=1.0, pan=0.0, bus=False):
        i = int(at * SR); j = min(s.n, i + len(snd)); a = snd[: j - i] * vol
        l, r = np.sqrt(0.5 * (1 - pan)), np.sqrt(0.5 * (1 + pan))
        if bus: s.bus[i:j] += a * l; s.busR[i:j] += a * r
        else: s.L[i:j] += a * l; s.R[i:j] += a * r
    def render(s, pump_depth=0.6, verb=0.25):
        p = pump(s.n, s.B, pump_depth)
        L = s.L + reverb(s.bus, verb) * p; R = s.R + reverb(np.roll(s.busR, 331), verb) * p
        return finish(L, R, s.bars, s.bpm)
def kick2(punch=1.0, tail=0.3):
    x = t(0.5); f = 48 + 160 * np.exp(-x * 35); ph = 2 * np.pi * np.cumsum(f) / SR
    click_ = hp(rng.standard_normal(len(x)), 4000) * env(len(x), 0.0003, 0.004, 6)
    return sat(np.sin(ph) * env(len(x), 0.001, tail, 3) * punch, 1.6) + click_ * 0.4
def pad_chord(ms, sec, cut=1800, det=0.008):
    x = t(sec); y = sum(supersaw(mtof(m), x, 3, det) for m in ms) / len(ms)
    e = np.minimum(1, x / (sec * 0.3)) * np.minimum(1, (sec - x) / (sec * 0.3)); return lp(y, cut) * e
def track_techno():
    m = Mix(16, 130); B = m.B; k = kick2(); h = hat(0.05); ho = hat(0.25, True); c = clap(); rumble = lp(kick2(0.8, 0.6), 160)
    for b in range(64):
        at = b * B; bar = b // 4
        if not (bar in (7, 15) and b % 4 == 3): m.add(k, at, 0.95)
        m.add(rumble, at + B / 2, 0.35, bus=True)
        m.add(ho, at + B / 2, 0.22, 0.3)
        for s16 in range(4): m.add(h, at + s16 * B / 4, 0.1 + 0.05 * (s16 == 2), -0.4 + 0.2 * s16)
        if b % 2 == 1 and bar >= 2: m.add(c, at, 0.4, bus=True)
        if bar >= 4 and b % 4 == 0:
            for j, mm in enumerate([48, 51, 48, 55]):
                x = t(B * 0.2); m.add(lp(saw(mtof(mm), x), 900 + 600 * np.sin(bar)) * env(len(x), 0.002, 0.08), at + j * B * 0.75, 0.25, 0.5 * (-1) ** j, bus=True)
    for bar in range(0, 16, 4): m.add(pad_chord([60, 63, 67], 4 * 4 * B, 900), bar * 4 * B, 0.12, bus=True)
    return m.render(0.5, 0.3)
def track_acid():
    m = Mix(16, 132); B = m.B; k = kick2(); h = hat(0.04); c = clap()
    seq = [36, 36, 48, 36, 39, 36, 46, 36, 36, 51, 36, 43, 36, 48, 39, 41]; acc = [1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0]
    for b in range(64):
        at = b * B; bar = b // 4
        m.add(k, at, 0.9); m.add(h, at + B / 2, 0.2, 0.3)
        if b % 2 == 1: m.add(c, at, 0.35, bus=True)
        for s16 in range(4):
            idx = (b * 4 + s16) % 16; cut = 400 + 2600 * (0.5 + 0.5 * np.sin(2 * np.pi * (b * 4 + s16) / 128))
            m.add(acid303(seq[idx], B / 4 * 0.95, cut, 0.9, 1.4 if acc[idx] else 0.8), at + s16 * B / 4, 0.32, 0.15 * np.sin(b), bus=bar >= 8)
    return m.render(0.45, 0.2)
def track_industrial():
    m = Mix(16, 138); B = m.B; k = sat(kick2(1.3, 0.25), 3); mh = metal_hit(); sn = snare(); h = hat(0.03)
    for b in range(64):
        at = b * B; bar = b // 4
        m.add(k, at, 1.0)
        if b % 4 == 3: m.add(k, at + B * 0.75, 0.6)
        m.add(mh, at + B / 2, 0.25 if bar % 2 else 0.15, 0.5 * (-1) ** b, bus=True)
        if b % 2 == 1: m.add(sn, at, 0.45, bus=True)
        for s16 in range(4): m.add(h, at + s16 * B / 4, 0.08, -0.3)
    x = t(m.n / SR); drn = sat(lp(saw(mtof(31), x) + saw(mtof(31) * 1.01, x), 300), 4) * 0.25
    noise = bp(rng.standard_normal(len(x)), 300, 1200) * 0.05 * (0.5 + 0.5 * np.sin(2 * np.pi * x / (8 * B)))
    m.L += (drn + noise) * pump(m.n, B, 0.8); m.R += (drn + np.roll(noise, 999)) * pump(m.n, B, 0.8)
    return m.render(0.7, 0.35)
def track_house():
    m = Mix(16, 124); B = m.B; k = kick2(0.9, 0.35); c = clap(); h = hat(0.12, True); sh = hat(0.03)
    prog = [[57, 60, 64, 67], [53, 57, 60, 64], [55, 59, 62, 65], [52, 55, 59, 64]]
    for b in range(64):
        at = b * B; bar = b // 4; ch = prog[bar % 4]
        m.add(k, at, 0.8); m.add(h, at + B / 2, 0.22, 0.25)
        for s16 in range(4): m.add(sh, at + s16 * B / 4 + (B * 0.03 if s16 % 2 else 0), 0.08, -0.35)
        if b % 2 == 1: m.add(c, at, 0.35, bus=True)
        if b % 2 == 0:
            x = t(B * 0.35); stab = sum(np.sin(2 * np.pi * mtof(n) * x) + 0.3 * np.sin(4 * np.pi * mtof(n) * x) for n in ch) / 4
            m.add(lp(stab, 2500) * env(len(x), 0.003, 0.18, 3), at + B * 0.5, 0.3, 0.2, bus=True)
        x = t(B * 0.45); bassn = ch[0] - 24 + (12 if b % 4 == 3 else 0)
        m.add(sat(np.sin(2 * np.pi * mtof(bassn) * x), 1.5) * env(len(x), 0.004, 0.3, 2), at + B * 0.5, 0.45)
        if b % 8 == 6:
            x = t(B * 0.25); m.add(np.sin(2 * np.pi * mtof(ch[3] + 12) * x) * env(len(x), 0.002, 0.15), at + B * 0.25, 0.12, -0.5, bus=True)
    return m.render(0.45, 0.35)
def track_ambient():
    m = Mix(8, 72); B = m.B; prog = [[45, 52, 57, 60, 64], [41, 48, 53, 57, 64], [43, 50, 55, 59, 62], [40, 47, 52, 55, 59]]
    for bar in range(8):
        m.add(pad_chord(prog[bar % 4], 4 * B * 1.6, 1400, 0.006), bar * 4 * B, 0.35, 0.3 * (-1) ** bar, bus=True)
        for j in range(4):
            n = prog[bar % 4][(j * 2 + bar) % 5] + 12; x = t(B * 1.5)
            bell = (np.sin(2 * np.pi * mtof(n) * x) + 0.4 * np.sin(2 * np.pi * mtof(n) * 2.76 * x)) * env(len(x), 0.002, 0.8, 3)
            m.add(bell, bar * 4 * B + j * B + 0.5 * B * (j % 2), 0.12, 0.6 * np.sin(j + bar), bus=True)
        m.add(lp(kick2(0.5, 0.5), 150), bar * 4 * B, 0.3); m.add(lp(kick2(0.5, 0.5), 150), bar * 4 * B + 2 * B, 0.2)
    x = t(m.n / SR); air = lp(hp(rng.standard_normal(len(x)), 3000), 8000) * 0.012 * (0.5 + 0.5 * np.sin(2 * np.pi * x / 9))
    m.L += air; m.R += np.roll(air, 4000)
    return m.render(0.0, 0.7)
def track_festival():
    m = Mix(16, 128); B = m.B; k = kick2(1.2, 0.3); c = clap(); sn = snare(); h = hat(0.1, True)
    prog = [[57, 60, 64], [53, 57, 60], [48, 52, 55], [55, 59, 62]]; lead = [69, 72, 76, 74, 72, 69, 67, 69]
    for b in range(64):
        at = b * B; bar = b // 4
        build = 4 <= bar < 8
        if not build: m.add(k, at, 1.0)
        m.add(h, at + B / 2, 0.2, 0.3)
        if build:
            rate = 1 if bar < 6 else (2 if bar < 7 else 4)
            for r in range(rate): m.add(sn, at + r * B / rate, 0.2 + 0.1 * (bar - 4), bus=True)
        elif b % 2 == 1: m.add(c, at, 0.45, bus=True)
        if bar >= 8 or bar < 4:
            x = t(B * 0.9); ch = prog[bar % 4]
            m.add(supersaw(mtof(ch[0] - 12), x, 5, 0.015) * env(len(x), 0.005, 0.5, 2), at, 0.12, bus=True)
        if bar >= 8:
            x = t(B * 0.45); n = lead[b % 8]
            m.add(lp(supersaw(mtof(n), x, 7, 0.02), 5000) * env(len(x), 0.005, 0.35, 2), at + (B * 0.5 if b % 2 else 0), 0.18, 0.4 * np.sin(b), bus=True)
        x = t(B * 0.5); m.add(sat(np.sin(2 * np.pi * mtof(prog[bar % 4][0] - 24) * x), 2) * env(len(x), 0.004, 0.3), at + B / 2, 0.4)
    x = t(4 * 4 * B); riser = hp(rng.standard_normal(len(x)), 500) * (x / x[-1]) ** 2 * 0.25
    m.add(lp(riser, 9000), 4 * 4 * B, 1.0, bus=True)
    for bar in range(0, 16, 4): m.add(pad_chord([57, 64, 69, 72], 4 * 4 * B, 3000, 0.02), bar * 4 * B, 0.08, bus=True)
    return m.render(0.65, 0.35)
def bathroom_ambience():
    L = 20.0; x = t(L); out = np.zeros(len(x))
    hum = (np.sin(2 * np.pi * 120 * x) * 0.12 + np.sin(2 * np.pi * 240 * x) * 0.05 + bp(rng.standard_normal(len(x)), 3000, 6000) * 0.01)
    hum *= 1 + 0.4 * ((rng.random(len(x) // 2205 + 1) > 0.985).repeat(2205)[: len(x)])
    out += hum
    beat = 60 / 128
    for i in range(int(L / beat)):
        kk = lp(kick2(1.0, 0.25), 120, 4) * 0.9; place(out, kk, i * beat)
        if i % 2 == 1: place(out, lp(clap(), 300, 4) * 0.3, i * beat)
    bass = lp(sat(np.sin(2 * np.pi * 55 * x), 2), 150, 4) * 0.25 * pump(len(x), beat, 0.9); out += bass
    for _ in range(40):
        at = rng.uniform(0, L - 0.3); f0 = rng.uniform(900, 2200); d = t(0.12)
        drip = np.sin(2 * np.pi * (f0 + 1800 * d) * d) * env(len(d), 0.0005, 0.03, 5) * rng.uniform(0.2, 0.5)
        place(out, reverb(np.pad(drip, (0, SR // 2)), 0.6), at)
    f = int(SR * 0.4); out[:f] *= np.linspace(0, 1, f); out[-f:] *= np.linspace(1, 0, f)
    return stereoize(out, np.roll(out, 600))
def sfx_riser():
    x = t(4); n = hp(rng.standard_normal(len(x)), 300); sweep = np.sin(2 * np.pi * np.cumsum(200 + 1800 * (x / 4) ** 2) / SR)
    return (bp(n, 500, 9000) * 0.5 + sweep * 0.4) * (x / 4) ** 2
def sfx_impact():
    x = t(2.5); boom = np.sin(2 * np.pi * np.cumsum(35 + 120 * np.exp(-x * 6)) / SR) * np.exp(-x * 1.5)
    return sat(boom * 1.5 + bp(rng.standard_normal(len(x)), 100, 4000) * np.exp(-x * 8) * 0.6, 2)
def sfx_cheer():
    x = t(3); n = sum(bp(rng.standard_normal(len(x)), 500 + i * 300, 1500 + i * 600) * (0.5 + 0.5 * np.sin(2 * np.pi * (3 + i) * x + i)) for i in range(5))
    return n * np.sin(np.pi * x / 3) ** 0.7
def sfx_pipe_clank():
    return reverb(np.pad(metal_hit(), (0, SR)), 0.7, 0.7)
def sfx_door_whoosh():
    x = t(0.7); return bp(rng.standard_normal(len(x)), 300, 2500) * np.sin(np.pi * x / 0.7) ** 2
def save_st(name, st):
    st = st / (np.max(np.abs(st)) or 1) * 0.89; wavfile.write(os.path.join(OUT, name + ".wav"), SR, (st * 32767).astype(np.int16)); print(name, round(len(st) / SR, 2), "s")
def make_v3():
    save_st("Track_Techno_Warehouse", track_techno()); save_st("Track_Acid_Squelch", track_acid())
    save_st("Track_Industrial_RedRoom", track_industrial()); save_st("Track_House_Velvet", track_house())
    save_st("Track_Ambient_Chill", track_ambient()); save_st("Track_Festival_BigRoom", track_festival())
    save_st("Music_Bathroom_Loop", bathroom_ambience())
    save("SFX_Riser", sfx_riser()); save("SFX_Impact", sfx_impact()); save("SFX_CrowdCheer", sfx_cheer())
    save("SFX_PipeClank", sfx_pipe_clank()); save("SFX_DoorWhoosh", sfx_door_whoosh())
if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    save("Music_Techno128_Loop", techno(), stereo=True)
    save("Loop_Kick128", loop(kick)); save("Loop_Hats128", loop(lambda: hat()))
    save("SFX_LaserZap", zap())
    for i in range(4): save(f"SFX_Footstep_{i+1}", footstep(i))
    save("SFX_DoorCreak", creak()); save("SFX_PillPickup", pickup())
    save("UI_Click", click()); save("UI_Hover", hover())
    pass  # ambience removed
    save("SFX_ToiletFlush", flush()); save("SFX_BassDrop", bassdrop())
    # Room tracks (added after the originals so their output stays identical)
    tech = techno()
    save("Music_Muffled_Loop", muffled(tech), stereo=True)
    save("Music_Chill_Loop", chill(), stereo=True)
    save("Music_VIP_House_Loop", house(), stereo=True)
    pass  # ambience removed
    make_v3()
