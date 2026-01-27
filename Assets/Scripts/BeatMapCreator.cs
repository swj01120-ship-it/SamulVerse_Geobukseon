using UnityEngine;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public bool randomizeDrums = true;       // 체크하면 저장 시 자동 랜덤
    public bool avoidConsecutiveSame = true; // 같은 북 연속 방지

    [Header("Timing")]
    public float recordingStartDelay = 3f;   // 녹음 시작 전 대기

    private void Start()
    {
        // AudioSource는 이미 있으면 재사용
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = audioClip;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.pitch = playbackSpeed;
    }

#if UNITY_EDITOR
    private void Update()
    {
        // ✅ 에디터에서만 키보드 입력을 읽는다 (빌드에서 아예 실행 안 됨)

        // R: 녹음 시작/중지
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRecording();
        }

        // Space: 재생/일시정지
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (audioSource != null)
            {
                if (audioSource.isPlaying) audioSource.Pause();
                else audioSource.Play();
            }
        }

        // === 재생 속도 조절 ===
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                playbackSpeed = Mathf.Max(0.25f, playbackSpeed - 0.25f);
                if (audioSource != null) audioSource.pitch = playbackSpeed;
                Debug.Log($"Speed: {playbackSpeed}x");
            }
            if (Input.GetKeyDown(KeyCode.Equals))
            {
                playbackSpeed = Mathf.Min(2f, playbackSpeed + 0.25f);
                if (audioSource != null) audioSource.pitch = playbackSpeed;
                Debug.Log($"Speed: {playbackSpeed}x");
            }
        }

        // === 타임라인 이동 ===
        if (audioSource != null && audioClip != null)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                audioSource.time = Mathf.Max(0, audioSource.time - 5f);

            if (Input.GetKeyDown(KeyCode.RightArrow))
                audioSource.time = Mathf.Min(audioClip.length, audioSource.time + 5f);
        }

        // === 녹음 중 조작 ===
        if (isRecording && audioSource != null && audioSource.isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) currentDrum = 0;
            if (Input.GetKeyDown(KeyCode.Alpha1)) currentDrum = 1;
            if (Input.GetKeyDown(KeyCode.Alpha2)) currentDrum = 2;
            if (Input.GetKeyDown(KeyCode.Alpha3)) currentDrum = 3;

            if (Input.GetKeyDown(KeyCode.O))
            {
                isObstacle = !isObstacle;
                Debug.Log($"Obstacle mode: {isObstacle}");
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                AddNote();
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                RemoveLastNote();
            }

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

    private void OnGUI()
    {
        // ✅ 이것도 에디터에서만 표시됨 (빌드에선 GUI 안 뜸)

        GUIStyle headerStyle = new GUIStyle { fontSize = 24, fontStyle = FontStyle.Bold };
        headerStyle.normal.textColor = Color.white;

        GUIStyle normalStyle = new GUIStyle { fontSize = 20 };
        normalStyle.normal.textColor = Color.white;

        GUIStyle smallStyle = new GUIStyle { fontSize = 16 };
        smallStyle.normal.textColor = Color.yellow;

        GUI.Label(new Rect(10, 10, 600, 30), $"BeatMap Creator - {songName}", headerStyle);

        if (audioClip != null && audioSource != null)
        {
            GUI.Label(new Rect(10, 45, 700, 30),
                $"Time: {audioSource.time:F2}s / {audioClip.length:F2}s | Speed: {playbackSpeed}x",
                normalStyle);
        }

        Color statusColor = isRecording ? Color.red : Color.green;
        normalStyle.normal.textColor = statusColor;

        string drumDisplay = randomizeDrums ? "AUTO" : currentDrum.ToString();
        GUI.Label(new Rect(10, 75, 700, 30),
            $"{(isRecording ? "● RECORDING" : "○ Stopped")} | Drum: {drumDisplay} | {(isObstacle ? "OBSTACLE" : "HIT")}",
            normalStyle);

        normalStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 105, 300, 30), $"Total Notes: {notes.Count}", normalStyle);

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
    }
#endif // UNITY_EDITOR

    private void ToggleRecording()
    {
        isRecording = !isRecording;

        if (audioSource == null || audioClip == null)
        {
            Debug.LogWarning("[BeatMapCreator] audioSource 또는 audioClip이 없습니다.");
            isRecording = false;
            return;
        }

        if (isRecording)
        {
            Debug.Log("=== Recording started! ===");
            if (randomizeDrums) Debug.Log("★ RANDOM DRUM MODE - Just hit Enter on beat!");
            Debug.Log($"★ Starting from {recordingStartDelay}s");

            notes.Clear();
            audioSource.Stop();
            audioSource.time = Mathf.Clamp(recordingStartDelay, 0f, audioClip.length);
            audioSource.Play();
        }
        else
        {
            Debug.Log($"=== Recording stopped! {notes.Count} notes ===");
            audioSource.Stop();
            audioSource.time = 0;
        }
    }

    private void AddNote()
    {
        if (audioSource == null) return;

        float currentTime = audioSource.time;
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

    private void RemoveLastNote()
    {
        if (notes.Count > 0)
        {
            notes.RemoveAt(notes.Count - 1);
            Debug.Log($"Removed last note. Total: {notes.Count}");
        }
    }

    private void DeleteNearestNote()
    {
        if (audioSource == null) return;

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
            notes = new List<NoteData>(notes)
        };

        if (randomizeDrums) RandomizeDrums(data);

        string json = JsonUtility.ToJson(data, true);

        string folderPath = Path.Combine(Application.dataPath, "Resources/BeatMaps/Manual");
        Directory.CreateDirectory(folderPath);

        string fileName = $"{songName}_Difficulty{difficulty}.json";
        string filePath = Path.Combine(folderPath, fileName);
        File.WriteAllText(filePath, json);

        Debug.Log($"★★★ Saved to: {filePath} ★★★");
        Debug.Log($"Total notes: {data.notes.Count}");
        if (randomizeDrums) Debug.Log("Drums randomized!");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    private void RandomizeDrums(BeatMapData data)
    {
        int lastDrum = -1;

        for (int i = 0; i < data.notes.Count; i++)
        {
            if (data.notes[i].drum == -1)
            {
                int newDrum;

                if (avoidConsecutiveSame && lastDrum != -1)
                {
                    do { newDrum = Random.Range(0, 4); }
                    while (newDrum == lastDrum);
                }
                else
                {
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
}
