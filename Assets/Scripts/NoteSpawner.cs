using UnityEngine;
using System.Reflection;

public class NoteSpawner : MonoBehaviour
{
    [Header("Points (필수)")]
    public Transform[] spawnPoints;   // 4
    public Transform[] targetPoints;  // 4

    [Header("Prefabs (필수)")]
    public GameObject notePrefab;
    public GameObject obstaclePrefab; // 필요 없으면 비워도 됨

    [Header("Spawn")]
    [Tooltip("true면 MusicManager의 currentBeat을 읽어 비트 기반 스폰 시도. 못 읽으면 시간 기반으로 fallback.")]
    public bool useBeatSync = true;

    [Tooltip("시간 기반 스폰 간격(초). 비트싱크가 안 잡힐 때 이걸로라도 노트가 나오게 함.")]
    public float fallbackSpawnInterval = 1.0f;

    [Tooltip("레인(0~3) 랜덤 스폰")]
    public bool randomLane = true;

    [Header("Note Move")]
    public float noteSpeed = 10f;

    [Header("Debug")]
    public bool verboseLog = true;

    private float lastFallbackSpawnTime = -999f;

    // 비트 동기용(Reflection으로 읽음: currentBeat)
    private object musicManagerInstance;
    private PropertyInfo currentBeatProp;
    private FieldInfo currentBeatField;
    private int lastBeatSpawned = -1;

    private void Start()
    {
        ValidateRefsOrLog();

        if (useBeatSync)
            BindMusicManagerBeatReflection();
    }

    private void Update()
    {
        if (!AreRefsReady()) return;

        if (useBeatSync && TryGetCurrentBeat(out int beat))
        {
            // beat가 변할 때마다 스폰(너무 많으면 여기서 조건 추가하면 됨)
            if (beat != lastBeatSpawned)
            {
                lastBeatSpawned = beat;
                SpawnOneNote();
            }
            return;
        }

        // ✅ fallback: 시간 기반 스폰
        if (Time.time - lastFallbackSpawnTime >= fallbackSpawnInterval)
        {
            lastFallbackSpawnTime = Time.time;
            SpawnOneNote();
        }
    }

    // --------------------
    // Spawn
    // --------------------
    private void SpawnOneNote()
    {
        int lane = randomLane ? Random.Range(0, 4) : 0;

        lane = Mathf.Clamp(lane, 0, 3);

        var sp = spawnPoints[lane];
        var tp = targetPoints[lane];

        if (sp == null || tp == null)
        {
            if (verboseLog) Debug.LogWarning($"[NoteSpawner] lane {lane} spawn/target가 null이라 스폰 스킵");
            return;
        }

        if (notePrefab == null)
        {
            Debug.LogError("[NoteSpawner] notePrefab이 비어있습니다. Inspector에 Note Prefab 넣으세요.");
            return;
        }

        var go = Instantiate(notePrefab, sp.position, sp.rotation);

        // Note 컴포넌트가 있으면 확실히 세팅
        var note = go.GetComponent<Note>();
        if (note != null)
        {
            note.speed = noteSpeed;
            //note.lane = lane;
            //note.SetTargetPosition(tp.position);
        }
        else
        {
            // Note가 없으면 최소 이동기 부착(그래도 화면에 보이게)
            var mover = go.AddComponent<SimpleMover>();
            mover.Init(tp, noteSpeed);
        }

        if (verboseLog)
            Debug.Log($"[NoteSpawner] Spawn Note lane={lane} sp={sp.name} -> tp={tp.name} (beatSync={(useBeatSync ? "ON" : "OFF")})");
    }

    private class SimpleMover : MonoBehaviour
    {
        private Transform target;
        private float speed;

        public void Init(Transform t, float s)
        {
            target = t;
            speed = Mathf.Max(0.01f, s);
        }

        private void Update()
        {
            if (target == null) return;
            Vector3 dir = target.position - transform.position;
            float dist = dir.magnitude;
            if (dist <= 0.05f) { Destroy(gameObject); return; }
            transform.position += dir.normalized * speed * Time.deltaTime;
        }
    }

    // --------------------
    // Beat reflection
    // --------------------
    private void BindMusicManagerBeatReflection()
    {
        // 씬에서 "MusicManager" 타입 이름을 가진 MonoBehaviour 찾기
        foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            if (mb.GetType().Name == "MusicManager")
            {
                musicManagerInstance = mb;
                var t = mb.GetType();

                currentBeatProp = t.GetProperty("currentBeat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                currentBeatField = t.GetField("currentBeat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (verboseLog)
                    Debug.Log($"[NoteSpawner] MusicManager bind OK. prop={(currentBeatProp != null)} field={(currentBeatField != null)}");

                return;
            }
        }

        if (verboseLog)
            Debug.LogWarning("[NoteSpawner] MusicManager를 못 찾아서 비트 싱크 불가 → 시간 기반 스폰으로 fallback 합니다.");
    }

    private bool TryGetCurrentBeat(out int beat)
    {
        beat = 0;
        if (musicManagerInstance == null) return false;

        try
        {
            if (currentBeatProp != null)
            {
                object v = currentBeatProp.GetValue(musicManagerInstance);
                if (v is int i) { beat = i; return true; }
            }

            if (currentBeatField != null)
            {
                object v = currentBeatField.GetValue(musicManagerInstance);
                if (v is int i) { beat = i; return true; }
            }
        }
        catch { }

        return false;
    }

    // --------------------
    // Validation
    // --------------------
    private void ValidateRefsOrLog()
    {
        if (notePrefab == null)
            Debug.LogError("[NoteSpawner] notePrefab이 비었습니다. Inspector에서 반드시 넣으세요.");

        if (spawnPoints == null || spawnPoints.Length < 4)
            Debug.LogError("[NoteSpawner] spawnPoints가 4개가 아닙니다. (0~3 레인)");

        if (targetPoints == null || targetPoints.Length < 4)
            Debug.LogError("[NoteSpawner] targetPoints가 4개가 아닙니다. (0~3 레인)");

        if (verboseLog && AreRefsReady())
            Debug.Log("[NoteSpawner] References OK.");
    }

    private bool AreRefsReady()
    {
        if (notePrefab == null) return false;
        if (spawnPoints == null || spawnPoints.Length < 4) return false;
        if (targetPoints == null || targetPoints.Length < 4) return false;

        // 4개 중 하나라도 null이면 노트가 특정 레인에서 “절대” 안 나옴
        for (int i = 0; i < 4; i++)
        {
            if (spawnPoints[i] == null) return false;
            if (targetPoints[i] == null) return false;
        }
        return true;
    }
}
