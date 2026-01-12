using UnityEngine;

public class JudgementVfxController : MonoBehaviour
{
    public enum Judgement { Perfect, Good, Miss }

    [Header("Hit_04 Children (Drag these)")]
    public GameObject lightRays;
    public GameObject shockwaveLeft;
    public GameObject shockwaveRight;
    public GameObject particles;

    [Header("Auto Cache")]
    public ParticleSystem[] allSystems;

    void Awake()
    {
        if (allSystems == null || allSystems.Length == 0)
            allSystems = GetComponentsInChildren<ParticleSystem>(true);

        // 시작 시 한번 터지는 문제 방지(PlayOnAwake가 켜져 있어도 강제로 정지/클리어)
        StopAndClearAll();

        // 시작 시 불필요한 활성화로 인해 OnEnable때 터질 수도 있어서 기본 비활성 권장
        SetActiveSafe(lightRays, false);
        SetActiveSafe(shockwaveLeft, false);
        SetActiveSafe(shockwaveRight, false);
        SetActiveSafe(particles, false);
    }

    void OnEnable()
    {
        // PlayOnAwake가 남아있거나 재활성 시 다시 터지는 걸 방지
        StopAndClearAll();
    }

    public void Play(Judgement j)
    {
        // 판정별 ON/OFF
        SetActiveSafe(lightRays, j == Judgement.Perfect);
        SetActiveSafe(shockwaveLeft, j == Judgement.Perfect || j == Judgement.Good);
        SetActiveSafe(shockwaveRight, j == Judgement.Perfect || j == Judgement.Good);
        SetActiveSafe(particles, true);

        // ⭐ 핵심: 매 타격마다 확실히 다시 터지게 Stop+Clear 후 Play
        foreach (var ps in allSystems)
        {
            if (ps == null) continue;
            if (!ps.gameObject.activeInHierarchy) continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    void StopAndClearAll()
    {
        if (allSystems == null) return;

        foreach (var ps in allSystems)
        {
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    static void SetActiveSafe(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf != active) go.SetActive(active);
    }
}
