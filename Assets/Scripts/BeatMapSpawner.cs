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

    [Header("Spawn Control")]
    public float stopSpawnBeforeSongEnd = 2f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private MusicManager musicManager;
    private BeatMapData beatMapData;
    private Queue<NoteData> upcomingNotes;

    private bool isSpawning = false;
    private bool spawningComplete = false;
    private Coroutine spawnRoutine;

    private void Awake()
    {
        isSpawning = false;
        spawningComplete = false;
    }

    private void OnEnable()
    {
        MainGameAutoStartController.OnSongStart += HandleSongStart;

        // ✅ 이미 시작된 뒤에 켜져도 바로 스폰 시작
        if (MainGameAutoStartController.SongStarted)
            HandleSongStart();
    }

    private void OnDisable()
    {
        MainGameAutoStartController.OnSongStart -= HandleSongStart;
    }

    private void Start()
    {
        musicManager = MusicManager.Instance;
        if (musicManager == null)
        {
            Debug.LogError("[BeatMapSpawner] MusicManager not found!");
            return;
        }

        // 씬에 기본 비트맵이 할당돼 있으면 로드(없어도 OK)
        if (beatMapJson != null)
            LoadBeatMap();

        if (beatMapData == null || beatMapData.notes == null || beatMapData.notes.Count == 0)
        {
            if (showDebugInfo)
                Debug.LogWarning("[BeatMapSpawner] BeatMap empty/not loaded yet. (Will be injected per TrackData)");
        }
    }

    public void SetBeatMap(TextAsset newBeatMap)
    {
        if (newBeatMap == null)
        {
            Debug.LogError("[BeatMapSpawner] SetBeatMap: newBeatMap is null!");
            beatMapJson = null;
            beatMapData = null;
            upcomingNotes = null;
            return;
        }

        beatMapJson = newBeatMap;
        LoadBeatMap();
    }

    private void HandleSongStart()
    {
        BeginSpawn();
    }

    public void BeginSpawn()
    {
        if (isSpawning) return;

        if (musicManager == null)
        {
            musicManager = MusicManager.Instance;
            if (musicManager == null)
            {
                Debug.LogError("[BeatMapSpawner] MusicManager not found!");
                return;
            }
        }

        // ✅ 필수 값 검증 (노트가 안 나오는 가장 흔한 원인)
        if (notePrefab == null)
        {
            Debug.LogError("[BeatMapSpawner] Note prefab not assigned!");
            return;
        }

        if (spawnPoints == null || targetPoints == null || spawnPoints.Length == 0 || targetPoints.Length == 0)
        {
            Debug.LogError("[BeatMapSpawner] spawnPoints/targetPoints not set!");
            return;
        }

        if (spawnPoints.Length != targetPoints.Length)
        {
            Debug.LogError($"[BeatMapSpawner] spawnPoints({spawnPoints.Length}) != targetPoints({targetPoints.Length})");
            return;
        }

        if (beatMapData == null || beatMapData.notes == null || beatMapData.notes.Count == 0)
        {
            Debug.LogError("[BeatMapSpawner] BeatMap is empty or not loaded!");
            return;
        }

        isSpawning = true;
        spawningComplete = false;

        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnFromBeatMap());

        if (showDebugInfo)
            Debug.Log($"[BeatMapSpawner] ✅ BeginSpawn() OK. Notes={beatMapData.notes.Count}, spawnOffset={spawnOffset}");
    }

    private void LoadBeatMap()
    {
        if (beatMapJson == null)
        {
            Debug.LogError("[BeatMapSpawner] BeatMap JSON not assigned!");
            beatMapData = null;
            upcomingNotes = null;
            return;
        }

        try
        {
            beatMapData = JsonUtility.FromJson<BeatMapData>(beatMapJson.text);

            if (beatMapData == null || beatMapData.notes == null)
            {
                Debug.LogError($"[BeatMapSpawner] Failed to parse BeatMap JSON: {beatMapJson.name}");
                return;
            }

            beatMapData.notes.Sort((a, b) => a.time.CompareTo(b.time));
            upcomingNotes = new Queue<NoteData>(beatMapData.notes);

            if (showDebugInfo)
            {
                Debug.Log($"[BeatMapSpawner] BeatMap loaded: {beatMapData.songName} (asset: {beatMapJson.name})");
                Debug.Log($"[BeatMapSpawner] Total notes: {beatMapData.notes.Count}");
                if (beatMapData.notes.Count > 0)
                    Debug.Log($"[BeatMapSpawner] First note time: {beatMapData.notes[0].time:F2}s");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BeatMapSpawner] Failed to load BeatMap: {e.Message}");
        }
    }

    private IEnumerator SpawnFromBeatMap()
    {
        float songLength = 9999f;
        if (musicManager != null && musicManager.audioSource != null && musicManager.audioSource.clip != null)
            songLength = musicManager.audioSource.clip.length;

        float stopSpawnTime = songLength - stopSpawnBeforeSongEnd;

        if (showDebugInfo)
        {
            Debug.Log($"[BeatMapSpawner] Start spawn. Song length: {songLength:F2}s");
            Debug.Log($"[BeatMapSpawner] Stop spawn time: {stopSpawnTime:F2}s");
        }

        // 첫 노트가 너무 빠른 경우 보정(기존 로직)
        if (beatMapData != null && beatMapData.notes != null && beatMapData.notes.Count > 0)
        {
            float firstNoteTime = beatMapData.notes[0].time;
            if (firstNoteTime < spawnOffset)
            {
                float waitTime = (spawnOffset - firstNoteTime) + 0.5f;
                if (showDebugInfo) Debug.Log($"[BeatMapSpawner] Waiting {waitTime:F2}s for first note...");
                yield return new WaitForSeconds(waitTime);
            }
        }

        int spawnedCount = 0;
        int totalNotes = beatMapData.notes.Count;

        while (upcomingNotes != null && upcomingNotes.Count > 0 && isSpawning)
        {
            float currentTime = (musicManager != null) ? musicManager.songPosition : Time.timeSinceLevelLoad;

            if (currentTime >= stopSpawnTime)
            {
                int remainingNotes = upcomingNotes.Count;
                if (showDebugInfo)
                {
                    Debug.Log($"[BeatMapSpawner] Reached stop time ({stopSpawnTime:F2}s). Stopping spawn.");
                    Debug.Log($"[BeatMapSpawner] Spawned {spawnedCount}/{totalNotes}, Skipped {remainingNotes}");
                }
                spawningComplete = true;
                yield break;
            }

            NoteData nextNote = upcomingNotes.Peek();

            if (currentTime >= nextNote.time - spawnOffset)
            {
                upcomingNotes.Dequeue();
                SpawnNote(nextNote);
                spawnedCount++;

                if (showDebugInfo)
                    Debug.Log($"[{spawnedCount}/{totalNotes}] Spawned {nextNote.type} at {currentTime:F2}s for drum {nextNote.drum}");
            }

            yield return null;
        }

        spawningComplete = true;
        if (showDebugInfo) Debug.Log($"[BeatMapSpawner] Spawn complete: {spawnedCount}/{totalNotes}");
    }

    private void SpawnNote(NoteData noteData)
    {
        if (noteData.drum < 0 || noteData.drum >= spawnPoints.Length)
        {
            Debug.LogError($"[BeatMapSpawner] Invalid drum index: {noteData.drum}");
            return;
        }

        if (spawnPoints[noteData.drum] == null || targetPoints[noteData.drum] == null)
        {
            Debug.LogError($"[BeatMapSpawner] Spawn or target point {noteData.drum} is null!");
            return;
        }

        if (noteData.type == "obstacle")
            SpawnObstacle(noteData.drum);
        else
            SpawnHitNote(noteData.drum);
    }

    private void SpawnHitNote(int drumIndex)
    {
        GameObject noteObj = Instantiate(notePrefab, spawnPoints[drumIndex].position, Quaternion.identity);

        Note note = noteObj.GetComponent<Note>();
        if (note != null)
        {
            note.speed = noteSpeed;
            note.targetPosition = targetPoints[drumIndex].position;

            string[] drumTypes = { "Jung", "Jang", "Book", "Jing" };
            if (drumIndex < drumTypes.Length)
                note.drumType = drumTypes[drumIndex];
        }
        else
        {
            Debug.LogWarning("[BeatMapSpawner] Spawned notePrefab has no Note component.");
        }
    }

    private void SpawnObstacle(int drumIndex)
    {
        if (obstaclePrefab == null)
        {
            Debug.LogWarning("[BeatMapSpawner] Obstacle prefab not assigned!");
            return;
        }

        Vector3 obstaclePos = spawnPoints[drumIndex].position;
        obstaclePos.y += 0.5f;

        Vector3 playerTarget = GetPlayerObstacleTarget(drumIndex);
        if (playerTarget == Vector3.zero)
        {
            Debug.LogError("[BeatMapSpawner] Could not find player camera!");
            return;
        }

        GameObject obstacleObj = Instantiate(obstaclePrefab, obstaclePos, Quaternion.identity);
        obstacleObj.name = $"Obstacle_ToPlayer_{drumIndex}";

        Obstacle obstacle = obstacleObj.GetComponent<Obstacle>();
        if (obstacle != null)
        {
            obstacle.speed = noteSpeed;
            obstacle.targetPosition = playerTarget;
        }
    }

    private Vector3 GetPlayerObstacleTarget(int drumIndex)
    {
        Transform playerCamera = null;

        GameObject cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
        if (cameraRig != null)
        {
            Transform trackingSpace = cameraRig.transform.Find("TrackingSpace");
            if (trackingSpace != null)
                playerCamera = trackingSpace.Find("CenterEyeAnchor");
        }

        if (playerCamera == null)
        {
            if (Camera.main == null)
            {
                Debug.LogError("[BeatMapSpawner] Cannot find player camera!");
                return Vector3.zero;
            }
            playerCamera = Camera.main.transform;
        }

        Vector3 baseTarget = playerCamera.position;

        switch (drumIndex)
        {
            case 0: baseTarget += playerCamera.right * -0.8f + Vector3.up * 0.3f; break;
            case 1: baseTarget += playerCamera.right * -0.5f + Vector3.up * -0.1f; break;
            case 2: baseTarget += playerCamera.right * 0.5f + Vector3.up * -0.1f; break;
            case 3: baseTarget += playerCamera.right * 0.8f + Vector3.up * 0.3f; break;
        }

        return baseTarget;
    }

    public void StopSpawning()
    {
        isSpawning = false;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
        Debug.Log("[BeatMapSpawner] Note spawning stopped!");
    }

    public bool IsSpawningComplete()
    {
        return spawningComplete;
    }
}
