using UnityEngine;
#if UNITY_EDITOR
using System.IO;
#endif

namespace ForevrTools.Audio
{
    /// <summary>
    /// Utility functions for procedural sound generation, including AudioClip export
    /// </summary>
    public static class ProceduralSoundUtility
    {
        /// <summary>
        /// Generate an AudioClip from ProceduralSoundParameters
        /// </summary>
        public static AudioClip GenerateAudioClip(ProceduralSoundParameters parameters, string clipName = "ProceduralSound")
        {
            if (parameters == null)
            {
                Debug.LogError("ProceduralSoundUtility: Cannot generate AudioClip from null parameters");
                return null;
            }

            int sampleRate = AudioSettings.outputSampleRate;
            int sampleCount = Mathf.CeilToInt(parameters.duration * sampleRate);

            // Create AudioClip
            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);

            // Generate samples
            float[] samples = new float[sampleCount];
            ProceduralSoundSynthesizer synthesizer = new ProceduralSoundSynthesizer();
            synthesizer.Initialize(parameters, sampleRate);

            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = synthesizer.GenerateSample();

                if (synthesizer.IsFinished())
                {
                    // Fill remaining with silence
                    for (int j = i + 1; j < sampleCount; j++)
                    {
                        samples[j] = 0f;
                    }
                    break;
                }
            }

            // Set data to clip
            clip.SetData(samples, 0);

            return clip;
        }

        /// <summary>
        /// Generate an AudioClip from ProceduralSoundParameters WITHOUT applying volume
        /// (useful for WebGL where volume is applied to AudioSource instead)
        /// </summary>
        public static AudioClip GenerateAudioClipWithoutVolume(ProceduralSoundParameters parameters, string clipName = "ProceduralSound")
        {
            if (parameters == null)
            {
                Debug.LogError("ProceduralSoundUtility: Cannot generate AudioClip from null parameters");
                return null;
            }

            // Clone parameters and set volume to 1.0 so it's not baked into samples
            ProceduralSoundParameters paramsWithoutVolume = parameters.Clone();
            paramsWithoutVolume.volume = 1f;

            int sampleRate = AudioSettings.outputSampleRate;
            int sampleCount = Mathf.CeilToInt(paramsWithoutVolume.duration * sampleRate);

            // Create AudioClip
            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);

            // Generate samples
            float[] samples = new float[sampleCount];
            ProceduralSoundSynthesizer synthesizer = new ProceduralSoundSynthesizer();
            synthesizer.Initialize(paramsWithoutVolume, sampleRate);

            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = synthesizer.GenerateSample();

                if (synthesizer.IsFinished())
                {
                    // Fill remaining with silence
                    for (int j = i + 1; j < sampleCount; j++)
                    {
                        samples[j] = 0f;
                    }
                    break;
                }
            }

            // Set data to clip
            clip.SetData(samples, 0);

            return clip;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Save an AudioClip as a WAV file asset (Editor only)
        /// </summary>
        public static void SaveAudioClipAsAsset(AudioClip clip, string path)
        {
            if (clip == null)
            {
                Debug.LogError("ProceduralSoundUtility: Cannot save null AudioClip");
                return;
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Get samples from clip
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Convert to 16-bit PCM and write WAV file
            WriteWavFile(path, samples, clip.frequency, clip.channels);

            // Refresh asset database
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"Saved AudioClip to {path}");
        }

        /// <summary>
        /// Write a WAV file from sample data
        /// </summary>
        private static void WriteWavFile(string filepath, float[] samples, int frequency, int channels)
        {
            using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                int sampleCount = samples.Length;
                int byteRate = frequency * channels * 2; // 16-bit = 2 bytes per sample
                int blockAlign = channels * 2;

                // WAV header
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + sampleCount * 2); // File size - 8
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

                // Format chunk
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16); // Subchunk size
                writer.Write((ushort)1); // Audio format (PCM)
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(byteRate);
                writer.Write((ushort)blockAlign);
                writer.Write((ushort)16); // Bits per sample

                // Data chunk
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(sampleCount * 2);

                // Write samples as 16-bit PCM
                foreach (float sample in samples)
                {
                    short value = (short)(sample * 32767f);
                    writer.Write(value);
                }
            }
        }
#endif

        /// <summary>
        /// Create a quick test sound for debugging
        /// </summary>
        public static ProceduralSoundParameters CreateTestSound()
        {
            ProceduralSoundParameters parameters = new ProceduralSoundParameters();
            parameters.waveform = WaveformType.Square;
            parameters.baseFrequency = 440f;
            parameters.duration = 0.3f;
            parameters.attackTime = 0.01f;
            parameters.decayTime = 0.1f;
            parameters.sustainLevel = 0.5f;
            parameters.releaseTime = 0.2f;
            parameters.volume = 0.5f;
            return parameters;
        }

        /// <summary>
        /// Validate parameters and log warnings for potential issues
        /// </summary>
        public static bool ValidateParameters(ProceduralSoundParameters parameters)
        {
            if (parameters == null)
            {
                Debug.LogError("ProceduralSoundUtility: Parameters are null");
                return false;
            }

            bool isValid = true;

            if (parameters.duration <= 0f)
            {
                Debug.LogWarning("ProceduralSoundUtility: Duration is zero or negative");
                isValid = false;
            }

            if (parameters.baseFrequency < 20f || parameters.baseFrequency > 20000f)
            {
                Debug.LogWarning($"ProceduralSoundUtility: Base frequency ({parameters.baseFrequency} Hz) is outside audible range");
            }

            float totalEnvelopeTime = parameters.attackTime + parameters.decayTime + parameters.releaseTime;
            if (totalEnvelopeTime > parameters.duration)
            {
                Debug.LogWarning($"ProceduralSoundUtility: Envelope time ({totalEnvelopeTime}s) exceeds duration ({parameters.duration}s)");
            }

            if (parameters.volume <= 0f)
            {
                Debug.LogWarning("ProceduralSoundUtility: Volume is zero or negative - sound will be silent");
            }

            return isValid;
        }

        /// <summary>
        /// Calculate the approximate file size if exported as WAV
        /// </summary>
        public static int CalculateWavFileSize(ProceduralSoundParameters parameters, int sampleRate = 44100)
        {
            if (parameters == null)
                return 0;

            int sampleCount = Mathf.CeilToInt(parameters.duration * sampleRate);
            int dataSize = sampleCount * 2; // 16-bit = 2 bytes per sample
            int totalSize = 44 + dataSize; // WAV header is 44 bytes

            return totalSize;
        }

        /// <summary>
        /// Get a human-readable description of the wave type
        /// </summary>
        public static string GetWaveformDescription(WaveformType waveform)
        {
            switch (waveform)
            {
                case WaveformType.Sine:
                    return "Pure tone, smooth and mellow";
                case WaveformType.Square:
                    return "Retro, harsh and electronic";
                case WaveformType.Sawtooth:
                    return "Buzzy, bright and edgy";
                case WaveformType.Triangle:
                    return "Soft square, gentle";
                case WaveformType.Noise:
                    return "White noise, chaotic";
                default:
                    return "Unknown waveform";
            }
        }
    }
}
