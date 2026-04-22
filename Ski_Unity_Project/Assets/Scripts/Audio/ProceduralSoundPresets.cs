using System.Collections.Generic;
using UnityEngine;

namespace ForevrTools.Audio
{
    /// <summary>
    /// Single preset entry containing name, category, and parameters
    /// </summary>
    [System.Serializable]
    public class SoundPreset
    {
        public string name;
        public string category;
        public ProceduralSoundParameters parameters;

        public SoundPreset(string name, string category, ProceduralSoundParameters parameters)
        {
            this.name = name;
            this.category = category;
            this.parameters = parameters.Clone();
        }
    }

    /// <summary>
    /// ScriptableObject that stores a database of sound presets.
    /// Can be created in Unity via Create menu and saved as an asset.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundPresets", menuName = "Chunky Ball/Audio/Procedural Sound Presets", order = 1)]
    public class ProceduralSoundPresets : ScriptableObject
    {
        [Tooltip("List of saved sound presets")]
        public List<SoundPreset> presets = new List<SoundPreset>();

        /// <summary>
        /// Get a preset by name (returns a clone to avoid modifying the original)
        /// </summary>
        public ProceduralSoundParameters GetPreset(string name)
        {
            var preset = presets.Find(p => p.name == name);
            if (preset != null && preset.parameters != null)
            {
                return preset.parameters.Clone();
            }

            Debug.LogWarning($"ProceduralSoundPresets: Preset '{name}' not found");
            return null;
        }

        /// <summary>
        /// Get all presets in a specific category
        /// </summary>
        public List<SoundPreset> GetPresetsByCategory(string category)
        {
            return presets.FindAll(p => p.category == category);
        }

        /// <summary>
        /// Save a new preset. If a preset with the same name exists, automatically appends a number.
        /// </summary>
        public void SavePreset(string name, string category, ProceduralSoundParameters parameters)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("ProceduralSoundPresets: Cannot save preset with empty name");
                return;
            }

            if (parameters == null)
            {
                Debug.LogError("ProceduralSoundPresets: Cannot save null parameters");
                return;
            }

            // Check if preset already exists - if so, create a unique name
            string uniqueName = name;
            if (HasPreset(uniqueName))
            {
                int counter = 1;
                while (HasPreset($"{name}_{counter}"))
                {
                    counter++;
                }
                uniqueName = $"{name}_{counter}";
                Debug.Log($"ProceduralSoundPresets: Preset '{name}' already exists, saving as '{uniqueName}'");
            }

            // Add new preset with unique name
            presets.Add(new SoundPreset(uniqueName, category, parameters));
            Debug.Log($"ProceduralSoundPresets: Added new preset '{uniqueName}'");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Update an existing preset (overwrites if it exists)
        /// </summary>
        public bool UpdatePreset(string name, string category, ProceduralSoundParameters parameters)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("ProceduralSoundPresets: Cannot update preset with empty name");
                return false;
            }

            if (parameters == null)
            {
                Debug.LogError("ProceduralSoundPresets: Cannot update with null parameters");
                return false;
            }

            var existingPreset = presets.Find(p => p.name == name);
            if (existingPreset != null)
            {
                existingPreset.category = category;
                existingPreset.parameters = parameters.Clone();
                Debug.Log($"ProceduralSoundPresets: Updated preset '{name}'");

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
                return true;
            }

            Debug.LogWarning($"ProceduralSoundPresets: Preset '{name}' not found for update");
            return false;
        }

        /// <summary>
        /// Remove a preset by name
        /// </summary>
        public bool RemovePreset(string name)
        {
            var preset = presets.Find(p => p.name == name);
            if (preset != null)
            {
                presets.Remove(preset);
                Debug.Log($"ProceduralSoundPresets: Removed preset '{name}'");

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
                return true;
            }

            Debug.LogWarning($"ProceduralSoundPresets: Preset '{name}' not found for removal");
            return false;
        }

        /// <summary>
        /// Check if a preset exists
        /// </summary>
        public bool HasPreset(string name)
        {
            return presets.Exists(p => p.name == name);
        }

        /// <summary>
        /// Get all preset names
        /// </summary>
        public List<string> GetAllPresetNames()
        {
            List<string> names = new List<string>();
            foreach (var preset in presets)
            {
                names.Add(preset.name);
            }
            return names;
        }

        /// <summary>
        /// Get all unique categories
        /// </summary>
        public List<string> GetAllCategories()
        {
            HashSet<string> categories = new HashSet<string>();
            foreach (var preset in presets)
            {
                if (!string.IsNullOrEmpty(preset.category))
                {
                    categories.Add(preset.category);
                }
            }
            return new List<string>(categories);
        }

        /// <summary>
        /// Clear all presets
        /// </summary>
        public void ClearAllPresets()
        {
            presets.Clear();
            Debug.Log("ProceduralSoundPresets: Cleared all presets");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
