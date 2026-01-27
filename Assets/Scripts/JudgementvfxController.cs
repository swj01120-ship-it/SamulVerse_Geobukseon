using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgementVfxController : MonoBehaviour
{

    public enum Judgement { Perfect, Good, Miss }

    [Header("Auto Cache (read only)")]
    [SerializeField] private ParticleSystem[] cachedSystems;
    public ParticleSystem[] Systems => cachedSystems;

    [Header("VFX Prefabs (Perfect=2, Good=2, Miss=1)")]
    public GameObject perfectPrefabA;
    public GameObject perfectPrefabB;
    public GameObject goodPrefabA;
    public GameObject goodPrefabB;
    public GameObject missPrefab;

    [Header("Spawn Points (중요)")]
    public Transform spawnPointA;
    public Transform spawnPointB;
    public Transform spawnPointMiss;

    [Tooltip("공통 스폰(최후 fallback). 보통 TargetPoint")]
    public Transform spawnPoint;

    public Vector3 localOffsetA = Vector3.zero;
    public Vector3 localOffsetB = Vector3.zero;
    public Vector3 localOffsetMiss = Vector3.zero;

    [Tooltip("true면 스폰포인트의 자식으로 붙임(위치 고정/따라다님)")]
    public bool parentToSpawnPoint = true;

    [Header("Timing (A -> B)")]
    public float rippleDelay = 0.05f;
    public float perDrumExtraDelay = 0f;

    [Header("Playback Control")]
    public bool restartInsteadOfOverlap = true;

    [Range(0.02f, 2.0f)] public float forceStopAfterA = 0.35f;
    [Range(0.02f, 2.0f)] public float forceStopAfterB = 0.60f;
    [Range(0.02f, 2.0f)] public float forceStopAfterMiss = 0.50f;

    public bool disableAfterStop = true;

    [Header("Global Multipliers (튜닝)")]
    [Range(0.05f, 1.0f)] public float lifetimeMul = 1.0f;
    [Range(0.05f, 1.5f)] public float sizeMul = 1.0f;
    [Range(0.1f, 4.0f)] public float simSpeedMul = 1.0f;
    [Range(0.0f, 1.0f)] public float emissionMul = 1.0f;
    [Range(0.0f, 1.0f)] public float burstMul = 1.0f;
    [Range(10, 1000)] public int maxParticlesClamp = 500;

    [Header("Debug")]
    public bool debugLog = false;

    private class Baseline
    {
        public float startLifetimeMul;
        public float startSizeMul;
        public float simSpeed;

        public bool rateConst;
        public float rateConstValue;

        public bool hasBursts;
        public ParticleSystem.Burst[] bursts;

        public int maxParticles;
    }

    private class Slot
    {
        public GameObject prefabAsset;
        public GameObject instanceRoot;
        public ParticleSystem[] systems;
        public Dictionary<ParticleSystem, Baseline> baseline = new Dictionary<ParticleSystem, Baseline>();
        public Coroutine stopRoutine;
    }

    private readonly Slot _slotA = new Slot();
    private readonly Slot _slotB = new Slot();
    private readonly Slot _slotMiss = new Slot();

    private Slot _activeSlot = null;
    private Judgement _preparedJudgement;
    private Coroutine _twoRoutine;

    public void Prepare(Judgement j)
    {
        _preparedJudgement = j;

        if (j == Judgement.Miss)
        {
            PrepareSlot(_slotMiss, missPrefab, GetSpawn(spawnPointMiss), localOffsetMiss);
            _activeSlot = _slotMiss;
            cachedSystems = _slotMiss.systems ?? System.Array.Empty<ParticleSystem>();
            return;
        }

        GameObject prefabA = (j == Judgement.Perfect) ? perfectPrefabA : goodPrefabA;
        PrepareSlot(_slotA, prefabA, GetSpawn(spawnPointA), localOffsetA);

        _activeSlot = _slotA;
        cachedSystems = _slotA.systems ?? System.Array.Empty<ParticleSystem>();
    }

    public void PlayPrepared()
    {
        if (_activeSlot == null || _activeSlot.systems == null || _activeSlot.systems.Length == 0)
        {
            if (debugLog) Debug.LogWarning($"[JudgementVfx] PlayPrepared but nothing prepared on {name}");
            return;
        }

        if (_twoRoutine != null)
        {
            StopCoroutine(_twoRoutine);
            _twoRoutine = null;
        }

        float stopA = (_preparedJudgement == Judgement.Miss) ? forceStopAfterMiss : forceStopAfterA;
        PlaySlotNow(_activeSlot, stopA);

        if (_preparedJudgement == Judgement.Perfect || _preparedJudgement == Judgement.Good)
        {
            GameObject prefabB = (_preparedJudgement == Judgement.Perfect) ? perfectPrefabB : goodPrefabB;
            float delay = Mathf.Max(0f, perDrumExtraDelay + rippleDelay);
            _twoRoutine = StartCoroutine(CoPlayB(prefabB, delay));
        }
    }

    // ✅ 딱 1개만 존재하도록 유지(중복 금지)
    public void Play(Judgement j)
    {
        Prepare(j);
        PlayPrepared();
    }

    private Transform GetSpawn(Transform preferred)
    {
        if (preferred != null) return preferred;
        if (spawnPoint != null) return spawnPoint;
        return transform;
    }

    private IEnumerator CoPlayB(GameObject prefabB, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        if (prefabB == null)
        {
            if (debugLog) Debug.LogWarning($"[JudgementVfx] PrefabB is NULL on {name}");
            _twoRoutine = null;
            yield break;
        }

        PrepareSlot(_slotB, prefabB, GetSpawn(spawnPointB), localOffsetB);
        PlaySlotNow(_slotB, forceStopAfterB);

        _twoRoutine = null;
    }

    private void PrepareSlot(Slot slot, GameObject prefab, Transform sp, Vector3 localOffset)
    {
        if (prefab == null)
        {
            slot.prefabAsset = null;
            slot.systems = System.Array.Empty<ParticleSystem>();
            if (debugLog) Debug.LogWarning($"[JudgementVfx] Prefab NULL on {name}");
            return;
        }

        Vector3 pos = sp.TransformPoint(localOffset);
        Quaternion rot = sp.rotation;

        if (slot.instanceRoot == null || slot.prefabAsset != prefab)
        {
            if (slot.instanceRoot != null) Destroy(slot.instanceRoot);

            slot.instanceRoot = parentToSpawnPoint
                ? Instantiate(prefab, pos, rot, sp)
                : Instantiate(prefab, pos, rot);

            slot.prefabAsset = prefab;
            slot.systems = slot.instanceRoot.GetComponentsInChildren<ParticleSystem>(true);
            CacheBaselines(slot);
        }
        else
        {
            slot.instanceRoot.transform.position = pos;
            slot.instanceRoot.transform.rotation = rot;

            if (slot.systems == null || slot.systems.Length == 0)
                slot.systems = slot.instanceRoot.GetComponentsInChildren<ParticleSystem>(true);

            if (slot.baseline.Count == 0)
                CacheBaselines(slot);
        }

        if (!slot.instanceRoot.activeSelf) slot.instanceRoot.SetActive(true);

        ApplyTuning(slot);

        if (restartInsteadOfOverlap)
            StopAndClearAll(slot);
    }

    private void PlaySlotNow(Slot slot, float stopAfter)
    {
        if (slot.systems == null || slot.systems.Length == 0) return;

        if (slot.stopRoutine != null) StopCoroutine(slot.stopRoutine);
        slot.stopRoutine = StartCoroutine(ForceStopRoutine(slot, stopAfter));

        for (int i = 0; i < slot.systems.Length; i++)
        {
            var ps = slot.systems[i];
            if (ps == null) continue;

            if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);

            if (restartInsteadOfOverlap)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ps.Play(true);
        }
    }

    private void CacheBaselines(Slot slot)
    {
        slot.baseline.Clear();
        if (slot.systems == null) return;

        foreach (var ps in slot.systems)
        {
            if (ps == null) continue;

            var b = new Baseline();

            var main = ps.main;
            b.startLifetimeMul = main.startLifetimeMultiplier;
            b.startSizeMul = main.startSizeMultiplier;
            b.simSpeed = main.simulationSpeed;
            b.maxParticles = main.maxParticles;

            var em = ps.emission;
            if (em.enabled)
            {
                var rate = em.rateOverTime;
                b.rateConst = (rate.mode == ParticleSystemCurveMode.Constant);
                if (b.rateConst) b.rateConstValue = rate.constant;

                b.hasBursts = em.burstCount > 0;
                if (b.hasBursts)
                {
                    b.bursts = new ParticleSystem.Burst[em.burstCount];
                    em.GetBursts(b.bursts);
                }
            }

            slot.baseline[ps] = b;
        }
    }

    private void ApplyTuning(Slot slot)
    {
        if (slot.systems == null) return;

        foreach (var ps in slot.systems)
        {
            if (ps == null) continue;
            if (!slot.baseline.TryGetValue(ps, out var b)) continue;

            var main = ps.main;
            main.startLifetimeMultiplier = Mathf.Max(0.01f, b.startLifetimeMul * lifetimeMul);
            main.startSizeMultiplier = Mathf.Max(0.01f, b.startSizeMul * sizeMul);
            main.simulationSpeed = Mathf.Max(0.01f, b.simSpeed * simSpeedMul);
            main.maxParticles = Mathf.Min(b.maxParticles, maxParticlesClamp);

            var em = ps.emission;
            if (em.enabled && b.rateConst)
            {
                var rate = em.rateOverTime;
                rate.constant = Mathf.Max(0f, b.rateConstValue * emissionMul);
                em.rateOverTime = rate;
            }

            if (em.enabled && b.hasBursts && b.bursts != null)
            {
                var bursts = (ParticleSystem.Burst[])b.bursts.Clone();
                for (int i = 0; i < bursts.Length; i++)
                {
                    int min = Mathf.RoundToInt(bursts[i].minCount * burstMul);
                    int max = Mathf.RoundToInt(bursts[i].maxCount * burstMul);
                    bursts[i].minCount = (short)Mathf.Clamp(min, 0, maxParticlesClamp);
                    bursts[i].maxCount = (short)Mathf.Clamp(max, 0, maxParticlesClamp);
                }
                em.SetBursts(bursts);
            }
        }
    }

    private IEnumerator ForceStopRoutine(Slot slot, float delay)
    {
        if (delay <= 0f) yield break;

        yield return new WaitForSecondsRealtime(delay);

        StopAndClearAll(slot);

        if (disableAfterStop && slot.instanceRoot != null)
            slot.instanceRoot.SetActive(false);

        slot.stopRoutine = null;
    }

    private void StopAndClearAll(Slot slot)
    {
        if (slot.systems == null) return;

        foreach (var ps in slot.systems)
        {
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }
    }
}
