using UnityEngine;
using UnityEngine.UIElements;
using System;
using Devvit;

/// <summary>
/// Standalone leaderboard UI component that can be placed anywhere.
/// Fetches and displays top 10 players and the current user's rank.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SaveDataManager saveDataManager;

    [Header("Settings")]
    [Tooltip("When true, calls RefreshLeaderboard() automatically in OnEnable(). " +
             "Enable if the leaderboard panel starts visible. Leave false (default) " +
             "if another component calls Show() + RefreshLeaderboard() manually.")]
    [SerializeField] private bool autoFetchOnEnable = false;

    // UI Elements
    private UIDocument uiDocument;
    private VisualElement leaderboardContainer;
    private Label leaderboardTitle;
    private VisualElement leaderboardEntriesContainer;
    private VisualElement userRankContainer;

    private void Awake()
    {
        // Get UI Document component
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("LeaderboardUI: UIDocument component is missing!");
            return;
        }

        // Query UI elements
        var root = uiDocument.rootVisualElement;
        leaderboardContainer = root.Q<VisualElement>("leaderboard-container");
        leaderboardTitle = root.Q<Label>("leaderboard-title");
        leaderboardEntriesContainer = root.Q<VisualElement>("leaderboard-entries");
        userRankContainer = root.Q<VisualElement>("user-rank-container");

        // Hide by default
        Hide();
    }

    private void Start()
    {
        // Subscribe to initialization complete event
        if (saveDataManager != null)
        {
            saveDataManager.OnDevvitInitialized += OnDevvitInitialized;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from event
        if (saveDataManager != null)
        {
            saveDataManager.OnDevvitInitialized -= OnDevvitInitialized;
        }
    }

    private void OnDevvitInitialized()
    {
        // Refresh leaderboard once initialization is complete (if it's visible)
        if (leaderboardContainer != null && leaderboardContainer.style.display == DisplayStyle.Flex)
        {
            RefreshLeaderboard();
        }
    }

    private void OnEnable()
    {
        if (autoFetchOnEnable)
        {
            RefreshLeaderboard();
        }
    }

    /// <summary>
    /// Fetches and displays the leaderboard data.
    /// </summary>
    public void RefreshLeaderboard()
    {
        // Show the container
        Show();

        // Check if SaveDataManager is still initializing
        if (saveDataManager != null && saveDataManager.IsInitializing())
        {
            ShowLoadingState();
            return;
        }

        if (saveDataManager == null || !saveDataManager.IsDevvitAvailable())
        {
            PopulateDummyLeaderboard();
            return;
        }

        // Show loading state while fetching
        ShowLoadingState();

        // Fetch leaderboard
        saveDataManager.FetchLeaderboard((leaderboardResponse) =>
        {
            if (leaderboardResponse == null)
            {
                ShowError("Failed to load leaderboard. Please try again.");
                return;
            }

            // Fetch user rank
            saveDataManager.FetchUserRank((userRankResponse) =>
            {
                PopulateLeaderboard(leaderboardResponse, userRankResponse);
            });
        });
    }

    private void ShowLoadingState()
    {
        if (leaderboardEntriesContainer != null)
        {
            leaderboardEntriesContainer.Clear();
            var loadingLabel = new Label("Loading leaderboard...");
            loadingLabel.style.color = Color.white;
            loadingLabel.style.fontSize = 18;
            loadingLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            loadingLabel.style.marginTop = 20;
            loadingLabel.style.marginBottom = 20;
            leaderboardEntriesContainer.Add(loadingLabel);
        }

        // Hide user rank while loading
        if (userRankContainer != null)
            userRankContainer.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Shows the leaderboard UI.
    /// </summary>
    public void Show()
    {
        if (leaderboardContainer != null)
        {
            leaderboardContainer.style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Hides the leaderboard UI.
    /// </summary>
    public void Hide()
    {
        if (leaderboardContainer != null)
        {
            leaderboardContainer.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// Sets the title of the leaderboard.
    /// </summary>
    public void SetTitle(string title)
    {
        if (leaderboardTitle != null)
        {
            leaderboardTitle.text = title;
        }
    }

    private void PopulateLeaderboard(LeaderboardResponse leaderboard, UserRankResponse userRank)
    {
        if (leaderboardEntriesContainer == null)
        {
            return;
        }

        // Clear existing entries
        leaderboardEntriesContainer.Clear();

        if (leaderboard.entries.Length == 0)
        {
            // No entries yet
            var noDataLabel = new Label("No scores yet. Be the first!");
            noDataLabel.style.color = Color.gray;
            noDataLabel.style.fontSize = 18;
            noDataLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noDataLabel.style.marginTop = 20;
            noDataLabel.style.marginBottom = 20;
            leaderboardEntriesContainer.Add(noDataLabel);

            // Hide user rank
            if (userRankContainer != null)
                userRankContainer.style.display = DisplayStyle.None;

            return;
        }

        // Get current user's username for highlighting
        string currentUsername = saveDataManager != null ? saveDataManager.GetUsername() : "";

        // Add top 10 entries
        foreach (var entry in leaderboard.entries)
        {
            // Highlight the entry if it's the current user
            bool isUserEntry = !string.IsNullOrEmpty(currentUsername) && entry.username == currentUsername;
            var entryElement = CreateLeaderboardEntry(entry.rank, entry.username, entry.score, isUserEntry);
            leaderboardEntriesContainer.Add(entryElement);
        }

        // Show user's rank at the bottom (unless they're already in top 10)
        if (userRankContainer != null)
        {
            userRankContainer.Clear();

            // Only show if user is NOT in top 10
            if (userRank != null && userRank.ranked && !userRank.isTopTen)
            {
                // Show user rank
                var userEntryElement = CreateLeaderboardEntry(userRank.rank, userRank.username, userRank.score, true);
                userRankContainer.Add(userEntryElement);
                userRankContainer.style.display = DisplayStyle.Flex;
            }
            else if (userRank != null && !userRank.ranked)
            {
                // User exists but hasn't scored yet - show unranked
                var userEntryElement = CreateLeaderboardEntry(0, userRank.username, 0, true);
                userRankContainer.Add(userEntryElement);
                userRankContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                // User is in top 10 or doesn't exist - hide container
                userRankContainer.style.display = DisplayStyle.None;
            }
        }
    }

    private VisualElement CreateLeaderboardEntry(int rank, string username, float score, bool isUserEntry)
    {

        var entry = new VisualElement();
        entry.AddToClassList("leaderboard-entry");
        if (isUserEntry)
        {
            entry.AddToClassList("leaderboard-entry--user");
        }

        // Show "-" for unranked players (rank 0)
        var rankLabel = new Label(rank == 0 ? "-" : $"{rank}.");
        rankLabel.AddToClassList("leaderboard-rank");

        var nameLabel = new Label(username);
        nameLabel.AddToClassList("leaderboard-name");

        var scoreLabel = new Label(Mathf.CeilToInt(score).ToString());
        scoreLabel.AddToClassList("leaderboard-score");

        entry.Add(rankLabel);
        entry.Add(nameLabel);
        entry.Add(scoreLabel);

        return entry;
    }

    private void ShowError(string errorMessage)
    {
        if (leaderboardEntriesContainer == null) return;

        leaderboardEntriesContainer.Clear();

        var errorLabel = new Label(errorMessage);
        errorLabel.style.color = Color.red;
        errorLabel.style.fontSize = 18;
        errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        errorLabel.style.marginTop = 20;
        errorLabel.style.marginBottom = 20;
        leaderboardEntriesContainer.Add(errorLabel);

        // Hide user rank
        if (userRankContainer != null)
            userRankContainer.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Populates the leaderboard with placeholder data when Devvit is unavailable.
    /// Called when: (a) SaveDataManager is null, (b) Devvit is not connected
    /// (e.g. Unity Editor without a running Devvit server), or (c) the leaderboard
    /// fetch failed. Includes the player's local high score in the correct position
    /// so the layout is realistic even without network access.
    /// </summary>
    private void PopulateDummyLeaderboard()
    {
        if (leaderboardEntriesContainer == null)
        {
            return;
        }

        // Clear existing entries
        leaderboardEntriesContainer.Clear();

        // Dummy skier usernames
        string[] dummyNames = new string[]
        {
            "SnowShredder",
            "PowderKing",
            "AlpineAce",
            "FrostyFlyer",
            "MountainMaster",
            "IceRider",
            "SlopeSlayer",
            "WinterWarrior",
            "ChillyChamp",
            "BlizzardBoss"
        };

        // Generate scores from 100k to 10k
        DevvitScoreWithData[] dummyScores = new DevvitScoreWithData[]
        {
            new DevvitScoreWithData(100000),
            new DevvitScoreWithData(85000),
            new DevvitScoreWithData(72000),
            new DevvitScoreWithData(61000),
            new DevvitScoreWithData(52000),
            new DevvitScoreWithData(44000),
            new DevvitScoreWithData(35000),
            new DevvitScoreWithData(27000),
            new DevvitScoreWithData(18000),
            new DevvitScoreWithData(10000)
        };

        // Get the user's local high score and username
        DevvitScoreWithData localHighScore = new DevvitScoreWithData();
        string localUsername = "You";
        int userRank = 0;
        bool userIsRanked = false;

        if (saveDataManager != null)
        {
            localHighScore = saveDataManager.GetScoreData();
            localUsername = saveDataManager.GetUsername();
            userIsRanked = localHighScore.score > 0;

            if (userIsRanked)
            {
                // Calculate where the user would rank
                userRank = 1; // Start at first place
                for (int i = 0; i < dummyScores.Length; i++)
                {
                    if (localHighScore.score <= dummyScores[i].score)
                    {
                        userRank++;
                    }
                }
            }
        }

        // Build the top 10 leaderboard
        bool userIsInTopTen = userIsRanked && userRank <= 10;
        int entriesAdded = 0;

        for (int i = 0; i < 10 && entriesAdded < 10; i++)
        {
            int currentRank = entriesAdded + 1;

            // Check if we should insert the user at this position
            if (userIsInTopTen && currentRank == userRank)
            {
                var userEntryElement = CreateLeaderboardEntry(userRank, localUsername, localHighScore.score, true);
                leaderboardEntriesContainer.Add(userEntryElement);
                entriesAdded++;

                // Continue with remaining dummy entries (skip one since user took a spot)
                if (entriesAdded < 10 && i < dummyScores.Length)
                {
                    currentRank = entriesAdded + 1;
                    var entryElement = CreateLeaderboardEntry(currentRank, dummyNames[i], dummyScores[i].score, false);
                    leaderboardEntriesContainer.Add(entryElement);
                    entriesAdded++;
                }
            }
            else if (i < dummyScores.Length)
            {
                var entryElement = CreateLeaderboardEntry(currentRank, dummyNames[i], dummyScores[i].score, false);
                leaderboardEntriesContainer.Add(entryElement);
                entriesAdded++;
            }
        }

        // Show the user's rank at the bottom if they're not in top 10
        if (userRankContainer != null)
        {
            userRankContainer.Clear();

            if (userIsRanked && !userIsInTopTen)
            {
                // User has a score but isn't in top 10 - show with calculated rank
                var userEntryElement = CreateLeaderboardEntry(userRank, localUsername, localHighScore.score, true);
                userRankContainer.Add(userEntryElement);
                userRankContainer.style.display = DisplayStyle.Flex;
            }
            else if (!userIsRanked)
            {
                // User hasn't scored yet - show unranked
                var userEntryElement = CreateLeaderboardEntry(0, localUsername, new float(), true);
                userRankContainer.Add(userEntryElement);
                userRankContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                // User is in top 10 - hide the separate container
                userRankContainer.style.display = DisplayStyle.None;
            }
        }
    }
}
