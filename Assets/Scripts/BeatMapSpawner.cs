using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Obstacle Offset (Variety)")]
    public bool enableObstacleOffset = true;

    [Tooltip("장애물 스폰 위치 좌/우 범위 (spawnPoint의 right 기준, 미터)")]
    public float obstacleSpawnX = 1.2f;

    [Tooltip("장애물 스폰 위치 상/하 범위 (spawnPoint의 up 기준, 미터)")]
    public float obstacleSpawnYUp = 0.6f;
    public float obstacleSpawnYDown = 0.4f;

    [Tooltip("장애물이 향하는 타겟(머리/타겟포인트) 분산 좌/우 범위 (targetPoint의 right 기준, 미터)")]
    public float obstacleTargetX = 0.6f;

    [Tooltip("장애물이 향하는 타겟 분산 상/하 범위 (targetPoint의 up 기준, 미터)")]
    public float obstacleTargetYUp = 0.3f;
    public float obstacleTargetYDown = 0.2f;

    [Tooltip("타겟 오프셋이 너무 중앙이면 다시 뽑기(피하기 너무 빡센 중앙 직격 방지). 0이면 끔")]
    public float targetDeadZoneRadius = 0.25f;

    [Tooltip("각 lane의 스폰 포인트 기준으로 오프셋을 줄지(권장). 끄면 월드축(X,Y) 기준")]
    public bool useLocalAxesFromPoints = true;


    [Header("Debug")]
    public bool showDebugInfo = true;

    [Header("Scene Guard")]
    [Tooltip("이 씬에서만 스폰 동작(다른 씬(Home/Tutorial)에서 오류 방지)")]
    public string onlyRunInSceneName = "MainGame";

    // ===== JSON 전용 모델 =====
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

    private static readonly string[] DRUM_TYPES = { "Jung", "Jang", "Book", "Jing" };

    // ===============================
    // ✅ 기존 프로젝트 호환 API
    // ===============================
    public void BeginSpawn() => HandleSongStart();
    public bool IsSpawningComplete() => _spawningComplete;

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

    private bool IsAllowedScene()
    {
        if (string.IsNullOrEmpty(onlyRunInSceneName)) return true;
        return SceneManager.GetActiveScene().name == onlyRunInSceneName;
    }

    private void OnEnable()
    {
        // ✅ Home/Tutorial 등에서는 아예 구독/동작 안 함
        if (!IsAllowedScene()) return;

        MainGameAutoStartController.OnSongStart -= HandleSongStart;
        MainGameAutoStartController.OnSongStart += HandleSongStart;

        if (MainGameAutoStartController.SongStarted)
            HandleSongStart();
    }

    private void OnDisable()
    {
        if (!IsAllowedScene()) return;

        MainGameAutoStartController.OnSongStart -= HandleSongStart;
        StopSpawning();
    }

    private void Start()
    {
        // ✅ Home/Tutorial에서 오류 방지
        if (!IsAllowedScene()) return;

        // ✅ Inspector에 비트맵이 비어있으면 TrackSession에서 자동 주입
        AutoInjectBeatMapIfNeeded();

        if (beatMapJson != null) LoadBeatMap();
    }

    private void AutoInjectBeatMapIfNeeded()
    {
        if (beatMapJson != null) return;

        if (TrackSession.Instance != null && TrackSession.Instance.SelectedTrack != null)
        {
            beatMapJson = TrackSession.Instance.SelectedTrack.beatmapJson;

            if (showDebugInfo)
                Debug.Log($"[BeatMapSpawner] beatMapJson auto-set from TrackSession: {(beatMapJson != null ? beatMapJson.name : "NULL")}");
        }
        else
        {
            if (showDebugInfo)
                Debug.LogWarning("[BeatMapSpawner] TrackSession/SelectedTrack 없음 (아직 주입 불가)");
        }
    }

    private void HandleSongStart()
    {
        // ✅ Home/Tutorial에서 절대 시작하지 않게
        if (!IsAllowedScene()) return;

        if (_started) return;

        // ✅ 시작 시점/비트맵 자동 주입 보강
        AutoInjectBeatMapIfNeeded();

        _dspStart = MainGameAutoStartController.SongStartDspTime;
        if (_dspStart <= 0) _dspStart = AudioSettings.dspTime;

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

        // ===== 기본 스폰/타겟 =====
        Vector3 spawnPos = sp.position;
        Quaternion spawnRot = sp.rotation;
        Vector3 targetPos = tp.position;

        // ===== 장애물만 다양화 =====
        if (isObstacle && enableObstacleOffset)
        {
            // 축 기준(로컬: 포인트의 right/up vs 월드 X/Y)
            Vector3 rightAxis = useLocalAxesFromPoints ? sp.right : Vector3.right;
            Vector3 upAxis = useLocalAxesFromPoints ? sp.up : Vector3.up;

            // 1) 스폰 위치 오프셋(장애물 출발점 흔들기)
            float sx = UnityEngine.Random.Range(-obstacleSpawnX, obstacleSpawnX);
            float sy = UnityEngine.Random.Range(-obstacleSpawnYDown, obstacleSpawnYUp);
            spawnPos += rightAxis * sx + upAxis * sy;

            // 타겟 쪽 축은 targetPoint 기준이 더 자연스러움
            Vector3 tRightAxis = useLocalAxesFromPoints ? tp.right : Vector3.right;
            Vector3 tUpAxis = useLocalAxesFromPoints ? tp.up : Vector3.up;

            // 2) 타겟 오프셋(너무 중앙 직격 방지 포함)
            Vector2 tOffset;
            int safety = 0;
            do
            {
                float tx = UnityEngine.Random.Range(-obstacleTargetX, obstacleTargetX);
                float ty = UnityEngine.Random.Range(-obstacleTargetYDown, obstacleTargetYUp);
                tOffset = new Vector2(tx, ty);

                safety++;
                if (safety > 30) break; // 무한루프 방지
            }
            while (targetDeadZoneRadius > 0f && tOffset.magnitude < targetDeadZoneRadius);

            targetPos += tRightAxis * tOffset.x + tUpAxis * tOffset.y;

            // (선택) 장애물이 진행방향을 바라보게 하고 싶으면 회전도 맞출 수 있음
            // 단, 프리팹 로컬축이 예민하면 꺼두는 게 안전해서 기본은 그대로 둠.
            // Vector3 dir = (targetPos - spawnPos);
            // if (dir.sqrMagnitude > 0.0001f) spawnRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        var go = Instantiate(prefab, spawnPos, spawnRot);

        string drumType = GetDrumType(lane);

        // 프로젝트마다 다를 수 있으니 "있으면 세팅"
        TrySet(go, "targetPoint", tp);
        TrySet(go, "target", tp);

        // ⭐ 여기 중요: Obstacle.cs는 Vector3 targetPosition을 사용함 :contentReference[oaicite:2]{index=2}
        // 그래서 장애물 타겟 오프셋이 적용되려면 tp.position 대신 targetPos를 넣어야 함.
        TrySet(go, "targetPosition", targetPos);

        TrySet(go, "speed", noteSpeed);
        TrySet(go, "moveSpeed", noteSpeed);
        TrySet(go, "noteSpeed", noteSpeed);
        TrySet(go, "drumType", drumType);

        if (showDebugInfo)
            Debug.Log($"✅ [BeatMapSpawner] Spawn {(isObstacle ? "Obstacle" : "Note")} lane={lane} drumType={drumType} hitTime={n.time:F2}");
    }


    private string GetDrumType(int drumIndex)
    {
        if (drumIndex >= 0 && drumIndex < DRUM_TYPES.Length)
            return DRUM_TYPES[drumIndex];

        Debug.LogWarning($"[BeatMapSpawner] Invalid drumIndex={drumIndex}, defaulting to Jung");
        return "Jung";
    }

    private static void TrySet(GameObject go, string memberName, object value)
    {
        if (go == null || value == null) return; // ✅ null 방어(크래시 방지)

        var valueType = value.GetType();

        foreach (var c in go.GetComponents<MonoBehaviour>())
        {
            if (c == null) continue;
            var t = c.GetType();

            var f = t.GetField(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && f.FieldType.IsAssignableFrom(valueType))
            {
                f.SetValue(c, value);
                return;
            }

            var p = t.GetProperty(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p != null && p.CanWrite && p.PropertyType.IsAssignableFrom(valueType))
            {
                p.SetValue(c, value, null);
                return;
            }
        }
    }
}
