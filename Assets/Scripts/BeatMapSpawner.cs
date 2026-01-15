using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatMapSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject notePrefab;
    public GameObject obstaclePrefab;

    [Header("Note Spawn/Target Points")]
    public Transform[] spawnPoints;
    public Transform[] targetPoints;

    [Header("BeatMap (JSON)")]
    public TextAsset beatMapJson;

    [Header("Movement/Timing")]
    public float noteSpeed = 5f;
    [Tooltip("노트 hit 시간보다 몇 초 먼저 스폰할지")]
    public float spawnOffset = 2f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // ===== JSON 전용 모델 (외부 클래스 의존 제거) =====
    [Serializable]
    private class BeatMapJsonData
    {
        public string songName;
        public int difficulty;
        public float bpm;
        public List<BeatMapJsonNote> notes;
    }

    [Serializable]
    private class BeatMapJsonNote
    {
        public float time;   // hit time (sec)
        public int drum;     // lane index
        public string type;  // "note" / "obstacle"
    }

    private BeatMapJsonData _data;
    private int _cursor;
    private bool _started;
    private bool _spawningComplete;
    private Coroutine _co;

    private double _dspStart = -1;

    // ⭐ drumType 매핑
    private static readonly string[] DRUM_TYPES = { "Jung", "Jang", "Book", "Jing" };

    // ===============================
    // ✅ 기존 프로젝트 호환 API
    // ===============================

    public void BeginSpawn()
    {
        HandleSongStart();
    }

    public bool IsSpawningComplete()
    {
        return _spawningComplete;
    }

    public void SetBeatMap(TextAsset newBeatMap)
    {
        beatMapJson = newBeatMap;
        LoadBeatMap();
    }

    public void StopSpawning()
    {
        _started = false;
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }
    }

    // ===============================

    private void OnEnable()
    {
        MainGameAutoStartController.OnSongStart -= HandleSongStart;
        MainGameAutoStartController.OnSongStart += HandleSongStart;

        if (MainGameAutoStartController.SongStarted)
            HandleSongStart();
    }

    private void OnDisable()
    {
        MainGameAutoStartController.OnSongStart -= HandleSongStart;
        StopSpawning();
    }

    private void Start()
    {
        if (beatMapJson != null) LoadBeatMap();
    }

    private void HandleSongStart()
    {
        if (_started) return;

        _dspStart = MainGameAutoStartController.SongStartDspTime;
        if (_dspStart <= 0)
        {
            _dspStart = AudioSettings.dspTime;
        }

        if (_data == null || _data.notes == null || _data.notes.Count == 0)
            LoadBeatMap();

        if (_data == null || _data.notes == null || _data.notes.Count == 0)
        {
            Debug.LogError("[BeatMapSpawner] BeatMap notes 비어있음. (JSON 파싱 실패/파일 미지정)");
            return;
        }

        _started = true;
        _spawningComplete = false;
        _cursor = 0;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoSpawnLoop());

        if (showDebugInfo)
            Debug.Log($"[BeatMapSpawner] START notes={_data.notes.Count}, dspStart={_dspStart:F4}, spawnOffset={spawnOffset:F2}");
    }

    private void LoadBeatMap()
    {
        _data = null;
        _cursor = 0;
        _spawningComplete = false;

        if (beatMapJson == null)
        {
            if (showDebugInfo) Debug.LogWarning("[BeatMapSpawner] beatMapJson is null (아직 주입 안 됨)");
            return;
        }

        string raw = beatMapJson.text;
        if (string.IsNullOrWhiteSpace(raw))
        {
            Debug.LogError($"[BeatMapSpawner] BeatMap JSON empty: {beatMapJson.name}");
            return;
        }

        try
        {
            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith("["))
            {
                raw = "{\"notes\":" + raw + "}";
            }

            _data = JsonUtility.FromJson<BeatMapJsonData>(raw);

            if (_data == null || _data.notes == null)
            {
                Debug.LogError($"[BeatMapSpawner] JSON parse failed: {beatMapJson.name}");
                _data = null;
                return;
            }

            _data.notes.Sort((a, b) => a.time.CompareTo(b.time));

            if (showDebugInfo)
                Debug.Log($"[BeatMapSpawner] Loaded: {beatMapJson.name} / notes={_data.notes.Count} / first={(_data.notes.Count > 0 ? _data.notes[0].time.ToString("F2") : "n/a")}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[BeatMapSpawner] LoadBeatMap exception: {e}");
            _data = null;
        }
    }

    private IEnumerator CoSpawnLoop()
    {
        while (_started && _data != null && _cursor < _data.notes.Count)
        {
            double elapsed = AudioSettings.dspTime - _dspStart;
            var n = _data.notes[_cursor];

            double spawnTime = Math.Max(0.0, n.time - spawnOffset);

            if (elapsed >= spawnTime)
            {
                SpawnFromJson(n);
                _cursor++;
                continue;
            }

            yield return null;
        }

        _spawningComplete = true;

        if (showDebugInfo)
            Debug.Log("[BeatMapSpawner] SpawnLoop finished.");
        _co = null;
    }

    private void SpawnFromJson(BeatMapJsonNote n)
    {
        if (spawnPoints == null || targetPoints == null || spawnPoints.Length == 0 || targetPoints.Length == 0)
        {
            Debug.LogError("[BeatMapSpawner] spawnPoints/targetPoints 비어있음");
            return;
        }

        int max = Mathf.Min(spawnPoints.Length, targetPoints.Length);
        int lane = Mathf.Clamp(n.drum, 0, max - 1);

        bool isObstacle = !string.IsNullOrEmpty(n.type) && n.type.ToLower().Contains("obstacle");
        GameObject prefab = isObstacle ? obstaclePrefab : notePrefab;

        if (prefab == null)
        {
            Debug.LogError("[BeatMapSpawner] notePrefab/obstaclePrefab 미지정");
            return;
        }

        Transform sp = spawnPoints[lane];
        Transform tp = targetPoints[lane];

        var go = Instantiate(prefab, sp.position, sp.rotation);

        // ⭐ drumType 설정 (0=Jung, 1=Jang, 2=Book, 3=Jing)
        string drumType = GetDrumType(lane);

        // 프로젝트마다 Note 스크립트 필드명이 다를 수 있어 "있으면 세팅" 방식
        TrySet(go, "targetPoint", tp);
        TrySet(go, "target", tp);
        TrySet(go, "targetPosition", tp.position);
        TrySet(go, "speed", noteSpeed);
        TrySet(go, "moveSpeed", noteSpeed);
        TrySet(go, "noteSpeed", noteSpeed);
        TrySet(go, "drumType", drumType); // ⭐ drumType 설정 추가!

        if (showDebugInfo)
            Debug.Log($"✅ [BeatMapSpawner] Spawn {(isObstacle ? "Obstacle" : "Note")} lane={lane} drumType={drumType} hitTime={n.time:F2}");
    }

    // ⭐ drumType 반환 메서드
    private string GetDrumType(int drumIndex)
    {
        if (drumIndex >= 0 && drumIndex < DRUM_TYPES.Length)
        {
            return DRUM_TYPES[drumIndex];
        }

        Debug.LogWarning($"[BeatMapSpawner] Invalid drumIndex={drumIndex}, defaulting to Jung");
        return "Jung";
    }

    private static void TrySet(GameObject go, string memberName, object value)
    {
        foreach (var c in go.GetComponents<MonoBehaviour>())
        {
            if (c == null) continue;
            var t = c.GetType();

            var f = t.GetField(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && f.FieldType.IsAssignableFrom(value.GetType()))
            {
                f.SetValue(c, value);
                return;
            }

            var p = t.GetProperty(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p != null && p.CanWrite && p.PropertyType.IsAssignableFrom(value.GetType()))
            {
                p.SetValue(c, value, null);
                return;
            }
        }
    }
}