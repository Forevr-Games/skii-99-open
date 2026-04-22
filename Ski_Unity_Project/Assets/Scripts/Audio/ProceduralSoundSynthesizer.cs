using System;
using UnityEngine;

namespace ForevrTools.Audio
{
    /// <summary>
    /// Core audio synthesis engine that generates samples based on ProceduralSoundParameters.
    /// Thread-safe and allocation-free for use in OnAudioFilterRead.
    /// </summary>
    public class ProceduralSoundSynthesizer
    {
        // Parameters
        private ProceduralSoundParameters parameters;
        private int sampleRate;

        // State tracking
        private float phase;                    // Current waveform phase [0, 1)
        private float time;                     // Current time in seconds
        private float currentFrequency;         // Frequency with slide applied
        private float currentFrequencySlide;    // Current slide rate with delta applied
        private bool isFinished;
        private bool looping;                   // Whether the sound should loop

        // Constants
        private const float TWO_PI = Mathf.PI * 2f;

        // Random number generator for noise
        private System.Random random;

        /// <summary>
        /// Initialize the synthesizer with parameters and sample rate
        /// </summary>
        public void Initialize(ProceduralSoundParameters parameters, int sampleRate, bool loop = false)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.sampleRate = sampleRate;
            this.looping = loop;

            Reset();
        }

        /// <summary>
        /// Set whether the sound should loop
        /// </summary>
        public void SetLooping(bool loop)
        {
            looping = loop;
        }

        /// <summary>
        /// Reset synthesis state to beginning
        /// </summary>
        public void Reset()
        {
            phase = 0f;
            time = 0f;
            currentFrequency = parameters.baseFrequency;
            currentFrequencySlide = parameters.frequencySlide;
            isFinished = false;
            random = new System.Random(DateTime.Now.Millisecond);
        }

        /// <summary>
        /// Check if the sound has finished playing
        /// </summary>
        public bool IsFinished()
        {
            return isFinished;
        }

        /// <summary>
        /// Generate the next audio sample
        /// </summary>
        public float GenerateSample()
        {
            if (isFinished)
                return 0f;

            // Check if we've exceeded the duration
            if (time >= parameters.duration)
            {
                if (looping)
                {
                    // Loop: reset to beginning
                    time = 0f;
                    phase = 0f;
                    currentFrequency = parameters.baseFrequency;
                    currentFrequencySlide = parameters.frequencySlide;
                }
                else
                {
                    isFinished = true;
                    return 0f;
                }
            }

            // Generate base waveform sample
            float sample = GenerateWaveform(parameters.waveform, phase);

            // Apply vibrato (frequency modulation)
            float frequency = ApplyVibrato(currentFrequency, time);

            // Apply ADSR envelope
            sample = ApplyEnvelope(sample, time);

            // Apply tremolo (amplitude modulation)
            sample = ApplyTremolo(sample, time);

            // Apply bit crushing
            sample = ApplyBitCrush(sample);

            // Apply master volume
            sample *= parameters.volume;

            // Clamp output to valid range
            sample = Mathf.Clamp(sample, -1f, 1f);

            // Advance phase based on current frequency
            float phaseIncrement = frequency / sampleRate;
            phase += phaseIncrement;

            // Wrap phase to [0, 1)
            if (phase >= 1f)
                phase -= 1f;

            // Apply frequency slide
            currentFrequency += currentFrequencySlide / sampleRate;
            currentFrequency = Mathf.Max(0f, currentFrequency); // Prevent negative frequency

            // Apply frequency delta slide (acceleration)
            currentFrequencySlide += parameters.frequencyDeltaSlide / sampleRate;

            // Advance time
            time += 1f / sampleRate;

            return sample;
        }

        /// <summary>
        /// Generate waveform sample for given phase [0, 1)
        /// </summary>
        private float GenerateWaveform(WaveformType type, float phase)
        {
            switch (type)
            {
                case WaveformType.Sine:
                    return Mathf.Sin(phase * TWO_PI);

                case WaveformType.Square:
                    return phase < 0.5f ? 1f : -1f;

                case WaveformType.Sawtooth:
                    return 2f * phase - 1f;

                case WaveformType.Triangle:
                    return 4f * Mathf.Abs(phase - 0.5f) - 1f;

                case WaveformType.Noise:
                    // White noise
                    return (float)(random.NextDouble() * 2.0 - 1.0);

                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Apply ADSR envelope to the sample
        /// </summary>
        private float ApplyEnvelope(float sample, float time)
        {
            float envelope = 0f;
            float totalEnvelopeTime = parameters.attackTime + parameters.decayTime;
            float releaseStartTime = parameters.duration - parameters.releaseTime;

            if (time < parameters.attackTime)
            {
                // Attack phase - fade in
                if (parameters.attackTime > 0f)
                    envelope = time / parameters.attackTime;
                else
                    envelope = 1f;
            }
            else if (time < totalEnvelopeTime)
            {
                // Decay phase - drop to sustain level
                if (parameters.decayTime > 0f)
                {
                    float decayProgress = (time - parameters.attackTime) / parameters.decayTime;
                    envelope = Mathf.Lerp(1f, parameters.sustainLevel, decayProgress);
                }
                else
                {
                    envelope = parameters.sustainLevel;
                }
            }
            else if (time < releaseStartTime)
            {
                // Sustain phase - hold at sustain level
                envelope = parameters.sustainLevel;
            }
            else
            {
                // Release phase - fade out
                if (parameters.releaseTime > 0f)
                {
                    float releaseProgress = (time - releaseStartTime) / parameters.releaseTime;
                    envelope = Mathf.Lerp(parameters.sustainLevel, 0f, releaseProgress);
                }
                else
                {
                    envelope = 0f;
                }
            }

            return sample * envelope;
        }

        /// <summary>
        /// Apply vibrato (pitch modulation) to frequency
        /// </summary>
        private float ApplyVibrato(float frequency, float time)
        {
            if (parameters.vibratoDepth <= 0f || parameters.vibratoRate <= 0f)
                return frequency;

            // Don't apply vibrato during delay period
            if (time < parameters.vibratoDelay)
                return frequency;

            float vibratoTime = time - parameters.vibratoDelay;
            float vibratoModulation = Mathf.Sin(vibratoTime * parameters.vibratoRate * TWO_PI);
            float frequencyModulation = vibratoModulation * parameters.vibratoDepth * frequency;

            return frequency + frequencyModulation;
        }

        /// <summary>
        /// Apply tremolo (amplitude modulation) to sample
        /// </summary>
        private float ApplyTremolo(float sample, float time)
        {
            if (parameters.tremoloDepth <= 0f || parameters.tremoloRate <= 0f)
                return sample;

            float tremoloModulation = Mathf.Sin(time * parameters.tremoloRate * TWO_PI);
            // Map [-1, 1] to [1 - depth, 1]
            float amplitudeModulation = 1f - parameters.tremoloDepth * (1f - tremoloModulation) * 0.5f;

            return sample * amplitudeModulation;
        }

        /// <summary>
        /// Apply bit crushing effect to sample
        /// </summary>
        private float ApplyBitCrush(float sample)
        {
            if (parameters.bitCrush <= 0f)
                return sample;

            // Reduce bit depth
            float levels = Mathf.Pow(2f, 16f - parameters.bitCrush);
            float crushedSample = Mathf.Round(sample * levels) / levels;

            return crushedSample;
        }
    }
}
