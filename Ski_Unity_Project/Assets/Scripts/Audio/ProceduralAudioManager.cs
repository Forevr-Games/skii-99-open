using UnityEngine;
using System.Collections.Generic;

namespace ForevrTools.Audio
{
    /// <summary>
    /// Abstract audio manager for playing procedural sounds.
    /// Manages a pool of sound generators for efficient playback.
    /// </summary>
    public class ProceduralAudioManager : MonoBehaviour
    {
        [Header("Preset Database")]
        [Tooltip("Reference to your sound presets ScriptableObject")]
        [SerializeField] private ProceduralSoundPresets presetDatabase;

        [Header("Sound Generators Pool")]
        [Tooltip("Number of simultaneous sounds that can play")]
        [SerializeField] private int poolSize = 5;

        // Pool of sound generators
        private ProceduralSoundGenerator[] soundPool;
        private int nextPoolIndex = 0;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: Cache pre-generated AudioClips and their volumes for all presets
        private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        private Dictionary<string, float> volumeCache = new Dictionary<string, float>();
        private bool clipsPreGenerated = false;
#endif

        private void Awake()
        {
            // Create sound generator pool
            InitializeSoundPool();

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Pre-generate all preset clips at startup
            PreGenerateClipsForWebGL();
#endif
        }

        /// <summary>
        /// Initialize the pool of sound generators
        /// </summary>
        private void InitializeSoundPool()
        {
            soundPool = new ProceduralSoundGenerator[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                // Create child GameObject for each generator
                GameObject generatorObj = new GameObject($"SoundGenerator_{i}");
                generatorObj.transform.SetParent(transform);

                // Add and initialize generator
                soundPool[i] = generatorObj.AddComponent<ProceduralSoundGenerator>();
            }

            Debug.Log($"ProceduralAudioManager: Initialized {poolSize} sound generators");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>
        /// Pre-generate all preset sounds as AudioClips for WebGL compatibility.
        /// WebGL doesn't support OnAudioFilterRead, so we generate clips at startup.
        /// </summary>
        private void PreGenerateClipsForWebGL()
        {
            if (presetDatabase == null)
            {
                Debug.LogWarning("ProceduralAudioManager: No preset database assigned, skipping WebGL clip pre-generation");
                return;
            }

            if (presetDatabase.presets == null || presetDatabase.presets.Count == 0)
            {
                Debug.LogWarning("ProceduralAudioManager: Preset database is empty, no clips to pre-generate");
                return;
            }

            Debug.Log($"ProceduralAudioManager: Pre-generating {presetDatabase.presets.Count} clips for WebGL...");

            foreach (var preset in presetDatabase.presets)
            {
                if (preset?.parameters != null)
                {
                    // Generate clip WITHOUT baking volume (we'll apply it to AudioSource instead)
                    AudioClip clip = ProceduralSoundUtility.GenerateAudioClipWithoutVolume(preset.parameters, preset.name);
                    if (clip != null)
                    {
                        clipCache[preset.name] = clip;
                        volumeCache[preset.name] = preset.parameters.volume; // Store volume separately
                    }
                    else
                    {
                        Debug.LogWarning($"ProceduralAudioManager: Failed to generate clip for preset '{preset.name}'");
                    }
                }
            }

            clipsPreGenerated = true;
            Debug.Log($"ProceduralAudioManager: Successfully pre-generated {clipCache.Count} clips for WebGL");
        }
#endif

        /// <summary>
        /// Play a sound from a preset by name/ID
        /// </summary>
        public void PlayPreset(string presetName)
        {
            if (presetDatabase == null)
            {
                Debug.LogWarning("ProceduralAudioManager: No preset database assigned!");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Use pre-generated cached clip with stored volume
            if (clipsPreGenerated && clipCache.TryGetValue(presetName, out AudioClip clip))
            {
                ProceduralSoundGenerator generator = GetAvailableGenerator();
                if (generator != null)
                {
                    float volume = volumeCache.TryGetValue(presetName, out float vol) ? vol : 1f;
                    generator.PlayClip(clip, volume);
                }
            }
            else
            {
                Debug.LogWarning($"ProceduralAudioManager: Preset '{presetName}' not found in WebGL clip cache");
            }
#else
            // Other platforms: Use OnAudioFilterRead approach
            var parameters = presetDatabase.GetPreset(presetName);
            if (parameters != null)
            {
                Debug.Log($"ProceduralAudioManager: Playing preset '{presetName}'");
                PlaySound(parameters);
            }
            else
            {
                Debug.LogWarning($"ProceduralAudioManager: Preset '{presetName}' not found in database");
            }
#endif
        }

        /// <summary>
        /// Play a sound from a preset by index in the database
        /// </summary>
        public void PlayPresetByIndex(int index)
        {
            if (presetDatabase == null)
            {
                Debug.LogWarning("ProceduralAudioManager: No preset database assigned!");
                return;
            }

            if (index < 0 || index >= presetDatabase.presets.Count)
            {
                Debug.LogWarning($"ProceduralAudioManager: Preset index {index} out of range (0-{presetDatabase.presets.Count - 1})");
                return;
            }

            var preset = presetDatabase.presets[index];
            if (preset?.parameters != null)
            {
                PlaySound(preset.parameters);
            }
            else
            {
                Debug.LogWarning($"ProceduralAudioManager: Preset at index {index} has no parameters");
            }
        }

        /// <summary>
        /// Play a sound with given parameters directly
        /// </summary>
        public void PlaySound(ProceduralSoundParameters parameters)
        {
            if (soundPool == null || soundPool.Length == 0)
            {
                Debug.LogError("ProceduralAudioManager: Sound pool not initialized!");
                return;
            }

            if (parameters == null)
            {
                Debug.LogWarning("ProceduralAudioManager: Cannot play null sound parameters!");
                return;
            }

            // Get an available generator (prefers free generators, then non-looping ones)
            ProceduralSoundGenerator generator = GetAvailableGenerator();

            // Play the sound
            generator.Play(parameters);
        }

        /// <summary>
        /// Play a procedurally generated random sound from a category
        /// </summary>
        public void PlayRandomSound(SoundCategory category, int seed = -1)
        {
            ProceduralSoundParameters parameters = new ProceduralSoundParameters();
            parameters.Randomize(category, seed);
            PlaySound(parameters);
        }

        /// <summary>
        /// Play a looping sound from a preset by name/ID
        /// Returns the generator so you can call Stop() on it later
        /// </summary>
        public ProceduralSoundGenerator PlayPresetLooping(string presetName)
        {
            if (presetDatabase == null)
            {
                Debug.LogWarning("ProceduralAudioManager: No preset database assigned!");
                return null;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Use pre-generated cached clip with looping and stored volume
            if (clipsPreGenerated && clipCache.TryGetValue(presetName, out AudioClip clip))
            {
                ProceduralSoundGenerator generator = GetAvailableGenerator();
                if (generator != null)
                {
                    float volume = volumeCache.TryGetValue(presetName, out float vol) ? vol : 1f;
                    return generator.PlayClipLooping(clip, volume);
                }
            }
            else
            {
                Debug.LogWarning($"ProceduralAudioManager: Preset '{presetName}' not found in WebGL clip cache");
            }
            return null;
#else
            // Other platforms: Use OnAudioFilterRead approach
            var parameters = presetDatabase.GetPreset(presetName);
            if (parameters != null)
            {
                Debug.Log($"ProceduralAudioManager: Playing looping preset '{presetName}'");
                return PlaySoundLooping(parameters);
            }
            else
            {
                Debug.LogWarning($"ProceduralAudioManager: Preset '{presetName}' not found in database");
                return null;
            }
#endif
        }

        /// <summary>
        /// Play a looping sound from a preset by index in the database
        /// Returns the generator so you can call Stop() on it later
        /// </summary>
        public ProceduralSoundGenerator PlayPresetLoopingByIndex(int index)
        {
            if (presetDatabase == null)
            {
                Debug.LogWarning("ProceduralAudioManager: No preset database assigned!");
                return null;
            }

            if (index < 0 || index >= presetDatabase.presets.Count)
            {
                Debug.LogWarning($"ProceduralAudioManager: Preset index {index} out of range (0-{presetDatabase.presets.Count - 1})");
                return null;
            }

            var preset = presetDatabase.presets[index];
            if (preset?.parameters != null)
            {
                return PlaySoundLooping(preset.parameters);
            }
            else
            {
                Debug.LogWarning($"ProceduralAudioManager: Preset at index {index} has no parameters");
                return null;
            }
        }

        /// <summary>
        /// Play a looping sound with given parameters directly
        /// Returns the generator so you can call Stop() on it later
        /// </summary>
        public ProceduralSoundGenerator PlaySoundLooping(ProceduralSoundParameters parameters)
        {
            if (soundPool == null || soundPool.Length == 0)
            {
                Debug.LogError("ProceduralAudioManager: Sound pool not initialized!");
                return null;
            }

            if (parameters == null)
            {
                Debug.LogWarning("ProceduralAudioManager: Cannot play null sound parameters!");
                return null;
            }

            // Get an available generator (prefers free generators, then non-looping ones)
            ProceduralSoundGenerator generator = GetAvailableGenerator();

            // Play the looping sound and return the generator
            return generator.PlayLooping(parameters);
        }

        /// <summary>
        /// Get an available generator from the pool
        /// Priority: 1) Free generators, 2) Non-looping generators, 3) Any generator
        /// This prevents looping sounds from being interrupted by one-shot sounds
        /// </summary>
        private ProceduralSoundGenerator GetAvailableGenerator()
        {
            // First pass: look for completely free generators
            for (int i = 0; i < soundPool.Length; i++)
            {
                int index = (nextPoolIndex + i) % soundPool.Length;
                if (!soundPool[index].IsPlaying())
                {
                    nextPoolIndex = (index + 1) % soundPool.Length;
                    return soundPool[index];
                }
            }

            // Second pass: look for non-looping generators (they'll finish soon)
            for (int i = 0; i < soundPool.Length; i++)
            {
                int index = (nextPoolIndex + i) % soundPool.Length;
                if (!soundPool[index].IsLooping())
                {
                    nextPoolIndex = (index + 1) % soundPool.Length;
                    return soundPool[index];
                }
            }

            // Last resort: use round-robin (will interrupt a looping sound)
            ProceduralSoundGenerator generator = soundPool[nextPoolIndex];
            nextPoolIndex = (nextPoolIndex + 1) % soundPool.Length;
            Debug.LogWarning("ProceduralAudioManager: All generators busy, interrupting a looping sound");
            return generator;
        }
    }
}
