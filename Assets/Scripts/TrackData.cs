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
    public AudioClip audioClip;

    [Header("영상")]
    public VideoClip previewVideo;
    public VideoClip gameVideo;

    [Header("비트맵 (JSON)")]
    [Tooltip("이 곡에서 사용할 비트맵 JSON(TextAsset)을 넣으세요.")]
    public TextAsset beatmapJson;

    [Header("썸네일")]
    public Sprite thumbnail;
}
