using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class BeatMapCreator : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip audioClip;
    private AudioSource audioSource;

    [Header("BeatMap Data")]
    public string songName = "Song1";
    public float bpm = 103f;
    public int difficulty = 1;  // 0=Easy, 1=Normal, 2=Hard
    public List<NoteData> notes = new List<NoteData>();

    [Header("Recording")]
    public bool isRecording = false;

    [Header("Current Selection")]
    public int currentDrum = 0;  // 0~3
    public bool isObstacle = false;

    [Header("Playback")]
    public float playbackSpeed = 1f;

    [Header("Random Settings")]
    public bool randomizeDrums = true;  // 체크하면 저장 시 자동 랜덤
    public bool avoidConsecutiveSame = true;  // 같은 북 연속 방지

    [Header("Timing")]
    public float recordingStartDelay = 3f;  // 녹음 시작 전 대기

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    void Update()
    {
        // === 기본 조작 ===

        // R: 녹음 시작/중지
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRecording();
        }

        // Space: 재생/일시정지
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (audioSource.isPlaying)
                audioSource.Pause();
            else
                audioSource.Play();
        }

        // === 재생 속도 조절 ===
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                playbackSpeed = Mathf.Max(0.25f, playbackSpeed - 0.25f);
                audioSource.pitch = playbackSpeed;
                Debug.Log($"Speed: {playbackSpeed}x");
            }
            if (Input.GetKeyDown(KeyCode.Equals))
            {
                playbackSpeed = Mathf.Min(2f, playbackSpeed + 0.25f);
                audioSource.pitch = playbackSpeed;
                Debug.Log($"Speed: {playbackSpeed}x");
            }
        }

        // === 타임라인 이동 ===
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            audioSource.time = Mathf.Max(0, audioSource.time - 5f);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            audioSource.time = Mathf.Min(audioClip.length, audioSource.time + 5f);

        // === 녹음 중 조작 ===
        if (isRecording && audioSource.isPlaying)
        {
            // 숫자 키 0~3: 북 선택 (randomizeDrums가 꺼져있을 때만 의미 있음)
            if (Input.GetKeyDown(KeyCode.Alpha0)) currentDrum = 0;
            if (Input.GetKeyDown(KeyCode.Alpha1)) currentDrum = 1;
            if (Input.GetKeyDown(KeyCode.Alpha2)) currentDrum = 2;
            if (Input.GetKeyDown(KeyCode.Alpha3)) currentDrum = 3;

            // O: 장애물 모드 토글
            if (Input.GetKeyDown(KeyCode.O))
            {
                isObstacle = !isObstacle;
                Debug.Log($"Obstacle mode: {isObstacle}");
            }

            // Enter: 노트 추가
            if (Input.GetKeyDown(KeyCode.Return))
            {
                AddNote();
            }

            // Backspace: 마지막 노트 삭제
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                RemoveLastNote();
            }

            // Delete: 가장 가까운 노트 삭제
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                DeleteNearestNote();
            }
        }

        // === 저장 ===
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
        {
            SaveBeatMap();
        }
    }

    void ToggleRecording()
    {
        isRecording = !isRecording;

        if (isRecording)
        {
            Debug.Log("=== Recording started! ===");
            if (randomizeDrums)
            {
                Debug.Log("★ RANDOM DRUM MODE - Just hit Enter on beat!");
            }
            Debug.Log($"★ Starting from {recordingStartDelay}s");

            notes.Clear();
            audioSource.Stop();
            audioSource.time = recordingStartDelay;  // ★ 3초부터 시작
            audioSource.Play();
        }
        else
        {
            Debug.Log($"=== Recording stopped! {notes.Count} notes ===");
            audioSource.Stop();
            audioSource.time = 0;
        }
    }

    void AddNote()
    {
        float currentTime = audioSource.time;

        // randomizeDrums가 켜져있으면 drum은 -1 (나중에 랜덤)
        int drumToSave = randomizeDrums ? -1 : currentDrum;

        NoteData newNote = new NoteData
        {
            time = currentTime,
            drum = drumToSave,
            type = isObstacle ? "obstacle" : "hit"
        };

        notes.Add(newNote);
        notes.Sort((a, b) => a.time.CompareTo(b.time));

        string drumInfo = randomizeDrums ? "RANDOM" : drumToSave.ToString();
        Debug.Log($"[{notes.Count}] {newNote.type} at {currentTime:F2}s (drum: {drumInfo})");
    }

    void RemoveLastNote()
    {
        if (notes.Count > 0)
        {
            notes.RemoveAt(notes.Count - 1);
            Debug.Log($"Removed last note. Total: {notes.Count}");
        }
    }

    void DeleteNearestNote()
    {
        float currentTime = audioSource.time;
        float minDistance = float.MaxValue;
        int indexToRemove = -1;

        for (int i = 0; i < notes.Count; i++)
        {
            float distance = Mathf.Abs(notes[i].time - currentTime);
            if (distance < minDistance && distance < 0.5f)
            {
                minDistance = distance;
                indexToRemove = i;
            }
        }

        if (indexToRemove >= 0)
        {
            notes.RemoveAt(indexToRemove);
            Debug.Log($"Deleted note at {indexToRemove}. Total: {notes.Count}");
        }
    }

    public void SaveBeatMap()
    {
        BeatMapData data = new BeatMapData
        {
            bpm = bpm,
            songName = songName,
            difficulty = difficulty,
            notes = new List<NoteData>(notes) // 복사본 생성
        };

        // 랜덤화 적용 (켜져있을 때만)
        if (randomizeDrums)
        {
            RandomizeDrums(data);
        }

        string json = JsonUtility.ToJson(data, true);

        // 폴더 생성
        string folderPath = Path.Combine(Application.dataPath, "Resources/BeatMaps/Manual");
        Directory.CreateDirectory(folderPath);

        // 저장
        string fileName = $"{songName}_Difficulty{difficulty}.json";
        string filePath = Path.Combine(folderPath, fileName);
        File.WriteAllText(filePath, json);

        Debug.Log($"★★★ Saved to: {filePath} ★★★");
        Debug.Log($"Total notes: {data.notes.Count}");
        if (randomizeDrums)
        {
            Debug.Log("Drums randomized!");
        }

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    void RandomizeDrums(BeatMapData data)
    {
        int lastDrum = -1;

        for (int i = 0; i < data.notes.Count; i++)
        {
            if (data.notes[i].drum == -1) // 랜덤이 필요한 노트
            {
                int newDrum;

                if (avoidConsecutiveSame && lastDrum != -1)
                {
                    // 이전 북과 다른 북 선택
                    do
                    {
                        newDrum = Random.Range(0, 4);
                    }
                    while (newDrum == lastDrum);
                }
                else
                {
                    // 완전 랜덤
                    newDrum = Random.Range(0, 4);
                }

                data.notes[i].drum = newDrum;
                lastDrum = newDrum;
            }
            else
            {
                lastDrum = data.notes[i].drum;
            }
        }
    }

    void OnGUI()
    {
        // 스타일 정의
        GUIStyle headerStyle = new GUIStyle();
        headerStyle.fontSize = 24;
        headerStyle.normal.textColor = Color.white;
        headerStyle.fontStyle = FontStyle.Bold;

        GUIStyle normalStyle = new GUIStyle();
        normalStyle.fontSize = 20;
        normalStyle.normal.textColor = Color.white;

        GUIStyle smallStyle = new GUIStyle();
        smallStyle.fontSize = 16;
        smallStyle.normal.textColor = Color.yellow;

        // === 헤더 ===
        GUI.Label(new Rect(10, 10, 600, 30),
            $"BeatMap Creator - {songName}", headerStyle);

        // === 타임라인 ===
        if (audioClip != null)
        {
            GUI.Label(new Rect(10, 45, 500, 30),
                $"Time: {audioSource.time:F2}s / {audioClip.length:F2}s | Speed: {playbackSpeed}x",
                normalStyle);
        }

        // === 현재 상태 ===
        Color statusColor = isRecording ? Color.red : Color.green;
        normalStyle.normal.textColor = statusColor;

        string drumDisplay = randomizeDrums ? "AUTO" : currentDrum.ToString();
        GUI.Label(new Rect(10, 75, 500, 30),
            $"{(isRecording ? "● RECORDING" : "○ Stopped")} | Drum: {drumDisplay} | {(isObstacle ? "OBSTACLE" : "HIT")}",
            normalStyle);

        normalStyle.normal.textColor = Color.white;

        // === 노트 카운트 ===
        GUI.Label(new Rect(10, 105, 300, 30),
            $"Total Notes: {notes.Count}", normalStyle);

        // === 랜덤 모드 표시 ===
        if (randomizeDrums)
        {
            GUIStyle randomStyle = new GUIStyle();
            randomStyle.fontSize = 22;
            randomStyle.normal.textColor = Color.yellow;
            randomStyle.fontStyle = FontStyle.Bold;

            GUI.Label(new Rect(10, 135, 600, 30),
                "★ RANDOM DRUM MODE ★ (Just hit Enter!)", randomStyle);
        }

        // === 조작법 ===
        int controlsY = randomizeDrums ? 170 : 150;
        GUI.Label(new Rect(10, controlsY, 600, 30), "Controls:", headerStyle);

        string controls =
            "R: Start/Stop Recording\n" +
            "Space: Play/Pause\n" +
            "← / →: Skip 5 seconds\n" +
            "Shift + - / +: Change Speed\n\n" +

            "While Recording:\n" +
            (randomizeDrums ? "" : "0-3: Select Drum\n") +
            "O: Toggle Obstacle Mode\n" +
            "Enter: Add Note\n" +
            "Backspace: Remove Last Note\n" +
            "Delete: Remove Nearest Note\n\n" +

            "Ctrl+S: Save BeatMap";

        GUI.Label(new Rect(10, controlsY + 35, 600, 400), controls, smallStyle);

        // === 최근 노트 표시 ===
        if (notes.Count > 0)
        {
            GUI.Label(new Rect(650, 10, 400, 30), "Recent Notes:", headerStyle);

            int displayCount = Mathf.Min(15, notes.Count);
            for (int i = notes.Count - displayCount; i < notes.Count; i++)
            {
                NoteData note = notes[i];
                Color noteColor = (note.type == "obstacle") ? Color.red : Color.green;
                smallStyle.normal.textColor = noteColor;

                string drumText = (note.drum == -1) ? "RND" : note.drum.ToString();

                int yPos = 45 + (i - (notes.Count - displayCount)) * 25;
                GUI.Label(new Rect(650, yPos, 400, 25),
                    $"{i}: {note.time:F2}s - Drum {drumText} - {note.type}",
                    smallStyle);
            }
        }
    }
}