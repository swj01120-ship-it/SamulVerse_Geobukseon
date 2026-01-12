using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "BeatMap", menuName = "Rhythm/BeatMap")]
public class BeatMap : ScriptableObject
{
    public AudioClip audioClip;
    public BeatMapData data;
    public TextAsset jsonFile;

    public VideoClip videoClip;

    public void LoadFromJson()
    {
        if (jsonFile != null)
        {
            data = JsonUtility.FromJson<BeatMapData>(jsonFile.text);
        }
    }

    void OnEnable()
    {
        LoadFromJson();
    }
}
