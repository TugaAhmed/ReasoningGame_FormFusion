using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Tobii.XR;

[System.Serializable]
public class GazeDataPoint
{
    public float time;
    public Vector3 gazeHitPoint;
    public string objectName;

    public Vector3 gazeOrigin;
    public Vector3 gazeDirection;

    public bool gazeIsValid;
    public bool isLeftEyeBlinking;
    public bool isRightEyeBlinking;

    public int levelIndex;
}

[System.Serializable]
public class BlinkEvent
{
    //Stores blink events separately
    public float time;
    public string eye;
    public int level;
}

public class GazeRecorder : MonoBehaviour
{
    [Header("Tobii Gaze Recorder")]
    public bool isRecording = true;

    [Header("Reference to LevelManager")]
    public LevelManager game;

    private List<GazeDataPoint> gazeDataList = new List<GazeDataPoint>();
    private List<BlinkEvent> blinkEvents = new List<BlinkEvent>();

    private bool lastLeftBlink = false;
    private bool lastRightBlink = false;

    private int lastSavedIndex = 0;
    private string gazeCsvPath;
    private string blinkCsvPath;

    void Start()
    {
        string gameFolder = Directory.GetParent(Application.dataPath).FullName;
        string dataFolder = Path.Combine(gameFolder, "data");
        if (!Directory.Exists(dataFolder))
            Directory.CreateDirectory(dataFolder);
        //gazeCsvPath = Path.Combine(Application.persistentDataPath, "GazeHitData.csv");
        //blinkCsvPath = Path.Combine(Application.persistentDataPath, "BlinkEvents.csv");

        gazeCsvPath = Path.Combine(dataFolder, "GazeHitData.csv");
        blinkCsvPath = Path.Combine(dataFolder, "BlinkEvents.csv");
        // Init CSV header
        File.WriteAllText(gazeCsvPath, "time;gazeOrigin_x;gazeOrigin_y;gazeOrigin_z;gazeDirection_x;gazeDirection_y;gazeDirection_z;" +
                                       "gazeIsValid;isLeftEyeBlinking;isRightEyeBlinking;gaze_hit_x;gaze_hit_y;gaze_hit_z;object_name;level\n");

        File.WriteAllText(blinkCsvPath, "time;eye;level\n");

        StartCoroutine(AutoSaveRoutine());
    }

    void Update()
    {
        if (!isRecording) return;
        RecordGazeData();
    }

    void RecordGazeData()
    {
        var eyeTrackingData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.World);

        bool leftBlink = eyeTrackingData.IsLeftEyeBlinking;
        bool rightBlink = eyeTrackingData.IsRightEyeBlinking;
        float currentTime = Time.time;

        if ((!lastLeftBlink && leftBlink) && (!lastRightBlink && rightBlink))
        {
            blinkEvents.Add(new BlinkEvent { time = currentTime, eye = "Both", level = game != null ? game.currentLevel : -1 });
        }
        else
        {
            if (!lastLeftBlink && leftBlink)
                blinkEvents.Add(new BlinkEvent { time = currentTime, eye = "Left", level = game != null ? game.currentLevel : -1 });
            if (!lastRightBlink && rightBlink)
                blinkEvents.Add(new BlinkEvent { time = currentTime, eye = "Right", level = game != null ? game.currentLevel : -1 });
        }

        lastLeftBlink = leftBlink;
        lastRightBlink = rightBlink;

        GazeDataPoint data = new GazeDataPoint
        {
            time = currentTime,
            gazeOrigin = eyeTrackingData.GazeRay.Origin,
            gazeDirection = eyeTrackingData.GazeRay.Direction,
            gazeIsValid = eyeTrackingData.GazeRay.IsValid,
            isLeftEyeBlinking = leftBlink,
            isRightEyeBlinking = rightBlink,
            levelIndex = game != null ? game.currentLevel : -1
        };

        if (eyeTrackingData.GazeRay.IsValid)
        {
            Ray gazeRay = new Ray(data.gazeOrigin, data.gazeDirection);
            if (Physics.Raycast(gazeRay, out RaycastHit hitInfo))
            {
                data.gazeHitPoint = hitInfo.point;
                data.objectName = hitInfo.collider.gameObject.name;
            }
        }

        gazeDataList.Add(data);
    }

    IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            SaveNewGazeData();
            SaveNewBlinkData();
        }
    }

    void SaveNewGazeData()
    {
        StringBuilder csv = new StringBuilder();

        for (int i = lastSavedIndex; i < gazeDataList.Count; i++)
        {
            var data = gazeDataList[i];
            csv.AppendLine($"{data.time};{data.gazeOrigin.x};{data.gazeOrigin.y};{data.gazeOrigin.z};" +
                           $"{data.gazeDirection.x};{data.gazeDirection.y};{data.gazeDirection.z};" +
                           $"{data.gazeIsValid};{data.isLeftEyeBlinking};{data.isRightEyeBlinking};" +
                           $"{data.gazeHitPoint.x};{data.gazeHitPoint.y};{data.gazeHitPoint.z};{data.objectName};{data.levelIndex}");
        }

        if (csv.Length > 0)
        {
            File.AppendAllText(gazeCsvPath, csv.ToString());
            lastSavedIndex = gazeDataList.Count;
            Debug.Log($"📁 Gaze data appended at {Time.time}");
        }
    }

    void SaveNewBlinkData()
    {
        if (blinkEvents.Count == 0) return;

        StringBuilder csv = new StringBuilder();
        foreach (var blink in blinkEvents)
        {
            csv.AppendLine($"{blink.time};{blink.eye};{blink.level}");
        }

        File.AppendAllText(blinkCsvPath, csv.ToString());
        blinkEvents.Clear(); //  Prevent duplication
        Debug.Log($"📁 Blink data appended at {Time.time}");
    }

    void OnApplicationQuit()
    {
        SaveNewGazeData(); // Save remaining unsaved data
        SaveNewBlinkData();
        Debug.Log("💾 All remaining data saved on quit.");
    }
}
