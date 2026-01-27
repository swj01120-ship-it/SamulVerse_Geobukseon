using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AutoBeatMapGenerator : MonoBehaviour
{
    [Header("=== 곡 정보 ===")]
    public string songName = "NewSong";
    public AudioClip audioClip;
    public float bpm = 120f;
    public int difficulty = 1; // 0=Easy, 1=Normal, 2=Hard

    [Header("=== 노트 생성 설정 ===")]
    [Tooltip("곡 시작 후 첫 노트가 나오기까지의 대기 시간 (초)")]
    public float startDelay = 3f;

    [Tooltip("곡 끝나기 몇 초 전까지 노트 생성할지")]
    public float endOffset = 2f;

    [Header("=== 노트 밀도 (박자당 노트 개수) ===")]
    [Tooltip("1박자당 노트 개수 - Easy: 0.5(2박마다), Normal: 1(매박), Hard: 2(반박)")]
    public float notesPerBeat = 1f;

    [Header("=== 장애물 설정 ===")]
    [Tooltip("전체 노트 중 장애물 비율 (0.0 ~ 1.0)")]
    [Range(0f, 1.0f)]
    public float obstacleRatio = 0.1f;

    [Tooltip("최소 장애물 간격 (몇 박자마다 장애물이 나올 수 있는지)")]
    public int minObstacleInterval = 8;

    [Header("=== 드럼 배치 패턴 ===")]
    public DrumPattern drumPattern = DrumPattern.BalancedRandom;

    [Header("=== 고급 설정 ===")]
    [Tooltip("같은 드럼 연속 방지")]
    public bool avoidConsecutiveSame = true;

    [Tooltip("랜덤 시드 (-1이면 매번 다르게)")]
    public int randomSeed = -1;

    public enum DrumPattern
    {
        Random,              // 완전 랜덤
        BalancedRandom,      // 균형잡힌 랜덤 (추천)
        Sequential,          // 0→1→2→3 순차
        Mirror,              // 0→1→2→3→3→2→1→0 거울
        Spiral,              // 0→1→3→2 나선
        LeftRight,           // 0→3→1→2 좌우 교차
        AlternatingPairs     // 0-1, 2-3 쌍 교대
    }

    [ContextMenu("Generate BeatMap")]
    public void GenerateBeatMap()
    {
        if (audioClip == null)
        {
            Debug.LogError("❌ AudioClip이 할당되지 않았습니다!");
            return;
        }

        if (randomSeed >= 0)
        {
            Random.InitState(randomSeed);
            Debug.Log($"🎲 Random Seed: {randomSeed}");
        }

        Debug.Log("=== 🎵 BeatMap 자동 생성 시작 ===");
        Debug.Log($"곡: {songName} | BPM: {bpm} | 길이: {audioClip.length:F2}초");

        // 1. 노트 타이밍 생성
        List<NoteData> notes = GenerateNoteTiming();

        // 2. 장애물 배치
        AssignObstacles(notes);

        // 3. 드럼 배치
        AssignDrums(notes);

        // 4. BeatMapData 생성
        BeatMapData beatMapData = new BeatMapData
        {
            songName = songName,
            bpm = bpm,
            difficulty = difficulty,
            notes = notes
        };

        // 5. 저장
        SaveBeatMap(beatMapData);

        Debug.Log("=== ✅ BeatMap 생성 완료! ===");
        Debug.Log($"총 노트: {notes.Count}개");
        Debug.Log($"Hit 노트: {notes.FindAll(n => n.type == "hit").Count}개");
        Debug.Log($"장애물: {notes.FindAll(n => n.type == "obstacle").Count}개");
    }

    List<NoteData> GenerateNoteTiming()
    {
        List<NoteData> notes = new List<NoteData>();

        float songLength = audioClip.length;
        float beatInterval = 60f / bpm; // 1박자 시간 (초)
        float noteInterval = beatInterval / notesPerBeat; // 노트 간격

        float currentTime = startDelay;
        float endTime = songLength - endOffset;

        Debug.Log($"📊 생성 범위: {startDelay:F2}초 ~ {endTime:F2}초");
        Debug.Log($"박자 간격: {beatInterval:F3}초 | 노트 간격: {noteInterval:F3}초");

        int noteCount = 0;

        while (currentTime < endTime)
        {
            NoteData note = new NoteData
            {
                time = currentTime,
                drum = -1, // 나중에 할당
                type = "hit" // 일단 전부 hit, 나중에 일부를 obstacle로 변경
            };

            notes.Add(note);
            currentTime += noteInterval;
            noteCount++;
        }

        Debug.Log($"✅ {noteCount}개의 노트 타이밍 생성 완료");

        return notes;
    }

    void AssignObstacles(List<NoteData> notes)
    {
        if (obstacleRatio <= 0f)
        {
            Debug.Log("⏩ 장애물 비율 0% - 스킵");
            return;
        }

        int totalNotes = notes.Count;
        int targetObstacleCount = Mathf.RoundToInt(totalNotes * obstacleRatio);

        float beatInterval = 60f / bpm;
        float minInterval = beatInterval * minObstacleInterval;

        // 이론상 최대치(간격만 고려)
        float startTime = notes[0].time;
        float endTime = notes[notes.Count - 1].time;
        float duration = Mathf.Max(0.01f, endTime - startTime);
        int theoreticalMax = Mathf.FloorToInt(duration / minInterval) + 1;

        Debug.Log($"🚧 목표 장애물: {targetObstacleCount}개 ({obstacleRatio * 100f:F1}%)");
        Debug.Log($"🚧 최소 간격: {minInterval:F2}s (minObstacleInterval={minObstacleInterval} beats), 이론상 최대≈{theoreticalMax}개");

        if (targetObstacleCount <= 0) return;

        // 목표치가 이론상 최대보다 크면, 애초에 불가능 → 로그로 튜닝 포인트 보여주기
        if (targetObstacleCount > theoreticalMax)
        {
            Debug.LogWarning($"⚠️ 목표 장애물({targetObstacleCount})이 간격 조건상 불가능합니다. " +
                             $"minObstacleInterval을 줄이거나 obstacleRatio를 낮추세요.");
            targetObstacleCount = theoreticalMax; // 가능한 만큼만이라도 채우기
        }

        // 슬롯 방식: 곡 구간을 targetObstacleCount 만큼 나누고, 각 구간에서 1개씩 찍기
        float slotSize = duration / targetObstacleCount;

        int placed = 0;
        float lastObstacleTime = -999f;

        for (int s = 0; s < targetObstacleCount; s++)
        {
            float slotStart = startTime + slotSize * s;
            float slotEnd = (s == targetObstacleCount - 1) ? endTime : slotStart + slotSize;

            // 슬롯 내부 후보 모으기
            int bestIndex = -1;
            int tries = 12; // 슬롯 안에서 랜덤 시도 횟수 (너무 무겁지 않게)

            for (int t = 0; t < tries; t++)
            {
                int idx = Random.Range(0, notes.Count);
                float time = notes[idx].time;

                if (time < slotStart || time >= slotEnd) continue;
                if (time - lastObstacleTime < minInterval) continue;
                if (notes[idx].type == "obstacle") continue;

                bestIndex = idx;
                break;
            }

            // 랜덤 시도 실패하면, 슬롯 안을 선형으로 훑어서라도 찾기(목표치 채우기 안정화)
            if (bestIndex == -1)
            {
                for (int i = 0; i < notes.Count; i++)
                {
                    float time = notes[i].time;
                    if (time < slotStart || time >= slotEnd) continue;
                    if (time - lastObstacleTime < minInterval) continue;
                    if (notes[i].type == "obstacle") continue;

                    bestIndex = i;
                    break;
                }
            }

            if (bestIndex != -1)
            {
                // 1번 방식 유지: hit를 obstacle로 "대체"
                notes[bestIndex].type = "obstacle";
                lastObstacleTime = notes[bestIndex].time;
                placed++;
            }
        }

        Debug.Log($"✅ 장애물 {placed}개 배치 완료 (요청 {Mathf.RoundToInt(totalNotes * obstacleRatio)}개)");
    }


    void AssignDrums(List<NoteData> notes)
    {
        Debug.Log($"🥁 드럼 배치 패턴: {drumPattern}");

        switch (drumPattern)
        {
            case DrumPattern.Random:
                AssignDrums_Random(notes);
                break;

            case DrumPattern.BalancedRandom:
                AssignDrums_BalancedRandom(notes);
                break;

            case DrumPattern.Sequential:
                AssignDrums_Sequential(notes);
                break;

            case DrumPattern.Mirror:
                AssignDrums_Mirror(notes);
                break;

            case DrumPattern.Spiral:
                AssignDrums_Spiral(notes);
                break;

            case DrumPattern.LeftRight:
                AssignDrums_LeftRight(notes);
                break;

            case DrumPattern.AlternatingPairs:
                AssignDrums_AlternatingPairs(notes);
                break;
        }

        Debug.Log("✅ 드럼 배치 완료");
    }

    void AssignDrums_Random(List<NoteData> notes)
    {
        int lastDrum = -1;

        foreach (NoteData note in notes)
        {
            if (note.type == "hit")
            {
                int newDrum;

                if (avoidConsecutiveSame && lastDrum != -1)
                {
                    do
                    {
                        newDrum = Random.Range(0, 4);
                    }
                    while (newDrum == lastDrum);
                }
                else
                {
                    newDrum = Random.Range(0, 4);
                }

                note.drum = newDrum;
                lastDrum = newDrum;
            }
        }
    }

    void AssignDrums_BalancedRandom(List<NoteData> notes)
    {
        // Hit 노트만 카운트
        int hitCount = 0;
        foreach (NoteData note in notes)
        {
            if (note.type == "hit")
                hitCount++;
        }

        // 각 드럼을 균등하게 분배
        List<int> drumPool = new List<int>();
        int perDrum = hitCount / 4;

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < perDrum; j++)
            {
                drumPool.Add(i);
            }
        }

        // 나머지는 랜덤으로
        while (drumPool.Count < hitCount)
        {
            drumPool.Add(Random.Range(0, 4));
        }

        // 셔플 (Fisher-Yates)
        for (int i = drumPool.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = drumPool[i];
            drumPool[i] = drumPool[randomIndex];
            drumPool[randomIndex] = temp;
        }

        // 적용
        int poolIndex = 0;
        foreach (NoteData note in notes)
        {
            if (note.type == "hit")
            {
                note.drum = drumPool[poolIndex];
                poolIndex++;
            }
        }
    }

    void AssignDrums_Sequential(List<NoteData> notes)
    {
        int drumIndex = 0;

        foreach (NoteData note in notes)
        {
            if (note.type == "hit")
            {
                note.drum = drumIndex;
                drumIndex = (drumIndex + 1) % 4;
            }
        }
    }

    void AssignDrums_Mirror(List<NoteData> notes)
    {
        int drumIndex = 0;
        int direction = 1;

        foreach (NoteData note in notes)
        {
            if (note.type == "hit")
            {
                note.drum = drumIndex;
                drumIndex += direction;

                if (drumIndex >= 3)
                    direction = -1;
                else if (drumIndex <= 0)
                    direction = 1;
            }
        }
    }

    void AssignDrums_Spiral(List<NoteData> notes)
    {
        int[] spiralOrder = { 0, 1, 3, 2 };
        int index = 0;

        foreach (NoteData note in notes)
        {
            if (note.type == "hit")
            {
                note.drum = spiralOrder[index % spiralOrder.Length];
                index++;
            }
        }
    }

    void AssignDrums_LeftRight(List<NoteData> notes)
    {
        int[] pattern = { 0, 3, 1, 2 };
        int index = 0;

        foreach (NoteData note in notes)
        {
            if (note.type == "hit")
            {
                note.drum = pattern[index % pattern.Length];
                index++;
            }
        }
    }

    void AssignDrums_AlternatingPairs(List<NoteData> notes)
    {
        int[][] pairs = new int[][] { new int[] { 0, 1 }, new int[] { 2, 3 } };
        int pairIndex = 0;
        int drumInPair = 0;

        foreach (NoteData note in notes)
        {
            if (note.type == "hit")
            {
                note.drum = pairs[pairIndex][drumInPair];

                drumInPair++;
                if (drumInPair >= 2)
                {
                    drumInPair = 0;
                    pairIndex = (pairIndex + 1) % 2;
                }
            }
        }
    }

    void SaveBeatMap(BeatMapData data)
    {
        string json = JsonUtility.ToJson(data, true);

        // 폴더 생성
        string folderPath = Path.Combine(Application.dataPath, "Resources/BeatMaps/Generated");
        Directory.CreateDirectory(folderPath);

        // 파일명
        string patternName = drumPattern.ToString();
        string fileName = $"{songName}_Difficulty{difficulty}_{patternName}.json";
        string filePath = Path.Combine(folderPath, fileName);

        // 저장
        File.WriteAllText(filePath, json);

        Debug.Log($"💾 저장 완료: {filePath}");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    // GUI로 정보 표시
    void OnGUI()
    {
        if (audioClip == null) return;

        GUIStyle headerStyle = new GUIStyle();
        headerStyle.fontSize = 24;
        headerStyle.normal.textColor = Color.yellow;
        headerStyle.fontStyle = FontStyle.Bold;

        GUIStyle normalStyle = new GUIStyle();
        normalStyle.fontSize = 18;
        normalStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 800, 30),
            "🎵 Auto BeatMap Generator", headerStyle);

        int y = 50;
        GUI.Label(new Rect(10, y, 800, 25),
            $"곡: {songName} | BPM: {bpm} | 길이: {audioClip.length:F1}초", normalStyle);

        y += 30;
        float beatInterval = 60f / bpm;
        float noteInterval = beatInterval / notesPerBeat;
        int estimatedNotes = Mathf.RoundToInt((audioClip.length - startDelay - endOffset) / noteInterval);

        GUI.Label(new Rect(10, y, 800, 25),
            $"예상 노트: {estimatedNotes}개 | 장애물: {Mathf.RoundToInt(estimatedNotes * obstacleRatio)}개", normalStyle);

        y += 30;
        GUI.Label(new Rect(10, y, 800, 25),
            $"패턴: {drumPattern}", normalStyle);

        y += 40;
        normalStyle.normal.textColor = Color.green;
        GUI.Label(new Rect(10, y, 800, 25),
            "우클릭 > Generate BeatMap 실행!", normalStyle);
    }
}