using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumHit : MonoBehaviour
{
    public AudioSource drumSound;
    public float minHitVelocity = 1f; // 최소 타격 속도
    private float lastHitTime;
    public float hitCooldown = 0.1f; // 연타 방지

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand") && Time.time - lastHitTime > hitCooldown)
        {
            HandVelocity handVel = other.GetComponent<HandVelocity>();

            if (handVel != null && handVel.currentVelocity > minHitVelocity)
            {
                HitDrum(handVel.currentVelocity);
                lastHitTime = Time.time;
            }
        }
    }

    void HitDrum(float velocity)
    {
        // 사운드 재생
        if (drumSound != null)
        {
            drumSound.volume = Mathf.Clamp01(velocity / 10f);
            drumSound.Play();
        }

        // 콘솔 출력 (테스트용)
        Debug.Log("북 타격! 속도: " + velocity);
    }
}
