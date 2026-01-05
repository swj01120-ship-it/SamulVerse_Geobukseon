using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BeatMapSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject notePrefab;
    public GameObject obstaclePrefab;

    [Header("Note Spawn/Target Points")]
    public Transform[] spawnPoints;
    public Transform[] targetPoints;

    [Header("BeatMap")]
    public TextAsset beatMapJson;
    public float noteSpeed = 5f;
    public float spawnOffset = 2f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private MusicManager musicManager;
    private BeatMapData beatMapData;
    private Queue<NoteData> upcomingNotes;
    private bool hasStarted = false;

    void Start()
    {
        musicManager = MusicManager.Instance;

        if (musicManager == null)
        {
            Debug.LogError("MusicManager not found!");
            return;
        }

        LoadBeatMap();

        if (beatMapData != null && beatMapData.notes.Count > 0)
        {
            StartCoroutine(SpawnFromBeatMap());
        }
        else
        {
            Debug.LogError("BeatMap is empty or not loaded!");
        }
    }

    void LoadBeatMap()
    {
        if (beatMapJson == null)
        {
            Debug.LogError("BeatMap JSON not assigned!");
            return;
        }

        try
        {
            beatMapData = JsonUtility.FromJson<BeatMapData>(beatMapJson.text);
            beatMapData.notes.Sort((a, b) => a.time.CompareTo(b.time));
            upcomingNotes = new Queue<NoteData>(beatMapData.notes);

            Debug.Log($"BeatMap loaded: {beatMapData.songName}");
            Debug.Log($"Total notes: {beatMapData.notes.Count}");
            Debug.Log($"First note time: {beatMapData.notes[0].time:F2}s");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load BeatMap: {e.Message}");
        }
    }

    IEnumerator SpawnFromBeatMap()
    {
        // 음악 시작 대기
        yield return new WaitForSeconds(1f);

        // MusicManager 시작 확인
        while (musicManager == null || musicManager.audioSource == null || !musicManager.audioSource.isPlaying)
        {
            yield return null;
        }

        Debug.Log("Music started! Beginning note spawn...");

        // ★ 첫 노트까지 추가 대기 ★
        if (beatMapData.notes.Count > 0)
        {
            float firstNoteTime = beatMapData.notes[0].time;
            Debug.Log($"First note at {firstNoteTime:F2}s, waiting...");

            // 첫 노트 시간이 spawnOffset보다 짧으면 대기
            if (firstNoteTime < spawnOffset)
            {
                float waitTime = spawnOffset - firstNoteTime + 0.5f;
                Debug.Log($"Waiting additional {waitTime:F2}s for first note...");
                yield return new WaitForSeconds(waitTime);
            }
        }

        hasStarted = true;
        int spawnedCount = 0;

        while (upcomingNotes.Count > 0)
        {
            NoteData nextNote = upcomingNotes.Peek();
            float currentTime = musicManager.songPosition;

            if (currentTime >= nextNote.time - spawnOffset)
            {
                upcomingNotes.Dequeue();
                SpawnNote(nextNote);
                spawnedCount++;

                if (showDebugInfo)
                {
                    Debug.Log($"[{spawnedCount}/{beatMapData.notes.Count}] Spawned {nextNote.type} at music {currentTime:F2}s (note time: {nextNote.time:F2}s) for drum {nextNote.drum}");
                }
            }

            yield return null;
        }

        Debug.Log($"All notes spawned! Total: {spawnedCount}");
    }

    void SpawnNote(NoteData noteData)
    {
        if (noteData.drum < 0 || noteData.drum >= spawnPoints.Length)
        {
            Debug.LogError($"Invalid drum index: {noteData.drum}");
            return;
        }

        if (spawnPoints[noteData.drum] == null || targetPoints[noteData.drum] == null)
        {
            Debug.LogError($"Spawn or target point {noteData.drum} is null!");
            return;
        }

        if (noteData.type == "obstacle")
        {
            SpawnObstacle(noteData.drum);
        }
        else
        {
            SpawnHitNote(noteData.drum);
        }
    }

    void SpawnHitNote(int drumIndex)
    {
        if (notePrefab == null)
        {
            Debug.LogError("Note prefab not assigned!");
            return;
        }

        GameObject noteObj = Instantiate(
            notePrefab,
            spawnPoints[drumIndex].position,
            Quaternion.identity
        );

        Note note = noteObj.GetComponent<Note>();
        if (note != null)
        {
            note.speed = noteSpeed;
            note.targetPosition = targetPoints[drumIndex].position;
        }
    }

    void SpawnObstacle(int drumIndex)
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("Obstacle prefab not assigned!");
            return;
        }

        if (drumIndex < 0 || drumIndex >= spawnPoints.Length)
        {
            Debug.LogError($"Invalid drum index: {drumIndex}");
            return;
        }

        if (spawnPoints[drumIndex] == null)
        {
            Debug.LogError($"SpawnPoint {drumIndex} is null!");
            return;
        }

        Vector3 obstaclePos = spawnPoints[drumIndex].position;
        obstaclePos.y += 0.5f;

        Vector3 playerTarget = GetPlayerObstacleTarget(drumIndex);

        if (playerTarget == Vector3.zero)
        {
            Debug.LogError("Could not find player camera!");
            return;
        }

        GameObject obstacleObj = Instantiate(
            obstaclePrefab,
            obstaclePos,
            Quaternion.identity
        );

        obstacleObj.name = $"Obstacle_ToPlayer_{drumIndex}";

        Obstacle obstacle = obstacleObj.GetComponent<Obstacle>();
        if (obstacle != null)
        {
            obstacle.speed = noteSpeed;
            obstacle.targetPosition = playerTarget;
        }
    }

    Vector3 GetPlayerObstacleTarget(int drumIndex)
    {
        Transform playerCamera = null;

        GameObject cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
        if (cameraRig != null)
        {
            Transform trackingSpace = cameraRig.transform.Find("TrackingSpace");
            if (trackingSpace != null)
            {
                playerCamera = trackingSpace.Find("CenterEyeAnchor");
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
            if (playerCamera == null)
            {
                Debug.LogError("Cannot find player camera!");
                return Vector3.zero;
            }
        }

        Vector3 baseTarget = playerCamera.position;

        switch (drumIndex)
        {
            case 0:
                baseTarget += playerCamera.right * -0.8f;
                baseTarget += Vector3.up * 0.3f;
                break;

            case 1:
                baseTarget += playerCamera.right * -0.5f;
                baseTarget += Vector3.up * -0.1f;
                break;

            case 2:
                baseTarget += playerCamera.right * 0.5f;
                baseTarget += Vector3.up * -0.1f;
                break;

            case 3:
                baseTarget += playerCamera.right * 0.8f;
                baseTarget += Vector3.up * 0.3f;
                break;
        }

        return baseTarget;
    }

    void OnGUI()
    {
        if (!showDebugInfo || !hasStarted) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.normal.textColor = Color.white;

        int remaining = upcomingNotes.Count;
        int total = beatMapData != null ? beatMapData.notes.Count : 0;
        int spawned = total - remaining;

        GUI.Label(new Rect(10, Screen.height - 60, 400, 30),
            $"Notes: {spawned}/{total} | Remaining: {remaining}", style);

        if (musicManager != null)
        {
            GUI.Label(new Rect(10, Screen.height - 30, 400, 30),
                $"Song Time: {musicManager.songPosition:F2}s", style);
        }
    }
}
