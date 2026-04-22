# Game Integration Guide - Landing Sound Example

This guide shows you how to hook up your saved landing sound preset to play when the player lands.

## What I Added

1. **OnLanded Event** in [SkiTrickSystem.cs:43](../SkiTrickSystem.cs#L43)
   - Fires whenever the player lands
   - Passes the airTime (how long they were in the air)

2. **ProceduralAudioManager** - A simple audio manager that:
   - Manages a pool of sound generators
   - Subscribes to game events
   - Plays your saved presets

## Setup Steps (5 Minutes)

### Step 1: Create the Audio Manager GameObject

1. In your game scene, create an empty GameObject
2. Name it **"AudioManager"**
3. Add the **ProceduralAudioManager** component

### Step 2: Assign References

In the **ProceduralAudioManager** Inspector:

1. **Preset Database** - Drag your SoundPresets ScriptableObject here (the one with your landing sound)
2. **Trick System** - Drag your SkiTrickSystem GameObject from the scene
3. **Landing Sound Preset** - Enter the exact name of your landing preset (e.g., "Landing")

### Step 3: Test It!

1. Press Play
2. Make your player jump and land
3. You should hear your landing sound!

## Configuration Options

### In ProceduralAudioManager Inspector:

- **Pool Size** - Number of simultaneous sounds (default: 5)
  - Increase if you need many sounds at once
  - Decrease to save memory

- **Landing Sound Preset** - Name of the preset to play on landing
  - Must match the name you saved in your SoundPresets
  - Can be changed at runtime

- **Play Landing Sounds** - Toggle to enable/disable landing sounds
  - Useful for testing or user settings

## How It Works

```
Player Lands
    ↓
SkiTrickSystem.HandleLanding() fires OnLanded event
    ↓
ProceduralAudioManager.OnPlayerLanded() receives event
    ↓
Looks up "Landing" preset in SoundPresets database
    ↓
Gets next available generator from pool
    ↓
Plays the sound!
```

## Adding More Sounds

### Example: Add Jump Sound

1. In SoundTester, create a jump sound and save it as "Jump"
2. In ProceduralAudioManager, add:
   ```csharp
   [SerializeField] private string jumpSoundPreset = "Jump";
   ```
3. Find where the player jumps in code
4. Call `audioManager.PlayPreset("Jump")`

### Example: Add Trick Sound

To play a sound when a trick is performed:

1. Save a trick sound preset (e.g., "Trick360")
2. In ProceduralAudioManager.Start(), subscribe to trick events:
   ```csharp
   trickSystem.OnTrickPerformed += (trick) => {
       if (trick.trickType == TrickType.Spin && trick.baseScore >= 360) {
           PlayPreset("Trick360");
       }
   };
   ```

### Example: Add Crash Sound

1. Save a crash sound preset (e.g., "Crash")
2. Add to ProceduralAudioManager:
   ```csharp
   [SerializeField] private SkiPlayerController playerController;

   void Start() {
       // Subscribe to crash event (you'd need to add this event to SkiPlayerController)
       playerController.OnCrash += () => PlayPreset("Crash");
   }
   ```

## Using Random Sounds Instead of Presets

If you want variation, use random generation instead of presets:

```csharp
// Instead of:
PlayPreset("Landing");

// Use:
PlayRandomSound(SoundCategory.Landing);
```

This generates a new landing sound each time!

## Advanced: Vary Sound Based on Impact

Want harder landings to sound different?

In `ProceduralAudioManager.OnPlayerLanded()`:

```csharp
public void OnPlayerLanded(float airTime)
{
    if (!playLandingSounds)
        return;

    // Soft landing (< 1 second airtime)
    if (airTime < 1f)
    {
        PlayPreset("LandingSoft");
    }
    // Hard landing (>= 1 second airtime)
    else
    {
        PlayPreset("LandingHard");
    }
}
```

Or modify the sound programmatically:

```csharp
public void OnPlayerLanded(float airTime)
{
    var parameters = presetDatabase.GetPreset(landingSoundPreset);
    if (parameters != null)
    {
        // Make harder landings lower pitch
        parameters.baseFrequency *= Mathf.Lerp(1f, 0.7f, airTime / 3f);

        // Make harder landings louder
        parameters.volume *= Mathf.Lerp(0.5f, 1f, airTime / 3f);

        PlaySound(parameters);
    }
}
```

## Troubleshooting

**No sound playing?**
- Check AudioManager is in the scene
- Verify Preset Database is assigned
- Check Trick System is assigned
- Make sure Landing Sound Preset name matches your saved preset exactly
- Verify "Play Landing Sounds" is checked

**Sound plays but cuts off?**
- Increase Pool Size (more sounds can overlap)
- Check sound Duration parameter is long enough

**Wrong sound playing?**
- Double-check the preset name matches exactly (case-sensitive!)
- Open your SoundPresets asset and verify the preset exists

**Sound too loud/quiet?**
- Adjust the Volume parameter in your preset
- Check Unity's Audio Mixer settings
- Check system volume

## Performance Notes

- Each sound generator in the pool is lightweight (~1KB memory)
- Procedural synthesis uses CPU instead of memory
- Pool size of 5 is good for most games
- If you need 10+ simultaneous sounds, increase pool size

## What's Next?

Now that landing sounds work, you can add sounds for:
- ✅ Landing (done!)
- Jumping/takeoff
- Tricks (spins, flips)
- Crashes/collisions
- UI buttons
- Combo banking
- Near misses
- Background ambience

Each follows the same pattern:
1. Create/save preset in SoundTester
2. Subscribe to game event in AudioManager
3. Call PlayPreset() when event fires

## Quick Reference

```csharp
// Play a saved preset by name
audioManager.PlayPreset("MySound");

// Play a random sound
audioManager.PlayRandomSound(SoundCategory.Jump);

// Play with custom parameters
ProceduralSoundParameters params = new ProceduralSoundParameters();
params.waveform = WaveformType.Square;
params.baseFrequency = 440f;
audioManager.PlaySound(params);

// Convenience methods
audioManager.PlayJumpSound();
audioManager.PlayHitSound();
audioManager.PlayExplosionSound();
```

---

**Congratulations! Your landing sound is now integrated into the game!** 🎵
