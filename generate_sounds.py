import wave
import struct
import math

def generate_chime(filename, start_freq, end_freq):
    sample_rate = 44100
    duration = 0.2
    n_samples = int(sample_rate * duration)
    
    with wave.open(filename, 'w') as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(sample_rate)
        
        phase = 0.0
        for i in range(n_samples):
            t = float(i) / sample_rate
            
            # Frequency ramp for the first 0.08s
            if t < 0.08:
                # Exponential ramp: f(t) = f0 * (f1/f0)^(t/0.08)
                freq = start_freq * (end_freq / start_freq) ** (t / 0.08)
            else:
                freq = end_freq
            
            # Update phase
            phase += 2 * math.pi * freq / sample_rate
            
            # Gain envelope
            if t < 0.01:
                # Linear ramp from 0 to 0.3
                gain = (t / 0.01) * 0.3
            else:
                # Exponential decay from 0.3 to 0.001 over 0.14s
                decay_t = t - 0.01
                # g(t) = g0 * (g1/g0)^(t/T)
                # 0.001 = 0.3 * (0.001/0.3)^(0.14/0.14)
                gain = 0.3 * (0.001 / 0.3) ** (decay_t / 0.14)
            
            # Generate sample
            sample = int(math.sin(phase) * gain * 32767.0)
            f.writeframes(struct.pack('<h', sample))

if __name__ == '__main__':
    # ON: 523.25 Hz (C5) -> 783.99 Hz (G5)
    generate_chime('mic_start_new.wav', 523.25, 783.99)
    # OFF: 783.99 Hz (G5) -> 523.25 Hz (C5)
    generate_chime('mic_stop_new.wav', 783.99, 523.25)
    print("Generated mic_start_new.wav and mic_stop_new.wav")
