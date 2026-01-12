using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "TrackData", menuName = "Game/Track Data")]
public class TrackData : ScriptableObject
{
    [Header("곡 정보")]
    public string trackName;
    public string artistName;
    public int bpm;
    public string difficulty;

    [Header("음악")]
    public AudioClip audioClip;   // ✅ 추가

    [Header("영상")]
    public VideoClip previewVideo;
    public VideoClip gameVideo;

    [Header("썸네일")]
    public Sprite thumbnail;

    public BeatMap beatMap;   // ✅ 게임씬에서 스폰에 사용할 비트맵

}
