using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NoteSpawnerMulti : MonoBehaviour
{
    [Header("Note Settings")]
    public GameObject notePrefab;
    public GameObject[] particlePrefabs;  // 북마다 다른 파티클 (나중에)

    [Header("4개 북 설정")]
    public Transform[] spawnPoints;       // 4개 생성 위치
    public Transform[] targetPoints;      // 4개 북 위치

    [Header("Rhythm Settings")]
    public float noteSpeed = 5f;
    public bool autoSpawn = true;
    public int beatsPerNote = 2;          // 몇 박자마다 노트 생성

    [Header("Pattern Settings")]
    public SpawnPattern pattern = SpawnPattern.Sequential;

    public enum SpawnPattern
    {
        Sequential,    // 순차: 0→1→2→3→0...
        Random,        // 랜덤
        Mirror,        // 거울: 0→1→2→3→3→2→1→0
        Pairs          // 쌍: 0+3, 1+2
    }

    private MusicManager musicManager;
    private int lastSpawnBeat = -1;
    private int currentDrumIndex = 0;
    private int patternDirection = 1;  // Mirror 패턴용

    void Start()
    {
        musicManager = MusicManager.Instance;

        if (musicManager == null)
        {
            Debug.LogError("MusicManager not found!");
            return;
        }

        // SpawnPoints, TargetPoints 검증
        if (spawnPoints.Length != 4 || targetPoints.Length != 4)
        {
            Debug.LogError("Must have exactly 4 spawn points and 4 target points!");
            return;
        }

        if (autoSpawn)
        {
            StartCoroutine(SpawnNotesWithMusic());
        }
    }

    IEnumerator SpawnNotesWithMusic()
    {
        // 음악 시작 대기
        yield return new WaitForSeconds(2f);

        while (true)
        {
            if (musicManager == null) yield break;

            int currentBeat = musicManager.currentBeat;

            // beatsPerNote 간격으로 노트 생성
            if (currentBeat % beatsPerNote == 0 && currentBeat != lastSpawnBeat && currentBeat > 0)
            {
                SpawnNotesByPattern();
                lastSpawnBeat = currentBeat;
            }

            yield return null;
        }
    }

    void SpawnNotesByPattern()
    {
        switch (pattern)
        {
            case SpawnPattern.Sequential:
                SpawnNoteForDrum(currentDrumIndex);
                currentDrumIndex = (currentDrumIndex + 1) % 4;
                break;

            case SpawnPattern.Random:
                int randomDrum = Random.Range(0, 4);
                SpawnNoteForDrum(randomDrum);
                break;

            case SpawnPattern.Mirror:
                SpawnNoteForDrum(currentDrumIndex);
                currentDrumIndex += patternDirection;

                if (currentDrumIndex >= 3)
                {
                    patternDirection = -1;  // 역방향
                }
                else if (currentDrumIndex <= 0)
                {
                    patternDirection = 1;   // 정방향
                }
                break;

            case SpawnPattern.Pairs:
                // 0+3 또는 1+2 쌍으로
                if (currentDrumIndex % 2 == 0)
                {
                    SpawnNoteForDrum(0);
                    SpawnNoteForDrum(3);
                }
                else
                {
                    SpawnNoteForDrum(1);
                    SpawnNoteForDrum(2);
                }
                currentDrumIndex++;
                break;
        }
    }

    void SpawnNoteForDrum(int drumIndex)
    {
        if (drumIndex < 0 || drumIndex >= spawnPoints.Length)
        {
            Debug.LogError($"Invalid drum index: {drumIndex}");
            return;
        }

        // 노트 생성
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

        // 파티클 생성 (나중에 추가)
        // SpawnParticleForDrum(drumIndex);

        Debug.Log($"Note spawned for drum {drumIndex} at beat {musicManager.currentBeat}");
    }

    // 나중에 파티클 추가용
    void SpawnParticleForDrum(int drumIndex)
    {
        if (particlePrefabs != null && drumIndex < particlePrefabs.Length)
        {
            if (particlePrefabs[drumIndex] != null)
            {
                Instantiate(
                    particlePrefabs[drumIndex],
                    spawnPoints[drumIndex].position,
                    Quaternion.identity
                );
            }
        }
    }

    // 난이도 조절용 (나중에)
    public void SetDifficulty(int difficulty)
    {
        switch (difficulty)
        {
            case 0: // 쉬움
                beatsPerNote = 4;
                noteSpeed = 3f;
                pattern = SpawnPattern.Sequential;
                break;
            case 1: // 보통
                beatsPerNote = 2;
                noteSpeed = 5f;
                pattern = SpawnPattern.Sequential;
                break;
            case 2: // 어려움
                beatsPerNote = 1;
                noteSpeed = 6f;
                pattern = SpawnPattern.Random;
                break;
        }
    }
}
