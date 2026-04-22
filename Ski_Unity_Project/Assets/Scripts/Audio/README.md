# Procedural Sound Generator - BFXR Style

A complete procedural sound synthesis system for Unity that generates sounds at runtime using waveforms, eliminating the need for audio files. Similar to BFXR but generates sounds in real-time.

## Features

- **5 Waveform Types**: Sine, Square, Sawtooth, Triangle, Noise
- **ADSR Envelope**: Full Attack, Decay, Sustain, Release control
- **Effects**: Frequency sweep, Vibrato, Tremolo, Bit crushing
- **Randomization**: Generate sounds by category (Jump, Explosion, Pickup, etc.)
- **Mutation**: Create variations of existing sounds
- **Preset System**: Save and load your favorite sounds
- **AudioClip Export**: Bake sounds to permanent audio files
- **Inspector Interface**: Visual controls with large buttons
- **Keyboard Shortcuts**: Rapid iteration workflow

## Quick Start

### 1. Create a Sound Tester GameObject

1. Create an empty GameObject in your scene (right-click in Hierarchy > Create Empty)
2. Name it "SoundTester"
3. Add the `ProceduralSoundGenerator` component
4. Add the `ProceduralSoundTester` component

The inspector will show a complete testing interface with buttons.

### 2. Create a Preset Database (Optional)

1. Right-click in Project window
2. Select Create > Chunky Ball > Audio > Procedural Sound Presets
3. Name it "SoundPresets"
4. Drag it to the "Preset Database" field in ProceduralSoundTester

### 3. Generate Your First Sound

1. Select the SoundTester GameObject
2. In the Inspector, choose a sound category (e.g., "Jump")
3. Click the **"🎲 Generate Random"** button
4. The sound will generate and play automatically!

### 4. Iterate and Refine

- Click **"🔀 Mutate"** to create variations
- Adjust parameters manually in the Inspector
- Click **"▶ Play Sound"** to preview changes
- Use **"💾 Save Preset"** to save sounds you like

## How It Works

### Core Components

1. **ProceduralSoundParameters** - Data class storing all synthesis parameters
2. **ProceduralSoundSynthesizer** - Core synthesis engine that generates audio samples
3. **ProceduralSoundGenerator** - MonoBehaviour that uses OnAudioFilterRead for playback
4. **ProceduralSoundTester** - Testing interface for experimentation
5. **ProceduralSoundPresets** - ScriptableObject preset database

### Audio Synthesis Flow

```
Parameters → Synthesizer → OnAudioFilterRead → Unity Audio System
```

Every audio frame (44,100 times per second), the synthesizer:
1. Generates a waveform sample at the current phase
2. Applies ADSR envelope
3. Applies effects (vibrato, tremolo, bit crushing)
4. Returns the sample to Unity's audio buffer

## Sound Categories

Each category generates different types of sounds:

- **Jump** - Rising pitch, short duration
- **Explosion** - Noise burst with long decay
- **Pickup** - High pitch, quick attack
- **Hit** - Low pitch, sharp impact
- **Laser** - Falling pitch, sustained
- **Powerup** - Rising pitch with vibrato
- **UIClick** - Very short, high frequency
- **Landing** - Low thud with optional noise
- **Swoosh** - Variable pitch with tremolo

## Parameters Explained

### Waveform
- **Sine**: Pure tone, smooth and mellow
- **Square**: Retro 8-bit, harsh
- **Sawtooth**: Buzzy, bright
- **Triangle**: Softer square
- **Noise**: White noise, chaos

### Frequency
- **Base Frequency**: Starting pitch in Hz (440 = middle A)
- **Frequency Slide**: Pitch change over time (positive = rising, negative = falling)
- **Delta Slide**: Acceleration of pitch change

### ADSR Envelope
- **Attack**: Fade-in time
- **Decay**: Drop from peak to sustain
- **Sustain Level**: Hold volume (0-1)
- **Release**: Fade-out time

### Effects
- **Vibrato**: Pitch oscillation (rate and depth)
- **Tremolo**: Volume oscillation (rate and depth)
- **Bit Crush**: Lo-fi reduction (0 = clean, 16 = crushed)

## Keyboard Shortcuts

When the scene is playing or in Edit mode:

- **Space** - Play current sound
- **R** - Generate random sound
- **M** - Mutate current sound
- **Ctrl+S** - Save preset

## Exporting Sounds

If you find a sound you want to keep permanently:

1. Click **"📤 Export to AudioClip"**
2. The sound will be saved to `Assets/Audio/[PresetName].wav`
3. You can now use it like any other AudioClip

**Note**: This saves space but uses CPU. Only export sounds if you want to reduce runtime CPU usage.

## Using in Your Game

### Basic Usage

```csharp
using ChunkyBall.Audio;

public class MyScript : MonoBehaviour
{
    [SerializeField] private ProceduralSoundGenerator generator;

    void PlayJumpSound()
    {
        ProceduralSoundParameters jumpSound = new ProceduralSoundParameters();
        jumpSound.Randomize(SoundCategory.Jump, seed: 12345);
        generator.Play(jumpSound);
    }
}
```

### Loading Presets

```csharp
[SerializeField] private ProceduralSoundPresets presetDatabase;
[SerializeField] private ProceduralSoundGenerator generator;

void PlayPreset(string presetName)
{
    var parameters = presetDatabase.GetPreset(presetName);
    if (parameters != null)
    {
        generator.Play(parameters);
    }
}
```

### Creating Sounds Programmatically

```csharp
ProceduralSoundParameters explosion = new ProceduralSoundParameters
{
    waveform = WaveformType.Noise,
    baseFrequency = 200f,
    frequencySlide = -400f,
    attackTime = 0.01f,
    decayTime = 0.4f,
    sustainLevel = 0.2f,
    releaseTime = 0.5f,
    duration = 0.8f,
    bitCrush = 4f,
    volume = 0.6f
};

generator.Play(explosion);
```

## Performance Notes

- **CPU Usage**: Procedural synthesis uses more CPU than playing AudioClips
- **Memory**: Virtually zero memory usage (just code)
- **Optimization**: OnAudioFilterRead is highly optimized and thread-safe
- **Multiple Sounds**: Use object pooling for many simultaneous sounds

## Future Integration

For game integration, you'll want to create a `ProceduralSoundManager` that:
- Maintains a pool of ProceduralSoundGenerator instances
- Maps game events to sound parameters
- Handles spatial audio (3D positioning)
- Manages sound priorities

## Technical Details

- **Sample Rate**: 44,100 Hz (default Unity setting)
- **Bit Depth**: 32-bit float internally, 16-bit for exports
- **Channels**: Mono (duplicated to stereo)
- **Latency**: Very low (OnAudioFilterRead is real-time)

## Troubleshooting

**No sound playing?**
- Check that AudioSource is enabled
- Verify volume is above 0
- Make sure duration is > 0
- Check Unity's audio mixer settings

**Sounds are clipping/distorting?**
- Reduce volume parameter
- Lower bit crush amount
- Check that multiple sounds aren't overlapping

**Performance issues?**
- Reduce the number of simultaneous sounds
- Use object pooling
- Consider exporting frequently-used sounds to AudioClips

## File Structure

```
Assets/Scripts/Audio/
├── ProceduralSoundParameters.cs       - Parameter data class
├── ProceduralSoundSynthesizer.cs      - Core synthesis engine
├── ProceduralSoundGenerator.cs        - Unity playback component
├── ProceduralSoundTester.cs           - Testing interface
├── ProceduralSoundPresets.cs          - Preset database
├── ProceduralSoundUtility.cs          - Helper functions
└── README.md                          - This file

Assets/Scripts/Editor/
└── ProceduralSoundTesterEditor.cs     - Custom Inspector UI

Assets/Audio/
└── Presets/                           - Preset assets location
```

## Credits

Inspired by BFXR/sfxr sound generation tools. Implemented entirely in C# for Unity with no external dependencies.

## License

Part of the Chunky Ball project. Use freely within this project.
