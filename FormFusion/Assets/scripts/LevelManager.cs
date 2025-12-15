using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TMPro;

[System.Serializable]
public class LevelEvent
{
    public int level;
    public float startTime;
    public float endTime;
    public string selectedObject; // Object selected by the player
    public bool isCorrect; // Whether the selection was correct
}

public class LevelManager : MonoBehaviour
{
    //Controls level flow, timing, UI updates, and logging.
    public static LevelManager Instance;

    public GameObject[] puzzles;
    public int currentLevel = 0;
    public float levelDuration = 15f;

    [Header("UI Elements")]
    public Slider progressBar; // Visual timer bar
    public Text countdownText;

    private Coroutine levelTimerCoroutine;

    private float levelStartTime;
    private string selectedObjectName = "";
    private bool objectWasCorrect = false;

    private List<LevelEvent> levelEvents = new List<LevelEvent>(); // Stored level data
    public TextMeshProUGUI levelText;  // Displays current level number


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Disable all puzzle GameObjects at start
        foreach (GameObject puzzle in puzzles)
            puzzle.SetActive(false);

        ActivateLevel(currentLevel);
        // Start countdown timer
        StartLevelTimer();
    }

    void Update()
    {
        // Update progress bar based on elapsed time
        if (progressBar != null && levelTimerCoroutine != null)
        {
            float elapsed = levelDuration - GetRemainingTime();
            progressBar.value = elapsed;
        }

        if (countdownText != null && levelTimerCoroutine != null)
        {
            countdownText.text = Mathf.Ceil(GetRemainingTime()).ToString() + "s";
        }
    }

    void StartLevelTimer()
    {
        if (levelTimerCoroutine != null)
            StopCoroutine(levelTimerCoroutine);

        levelStartTime = Time.time;
        if (progressBar != null)
        {
            progressBar.maxValue = levelDuration;
            progressBar.value = 0f;
        }

        levelTimerCoroutine = StartCoroutine(LevelTimer());
    }

    IEnumerator LevelTimer()
    {
        // Wait until time runs out
        yield return new WaitForSeconds(levelDuration);
        // Move to the next level
        NextLevel();
    }

    float GetRemainingTime()
    {
        return Mathf.Max(levelDuration - (Time.time - levelStartTime), 0f);
    }

    public void RecordSelection(string objectName, bool isCorrect)
    {
        //Record player selection
        selectedObjectName = objectName;
        objectWasCorrect = isCorrect;
    }

    public void NextLevel()
    {
        // Stop timer coroutine
        if (levelTimerCoroutine != null)
            StopCoroutine(levelTimerCoroutine);

        float endTime = Time.time;

        // Log the level event
        levelEvents.Add(new LevelEvent
        {
            level = currentLevel,
            startTime = levelStartTime,
            endTime = endTime,
            selectedObject = selectedObjectName,
            isCorrect = objectWasCorrect
        });

        // Reset selection state for next level
        selectedObjectName = "";
        objectWasCorrect = false;

        puzzles[currentLevel].SetActive(false);
        //currentLevel = (currentLevel + 1) % puzzles.Length;

        
        if (currentLevel < puzzles.Length - 1)
        {
            currentLevel++;
            ActivateLevel(currentLevel);
            StartLevelTimer();
        }
        else
        {
            // No more levels to activate, game is over or show something else
            Debug.Log("Game Over! All levels completed.");
            puzzles[currentLevel].SetActive(false);
            progressBar.gameObject.SetActive(false);
            currentLevel++;
        }

    }

    void ActivateLevel(int level)
    {
        // Enable puzzle for this level
        if (level >= 0 && level < puzzles.Length)
            puzzles[level].SetActive(true);

        // Update level text
        if (levelText != null)
        {
            levelText.text = "Level: " + (level); // +1 to show user-friendly level index (1-based)
        }
    }

    void OnApplicationQuit()
    {
        // Save all recorded level events
        SaveLevelEventsCSV("LevelEvents.csv");
    }

    void SaveLevelEventsCSV(string fileName)
    {
        string gameFolder = Directory.GetParent(Application.dataPath).FullName;
        string dataFolder = Path.Combine(gameFolder, "data");
        if (!Directory.Exists(dataFolder))
            Directory.CreateDirectory(dataFolder);

        //string path = Path.Combine(Application.dataPath, fileName);
        string path = Path.Combine(dataFolder, fileName);
        StringBuilder csv = new StringBuilder();

        csv.AppendLine("level;startTime;endTime;duration;selectedObject;isCorrect");

        foreach (var evt in levelEvents)
        {
            float duration = evt.endTime - evt.startTime;
            csv.AppendLine($"{evt.level};{evt.startTime};{evt.endTime};{duration};{evt.selectedObject};{evt.isCorrect}");
        }

        File.WriteAllText(path, csv.ToString());
        Debug.Log($"✅ Level event data saved to: {path}");
    }
}
