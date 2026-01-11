using UnityEngine;

public class JudgementVfxController : MonoBehaviour
{
    public enum Judgement { Perfect, Good, Miss }

    [Header("Sub Objects (Drag the child objects)")]
    public GameObject lightRays;
    public GameObject shockwaveLeft;
    public GameObject shockwaveRight;
    public GameObject particles;

    [Header("Particle Systems (auto-find if empty)")]
    public ParticleSystem[] allSystems;

    [Header("Quest Friendly Defaults")]
    [Tooltip("씬 시작 시 자동 재생 방지")]
    public bool stopOnAwake = true;

    void Awake()
    {
        if (allSystems == null || allSystems.Length == 0)
            allSystems = GetComponentsInChildren<ParticleSystem>(true);

        if (stopOnAwake)
        {
            foreach (var ps in allSystems)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    public void Play(Judgement judgement)
    {
        // 1) 판정별로 서브 오브젝트 ON/OFF
        // Perfect: LightRays + Shockwave + Particles
        // Good: Shockwave + Particles
        // Miss: Particles만 (약하게 보일 예정)
        SetActiveSafe(lightRays, judgement == Judgement.Perfect);
        SetActiveSafe(shockwaveLeft, judgement == Judgement.Perfect || judgement == Judgement.Good);
        SetActiveSafe(shockwaveRight, judgement == Judgement.Perfect || judgement == Judgement.Good);
        SetActiveSafe(particles, true);

        // 2) 활성화된 시스템만 재생
        foreach (var ps in allSystems)
        {
            if (ps == null) continue;
            if (!ps.gameObject.activeInHierarchy) continue;
            ps.Play(true);
        }
    }

    static void SetActiveSafe(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf != active) go.SetActive(active);
    }
}
