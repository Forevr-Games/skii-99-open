using UnityEngine;

namespace ForevrTools.Audio
{
    /// <summary>
    /// Unity MonoBehaviour that plays procedurally generated sounds using OnAudioFilterRead.
    /// Manages the lifecycle of ProceduralSoundSynthesizer and provides thread-safe playback.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralSoundGenerator : MonoBehaviour
    {
        private AudioSource audioSource;
        private ProceduralSoundSynthesizer synthesizer;
        private ProceduralSoundParameters currentParameters;
        private bool isPlaying;
        private bool isLooping;
        private readonly object lockObject = new object();

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Configure AudioSource for procedural generation
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // 2D sound by default

            synthesizer = new ProceduralSoundSynthesizer();
        }

        /// <summary>
        /// Play a sound with the given parameters
        /// </summary>
        public void Play(ProceduralSoundParameters parameters)
        {
            if (parameters == null)
            {
                Debug.LogError("ProceduralSoundGenerator: Cannot play with null parameters");
                return;
            }

            // Ensure components are initialized (handles Editor usage)
            EnsureInitialized();

            lock (lockObject)
            {
                // Clone parameters to avoid external modifications during playback
                currentParameters = parameters.Clone();
                currentParameters.Clamp();

                // Initialize synthesizer (non-looping)
                synthesizer.Initialize(currentParameters, AudioSettings.outputSampleRate, false);

                // Start playback
                isPlaying = true;
                isLooping = false;

                // Enable the AudioSource
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
        }

        /// <summary>
        /// Play a looping sound with given parameters. Returns this generator so it can be stopped later.
        /// </summary>
        public ProceduralSoundGenerator PlayLooping(ProceduralSoundParameters parameters)
        {
            if (parameters == null)
            {
                Debug.LogError("ProceduralSoundGenerator: Cannot play with null parameters");
                return null;
            }

            // Ensure components are initialized (handles Editor usage)
            EnsureInitialized();

            lock (lockObject)
            {
                // Clone parameters to avoid external modifications during playback
                currentParameters = parameters.Clone();
                currentParameters.Clamp();

                // Initialize synthesizer (looping)
                synthesizer.Initialize(currentParameters, AudioSettings.outputSampleRate, true);

                // Start playback
                isPlaying = true;
                isLooping = true;

                // Enable the AudioSource
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }

            return this;
        }

        /// <summary>
        /// Ensure the generator is initialized (for Editor usage)
        /// </summary>
        private void EnsureInitialized()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }

                // Configure AudioSource for procedural generation
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f; // 2D sound by default
            }

            if (synthesizer == null)
            {
                synthesizer = new ProceduralSoundSynthesizer();
            }
        }

        /// <summary>
        /// Stop the current sound
        /// </summary>
        public void Stop()
        {
            EnsureInitialized();

            lock (lockObject)
            {
                isPlaying = false;
                isLooping = false;
                if (audioSource != null)
                {
                    audioSource.Stop();
#if UNITY_WEBGL && !UNITY_EDITOR
                    // WebGL: Clear the clip reference
                    audioSource.clip = null;
#endif
                }
            }
        }

        /// <summary>
        /// Check if currently playing a sound
        /// </summary>
        public bool IsPlaying()
        {
            EnsureInitialized();

            lock (lockObject)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // WebGL: Check both our flag and AudioSource state
                // Update our flag if AudioSource has stopped
                if (isPlaying && audioSource != null && !audioSource.isPlaying && !isLooping)
                {
                    isPlaying = false;
                }
#endif
                return isPlaying;
            }
        }

        /// <summary>
        /// Check if currently playing a looping sound
        /// </summary>
        public bool IsLooping()
        {
            lock (lockObject)
            {
                return isPlaying && isLooping;
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>
        /// WebGL: Play a pre-generated AudioClip (non-looping)
        /// Used instead of OnAudioFilterRead which doesn't work in WebGL
        /// </summary>
        /// <param name="clip">The AudioClip to play</param>
        /// <param name="volume">Volume to play at (0-1), defaults to 1.0</param>
        public void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                Debug.LogError("ProceduralSoundGenerator: Cannot play null AudioClip");
                return;
            }

            EnsureInitialized();

            lock (lockObject)
            {
                // Stop any current playback
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                // Configure for one-shot playback
                audioSource.clip = clip;
                audioSource.loop = false;
                audioSource.volume = Mathf.Clamp01(volume); // Apply volume from preset
                audioSource.Play();

                isPlaying = true;
                isLooping = false;
            }
        }

        /// <summary>
        /// WebGL: Play a pre-generated AudioClip with looping enabled
        /// Returns this generator so it can be stopped later
        /// </summary>
        /// <param name="clip">The AudioClip to play</param>
        /// <param name="volume">Volume to play at (0-1), defaults to 1.0</param>
        public ProceduralSoundGenerator PlayClipLooping(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                Debug.LogError("ProceduralSoundGenerator: Cannot play null AudioClip");
                return null;
            }

            EnsureInitialized();

            lock (lockObject)
            {
                // Stop any current playback
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                // Configure for looping playback
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.volume = Mathf.Clamp01(volume); // Apply volume from preset
                audioSource.Play();

                isPlaying = true;
                isLooping = true;
            }

            return this;
        }
#endif

        /// <summary>
        /// Unity audio callback - generates audio samples in real-time
        /// Called on the audio thread, so must be thread-safe
        /// NOTE: OnAudioFilterRead is not supported in WebGL builds
        /// </summary>
#if !UNITY_WEBGL || UNITY_EDITOR
        private void OnAudioFilterRead(float[] data, int channels)
        {
            lock (lockObject)
            {
                if (!isPlaying || synthesizer == null || currentParameters == null)
                {
                    // Fill with silence
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = 0f;
                    }
                    return;
                }

                int samplesNeeded = data.Length / channels;

                for (int i = 0; i < samplesNeeded; i++)
                {
                    // Generate one sample
                    float sample = synthesizer.GenerateSample();

                    // Write to all channels (stereo/mono)
                    for (int channel = 0; channel < channels; channel++)
                    {
                        data[i * channels + channel] = sample;
                    }

                    // Check if synthesis is complete
                    if (synthesizer.IsFinished())
                    {
                        isPlaying = false;

                        // Fill remaining buffer with silence
                        for (int j = (i + 1) * channels; j < data.Length; j++)
                        {
                            data[j] = 0f;
                        }

                        break;
                    }
                }
            }
        }
#endif

        private void OnDestroy()
        {
            Stop();
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
