using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoteData
{
    public float time;          // 노래 시작 후 몇 초
    public int drum;            // 0~3 (어느 북)
    public string type;         // "hit" 또는 "obstacle"
}

[System.Serializable]
public class BeatMapData
{
    public float bpm;
    public string songName;
    public int difficulty;      // 0=Easy, 1=Normal, 2=Hard
    public List<NoteData> notes = new List<NoteData>();
}
