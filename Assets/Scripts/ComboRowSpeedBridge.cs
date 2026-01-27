using System.Collections.Generic;
using UnityEngine;

public class ComboRowSpeedBridge : MonoBehaviour
{
    [Header("Combo Source")]
    [Tooltip("외부에서 콤보를 넣어줄 때 사용 (예: GameManager가 SetCombo 호출)")]
    public int currentCombo = 0;

    [Header("Rowing NPC Animators (Dogs)")]
    [Tooltip("노젓는 강아지 Animator들 (여러 마리 가능)")]
    public List<Animator> dogAnimators = new List<Animator>();

    [Header("Speed Mapping")]
    [Tooltip("콤보 0일 때 애니 속도")]
    public float baseAnimSpeed = 1.0f;

    [Tooltip("콤보가 충분히 높을 때 도달할 최대 애니 속도")]
    public float maxAnimSpeed = 1.8f;

    [Tooltip("이 콤보에 도달하면 maxAnimSpeed 근처까지 가도록 맵핑")]
    public int comboForMaxSpeed = 60;

    [Tooltip("콤보가 올라갈수록 속도가 빨라지는 곡선(0~1 입력, 0~1 출력)")]
    public AnimationCurve speedCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 0.35f),
            new Keyframe(0.6f, 0.8f),
            new Keyframe(1f, 1f)
        );

    [Header("Smoothing")]
    [Tooltip("속도 반영이 부드럽게 따라오는 정도(값이 클수록 빠르게 반응)")]
    public float speedLerp = 6f;

    float _smoothedSpeed;

    void Awake()
    {
        _smoothedSpeed = baseAnimSpeed;
    }

    void Update()
    {
        float t01 = (comboForMaxSpeed <= 0) ? 0f : Mathf.Clamp01((float)currentCombo / comboForMaxSpeed);
        float shaped = speedCurve != null ? Mathf.Clamp01(speedCurve.Evaluate(t01)) : t01;

        float targetSpeed = Mathf.Lerp(baseAnimSpeed, maxAnimSpeed, shaped);
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, targetSpeed, 1f - Mathf.Exp(-speedLerp * Time.deltaTime));

        for (int i = 0; i < dogAnimators.Count; i++)
        {
            var a = dogAnimators[i];
            if (a == null) continue;
            a.speed = _smoothedSpeed;
        }
    }

    // ✅ 외부(콤보 시스템)에서 호출용
    public void SetCombo(int combo)
    {
        currentCombo = Mathf.Max(0, combo);
    }

    // ✅ 콤보 끊김 처리(원하면 사용)
    public void ResetCombo()
    {
        currentCombo = 0;
    }
}
