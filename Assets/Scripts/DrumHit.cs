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

    [Header("Detection Settings")]
    public float noteDetectionRadius = 1.5f;
    public Transform targetPoint;
    public float hitCooldown = 0.2f; // ⭐ 쿨다운 시간 (초)

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

    private Color originalColor;
    private float colorResetTime;
    private float lastHitTime = -999f; // ⭐ 마지막 타격 시간

    void Start()
    {
        if (drumRenderer != null)
        {
            originalColor = drumRenderer.material.color;
        }

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
        {
            targetPoint = transform;
        }

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
    }

    void Update()
    {
        if (Time.time > colorResetTime && drumRenderer != null)
        {
            drumRenderer.material.color = originalColor;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DrumStick"))
        {
            // ⭐ 쿨다운 체크
            if (Time.time - lastHitTime < hitCooldown)
            {
                Debug.Log($"[{drumType}] Cooldown active, ignoring hit");
                return;
            }

            // ⭐ 속도 체크 추가
            DrumStickController drumStick = other.GetComponent<DrumStickController>();
            if (drumStick != null)
            {
                float speed = drumStick.currentSpeed;
                Debug.Log($"[{drumType}] DrumStick speed: {speed:F2}");

                // 최소 속도 이하면 무시
                if (speed < 0.5f) // ⭐ 최소 속도 (조절 가능)
                {
                    Debug.Log($"[{drumType}] Speed too low, ignoring");
                    return;
                }
            }

            lastHitTime = Time.time; // ⭐ 타격 시간 기록
            Debug.Log($"{gameObject.name} 타격 감지!");
            OnDrumHit();
        }
    }

    void OnDrumHit()
    {
        // 튜토리얼 2단계(북 치기 연습)에서만 특별 처리
        if (TutorialManager.Instance != null &&
            TutorialManager.Instance.currentStep == TutorialManager.TutorialStep.DrumBasics)
        {
            // 소리
            if (drumAudioSource != null && hitSound != null)
            {
                drumAudioSource.PlayOneShot(hitSound, 1.0f);
            }

            // 햅틱
            OVRInput.SetControllerVibration(hapticStrength, hapticDuration, controllerHand);

            // 파티클
            PlayParticle(Color.yellow);

            // 튜토리얼 카운트 증가
            int drumIndex = GetDrumIndex();
            TutorialManager.Instance.OnDrumHitInTutorial(drumIndex);
            Debug.Log($"✅ [2단계] Tutorial drum hit: {drumType} (Index: {drumIndex})");

            return;
        }

        // === 나머지 단계는 정상 판정 로직 실행 ===

        // 소리
        if (drumAudioSource != null && hitSound != null)
        {
            drumAudioSource.PlayOneShot(hitSound, 1.0f);
            Debug.Log($"[{drumType}] Sound played!");
        }

        // 햅틱
        OVRInput.SetControllerVibration(hapticStrength, hapticDuration, controllerHand);

        // 판정
        Note closestNote = FindClosestNote();

        if (closestNote != null)
        {
            float distance = Vector3.Distance(closestNote.transform.position, targetPoint.position);

            Debug.Log($"[{drumType}] Note found! Distance: {distance:F3}, Perfect: {closestNote.perfectWindow:F3}, Good: {closestNote.goodWindow:F3}");

            if (distance <= closestNote.perfectWindow)
            {
                closestNote.OnHit(true);
                PlayParticle(perfectColor);
                FlashDrum(perfectColor);
                Debug.Log($"[{drumType}] ★ PERFECT! ★");
            }
            else if (distance <= closestNote.goodWindow)
            {
                closestNote.OnHit(false);
                PlayParticle(goodColor);
                FlashDrum(goodColor);
                Debug.Log($"[{drumType}] ★ GOOD ★");
            }
            else
            {
                PlayParticle(missColor);
                FlashDrum(missColor);
                Debug.Log($"[{drumType}] ★ MISS ★ (Too far: {distance:F3})");
            }
        }
        else
        {
            PlayParticle(Color.white);
            Debug.Log("노트 없음 - 자유 타격");
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
            default:
                Debug.LogWarning($"Unknown drum type: {drumType}");
                return 0;
        }
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
        if (drumRenderer != null)
        {
            drumRenderer.material.color = color;
            colorResetTime = Time.time + 0.15f;
        }
    }

    Note FindClosestNote()
    {
        Note[] allNotes = FindObjectsOfType<Note>();

        if (allNotes.Length == 0)
            return null;

        Note closestNote = null;
        float closestDistance = float.MaxValue;

        foreach (Note note in allNotes)
        {
            if (note.hasBeenHit)
                continue;

            if (note.drumType != drumType)
            {
                continue;
            }

            float distance = Vector3.Distance(note.transform.position, targetPoint.position);

            if (distance <= noteDetectionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closestNote = note;
            }
        }

        return closestNote;
    }

    void OnDrawGizmos()
    {
        if (!showDebugSphere) return;

        Transform target = targetPoint != null ? targetPoint : transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, noteDetectionRadius);
    }
}