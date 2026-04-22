using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum TrickType
{
    Hangtime,    // Per-second airtime
    Spin,        // Rotation tricks (Y-axis)
    Backflip,    // Backflip tricks (X-axis)
    NearMiss     // Close obstacle passes
}

[Serializable]
public class TrickScoreSettings
{
    [Header("Hangtime Settings")]
    public float hangtimeScorePerSecond = 50f;

    [Header("Near Miss Settings")]
    public float nearMissBaseScore = 25f;
    public float nearMissCloseMultiplier = 2f; // For very close passes

    [Header("Spin Scores")]
    public int spin180Score = 100;
    public int spin360Score = 250;
    public int spin540Score = 400;
    public int spin720Score = 600;
    public int spin900Score = 800;
    public int spin1080Score = 1000;

    [Header("Backflip Scores")]
    public int backflipScore = 300; // Per 360° backflip

    [Header("Combo Settings")]
    public float comboTimeWindow = 3f;
    public int comboMultiplierIncrement = 1; // +1 per trick

    public int GetSpinScore(int degrees)
    {
        // Handle predefined scores for common spins
        switch (degrees)
        {
            case 180: return spin180Score;
            case 360: return spin360Score;
            case 540: return spin540Score;
            case 720: return spin720Score;
            case 900: return spin900Score;
            case 1080: return spin1080Score;
        }

        // For spins beyond 1080, calculate dynamically
        // Pattern: 1080 = 1000, then +200 per additional 180° increment
        if (degrees > 1080 && degrees % 180 == 0)
        {
            int increment = degrees / 180; // How many 180° rotations
            int incrementsBeyond1080 = increment - 6; // 1080 is 6 increments
            return spin1080Score + (incrementsBeyond1080 * 200);
        }

        return 0;
    }
}

public class TrickInstance
{
    public TrickType type;
    public string displayName;
    public int baseScore;
    public int multiplier;
    public float timestamp;

    public int FinalScore => baseScore * multiplier;
}

public class ComboState
{
    public List<TrickInstance> tricks = new List<TrickInstance>();
    public int currentMultiplier = 1;
    public float lastTrickTime;
    public bool isActive;

    public int GetBaseScore()
    {
        return tricks.Sum(t => t.baseScore);
    }

    public int GetComboChainCount()
    {
        return tricks.Count;
    }

    public int GetTotalScore()
    {
        // Base score × combo chain multiplier
        return GetBaseScore() * GetComboChainCount();
    }

    public string GetComboString()
    {
        // Format: "Near Miss! (x5) + Hangtime (x2) + 720° Spin"
        // Group all tricks by display name and show counts
        StringBuilder sb = new StringBuilder();

        // Group tricks by display name while preserving first occurrence order
        var groupedTricks = tricks
            .GroupBy(t => t.displayName)
            .Select(g => new { DisplayName = g.Key, Count = g.Count(), FirstIndex = tricks.IndexOf(g.First()) })
            .OrderBy(x => x.FirstIndex);

        bool first = true;
        foreach (var group in groupedTricks)
        {
            if (!first)
                sb.Append("\n+ ");

            sb.Append(group.DisplayName);

            // Show count if more than 1
            if (group.Count > 1)
                sb.Append($" (x{group.Count})");

            first = false;
        }

        return sb.ToString();
    }
}
