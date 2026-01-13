using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "RhythmGame/Track Data", fileName = "TD_NewTrack")]
public class TrackData : ScriptableObject
{
    [Header("곡 정보")]
    public string trackName;
    public string artistName;
    public int bpm = 120;
    public string difficulty = "normal";

    [Header("음악")]
    public AudioClip audioClip;

    [Header("영상")]
    public VideoClip previewVideo;
    public VideoClip gameVideo;

    [Header("썸네일")]
    public Sprite thumbnail;

    [Header("비트맵 (JSON 파일: TextAsset)")]
    public TextAsset beatMap; // ✅ 이제 Resources/Generated json 파일 드래그 가능
}
