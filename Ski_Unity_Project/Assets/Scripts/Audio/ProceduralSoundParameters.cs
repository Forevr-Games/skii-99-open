using System;
using UnityEngine;

namespace ForevrTools.Audio
{
    /// <summary>
    /// Waveform types for sound synthesis
    /// </summary>
    public enum WaveformType
    {
        Sine,       // Pure tone, smooth
        Square,     // Retro, harsh
        Sawtooth,   // Buzzy, bright
        Triangle,   // Soft square
        Noise       // White noise, explosions
    }

    /// <summary>
    /// Sound categories for preset randomization
    /// </summary>
    public enum SoundCategory
    {
        Jump,
        Explosion,
        Pickup,
        Hit,
        Laser,
        Powerup,
        UIClick,
        Landing,
        Swoosh,
        Wind
    }

    /// <summary>
    /// Complete parameter set for procedural sound synthesis.
    /// Serializable for use in Inspector and preset system.
    /// </summary>
    [Serializable]
    public class ProceduralSoundParameters
    {
        [Header("Waveform")]
        [Tooltip("Type of waveform to generate")]
        public WaveformType waveform = WaveformType.Square;

        [Header("Frequency")]
        [Tooltip("Starting frequency in Hz")]
        [Range(20f, 2000f)]
        public float baseFrequency = 440f;

        [Tooltip("Frequency change over time in Hz/sec (positive = rising pitch, negative = falling pitch)")]
        [Range(-2000f, 2000f)]
        public float frequencySlide = 0f;

        [Tooltip("Rate of frequency change acceleration")]
        [Range(-1f, 1f)]
        public float frequencyDeltaSlide = 0f;

        [Header("ADSR Envelope")]
        [Tooltip("Attack time - fade in duration in seconds")]
        [Range(0f, 1f)]
        public float attackTime = 0.01f;

        [Tooltip("Decay time - time to drop from peak to sustain level")]
        [Range(0f, 1f)]
        public float decayTime = 0.1f;

        [Tooltip("Sustain level - volume during sustain phase (0-1)")]
        [Range(0f, 1f)]
        public float sustainLevel = 0.5f;

        [Tooltip("Release time - fade out duration in seconds")]
        [Range(0f, 1f)]
        public float releaseTime = 0.2f;

        [Tooltip("Total sound duration in seconds")]
        [Range(0.05f, 3f)]
        public float duration = 0.3f;

        [Header("Vibrato (Pitch Modulation)")]
        [Tooltip("Vibrato rate in Hz")]
        [Range(0f, 20f)]
        public float vibratoRate = 0f;

        [Tooltip("Vibrato depth as percentage of frequency")]
        [Range(0f, 0.5f)]
        public float vibratoDepth = 0f;

        [Tooltip("Delay before vibrato starts in seconds")]
        [Range(0f, 1f)]
        public float vibratoDelay = 0f;

        [Header("Tremolo (Amplitude Modulation)")]
        [Tooltip("Tremolo rate in Hz")]
        [Range(0f, 20f)]
        public float tremoloRate = 0f;

        [Tooltip("Tremolo depth (0 = none, 1 = full amplitude modulation)")]
        [Range(0f, 1f)]
        public float tremoloDepth = 0f;

        [Header("Bit Crushing")]
        [Tooltip("Bit depth reduction for lo-fi effect (0 = none, higher = more crushed)")]
        [Range(0f, 16f)]
        public float bitCrush = 0f;

        [Header("Volume")]
        [Tooltip("Master volume (0-1)")]
        [Range(0f, 1f)]
        public float volume = 0.5f;

        /// <summary>
        /// Creates a deep copy of these parameters
        /// </summary>
        public ProceduralSoundParameters Clone()
        {
            return new ProceduralSoundParameters
            {
                waveform = this.waveform,
                baseFrequency = this.baseFrequency,
                frequencySlide = this.frequencySlide,
                frequencyDeltaSlide = this.frequencyDeltaSlide,
                attackTime = this.attackTime,
                decayTime = this.decayTime,
                sustainLevel = this.sustainLevel,
                releaseTime = this.releaseTime,
                duration = this.duration,
                vibratoRate = this.vibratoRate,
                vibratoDepth = this.vibratoDepth,
                vibratoDelay = this.vibratoDelay,
                tremoloRate = this.tremoloRate,
                tremoloDepth = this.tremoloDepth,
                bitCrush = this.bitCrush,
                volume = this.volume
            };
        }

        /// <summary>
        /// Clamps all parameters to valid ranges
        /// </summary>
        public void Clamp()
        {
            baseFrequency = Mathf.Clamp(baseFrequency, 20f, 2000f);
            frequencySlide = Mathf.Clamp(frequencySlide, -2000f, 2000f);
            frequencyDeltaSlide = Mathf.Clamp(frequencyDeltaSlide, -1f, 1f);
            attackTime = Mathf.Clamp(attackTime, 0f, 1f);
            decayTime = Mathf.Clamp(decayTime, 0f, 1f);
            sustainLevel = Mathf.Clamp01(sustainLevel);
            releaseTime = Mathf.Clamp(releaseTime, 0f, 1f);
            duration = Mathf.Clamp(duration, 0.05f, 3f);
            vibratoRate = Mathf.Clamp(vibratoRate, 0f, 20f);
            vibratoDepth = Mathf.Clamp(vibratoDepth, 0f, 0.5f);
            vibratoDelay = Mathf.Clamp(vibratoDelay, 0f, 1f);
            tremoloRate = Mathf.Clamp(tremoloRate, 0f, 20f);
            tremoloDepth = Mathf.Clamp01(tremoloDepth);
            bitCrush = Mathf.Clamp(bitCrush, 0f, 16f);
            volume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Sets default parameters for a basic beep sound
        /// </summary>
        public void SetDefaults()
        {
            waveform = WaveformType.Square;
            baseFrequency = 440f;
            frequencySlide = 0f;
            frequencyDeltaSlide = 0f;
            attackTime = 0.01f;
            decayTime = 0.1f;
            sustainLevel = 0.5f;
            releaseTime = 0.2f;
            duration = 0.3f;
            vibratoRate = 0f;
            vibratoDepth = 0f;
            vibratoDelay = 0f;
            tremoloRate = 0f;
            tremoloDepth = 0f;
            bitCrush = 0f;
            volume = 0.5f;
        }

        /// <summary>
        /// Randomize parameters based on sound category
        /// </summary>
        public void Randomize(SoundCategory category, int seed = -1)
        {
            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            if (seed >= 0)
                UnityEngine.Random.InitState(seed);

            switch (category)
            {
                case SoundCategory.Jump:
                    RandomizeJump();
                    break;
                case SoundCategory.Explosion:
                    RandomizeExplosion();
                    break;
                case SoundCategory.Pickup:
                    RandomizePickup();
                    break;
                case SoundCategory.Hit:
                    RandomizeHit();
                    break;
                case SoundCategory.Laser:
                    RandomizeLaser();
                    break;
                case SoundCategory.Powerup:
                    RandomizePowerup();
                    break;
                case SoundCategory.UIClick:
                    RandomizeUIClick();
                    break;
                case SoundCategory.Landing:
                    RandomizeLanding();
                    break;
                case SoundCategory.Swoosh:
                    RandomizeSwoosh();
                    break;
                case SoundCategory.Wind:
                    RandomizeWind();
                    break;
            }

            if (seed >= 0)
                UnityEngine.Random.state = oldState;

            Clamp();
        }

        /// <summary>
        /// Mutate the current parameters by a random amount
        /// </summary>
        public void Mutate(float mutationAmount = 0.2f)
        {
            baseFrequency *= UnityEngine.Random.Range(1f - mutationAmount, 1f + mutationAmount);
            frequencySlide += UnityEngine.Random.Range(-mutationAmount * 500f, mutationAmount * 500f);
            attackTime *= UnityEngine.Random.Range(1f - mutationAmount, 1f + mutationAmount);
            decayTime *= UnityEngine.Random.Range(1f - mutationAmount, 1f + mutationAmount);
            sustainLevel *= UnityEngine.Random.Range(1f - mutationAmount, 1f + mutationAmount);
            releaseTime *= UnityEngine.Random.Range(1f - mutationAmount, 1f + mutationAmount);
            duration *= UnityEngine.Random.Range(1f - mutationAmount, 1f + mutationAmount);

            if (UnityEngine.Random.value < 0.3f)
                vibratoRate *= UnityEngine.Random.Range(0.5f, 1.5f);
            if (UnityEngine.Random.value < 0.3f)
                tremoloRate *= UnityEngine.Random.Range(0.5f, 1.5f);

            Clamp();
        }

        // Category-specific randomization methods
        private void RandomizeJump()
        {
            waveform = UnityEngine.Random.value > 0.5f ? WaveformType.Square : WaveformType.Triangle;
            baseFrequency = UnityEngine.Random.Range(200f, 600f);
            frequencySlide = UnityEngine.Random.Range(300f, 800f); // Rising pitch
            attackTime = UnityEngine.Random.Range(0.01f, 0.05f);
            decayTime = UnityEngine.Random.Range(0.05f, 0.15f);
            sustainLevel = UnityEngine.Random.Range(0.3f, 0.7f);
            releaseTime = UnityEngine.Random.Range(0.1f, 0.2f);
            duration = UnityEngine.Random.Range(0.15f, 0.3f);
            volume = UnityEngine.Random.Range(0.4f, 0.6f);
        }

        private void RandomizeExplosion()
        {
            waveform = WaveformType.Noise;
            baseFrequency = UnityEngine.Random.Range(100f, 400f);
            frequencySlide = UnityEngine.Random.Range(-500f, -200f); // Falling pitch
            attackTime = UnityEngine.Random.Range(0.001f, 0.01f);
            decayTime = UnityEngine.Random.Range(0.2f, 0.5f);
            sustainLevel = UnityEngine.Random.Range(0.1f, 0.3f);
            releaseTime = UnityEngine.Random.Range(0.3f, 0.6f);
            duration = UnityEngine.Random.Range(0.4f, 0.8f);
            bitCrush = UnityEngine.Random.Range(2f, 8f);
            volume = UnityEngine.Random.Range(0.5f, 0.7f);
        }

        private void RandomizePickup()
        {
            waveform = UnityEngine.Random.value > 0.5f ? WaveformType.Sine : WaveformType.Triangle;
            baseFrequency = UnityEngine.Random.Range(600f, 1200f);
            frequencySlide = UnityEngine.Random.Range(200f, 600f);
            attackTime = UnityEngine.Random.Range(0.001f, 0.02f);
            decayTime = UnityEngine.Random.Range(0.05f, 0.1f);
            sustainLevel = UnityEngine.Random.Range(0.2f, 0.4f);
            releaseTime = UnityEngine.Random.Range(0.05f, 0.1f);
            duration = UnityEngine.Random.Range(0.1f, 0.2f);
            vibratoRate = UnityEngine.Random.Range(0f, 10f);
            vibratoDepth = UnityEngine.Random.Range(0f, 0.2f);
            volume = UnityEngine.Random.Range(0.3f, 0.5f);
        }

        private void RandomizeHit()
        {
            waveform = UnityEngine.Random.value > 0.7f ? WaveformType.Noise : WaveformType.Square;
            baseFrequency = UnityEngine.Random.Range(80f, 200f);
            frequencySlide = UnityEngine.Random.Range(-300f, -100f);
            attackTime = UnityEngine.Random.Range(0.001f, 0.01f);
            decayTime = UnityEngine.Random.Range(0.05f, 0.15f);
            sustainLevel = UnityEngine.Random.Range(0.1f, 0.3f);
            releaseTime = UnityEngine.Random.Range(0.1f, 0.2f);
            duration = UnityEngine.Random.Range(0.1f, 0.25f);
            bitCrush = UnityEngine.Random.Range(0f, 4f);
            volume = UnityEngine.Random.Range(0.5f, 0.7f);
        }

        private void RandomizeLaser()
        {
            waveform = UnityEngine.Random.value > 0.3f ? WaveformType.Sawtooth : WaveformType.Square;
            baseFrequency = UnityEngine.Random.Range(600f, 1500f);
            frequencySlide = UnityEngine.Random.Range(-1200f, -400f); // Falling pitch
            attackTime = UnityEngine.Random.Range(0.001f, 0.02f);
            decayTime = UnityEngine.Random.Range(0.1f, 0.2f);
            sustainLevel = UnityEngine.Random.Range(0.3f, 0.6f);
            releaseTime = UnityEngine.Random.Range(0.1f, 0.2f);
            duration = UnityEngine.Random.Range(0.2f, 0.4f);
            vibratoRate = UnityEngine.Random.Range(0f, 15f);
            vibratoDepth = UnityEngine.Random.Range(0f, 0.1f);
            volume = UnityEngine.Random.Range(0.4f, 0.6f);
        }

        private void RandomizePowerup()
        {
            waveform = UnityEngine.Random.value > 0.5f ? WaveformType.Sine : WaveformType.Triangle;
            baseFrequency = UnityEngine.Random.Range(400f, 800f);
            frequencySlide = UnityEngine.Random.Range(400f, 1000f); // Rising pitch
            attackTime = UnityEngine.Random.Range(0.01f, 0.05f);
            decayTime = UnityEngine.Random.Range(0.1f, 0.2f);
            sustainLevel = UnityEngine.Random.Range(0.4f, 0.7f);
            releaseTime = UnityEngine.Random.Range(0.2f, 0.4f);
            duration = UnityEngine.Random.Range(0.4f, 0.8f);
            vibratoRate = UnityEngine.Random.Range(5f, 15f);
            vibratoDepth = UnityEngine.Random.Range(0.05f, 0.2f);
            volume = UnityEngine.Random.Range(0.4f, 0.6f);
        }

        private void RandomizeUIClick()
        {
            waveform = UnityEngine.Random.value > 0.5f ? WaveformType.Square : WaveformType.Sine;
            baseFrequency = UnityEngine.Random.Range(800f, 1500f);
            frequencySlide = UnityEngine.Random.Range(-200f, 200f);
            attackTime = UnityEngine.Random.Range(0.001f, 0.005f);
            decayTime = UnityEngine.Random.Range(0.01f, 0.03f);
            sustainLevel = UnityEngine.Random.Range(0.1f, 0.3f);
            releaseTime = UnityEngine.Random.Range(0.02f, 0.05f);
            duration = UnityEngine.Random.Range(0.05f, 0.1f);
            volume = UnityEngine.Random.Range(0.3f, 0.5f);
        }

        private void RandomizeLanding()
        {
            waveform = UnityEngine.Random.value > 0.6f ? WaveformType.Noise : WaveformType.Square;
            baseFrequency = UnityEngine.Random.Range(60f, 150f);
            frequencySlide = UnityEngine.Random.Range(-200f, 0f);
            attackTime = UnityEngine.Random.Range(0.001f, 0.01f);
            decayTime = UnityEngine.Random.Range(0.1f, 0.3f);
            sustainLevel = UnityEngine.Random.Range(0.2f, 0.4f);
            releaseTime = UnityEngine.Random.Range(0.1f, 0.3f);
            duration = UnityEngine.Random.Range(0.2f, 0.4f);
            bitCrush = UnityEngine.Random.Range(0f, 6f);
            volume = UnityEngine.Random.Range(0.4f, 0.6f);
        }

        private void RandomizeSwoosh()
        {
            waveform = UnityEngine.Random.value > 0.5f ? WaveformType.Noise : WaveformType.Sawtooth;
            baseFrequency = UnityEngine.Random.Range(400f, 800f);
            frequencySlide = UnityEngine.Random.Range(-600f, 600f);
            attackTime = UnityEngine.Random.Range(0.01f, 0.05f);
            decayTime = UnityEngine.Random.Range(0.05f, 0.15f);
            sustainLevel = UnityEngine.Random.Range(0.3f, 0.6f);
            releaseTime = UnityEngine.Random.Range(0.1f, 0.2f);
            duration = UnityEngine.Random.Range(0.2f, 0.5f);
            tremoloRate = UnityEngine.Random.Range(5f, 15f);
            tremoloDepth = UnityEngine.Random.Range(0.2f, 0.5f);
            volume = UnityEngine.Random.Range(0.3f, 0.5f);
        }

        private void RandomizeWind()
        {
            waveform = WaveformType.Noise;
            baseFrequency = UnityEngine.Random.Range(100f, 300f);
            frequencySlide = UnityEngine.Random.Range(-100f, -20f); // Slight descending for howling effect
            frequencyDeltaSlide = 0f;

            // Slow fade in and out
            attackTime = UnityEngine.Random.Range(0.2f, 0.5f);
            decayTime = UnityEngine.Random.Range(0.05f, 0.15f);
            sustainLevel = UnityEngine.Random.Range(0.7f, 0.85f);
            releaseTime = UnityEngine.Random.Range(0.5f, 1.0f);
            duration = UnityEngine.Random.Range(2.5f, 3.0f);

            // Tremolo for gusts
            tremoloRate = UnityEngine.Random.Range(0.5f, 2.5f);
            tremoloDepth = UnityEngine.Random.Range(0.3f, 0.6f);

            // Vibrato for movement
            vibratoRate = UnityEngine.Random.Range(0.3f, 1.2f);
            vibratoDepth = UnityEngine.Random.Range(0.1f, 0.25f);
            vibratoDelay = 0f;

            // Optional bit crush for texture
            bitCrush = UnityEngine.Random.Range(0f, 2f);

            volume = UnityEngine.Random.Range(0.3f, 0.5f);
        }
    }
}
