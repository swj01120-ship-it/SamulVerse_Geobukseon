using System.Collections;
using UnityEngine;
using System.Collections.Generic;
public class DrumHit : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource drumAudioSource;
    public AudioClip hitSound;

    [Header("Detection Settings")]
    public float noteDetectionRadius = 0.5f;
    public Transform targetPoint;

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
    }

    void Update()
    {
        if (Time.time > colorResetTime && drumRenderer != null)
        {
            drumRenderer.material.color = originalColor;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("DrumStick"))
        {
            OnDrumHit();
        }
    }

    void OnDrumHit()
    {
        // ¼Ò¸®
        if (drumAudioSource != null && hitSound != null)
        {
            drumAudioSource.PlayOneShot(hitSound, 1.0f);
        }

        // ÇÝÆ½
        OVRInput.SetControllerVibration(hapticStrength, hapticDuration, controllerHand);

        // ÆÇÁ¤
        Note closestNote = FindClosestNote();

        if (closestNote != null)
        {
            float distance = Vector3.Distance(closestNote.transform.position, targetPoint.position);

            if (distance <= closestNote.perfectWindow)
            {
                closestNote.OnHit(true);
                PlayParticle(perfectColor);
                FlashDrum(perfectColor);
                Debug.Log("¡Ú PERFECT! ¡Ú");
            }
            else if (distance <= closestNote.goodWindow)
            {
                closestNote.OnHit(false);
                PlayParticle(goodColor);
                FlashDrum(goodColor);
                Debug.Log("¡Ú GOOD ¡Ú");
            }
            else
            {
                PlayParticle(missColor);
            }
        }
        else
        {
            PlayParticle(Color.white);
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
