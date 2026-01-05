using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class BeatMapRandomizer : MonoBehaviour
{
    [Header("Input")]
    public TextAsset inputBeatMap;

    [Header("Settings")]
    public bool avoidConsecutiveSame = true;
    public string outputName = "Randomized";

    [Header("Pattern Mode")]
    public PatternMode pattern = PatternMode.Random;

    [Header("Advanced Settings")]
    public int randomSeed = -1;  // -1이면 랜덤, 값 입력하면 고정 패턴

    public enum PatternMode
    {
        Random,              // 완전 랜덤
        Sequential,          // 0→1→2→3→0...
        Mirror,              // 0→1→2→3→3→2→1→0
        LeftRight,           // 0→3→1→2→0→3... (양쪽 번갈아)
        BalancedRandom,      // 랜덤이지만 각 북 균등하게
        AlternatingPairs,    // 0-1, 2-3, 0-1, 2-3...
        Spiral              // 0→1→3→2→0... (나선형)
    }

    [ContextMenu("Randomize BeatMap")]
    public void RandomizeBeatMap()
    {
        if (inputBeatMap == null)
        {
            Debug.LogError("Input BeatMap not assigned!");
            return;
        }

        // 시드 설정
        if (randomSeed >= 0)
        {
            Random.InitState(randomSeed);
            Debug.Log($"Using random seed: {randomSeed}");
        }

        // JSON 로드
        BeatMapData data = JsonUtility.FromJson<BeatMapData>(inputBeatMap.text);

        Debug.Log($"Loaded BeatMap: {data.songName}");
        Debug.Log($"Total notes: {data.notes.Count}");

        // 랜덤화 적용
        switch (pattern)
        {
            case PatternMode.Random:
                ApplyRandomPattern(data);
                break;
            case PatternMode.Sequential:
                ApplySequentialPattern(data);
                break;
            case PatternMode.Mirror:
                ApplyMirrorPattern(data);
                break;
            case PatternMode.LeftRight:
                ApplyLeftRightPattern(data);
                break;
            case PatternMode.BalancedRandom:
                ApplyBalancedRandomPattern(data);
                break;
            case PatternMode.AlternatingPairs:
                ApplyAlternatingPairsPattern(data);
                break;
            case PatternMode.Spiral:
                ApplySpiralPattern(data);
                break;
        }

        // 저장
        SaveRandomized(data);
    }

    void ApplyRandomPattern(BeatMapData data)
    {
        int lastDrum = -1;
        int randomCount = 0;

        foreach (NoteData note in data.notes)
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
                randomCount++;
            }
        }

        Debug.Log($"Applied Random pattern to {randomCount} notes");
    }

    void ApplySequentialPattern(BeatMapData data)
    {
        int drumIndex = 0;
        int count = 0;

        foreach (NoteData note in data.notes)
        {
            if (note.type == "hit")
            {
                note.drum = drumIndex;
                drumIndex = (drumIndex + 1) % 4;
                count++;
            }
        }

        Debug.Log($"Applied Sequential pattern to {count} notes");
    }

    void ApplyMirrorPattern(BeatMapData data)
    {
        int drumIndex = 0;
        int direction = 1;
        int count = 0;

        foreach (NoteData note in data.notes)
        {
            if (note.type == "hit")
            {
                note.drum = drumIndex;
                drumIndex += direction;

                if (drumIndex >= 3)
                    direction = -1;
                else if (drumIndex <= 0)
                    direction = 1;

                count++;
            }
        }

        Debug.Log($"Applied Mirror pattern to {count} notes");
    }

    void ApplyLeftRightPattern(BeatMapData data)
    {
        int[] patternArray = { 0, 3, 1, 2 }; // 왼쪽-오른쪽-중왼-중오
        int index = 0;
        int count = 0;

        foreach (NoteData note in data.notes)
        {
            if (note.type == "hit")
            {
                note.drum = patternArray[index % patternArray.Length];
                index++;
                count++;
            }
        }

        Debug.Log($"Applied LeftRight pattern to {count} notes");
    }

    void ApplyBalancedRandomPattern(BeatMapData data)
    {
        // 각 북을 균등하게 사용
        List<int> drumPool = new List<int>();
        int hitCount = 0;

        foreach (NoteData note in data.notes)
        {
            if (note.type == "hit")
                hitCount++;
        }

        // 각 북을 균등하게 풀에 추가
        int perDrum = hitCount / 4;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < perDrum; j++)
            {
                drumPool.Add(i);
            }
        }

        // 나머지 랜덤으로 채우기
        while (drumPool.Count < hitCount)
        {
            drumPool.Add(Random.Range(0, 4));
        }

        // 섞기 (Fisher-Yates shuffle)
        for (int i = drumPool.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = drumPool[i];
            drumPool[i] = drumPool[randomIndex];
            drumPool[randomIndex] = temp;
        }

        // 적용
        int poolIndex = 0;
        foreach (NoteData note in data.notes)
        {
            if (note.type == "hit")
            {
                note.drum = drumPool[poolIndex];
                poolIndex++;
            }
        }

        Debug.Log($"Applied BalancedRandom pattern to {hitCount} notes");
    }

    void ApplyAlternatingPairsPattern(BeatMapData data)
    {
        int[][] pairs = new int[][] { new int[] { 0, 1 }, new int[] { 2, 3 } };
        int pairIndex = 0;
        int drumInPair = 0;
        int count = 0;

        foreach (NoteData note in data.notes)
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

                count++;
            }
        }

        Debug.Log($"Applied AlternatingPairs pattern to {count} notes");
    }

    void ApplySpiralPattern(BeatMapData data)
    {
        int[] spiralOrder = { 0, 1, 3, 2 }; // 나선형
        int index = 0;
        int count = 0;

        foreach (NoteData note in data.notes)
        {
            if (note.type == "hit")
            {
                note.drum = spiralOrder[index % spiralOrder.Length];
                index++;
                count++;
            }
        }

        Debug.Log($"Applied Spiral pattern to {count} notes");
    }

    void SaveRandomized(BeatMapData data)
    {
        string json = JsonUtility.ToJson(data, true);

        string folderPath = Path.Combine(Application.dataPath, "Resources/BeatMaps/Randomized");
        Directory.CreateDirectory(folderPath);

        string fileName = $"{data.songName}_{outputName}_{pattern}.json";
        string filePath = Path.Combine(folderPath, fileName);

        File.WriteAllText(filePath, json);

        Debug.Log($"★★★ Randomized BeatMap saved ★★★");
        Debug.Log($"File: {filePath}");
        Debug.Log($"Pattern: {pattern}");
        Debug.Log($"Avoid Consecutive: {avoidConsecutiveSame}");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
