using System.Collections;
using UnityEngine;
using System.Collections.Generic;

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

    [Header("Particle")]
    public ParticleSystem hitParticle;

    [Header("Judgment Colors")]
    public Color perfectColor = new Color(1f, 0.9f, 0f);
    public Color goodColor = new Color(0.2f, 0.8f, 1f);
    public Color missColor = new Color(1f, 0.2f, 0.2f);

    [Header("Visual Feedback")]
    public Renderer drumRenderer;

    [Header("Debug")]
    public bool showDebugSphere = true;
    public float noteDetectionRadius = 1.5f;

    [Header("Audio Layering")]
    public AudioClip perfectAccent;   // Perfect 전용 '팅' 같은 소리
    public AudioClip goodAccent;      // Good 전용(선택)
    [Range(0f, 2f)] public float accentVolume = 0.8f;

    [Header("Haptic Tuning")]
    public float perfectHapticStrength = 1.0f;
    public float perfectHapticDuration = 0.08f;
    public float goodHapticStrength = 0.7f;
    public float goodHapticDuration = 0.06f;
    public float missHapticStrength = 0.25f;
    public float missHapticDuration = 0.04f;

    [Header("Emission Flash (Built-in Standard)")]
    public Renderer emissionRenderer;           // 네온 머티리얼이 붙은 렌더러(드럼 커버)
    public Color emissionBaseColor = Color.red; // 기본 발광색
    public float emissionBaseIntensity = 1.0f;
    public float emissionHitBoost = 3.0f;       // 타격 순간 추가 강도
    public float emissionReturnSpeed = 16f;     // 복귀 속도

    private Color originalColor;
    private float colorResetTime;

    void Start()
    {
        if (drumRenderer != null) originalColor = drumRenderer.material.color;

        if (targetPoint == null) targetPoint = transform; // 안전장치

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
        if (emissionRenderer != null)
        {
            var mat = emissionRenderer.material;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionBaseColor * emissionBaseIntensity);
        }
    }

   
    void Update()
    {
        if (Time.time > colorResetTime && drumRenderer != null)
        {
            drumRenderer.material.color = originalColor;
        }
        if (emissionRenderer != null)
        {
            var mat = emissionRenderer.material;
            Color current = mat.GetColor("_EmissionColor");
            Color target = emissionBaseColor * emissionBaseIntensity;
            Color next = Color.Lerp(current, target, Time.deltaTime * emissionReturnSpeed);
            mat.SetColor("_EmissionColor", next);
        }
    }

    // ⭐ Note에서 호출
    public void OnNoteHit(bool isPerfect, Vector3 hitPosition)
    {
        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;

        // 1) 기본 타격 사운드
        if (drumAudioSource != null && hitSound != null)
            drumAudioSource.PlayOneShot(hitSound, 1.0f);

        // 2) Perfect/Good 어택 레이어
        if (drumAudioSource != null)
        {
            if (isPerfect && perfectAccent != null)
                drumAudioSource.PlayOneShot(perfectAccent, accentVolume);
            else if (!isPerfect && goodAccent != null)
                drumAudioSource.PlayOneShot(goodAccent, accentVolume);
        }

        // 3) 햅틱 차등
        if (isPerfect)
            StartCoroutine(HapticPulse(perfectHapticStrength, perfectHapticDuration));
        else
            StartCoroutine(HapticPulse(goodHapticStrength, goodHapticDuration));

        // 4) 파티클/드럼 컬러(기존 유지)
        if (isPerfect)
        {
            PlayParticle(perfectColor);
            FlashDrum(perfectColor);
        }
        else
        {
            PlayParticle(goodColor);
            FlashDrum(goodColor);
        }

        // 5) Emission 플래시(네온 펄스)
        BoostEmission(isPerfect ? emissionHitBoost : emissionHitBoost * 0.6f);
    }

    public void OnNoteMiss(Vector3 notePosition)
    {
        PlayParticle(missColor);
        FlashDrum(missColor);
        StartCoroutine(HapticPulse(missHapticStrength, missHapticDuration));
        BoostEmission(emissionHitBoost * 0.25f);
    }

    IEnumerator HapticPulse(float strength, float duration)
    {
        OVRInput.SetControllerVibration(strength, strength, controllerHand);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, controllerHand);
    }

    void BoostEmission(float boost)
    {
        if (emissionRenderer == null) return;
        var mat = emissionRenderer.material;
        mat.EnableKeyword("_EMISSION");

        Color boosted = emissionBaseColor * (emissionBaseIntensity + boost);
        mat.SetColor("_EmissionColor", boosted);
    }

    void PlayParticle(Color color)
    {
        if (hitParticle == null) return;
        var main = hitParticle.main;
        main.startColor = color;
        hitParticle.Play();
    }

    void FlashDrum(Color color)
    {
        if (drumRenderer == null) return;
        drumRenderer.material.color = color;
        colorResetTime = Time.time + 0.15f;
    }


// ⭐ 튜토리얼 2단계 전용
void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DrumStick"))
        {
            // ⭐ 튜토리얼에서는 더 긴 쿨다운 (0.5초)
            float tutorialCooldown = 0.5f;

            if (Time.time - lastHitTime < tutorialCooldown)
            {
                return;
            }

            // 튜토리얼 2단계에서만
            if (TutorialManager.Instance != null &&
                TutorialManager.Instance.currentStep == TutorialManager.TutorialStep.DrumBasics)
            {
                lastHitTime = Time.time;

                // 소리
                if (drumAudioSource != null && hitSound != null)
                {
                    drumAudioSource.PlayOneShot(hitSound, 2.0f);
                }

                // 햅틱
                OVRInput.SetControllerVibration(hapticStrength, hapticDuration, controllerHand);

                // 파티클
                PlayParticle(Color.yellow);

                // 튜토리얼 카운트
                int drumIndex = GetDrumIndex();
                TutorialManager.Instance.OnDrumHitInTutorial(drumIndex);
                Debug.Log($"✅ [2단계] {drumType} 타격");
            }
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
