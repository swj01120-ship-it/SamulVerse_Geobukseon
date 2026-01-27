using UnityEngine;

public class SkyboxDriver : MonoBehaviour
{
    [Header("Skybox")]
    public Material sourceSkybox;     // SkyEerie (에셋)
    private Material runtimeSkybox;

    [Header("Combo Speed")]
    public float speedMul = 1f;       // ComboSystem에서 주입

    [Header("Forward Feel Tuning")]
    [Tooltip("기본 회전(빙글빙글 느낌 방지 위해 낮게 추천: 0~0.5)")]
    public float baseRotateSpeed = 0.25f;

    [Tooltip("좌/우로 지나가는 느낌(진폭). 10~25 사이 추천")]
    public float swayAngle = 18f;

    [Tooltip("좌/우 스윙 주기(Hz). 0.08~0.18 추천")]
    public float swayFrequency = 0.12f;

    [Tooltip("속도 올라갈수록 sway도 커지게 할지")]
    public bool scaleSwayWithSpeed = true;

    [Tooltip("속도 올라갈수록 sway 주기도 빨라지게 할지")]
    public bool scaleFreqWithSpeed = true;

    private float baseRot = 0f;

    void Awake()
    {
        if (sourceSkybox == null)
        {
            Debug.LogError("[SkyboxDriver] sourceSkybox가 비어있음");
            enabled = false;
            return;
        }

        runtimeSkybox = new Material(sourceSkybox);
        RenderSettings.skybox = runtimeSkybox;
    }

    void Update()
    {
        if (runtimeSkybox == null) return;

        // 1) 빙글빙글 회전은 아주 약하게만
        baseRot += baseRotateSpeed * speedMul * Time.deltaTime;

        // 2) 좌/우로 지나가는 느낌(스윙)
        float sMul = scaleSwayWithSpeed ? Mathf.Lerp(1f, 1.6f, Mathf.Clamp01(speedMul - 1f)) : 1f;
        float fMul = scaleFreqWithSpeed ? Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(speedMul - 1f)) : 1f;

        float angle = swayAngle * sMul;
        float freq = swayFrequency * fMul;

        // sin 파형으로 좌/우 흔들림
        float sway = Mathf.Sin(Time.time * (Mathf.PI * 2f) * freq) * angle;

        // 최종 회전값: baseRot + sway
        float rot = baseRot + sway;

        if (runtimeSkybox.HasProperty("_Rotation"))
        {
            runtimeSkybox.SetFloat("_Rotation", rot);
        }
    }

    void OnDestroy()
    {
        if (runtimeSkybox != null)
            Destroy(runtimeSkybox);
    }
}
