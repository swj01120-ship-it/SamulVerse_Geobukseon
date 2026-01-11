using System.Collections;
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
    public float hitCooldown = 0.3f;
    private float lastHitTime = -999f;

    [Header("Haptic Feedback")]
    public OVRInput.Controller controllerHand;
    public float hapticStrength = 0.8f;
    public float hapticDuration = 0.1f;

    [Header("Particle (Base)")]
    public ParticleSystem hitParticle;

    [Header("Sub Particles (Judgement)")]
    public ParticleSystem perfectSubParticle;
    public ParticleSystem goodSubParticle;
    public ParticleSystem missSubParticle;

    [Header("Judgment Colors")]
    public Color perfectColor = new Color(1f, 0.9f, 0f);
    public Color goodColor = new Color(0.2f, 0.8f, 1f);
    public Color missColor = new Color(1f, 0.2f, 0.2f);

    [Header("Visual Feedback - Drum")]
    public Renderer drumRenderer;

    [Header("Visual Feedback - Wire (Emission)")]
    public Renderer wireRenderer;
    [Tooltip("Wire 머티리얼의 Emission 기본 색(머티리얼과 동일 권장)")]
    public Color wireEmissionBaseColor = Color.cyan;
    [Tooltip("Wire 기본 Emission 강도")]
    public float wireBaseIntensity = 1.0f;
    [Tooltip("타격 시 추가 Emission (기본)")]
    public float wireHitBoost = 2.0f;
    [Tooltip("번쩍 유지 시간(초)")]
    public float wireFlashHold = 0.08f;
    [Tooltip("원래 강도로 돌아가는 시간(초)")]
    public float wireReturnTime = 0.12f;

    [Header("Combo Tier Boost")]
    [Tooltip("콤보 0~9 배율")]
    public float tierMul_0 = 1.0f;
    [Tooltip("콤보 10~29 배율")]
    public float tierMul_10 = 1.25f;
    [Tooltip("콤보 30~49 배율")]
    public float tierMul_30 = 1.6f;
    [Tooltip("콤보 50+ 배율")]
    public float tierMul_50 = 2.1f;

    [Header("Particle Strength Boost")]
    [Tooltip("타격 파티클 크기 배율(콤보 티어 배율이 곱해짐)")]
    public float particleSizeBaseMul = 1.0f;
    [Tooltip("Perfect는 파티클을 더 키우고 싶으면")]
    public float perfectExtraParticleMul = 1.15f;
    [Tooltip("Good는 파티클을 약간만")]
    public float goodExtraParticleMul = 1.0f;
    [Tooltip("Miss 파티클 배율")]
    public float missExtraParticleMul = 0.9f;

    [Header("Debug")]
    public bool showDebugSphere = true;
    public float noteDetectionRadius = 1.5f;

    private Color originalColor;
    private float colorResetTime;

    // Wire emission runtime
    private MaterialPropertyBlock _mpb;
    private int _emissionColorId;
    private float _wireHoldUntil;
    private float _wireReturnStartTime;
    private Color _wireCurrentEmission;

    void Start()
    {
        // Drum original color
        if (drumRenderer != null)
            originalColor = drumRenderer.material.color;

        // Auto find target point (기존 로직 유지)
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

        if (targetPoint == null)
            targetPoint = transform;

        // Ensure audio source
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

        // Wire emission init
        _mpb = new MaterialPropertyBlock();
        _emissionColorId = Shader.PropertyToID("_EmissionColor");

        if (wireRenderer != null)
        {
            // keyword는 material에 걸어줘야 적용되는 경우가 많음
            var mat = wireRenderer.material;
            mat.EnableKeyword("_EMISSION");

            _wireCurrentEmission = wireEmissionBaseColor * wireBaseIntensity;
            ApplyWireEmission(_wireCurrentEmission);
        }

        // 서브 파티클은 기본 Stop (원치 않으면 삭제)
        StopAllSubParticles();

        Debug.Log($"[DrumHit-{drumType}] init complete. targetPoint: {targetPoint.position}");
    }

    void Update()
    {
        // Drum color reset
        if (Time.time > colorResetTime && drumRenderer != null)
            drumRenderer.material.color = originalColor;

        // Wire emission return
        UpdateWireEmissionReturn();
    }

    // ⭐ Note에서 호출
    public void OnNoteHit(bool isPerfect, Vector3 hitPosition)
    {
        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;

        // 사운드
        if (drumAudioSource != null && hitSound != null)
            drumAudioSource.PlayOneShot(hitSound, 1.0f);

        // 햅틱
        OVRInput.SetControllerVibration(hapticStrength, hapticDuration, controllerHand);

        // 콤보 티어 배율
        float tierMul = GetComboTierMultiplier();

        if (isPerfect)
        {
            PlayHitVfx(perfectColor, tierMul * perfectExtraParticleMul, Judgement.Perfect);
            FlashDrum(perfectColor);
            FlashWire(tierMul, isPerfect: true);
        }
        else
        {
            PlayHitVfx(goodColor, tierMul * goodExtraParticleMul, Judgement.Good);
            FlashDrum(goodColor);
            FlashWire(tierMul, isPerfect: false);
        }
    }

    public void OnNoteMiss(Vector3 notePosition)
    {
        float tierMul = GetComboTierMultiplier(); // miss도 티어 반영을 원치 않으면 1.0f로 고정해도 됨
        PlayHitVfx(missColor, tierMul * missExtraParticleMul, Judgement.Miss);
        FlashDrum(missColor);
        FlashWire(tierMul * 0.35f, isPerfect: false); // miss는 약하게(원치 않으면 0으로)
    }

    // ----------------------------
    // VFX Helpers
    // ----------------------------

    enum Judgement { Perfect, Good, Miss }

    void PlayHitVfx(Color color, float particleMul, Judgement judge)
    {
        // 1) Base particle
        if (hitParticle != null)
        {
            var main = hitParticle.main;
            main.startColor = color;

            // startSizeMultiplier는 "값"이 아니라 "배율"이라 안전하게 올리기 좋음
            main.startSizeMultiplier = particleSizeBaseMul * particleMul;

            hitParticle.Play(true);
        }

        // 2) Sub particle ON/OFF
        switch (judge)
        {
            case Judgement.Perfect:
                PlayOnlySub(perfectSubParticle);
                break;
            case Judgement.Good:
                PlayOnlySub(goodSubParticle);
                break;
            case Judgement.Miss:
                PlayOnlySub(missSubParticle);
                break;
        }
    }

    void PlayOnlySub(ParticleSystem target)
    {
        if (perfectSubParticle != null && perfectSubParticle != target) perfectSubParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (goodSubParticle != null && goodSubParticle != target) goodSubParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (missSubParticle != null && missSubParticle != target) missSubParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (target != null)
            target.Play(true);
    }

    void StopAllSubParticles()
    {
        if (perfectSubParticle != null) perfectSubParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (goodSubParticle != null) goodSubParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (missSubParticle != null) missSubParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void FlashDrum(Color color)
    {
        if (drumRenderer == null) return;
        drumRenderer.material.color = color;
        colorResetTime = Time.time + 0.15f;
    }

    void FlashWire(float tierMul, bool isPerfect)
    {
        if (wireRenderer == null) return;

        // Perfect는 좀 더 강하게
        float perfectMul = isPerfect ? 1.15f : 1.0f;

        float boost = wireHitBoost * tierMul * perfectMul;

        _wireHoldUntil = Time.time + wireFlashHold;
        _wireReturnStartTime = _wireHoldUntil; // hold 끝난 순간부터 return 시작

        _wireCurrentEmission = wireEmissionBaseColor * (wireBaseIntensity + boost);
        ApplyWireEmission(_wireCurrentEmission);
    }

    void UpdateWireEmissionReturn()
    {
        if (wireRenderer == null) return;

        // hold중이면 유지
        if (Time.time <= _wireHoldUntil) return;

        // return phase
        if (wireReturnTime <= 0f)
        {
            ApplyWireEmission(wireEmissionBaseColor * wireBaseIntensity);
            return;
        }

        float t = Mathf.Clamp01((Time.time - _wireReturnStartTime) / wireReturnTime);

        Color baseEmission = wireEmissionBaseColor * wireBaseIntensity;
        Color lerped = Color.Lerp(_wireCurrentEmission, baseEmission, t);

        ApplyWireEmission(lerped);
    }

    void ApplyWireEmission(Color emission)
    {
        if (wireRenderer == null) return;

        wireRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_emissionColorId, emission);
        wireRenderer.SetPropertyBlock(_mpb);
    }

    float GetComboTierMultiplier()
    {
        // 1) RhythmGameManager combo 우선
        int combo = 0;

        if (RhythmGameManager.Instance != null)
        {
            combo = RhythmGameManager.Instance.combo;
        }
        else
        {
            // 2) ComboSystem이 따로 쓰일 때 대비 (씬에 존재하면 찾아서 사용)
            ComboSystem cs = FindObjectOfType<ComboSystem>();
            if (cs != null) combo = cs.GetCurrentCombo();
        }

        if (combo >= 50) return tierMul_50;
        if (combo >= 30) return tierMul_30;
        if (combo >= 10) return tierMul_10;
        return tierMul_0;
    }

    // ⭐ 튜토리얼 2단계 전용 (기존 유지)
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

            OVRInput.SetControllerVibration(hapticStrength, hapticDuration, controllerHand);

            // 튜토리얼은 노란색 연출
            PlayHitVfx(Color.yellow, 1.0f, Judgement.Good);

            int drumIndex = GetDrumIndex();
            TutorialManager.Instance.OnDrumHitInTutorial(drumIndex);
            Debug.Log($"✅ [2단계] {drumType} 타격");
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

    void OnDrawGizmos()
    {
        if (!showDebugSphere) return;
        Transform target = targetPoint != null ? targetPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, noteDetectionRadius);
    }
}
