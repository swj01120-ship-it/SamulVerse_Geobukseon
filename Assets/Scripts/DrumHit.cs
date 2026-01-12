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

    [Header("Haptic Feedback")]
    public OVRInput.Controller controllerHand = OVRInput.Controller.RTouch;
    [Tooltip("진동 강도(0~1 권장)")]
    public float hapticStrength = 0.8f;
    [Tooltip("진동 지속 시간(초)")]
    public float hapticDuration = 0.06f;
    Coroutine _hapticRoutine;

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
    [Tooltip("타격 시 추가 Emission(기본). Quest에서 눈에 띄게 하려면 8~24 추천")]
    public float wireHitBoost = 14.0f;
    public float wireFlashHold = 0.10f;
    public float wireReturnTime = 0.12f;

    // Wire runtime
    private MaterialPropertyBlock _mpb;
    private int _emissionColorId;
    private float _wireHoldUntil;
    private float _wireReturnStartTime;
    private Color _wirePeakEmission;

    [Header("Hit_04 VFX (Judgement ON/OFF)")]
    [Tooltip("Cover 하위 Hit_04 루트의 JudgementVfxController를 연결")]
    public JudgementVfxController hit04;
    [Tooltip("VFX를 TargetPoint 위치로 이동시켜 재생할지")]
    public bool vfxFollowTargetPoint = true;

    [Header("Combo Tier (VFX + Wire)")]
    [Tooltip("씬에 ComboSystem이 있으면 드래그(권장). 없으면 RhythmGameManager combo 사용")]
    public ComboSystem comboSystem;

    public int tier1 = 10;
    public int tier2 = 30;
    public int tier3 = 50;

    [Tooltip("콤보에 따른 VFX 강도 배율(Quest는 2.0 이상 과하면 비추천)")]
    public float tier0Vfx = 1.0f;
    public float tier1Vfx = 1.15f;
    public float tier2Vfx = 1.35f;
    public float tier3Vfx = 1.60f;

    [Tooltip("콤보에 따른 Wire 번쩍 배율")]
    public float tier0Wire = 1.0f;
    public float tier1Wire = 1.15f;
    public float tier2Wire = 1.35f;
    public float tier3Wire = 1.60f;

    [Header("Hit_04 Strength Tuning (Quest Safe Clamp)")]
    [Tooltip("VFX 강도 배율 상한(Quest 안전용)")]
    public float vfxMulClampMax = 2.0f;
    [Tooltip("Burst/Rate 상한(Quest 안전용)")]
    public float emissionRateClampMax = 200f;
    public int burstMinClampMax = 80;
    public int burstMaxClampMax = 120;

    // ---- Hit_04 baseline cache (누적 방지) ----
    class VfxBaseline
    {
        public float startSizeMul;
        public bool hasRateConst;
        public float rateConst;
        public bool hasBursts;
        public ParticleSystem.Burst[] bursts;
    }
    readonly Dictionary<ParticleSystem, VfxBaseline> _vfxBase = new();

    void Start()
    {
        // Drum original color
        if (drumRenderer != null)
            originalColor = drumRenderer.material.color;

        // Auto find target point (기존 흐름 유지) :contentReference[oaicite:2]{index=2}
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

        // ComboSystem ensure
        if (comboSystem == null)
            comboSystem = FindObjectOfType<ComboSystem>();

        // Hit_04 controller ensure
        if (hit04 == null)
            hit04 = GetComponentInChildren<JudgementVfxController>(true);

        // Wire emission init
        _mpb = new MaterialPropertyBlock();
        _emissionColorId = Shader.PropertyToID("_EmissionColor");

        if (wireRenderer != null)
        {
            wireRenderer.material.EnableKeyword("_EMISSION");
            ApplyWireEmission(wireEmissionBaseColor * wireBaseIntensity);
        }

        CacheHit04Baselines();
    }

    void Update()
    {
        // Drum tint reset
        if (Time.time > colorResetTime && drumRenderer != null)
            drumRenderer.material.color = originalColor;

        // Wire emission return
        UpdateWireEmissionReturn();
    }

    // =====================
    // Note -> DrumHit API
    // =====================
    public void OnNoteHit(bool isPerfect, Vector3 hitPosition)
    {
        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;

        // Sound (기존 유지) :contentReference[oaicite:3]{index=3}
        if (drumAudioSource != null && hitSound != null)
            drumAudioSource.PlayOneShot(hitSound, 1.0f);

        // Haptic (안정화: 일정 시간 후 끄기)
        PulseHaptics(hapticStrength, hapticDuration);

        // Combo multipliers
        float vfxMul = GetTierVfxMultiplier();
        float wireMul = GetTierWireMultiplier();

        // Judgement VFX (Hit_04 ON/OFF)
        PlayHit04(isPerfect ? JudgementVfxController.Judgement.Perfect
                            : JudgementVfxController.Judgement.Good, vfxMul);

        // Optional drum tint
        if (drumRenderer != null)
        {
            FlashDrum(isPerfect ? perfectColor : goodColor);
        }

        // Wire flash (+ perfect bonus)
        float perfectMul = isPerfect ? 1.15f : 1.0f;
        FlashWire(wireMul * perfectMul);
    }

    public void OnNoteMiss(Vector3 notePosition)
    {
        // Miss는 짧고 약하게(원하면 값을 조절)
        PulseHaptics(hapticStrength * 0.35f, hapticDuration * 0.75f);

        float vfxMul = GetTierVfxMultiplier();
        float wireMul = GetTierWireMultiplier();

        PlayHit04(JudgementVfxController.Judgement.Miss, vfxMul);

        if (drumRenderer != null)
        {
            FlashDrum(missColor);
        }

        // Miss는 Wire 약하게(원치 않으면 0으로)
        FlashWire(wireMul * 0.35f);
    }

    // =====================
    // Hit_04 (JudgementVfxController)
    // =====================
    void CacheHit04Baselines()
    {
        if (hit04 == null) return;

        // hit04가 Awake에서 allSystems 캐시/Stop 처리하는 구조 :contentReference[oaicite:4]{index=4}
        if (hit04.allSystems == null || hit04.allSystems.Length == 0)
            hit04.allSystems = hit04.GetComponentsInChildren<ParticleSystem>(true);

        _vfxBase.Clear();

        foreach (var ps in hit04.allSystems)
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
        if (hit04 == null || hit04.allSystems == null) return;

        float m = Mathf.Clamp(mul, 1.0f, vfxMulClampMax);

        foreach (var ps in hit04.allSystems)
        {
            if (ps == null) continue;
            if (!_vfxBase.TryGetValue(ps, out var baseLine)) continue;

            // Main size
            var main = ps.main;
            main.startSizeMultiplier = Mathf.Clamp(baseLine.startSizeMul * m, 0.02f, 3.0f);

            // Emission
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

        if (vfxFollowTargetPoint && targetPoint != null)
            hit04.transform.position = targetPoint.position;

        // 강도 적용(누적 방지 캐시 기반)
        ApplyHit04Strength(strengthMul);

        // 자식 ON/OFF + 활성 PS Play :contentReference[oaicite:5]{index=5}
        hit04.Play(j);
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
    // Drum tint (optional)
    // =====================
    void FlashDrum(Color color)
    {
        drumRenderer.material.color = color;
        colorResetTime = Time.time + 0.12f;
    }

    // =====================
    // Haptics
    // =====================
    void PulseHaptics(float strength, float duration)
    {
        if (_hapticRoutine != null) StopCoroutine(_hapticRoutine);
        _hapticRoutine = StartCoroutine(HapticRoutine(strength, duration));
    }

    IEnumerator HapticRoutine(float strength, float duration)
    {
        // (freq, amp, controller) 형태라 가정하고 동일값 사용
        OVRInput.SetControllerVibration(strength, strength, controllerHand);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, controllerHand);
    }

    // =====================
    // Tutorial (기존 유지)
    // =====================
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("DrumStick")) return;

        float tutorialCooldown = 0.5f;
        if (Time.time - lastHitTime < tutorialCooldown) return;

        if (TutorialManager.Instance != null &&
            TutorialManager.Instance.currentStep == TutorialManager.TutorialStep.DrumBasics)
        {
            lastHitTime = Time.time;

            if (drumAudioSource != null && hitSound != null)
                drumAudioSource.PlayOneShot(hitSound, 2.0f);

            PulseHaptics(hapticStrength, hapticDuration);

            float vfxMul = GetTierVfxMultiplier();
            float wireMul = GetTierWireMultiplier();

            // 튜토리얼은 Good 느낌으로 연출(원하면 Perfect로 변경)
            PlayHit04(JudgementVfxController.Judgement.Good, vfxMul);
            FlashWire(wireMul * 0.7f);

            if (drumRenderer != null) FlashDrum(Color.yellow);

            int drumIndex = GetDrumIndex();
            TutorialManager.Instance.OnDrumHitInTutorial(drumIndex);
        }
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
