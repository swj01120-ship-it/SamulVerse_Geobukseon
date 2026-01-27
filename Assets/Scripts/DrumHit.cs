using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumHit : MonoBehaviour
{
    [Header("Drum Type")]
    public string drumType = "Jung";

    [Header("Audio")]
    public AudioSource drumAudioSource;
    public AudioClip hitSound;

    [Header("Settings")]
    public Transform targetPoint;
    public float hitCooldown = 0.10f;
    private float lastHitTime = -999f;

    [Header("Physics Safety")]
    [Tooltip("Cover 콜라이더를 자동으로 Trigger로 만듭니다(타격 이벤트 확실하게).")]
    public bool forceTriggerCollider = true;

    [Header("Judgement (Distance Based)")]
    [Tooltip("TargetPoint와 노트 거리 기준 Perfect")]
    public float perfectDistance = 0.12f;
    [Tooltip("TargetPoint와 노트 거리 기준 Good")]
    public float goodDistance = 0.25f;
    [Tooltip("이 거리 밖 노트는 판정 후보에서 제외")]
    public float searchMaxDistance = 1.0f;

    [Header("Note Filter")]
    [Tooltip("노트의 drumType이 비어있을 때도 판정 대상으로 포함할지(디버그용)")]
    public bool allowEmptyNoteDrumType = false;

    [Header("Haptic Feedback")]
    [Tooltip("기본 컨트롤러(스틱에서 컨트롤러를 못 찾았을 때만 사용)")]
    public OVRInput.Controller controllerHand = OVRInput.Controller.RTouch;
    public float hapticStrength = 0.8f;
    public float hapticDuration = 0.06f;
    private Coroutine _hapticRoutine;

    [Header("Judgment Colors (Optional: base particle/drum tint)")]
    public Color perfectColor = new Color(1f, 0.9f, 0f);
    public Color goodColor = new Color(0.2f, 0.8f, 1f);
    public Color missColor = new Color(1f, 0.2f, 0.2f);

    [Header("Visual Feedback - Drum (Optional)")]
    public Renderer drumRenderer;
    private Color originalColor;
    private float colorResetTime;

    [Header("Visual Feedback - Wire (Emission)")]
    public Renderer wireRenderer;
    public Color wireEmissionBaseColor = Color.cyan;
    public float wireBaseIntensity = 0f;
    public float wireHitBoost = 14.0f;
    public float wireFlashHold = 0.10f;
    public float wireReturnTime = 0.12f;

    private MaterialPropertyBlock _mpb;
    private int _emissionColorId;
    private float _wireHoldUntil;
    private float _wireReturnStartTime;
    private Color _wirePeakEmission;

    [Header("Hit_04 VFX (Judgement ON/OFF)")]
    public JudgementVfxController hit04;
    public bool vfxFollowTargetPoint = true;

    [Header("Combo Tier (VFX + Wire)")]
    public ComboSystem comboSystem;

    public int tier1 = 10;
    public int tier2 = 30;
    public int tier3 = 50;

    public float tier0Vfx = 1.0f;
    public float tier1Vfx = 1.15f;
    public float tier2Vfx = 1.35f;
    public float tier3Vfx = 1.60f;

    public float tier0Wire = 1.0f;
    public float tier1Wire = 1.15f;
    public float tier2Wire = 1.35f;
    public float tier3Wire = 1.60f;

    [Header("Hit_04 Strength Tuning (Quest Safe Clamp)")]
    public float vfxMulClampMax = 2.0f;
    public float emissionRateClampMax = 200f;
    public int burstMinClampMax = 80;
    public int burstMaxClampMax = 120;

    private class VfxBaseline
    {
        public float startSizeMul;
        public bool hasRateConst;
        public float rateConst;
        public bool hasBursts;
        public ParticleSystem.Burst[] bursts;
    }

    private readonly Dictionary<ParticleSystem, VfxBaseline> _vfxBase = new Dictionary<ParticleSystem, VfxBaseline>();

    private void Start()
    {
        // ✅ Collider trigger 강제 (타격 이벤트 확실화)
        if (forceTriggerCollider)
        {
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var bc = gameObject.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                Debug.LogWarning($"[DrumHit] '{name}'에 Collider가 없어서 BoxCollider(Trigger) 자동 추가");
            }
            else if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[DrumHit] '{name}' Collider가 Trigger가 아니어서 자동 ON 처리");
            }
        }

        if (drumRenderer != null)
            originalColor = drumRenderer.material.color;

        // targetPoint 자동찾기(기존 흐름 유지)
        if (targetPoint == null)
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                foreach (Transform child in parent)
                {
                    if (child.name.Contains("TargetPoint") || child.name.Contains("Target"))
                    {
                        targetPoint = child;
                        break;
                    }
                }
            }
        }
        if (targetPoint == null) targetPoint = transform;

        // AudioSource ensure
        if (drumAudioSource == null)
        {
            drumAudioSource = GetComponent<AudioSource>();
            if (drumAudioSource == null)
            {
                drumAudioSource = gameObject.AddComponent<AudioSource>();
                drumAudioSource.spatialBlend = 0f;
                drumAudioSource.playOnAwake = false;
            }
        }

        if (comboSystem == null)
            comboSystem = FindObjectOfType<ComboSystem>();

        // VFX 컨트롤러 자동 연결
        if (hit04 == null && targetPoint != null)
            hit04 = targetPoint.GetComponent<JudgementVfxController>();

        if (hit04 == null)
            hit04 = GetComponentInChildren<JudgementVfxController>(true);

        _mpb = new MaterialPropertyBlock();
        _emissionColorId = Shader.PropertyToID("_EmissionColor");

        if (wireRenderer != null)
        {
            wireRenderer.material.EnableKeyword("_EMISSION");
            ApplyWireEmission(wireEmissionBaseColor * wireBaseIntensity);
        }

        CacheHit04Baselines();
    }

    private void Update()
    {
        if (Time.time > colorResetTime && drumRenderer != null)
            drumRenderer.material.color = originalColor;

        UpdateWireEmissionReturn();
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleStickContact(other.gameObject, "Trigger");
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleStickContact(collision.gameObject, "Collision");
    }

    private void HandleStickContact(GameObject otherGo, string via)
    {
        if (!IsDrumStick(otherGo)) return;

        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;

        // 어떤 컨트롤러가 쳤는지 스틱에서 가져오기(없으면 기본값)
        var stick = otherGo.GetComponentInParent<DrumStickController>();
        var ctrl = (stick != null) ? stick.controller : controllerHand;

        // ✅ 1) 노트 판정 시도 (메인게임에서만 점수 적용됨)
        bool judged = TryJudgeNote(out bool isPerfect);

        // ✅ 2) 무조건 타격 연출(튜토리얼/메인게임 공통)
        if (drumAudioSource != null && hitSound != null)
            drumAudioSource.PlayOneShot(hitSound, 1.0f);

        PulseHaptics(ctrl, hapticStrength, hapticDuration);

        float vfxMul = GetTierVfxMultiplier();
        float wireMul = GetTierWireMultiplier();

        if (hit04 == null)
        {
            Debug.LogWarning($"[DrumHit] Hit04(JudgementVfxController)가 NULL이라 파티클이 재생되지 않음. (via={via}, drum={name})");
        }
        else
        {
            if (judged)
                PlayHit04(isPerfect ? JudgementVfxController.Judgement.Perfect : JudgementVfxController.Judgement.Good, vfxMul);
            else
                PlayHit04(JudgementVfxController.Judgement.Miss, vfxMul);
        }

        // 색/와이어
        if (judged)
        {
            FlashWire(wireMul * (isPerfect ? 1.15f : 1.0f));
            if (drumRenderer != null) FlashDrum(isPerfect ? perfectColor : goodColor);
        }
        else
        {
            FlashWire(wireMul * 0.35f);
            if (drumRenderer != null) FlashDrum(missColor);
        }

        // ✅ 튜토리얼 단계일 때만 튜토리얼 카운트 처리(기능 유지)
        if (TutorialManager.Instance != null &&
            TutorialManager.Instance.currentStep == TutorialManager.TutorialStep.DrumBasics)
        {
            int drumIndex = GetDrumIndex();
            TutorialManager.Instance.OnDrumHitInTutorial(drumIndex);
        }

        Debug.Log($"[DrumHit] HIT via={via} drum={name} drumType={drumType} judged={judged} perfect={isPerfect}");
    }

    // =========================
    // ✅ 판정 핵심
    // =========================
    private bool TryJudgeNote(out bool isPerfect)
    {
        isPerfect = false;

        var rgm = RhythmGameManager.Instance;
        if (rgm == null) return false;

        if (targetPoint == null) targetPoint = transform;

        Note best = null;
        float bestDist = float.MaxValue;

        var notes = FindObjectsOfType<Note>();
        for (int i = 0; i < notes.Length; i++)
        {
            var n = notes[i];
            if (n == null) continue;

            if (!string.IsNullOrEmpty(n.drumType))
            {
                if (n.drumType != drumType) continue;
            }
            else
            {
                if (!allowEmptyNoteDrumType) continue;
            }

            float d = Vector3.Distance(n.transform.position, targetPoint.position);
            if (d > searchMaxDistance) continue;

            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }

        if (best == null)
        {
            rgm.OnMiss();
            return false;
        }

        if (bestDist <= perfectDistance)
        {
            isPerfect = true;
            rgm.OnPerfect();
            Destroy(best.gameObject);
            return true;
        }

        if (bestDist <= goodDistance)
        {
            isPerfect = false;
            rgm.OnGood();
            Destroy(best.gameObject);
            return true;
        }

        rgm.OnMiss();
        return false;
    }

    private bool IsDrumStick(GameObject go)
    {
        if (go == null) return false;
        if (go.CompareTag("DrumStick")) return true;

        var p = go.transform.parent;
        if (p != null && p.CompareTag("DrumStick")) return true;

        return go.GetComponentInParent<DrumStickController>() != null;
    }

    // =====================
    // Note -> DrumHit API (기존 유지)
    // =====================
    public void OnNoteHit(bool isPerfect, Vector3 hitPosition)
    {
        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;

        if (drumAudioSource != null && hitSound != null)
            drumAudioSource.PlayOneShot(hitSound, 1.0f);

        PulseHaptics(controllerHand, hapticStrength, hapticDuration);

        float vfxMul = GetTierVfxMultiplier();
        float wireMul = GetTierWireMultiplier();

        PlayHit04(isPerfect ? JudgementVfxController.Judgement.Perfect
                            : JudgementVfxController.Judgement.Good, vfxMul);

        if (drumRenderer != null)
            FlashDrum(isPerfect ? perfectColor : goodColor);

        float perfectMul = isPerfect ? 1.15f : 1.0f;
        FlashWire(wireMul * perfectMul);
    }

    public void OnNoteMiss(Vector3 notePosition)
    {
        PulseHaptics(controllerHand, hapticStrength * 0.35f, hapticDuration * 0.75f);

        float vfxMul = GetTierVfxMultiplier();
        float wireMul = GetTierWireMultiplier();

        PlayHit04(JudgementVfxController.Judgement.Miss, vfxMul);

        if (drumRenderer != null)
            FlashDrum(missColor);

        FlashWire(wireMul * 0.35f);
    }

    // =====================
    // Hit_04
    // =====================
    private ParticleSystem[] GetHit04Systems()
    {
        if (hit04 == null) return null;

        // ✅ allSystems 사용 금지. (팀프로젝트 충돌 방지)
        var systems = hit04.Systems;

        if (systems == null || systems.Length == 0)
            systems = hit04.GetComponentsInChildren<ParticleSystem>(true);

        return systems;
    }

    void CacheHit04Baselines()
    {
        if (hit04 == null) return;

        var systems = GetHit04Systems();
        if (systems == null || systems.Length == 0) return;

        _vfxBase.Clear();

        foreach (var ps in systems)
        {
            if (ps == null) continue;

            var baseLine = new VfxBaseline();
            var main = ps.main;
            baseLine.startSizeMul = main.startSizeMultiplier;

            var em = ps.emission;
            if (em.enabled)
            {
                var rate = em.rateOverTime;
                baseLine.hasRateConst = (rate.mode == ParticleSystemCurveMode.Constant);
                if (baseLine.hasRateConst) baseLine.rateConst = rate.constant;

                baseLine.hasBursts = em.burstCount > 0;
                if (baseLine.hasBursts)
                {
                    baseLine.bursts = new ParticleSystem.Burst[em.burstCount];
                    em.GetBursts(baseLine.bursts);
                }
            }

            _vfxBase[ps] = baseLine;
        }
    }

    void ApplyHit04Strength(float mul)
    {
        if (hit04 == null) return;

        var systems = GetHit04Systems();
        if (systems == null || systems.Length == 0) return;

        float m = Mathf.Clamp(mul, 1.0f, vfxMulClampMax);

        foreach (var ps in systems)
        {
            if (ps == null) continue;
            if (!_vfxBase.TryGetValue(ps, out var baseLine)) continue;

            var main = ps.main;
            main.startSizeMultiplier = Mathf.Clamp(baseLine.startSizeMul * m, 0.02f, 3.0f);

            var em = ps.emission;
            if (!em.enabled) continue;

            if (baseLine.hasRateConst)
            {
                var rate = em.rateOverTime;
                rate.constant = Mathf.Clamp(baseLine.rateConst * m, 0f, emissionRateClampMax);
                em.rateOverTime = rate;
            }

            if (baseLine.hasBursts && baseLine.bursts != null)
            {
                var bursts = (ParticleSystem.Burst[])baseLine.bursts.Clone();
                for (int i = 0; i < bursts.Length; i++)
                {
                    int min = Mathf.RoundToInt(bursts[i].minCount * m);
                    int max = Mathf.RoundToInt(bursts[i].maxCount * m);

                    bursts[i].minCount = (short)Mathf.Clamp(min, 0, burstMinClampMax);
                    bursts[i].maxCount = (short)Mathf.Clamp(max, 0, burstMaxClampMax);
                }
                em.SetBursts(bursts);
            }
        }
    }

    void PlayHit04(JudgementVfxController.Judgement j, float strengthMul)
    {
        if (hit04 == null) return;

        if (vfxFollowTargetPoint && targetPoint != null && hit04.transform.parent != targetPoint)
            hit04.transform.position = targetPoint.position;

        // 1) 이번 판정에 맞는 프리팹 인스턴스 준비 (Systems 갱신)
        hit04.Prepare(j);

        // 2) 베이스라인 캐시 갱신
        CacheHit04Baselines();

        // 3) 강도 적용
        ApplyHit04Strength(strengthMul);

        // 4) 실제 재생
        hit04.PlayPrepared();
    }

    // =====================
    // Wire Emission
    // =====================
    void FlashWire(float tierMul)
    {
        if (wireRenderer == null) return;

        float boost = wireHitBoost * tierMul;

        _wireHoldUntil = Time.time + wireFlashHold;
        _wireReturnStartTime = _wireHoldUntil;

        _wirePeakEmission = wireEmissionBaseColor * (wireBaseIntensity + boost);
        ApplyWireEmission(_wirePeakEmission);
    }

    void UpdateWireEmissionReturn()
    {
        if (wireRenderer == null) return;
        if (Time.time <= _wireHoldUntil) return;

        if (wireReturnTime <= 0f)
        {
            ApplyWireEmission(wireEmissionBaseColor * wireBaseIntensity);
            return;
        }

        float t = Mathf.Clamp01((Time.time - _wireReturnStartTime) / wireReturnTime);
        Color baseEmission = wireEmissionBaseColor * wireBaseIntensity;
        Color lerp = Color.Lerp(_wirePeakEmission, baseEmission, t);

        ApplyWireEmission(lerp);
    }

    void ApplyWireEmission(Color emission)
    {
        if (wireRenderer == null) return;

        wireRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_emissionColorId, emission);
        wireRenderer.SetPropertyBlock(_mpb);
    }

    // =====================
    // Combo Tier
    // =====================
    int GetComboSafe()
    {
        if (RhythmGameManager.Instance != null)
            return RhythmGameManager.Instance.combo;

        if (comboSystem != null)
            return comboSystem.GetCurrentCombo();

        return 0;
    }

    float GetTierVfxMultiplier()
    {
        int c = GetComboSafe();
        if (c >= tier3) return tier3Vfx;
        if (c >= tier2) return tier2Vfx;
        if (c >= tier1) return tier1Vfx;
        return tier0Vfx;
    }

    float GetTierWireMultiplier()
    {
        int c = GetComboSafe();
        if (c >= tier3) return tier3Wire;
        if (c >= tier2) return tier2Wire;
        if (c >= tier1) return tier1Wire;
        return tier0Wire;
    }

    // =====================
    // Drum tint
    // =====================
    void FlashDrum(Color color)
    {
        if (drumRenderer == null) return;
        drumRenderer.material.color = color;
        colorResetTime = Time.time + 0.12f;
    }

    // =====================
    // Haptics
    // =====================
    void PulseHaptics(OVRInput.Controller ctrl, float strength, float duration)
    {
        if (_hapticRoutine != null) StopCoroutine(_hapticRoutine);
        _hapticRoutine = StartCoroutine(HapticRoutine(ctrl, strength, duration));
    }

    IEnumerator HapticRoutine(OVRInput.Controller ctrl, float strength, float duration)
    {
        OVRInput.SetControllerVibration(strength, strength, ctrl);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, ctrl);
    }

    int GetDrumIndex()
    {
        switch (drumType)
        {
            case "Jung": return 0;
            case "Jang": return 1;
            case "Book": return 2;
            case "Jing": return 3;
            default: return 0;
        }
    }
}
