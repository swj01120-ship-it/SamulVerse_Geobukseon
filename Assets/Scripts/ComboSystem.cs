using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ComboSystem : MonoBehaviour
{
    [Header("콤보 설정")]
    public int currentCombo = 0;
    public float comboResetTime = 2f; // 2초간 타격 없으면 콤보 리셋
    private float lastHitTime;

    [Header("NPC 연동")]
    public Animator[] npcAnimators; // NPC Animator 배열
    public string rowingAnimationName = "Rowing"; // 노 젓기 애니메이션 이름

    [Header("콤보 단계별 속도")]
    public float[] speedLevels = { 0.5f, 1f, 1.5f, 2f, 2.5f }; // 콤보 구간별 속도
    public int[] comboThresholds = { 0, 10, 30, 50, 100 }; // 콤보 임계값

    void Update()
    {
        // 콤보 리셋 체크
        if (Time.time - lastHitTime > comboResetTime && currentCombo > 0)
        {
            ResetCombo();
        }
    }

    public void AddCombo()
    {
        currentCombo++;
        lastHitTime = Time.time;
        UpdateNPCSpeed();

        Debug.Log($"콤보: {currentCombo}");
    }

    void UpdateNPCSpeed()
    {
        float speed = GetSpeedForCombo(currentCombo);

        // 모든 NPC의 애니메이션 속도 조절
        foreach (Animator npc in npcAnimators)
        {
            if (npc != null)
            {
                npc.speed = speed;
            }
        }
    }

    float GetSpeedForCombo(int combo)
    {
        for (int i = comboThresholds.Length - 1; i >= 0; i--)
        {
            if (combo >= comboThresholds[i])
            {
                return speedLevels[i];
            }
        }
        return speedLevels[0];
    }

    void ResetCombo()
    {
        currentCombo = 0;
        UpdateNPCSpeed();
        Debug.Log("콤보 리셋!");
    }
}