using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Note Settings")]
    public GameObject notePrefab;
    public Transform spawnPoint;
    public Transform targetPoint;

    [Header("Rhythm Settings")]
    public float noteSpeed = 5f;
    public float spawnOffset = 2f;        // 박자보다 몇 초 전에 생성

    [Header("Pattern")]
    public bool autoSpawn = true;
    public int beatsPerNote = 1;          // 몇 박자마다 노트 생성 (1=매 박자)

    private MusicManager musicManager;
    private int lastSpawnBeat = -1;

    void Start()
    {
        musicManager = MusicManager.Instance;

        if (musicManager == null)
        {
            Debug.LogError("MusicManager not found!");
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
        yield return new WaitForSeconds(1f);

        while (true)
        {
            // 현재 박자 체크
            int currentBeat = musicManager.currentBeat;

            // beatsPerNote 간격으로 노트 생성
            if (currentBeat % beatsPerNote == 0 && currentBeat != lastSpawnBeat)
            {
                SpawnNote();
                lastSpawnBeat = currentBeat;
            }

            yield return null; // 매 프레임 체크
        }
    }

    public void SpawnNote()
    {
        GameObject noteObj = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
        Note note = noteObj.GetComponent<Note>();

        if (note != null)
        {
            note.speed = noteSpeed;
            note.targetPosition = targetPoint.position;
        }

        Debug.Log($"Note spawned at beat {musicManager.currentBeat}");
    }
}
