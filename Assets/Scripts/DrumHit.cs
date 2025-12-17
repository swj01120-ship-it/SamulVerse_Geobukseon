using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumHit : MonoBehaviour
{
    public AudioSource drumSound;
    public float minHitVelocity = 1f;
    private float lastHitTime;
    public float hitCooldown = 0.1f;

    void OnTriggerEnter(Collider other) // OnCollisionEnter에서 변경
    {
        if (other.CompareTag("DrumStick") && Time.time - lastHitTime > hitCooldown)
        {
            DrumStickController stick = other.GetComponent<DrumStickController>();

            if (stick != null && stick.currentVelocity > minHitVelocity)
            {
                HitDrum(stick.currentVelocity, stick);
                lastHitTime = Time.time;
            }
        }
    }

    void HitDrum(float velocity, DrumStickController stick)
    {
        // 사운드 재생
        if (drumSound != null)
        {
            drumSound.volume = Mathf.Clamp01(velocity / 10f);
            drumSound.Play();
        }

        // 햅틱 진동
        float hapticIntensity = Mathf.Clamp01(velocity / 10f);
        stick.TriggerHaptic(hapticIntensity);

        Debug.Log("북 타격! 속도: " + velocity);
    }
}

