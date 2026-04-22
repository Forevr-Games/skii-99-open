# Procedural Sound Generator - Quick Start Guide

## Setup (3 Minutes)

### Step 1: Create the Sound Tester

1. Open your Unity scene (or create a new test scene)
2. Right-click in the Hierarchy window
3. Create Empty GameObject
4. Name it **"SoundTester"**
5. With SoundTester selected, click **Add Component**
6. Search for and add **ProceduralSoundGenerator**
7. Click **Add Component** again
8. Search for and add **ProceduralSoundTester**

That's it! You now have a working sound generator.

### Step 2: Generate Your First Sound

1. Select the **SoundTester** GameObject in the Hierarchy
2. Look at the Inspector window - you'll see lots of controls
3. Find the **"Randomization Settings"** section
4. Choose a category from the **"Random Category"** dropdown (try "Jump")
5. Click the big green **"🎲 Generate Random"** button

You should hear a sound! It's generated entirely from code - no audio files!

### Step 3: Experiment

Try these buttons:
- **"🔀 Mutate"** - Create variations of the current sound
- **"▶ Play Sound"** - Replay the current sound
- **"■ Stop"** - Stop playback

Try different categories:
- **Jump** - Bouncy, rising pitch sounds
- **Explosion** - Big booms and impacts
- **Pickup** - Coin/collectible sounds
- **Hit** - Impact sounds
- **Laser** - Pew pew sci-fi sounds
- **UIClick** - Button click sounds

### Step 4: Save Your Favorite Sounds (Optional)

If you want to save sounds as presets:

1. Right-click in the Project window (Assets folder)
2. Go to **Create > Chunky Ball > Audio > Procedural Sound Presets**
3. Name it **"MySoundPresets"**
4. In the SoundTester Inspector, drag this asset to the **"Preset Database"** field
5. Generate a sound you like
6. Enter a name in the **"Preset Name"** field (e.g., "MyJumpSound")
7. Click **"💾 Save Preset"**

Now your sound is saved and can be loaded anytime!

## Understanding the Parameters

The Inspector shows many parameters. Here's what the main ones do:

### Waveform Type
- **Square** - Classic 8-bit video game sound
- **Sine** - Smooth, pure tone
- **Sawtooth** - Buzzy, bright
- **Triangle** - Softer square wave
- **Noise** - Static/explosion sounds

### Frequency Settings
- **Base Frequency** - Starting pitch (440 Hz = musical note A)
  - Low (80-200 Hz) = deep bass sounds
  - Mid (400-800 Hz) = normal sounds
  - High (1000+ Hz) = bright, chirpy sounds
- **Frequency Slide** - Pitch change over time
  - Positive values = pitch goes up
  - Negative values = pitch goes down
  - Zero = no change

### ADSR Envelope (Sound Shape)
- **Attack Time** - How fast the sound fades in (0.01 = instant, 0.1+ = gradual)
- **Decay Time** - How fast it drops to sustain level
- **Sustain Level** - Volume during the middle part (0.5 = 50%)
- **Release Time** - How fast it fades out at the end
- **Duration** - Total length of the sound in seconds

### Effects
- **Vibrato** - Wiggle the pitch (rate = speed, depth = amount)
- **Tremolo** - Wiggle the volume (rate = speed, depth = amount)
- **Bit Crush** - Make it sound lo-fi/crunchy (0 = clean, 16 = very crunchy)

## Keyboard Shortcuts

When Unity is playing or in Edit mode:
- **Space** - Play the current sound
- **R** - Generate a random sound
- **M** - Mutate the current sound
- **Ctrl+S** - Save as preset

## Tips for Getting Good Sounds

1. **Start Random** - Always start with "Generate Random" for your category
2. **Mutate First** - Use Mutate a few times before tweaking manually
3. **Small Changes** - Adjust parameters by small amounts (10-20%)
4. **Save Often** - Save sounds you like immediately as presets
5. **Listen to Duration** - Make sure Duration is long enough for your envelope

## Exporting Sounds

Once you find a sound you love:

1. Click **"📤 Export to AudioClip"**
2. Unity will create a `.wav` file in `Assets/Audio/`
3. You can now use it like any normal AudioClip in Unity
4. This converts the procedural sound to a permanent file

**When to Export:**
- When you want to reduce CPU usage in the final game
- When you need to use the sound in animations or timeline
- When you want to share the sound with others

**When to Keep Procedural:**
- When you want variation (every sound is slightly different)
- When you want to save disk space (no audio files)
- When you're still experimenting

## Common Sound Recipes

### Jump Sound
- Waveform: Square or Triangle
- Base Frequency: 200-600 Hz
- Frequency Slide: +400 to +800 (rising pitch)
- Duration: 0.15-0.3 seconds
- Quick attack (0.01s), medium release (0.1-0.2s)

### Explosion
- Waveform: Noise
- Base Frequency: 100-300 Hz
- Frequency Slide: -500 (falling pitch)
- Duration: 0.5-1.0 seconds
- Instant attack (0.001s), long decay and release (0.4-0.6s)
- Add Bit Crush: 4-8 for extra crunch

### Coin Pickup
- Waveform: Sine or Triangle
- Base Frequency: 800-1200 Hz
- Frequency Slide: +300 to +600
- Duration: 0.1-0.2 seconds
- Very short attack (0.001s), quick release (0.05s)
- Optional: Add slight vibrato (rate: 10, depth: 0.1)

### UI Click
- Waveform: Square
- Base Frequency: 1000-1500 Hz
- Frequency Slide: 0 to -200
- Duration: 0.05-0.1 seconds
- Instant attack and release

## Troubleshooting

**I don't hear anything!**
- Make sure you clicked "Generate Random" or "Play Sound"
- Check the Volume parameter is above 0
- Check Unity's audio mixer volume
- Make sure Duration is greater than 0

**Sound is too quiet/loud**
- Adjust the **Volume** parameter (0-1 range)
- Volume of 0.5 (50%) is usually good

**Sound cuts off early**
- Increase the **Duration** parameter
- Make sure Release Time isn't longer than Duration

**Sound is distorted**
- Lower the Volume
- Reduce Bit Crush amount
- Try a different waveform (Sine is cleanest)

## Next Steps

Now that you have working procedural sounds, you can:

1. **Test Different Categories** - Try all 9 sound categories
2. **Save a Library** - Create presets for common sounds you'll need
3. **Integrate with Game** - Use the sounds in your game code
4. **Experiment** - Try wild parameter combinations!

For more details, see the full [README.md](README.md).

## Questions?

Common questions:

**Q: Can I use this in my published game?**
A: Yes! It's part of your project.

**Q: Does this work on mobile?**
A: Yes, but be mindful of CPU usage. Export frequently-used sounds.

**Q: Can I create music with this?**
A: It's designed for sound effects, not music. But you could try!

**Q: How much space does this save?**
A: A typical sound effect is 50-200 KB. Procedural code is ~50 KB total for ALL sounds.

**Q: Is it faster than loading AudioClips?**
A: Slightly slower (more CPU), but saves memory and disk space.

---

**Have fun making sounds! 🎵🎮**
