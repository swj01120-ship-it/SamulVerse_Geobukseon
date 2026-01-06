using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComboSystem : MonoBehaviour
{
    [Header("UI")]
    public Text comboText;
    public Animator npcAnimator;
    public string speedParameterName = "Speed";

    [Header("Settings")]
    public int baseCombo = 0;
    public float minSpeed = 0.5f;
    public float maxSpeed = 2.0f;
    public int maxComboForSpeed = 50;

    [Header("Combo Particles")]
    public ParticleSystem combo10Particle;
    public ParticleSystem combo30Particle;
    public ParticleSystem combo50Particle;

    private int currentCombo = 0;
    private bool combo10Achieved = false;
    private bool combo30Achieved = false;
    private bool combo50Achieved = false;

    // Unity 메시지 | 최초 1회
    void Start()
    {
        currentCombo = baseCombo;
        UpdateComboUI();
        UpdateNPCSpeed();

        // 파티클 초기화 - 자동 재생 방지
        if (combo10Particle != null) combo10Particle.Stop();
        if (combo30Particle != null) combo30Particle.Stop();
        if (combo50Particle != null) combo50Particle.Stop();
    }

    // 콤보 증가
    public void AddCombo()
    {
        currentCombo++;
        UpdateComboUI();
        UpdateNPCSpeed();
        CheckComboMilestones();
    }

    // 콤보 초기화
    public void ResetCombo()
    {
        currentCombo = 0;
        combo10Achieved = false;
        combo30Achieved = false;
        combo50Achieved = false;
        UpdateComboUI();
        UpdateNPCSpeed();
    }

    // 콤보 마일스톤 체크 및 파티클 재생
    private void CheckComboMilestones()
    {
        // 10 콤보 달성
        if (currentCombo == 10 && !combo10Achieved && combo10Particle != null)
        {
            combo10Particle.Play();
            combo10Achieved = true;
        }
        // 30 콤보 달성
        else if (currentCombo == 30 && !combo30Achieved && combo30Particle != null)
        {
            combo30Particle.Play();
            combo30Achieved = true;
        }
        // 50 콤보 달성
        else if (currentCombo == 50 && !combo50Achieved && combo50Particle != null)
        {
            combo50Particle.Play();
            combo50Achieved = true;
        }
    }

    // UI 업데이트
    private void UpdateComboUI()
    {
        if (comboText != null)
        {
            comboText.text = currentCombo.ToString();
        }
    }

    // NPC 애니메이터 속도 업데이트
    private void UpdateNPCSpeed()
    {
        if (npcAnimator != null)
        {
            float normalizedCombo = Mathf.Clamp01((float)currentCombo / maxComboForSpeed);
            float speed = Mathf.Lerp(minSpeed, maxSpeed, normalizedCombo);
            npcAnimator.SetFloat(speedParameterName, speed);
        }
    }

    // 현재 콤보 반환 (외부 참조용)
    public int GetCurrentCombo()
    {
        return currentCombo;
    }
}