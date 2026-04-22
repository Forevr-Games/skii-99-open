using UnityEngine;

namespace ForevrTools.Audio
{
    /// <summary>
    /// Testing interface for procedural sound generation.
    /// Provides Inspector controls for generating, playing, and saving sounds.
    /// </summary>
    public class ProceduralSoundTester : MonoBehaviour
    {
        [Header("Sound Generator")]
        [Tooltip("Reference to the ProceduralSoundGenerator component")]
        [SerializeField] private ProceduralSoundGenerator generator;

        [Header("Current Sound Parameters")]
        [Tooltip("Current sound parameters - modify these to hear changes")]
        [SerializeField] private ProceduralSoundParameters currentParameters = new ProceduralSoundParameters();

        [Header("Randomization Settings")]
        [Tooltip("Sound category for random generation")]
        [SerializeField] private SoundCategory randomCategory = SoundCategory.Jump;

        [Tooltip("Random seed (-1 for random seed each time)")]
        [SerializeField] private int randomSeed = -1;

        [Header("Mutation Settings")]
        [Tooltip("Amount of variation when mutating (0.1 = 10%, 0.5 = 50%)")]
        [SerializeField] [Range(0.1f, 0.5f)] private float mutationAmount = 0.2f;

        [Header("Preset Management")]
        [Tooltip("Preset database asset (create via Create menu > Chunky Ball > Audio > Procedural Sound Presets)")]
        [SerializeField] private ProceduralSoundPresets presetDatabase;

        [Tooltip("Name for saving/loading presets")]
        [SerializeField] private string presetName = "MySound";

        [Tooltip("Index for loading presets (0 = first preset)")]
        [SerializeField] private int presetIndex = 0;

        [Tooltip("Load preset by name on start")]
        [SerializeField] private bool loadPresetOnStart = false;

        [Tooltip("Preset to load on start")]
        [SerializeField] private string startPresetName = "";

        // Track currently loaded preset for UI display
        private int currentPresetIndex = -1;
        private string currentPresetName = "None";

        private void Awake()
        {
            // Auto-setup generator if not assigned
            if (generator == null)
            {
                generator = GetComponent<ProceduralSoundGenerator>();
                if (generator == null)
                {
                    generator = gameObject.AddComponent<ProceduralSoundGenerator>();
                }
            }

            // Set default parameters if needed
            if (currentParameters == null)
            {
                currentParameters = new ProceduralSoundParameters();
                currentParameters.SetDefaults();
            }
        }

        private void Start()
        {
            if (loadPresetOnStart && presetDatabase != null && !string.IsNullOrEmpty(startPresetName))
            {
                LoadPreset(startPresetName);
            }
        }

        /// <summary>
        /// Play the current sound parameters
        /// </summary>
        public void PlaySound()
        {
            if (generator == null)
            {
                Debug.LogError("ProceduralSoundTester: No generator assigned!");
                return;
            }

            if (currentParameters == null)
            {
                Debug.LogError("ProceduralSoundTester: No parameters set!");
                return;
            }

            generator.Play(currentParameters);
        }

        /// <summary>
        /// Stop the currently playing sound
        /// </summary>
        public void StopSound()
        {
            if (generator != null)
            {
                generator.Stop();
            }
        }

        /// <summary>
        /// Generate a random sound based on the selected category
        /// </summary>
        public void GenerateRandom()
        {
            if (currentParameters == null)
            {
                currentParameters = new ProceduralSoundParameters();
            }

            currentParameters.Randomize(randomCategory, randomSeed);
            PlaySound();
        }

        /// <summary>
        /// Mutate the current sound parameters
        /// </summary>
        public void MutateSound()
        {
            if (currentParameters == null)
            {
                Debug.LogWarning("ProceduralSoundTester: No parameters to mutate, generating random sound instead");
                GenerateRandom();
                return;
            }

            currentParameters.Mutate(mutationAmount);
            PlaySound();
        }

        /// <summary>
        /// Save current parameters as a preset
        /// </summary>
        public void SavePreset()
        {
            if (presetDatabase == null)
            {
                Debug.LogError("ProceduralSoundTester: No preset database assigned! Create one via Create menu > Chunky Ball > Audio > Procedural Sound Presets");
                return;
            }

            if (string.IsNullOrEmpty(presetName))
            {
                Debug.LogError("ProceduralSoundTester: Preset name is empty!");
                return;
            }

            if (currentParameters == null)
            {
                Debug.LogError("ProceduralSoundTester: No parameters to save!");
                return;
            }

            presetDatabase.SavePreset(presetName, randomCategory.ToString(), currentParameters);
            Debug.Log($"Saved preset '{presetName}' to database");
        }

        /// <summary>
        /// Load a preset by name
        /// </summary>
        public void LoadPreset(string name)
        {
            if (presetDatabase == null)
            {
                Debug.LogError("ProceduralSoundTester: No preset database assigned!");
                return;
            }

            var parameters = presetDatabase.GetPreset(name);
            if (parameters != null)
            {
                currentParameters = parameters;
                Debug.Log($"Loaded preset '{name}'");
            }
        }

        /// <summary>
        /// Load the preset specified in the presetName field
        /// </summary>
        public void LoadPreset()
        {
            LoadPreset(presetName);
        }

        /// <summary>
        /// Load a preset by index
        /// </summary>
        public void LoadPresetByIndex()
        {
            if (presetDatabase == null)
            {
                Debug.LogError("ProceduralSoundTester: No preset database assigned!");
                return;
            }

            if (presetIndex < 0 || presetIndex >= presetDatabase.presets.Count)
            {
                Debug.LogError($"ProceduralSoundTester: Invalid preset index {presetIndex}. Valid range: 0-{presetDatabase.presets.Count - 1}");
                return;
            }

            var preset = presetDatabase.presets[presetIndex];
            if (preset?.parameters != null)
            {
                currentParameters = preset.parameters.Clone();
                currentPresetIndex = presetIndex;
                currentPresetName = preset.name;
                Debug.Log($"Loaded preset #{presetIndex}: '{preset.name}'");
            }
        }

        /// <summary>
        /// Load the next preset in the database
        /// </summary>
        public void LoadNextPreset()
        {
            if (presetDatabase == null)
            {
                Debug.LogError("ProceduralSoundTester: No preset database assigned!");
                return;
            }

            if (presetDatabase.presets.Count == 0)
            {
                Debug.LogError("ProceduralSoundTester: No presets available in database!");
                return;
            }

            // Move to next preset (wrap around to 0 if at the end)
            currentPresetIndex = (currentPresetIndex + 1) % presetDatabase.presets.Count;
            presetIndex = currentPresetIndex;

            var preset = presetDatabase.presets[currentPresetIndex];
            if (preset?.parameters != null)
            {
                currentParameters = preset.parameters.Clone();
                currentPresetName = preset.name;
                Debug.Log($"Loaded preset #{currentPresetIndex}: '{preset.name}'");
            }
        }

        /// <summary>
        /// Load the previous preset in the database
        /// </summary>
        public void LoadPreviousPreset()
        {
            if (presetDatabase == null)
            {
                Debug.LogError("ProceduralSoundTester: No preset database assigned!");
                return;
            }

            if (presetDatabase.presets.Count == 0)
            {
                Debug.LogError("ProceduralSoundTester: No presets available in database!");
                return;
            }

            // Move to previous preset (wrap around to last if at the beginning)
            currentPresetIndex--;
            if (currentPresetIndex < 0)
            {
                currentPresetIndex = presetDatabase.presets.Count - 1;
            }
            presetIndex = currentPresetIndex;

            var preset = presetDatabase.presets[currentPresetIndex];
            if (preset?.parameters != null)
            {
                currentParameters = preset.parameters.Clone();
                currentPresetName = preset.name;
                Debug.Log($"Loaded preset #{currentPresetIndex}: '{preset.name}'");
            }
        }

        /// <summary>
        /// Get the name of the currently loaded preset
        /// </summary>
        public string GetCurrentPresetName()
        {
            return currentPresetName;
        }

        /// <summary>
        /// Get the index of the currently loaded preset
        /// </summary>
        public int GetCurrentPresetIndex()
        {
            return currentPresetIndex;
        }

        /// <summary>
        /// Export current sound to an AudioClip asset
        /// </summary>
        public void ExportAudioClip()
        {
            if (currentParameters == null)
            {
                Debug.LogError("ProceduralSoundTester: No parameters to export!");
                return;
            }

            AudioClip clip = ProceduralSoundUtility.GenerateAudioClip(currentParameters, presetName);
            if (clip != null)
            {
#if UNITY_EDITOR
                ProceduralSoundUtility.SaveAudioClipAsAsset(clip, $"Assets/Audio/{presetName}.wav");
                Debug.Log($"Exported AudioClip to Assets/Audio/{presetName}.wav");
#else
                Debug.Log($"Generated AudioClip '{presetName}' (saving only works in Editor)");
#endif
            }
        }

        /// <summary>
        /// Reset to default parameters
        /// </summary>
        public void ResetToDefaults()
        {
            if (currentParameters == null)
            {
                currentParameters = new ProceduralSoundParameters();
            }

            currentParameters.SetDefaults();
            Debug.Log("Reset to default parameters");
        }

        // Keyboard shortcuts for rapid testing (commented out because project uses new Input System)
        // To enable: Go to Edit > Project Settings > Player > Active Input Handling > Change to "Both" or "Input Manager (Old)"
        /*
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PlaySound();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                GenerateRandom();
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                MutateSound();
            }

            if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl))
            {
                SavePreset();
            }
        }
        */
    }
}
