using System.Collections.Generic;
using UnityEngine;

public class PaddleRigController_Manual : MonoBehaviour
{
    [Header("Sync Source (optional)")]
    public bool syncToAnimator = true;
    public Animator referenceAnimator;
    public int animatorLayer = 0;

    [Header("Speed / Phase (Global)")]
    public float paddleSpeedMultiplier = 1.0f;
    [Range(0f, 1f)] public float globalPhaseOffset = 0f;

    [Header("Targets (Drag exact transforms here)")]
    public List<Transform> leftTargets = new List<Transform>();
    public List<Transform> rightTargets = new List<Transform>();

    [Tooltip("011 같은 일반 Mesh 패들")]
    public List<Transform> meshTargets = new List<Transform>();

    [Header("Mesh Only Phase Fix (011 엇박자 보정)")]
    [Range(-1f, 1f)]
    [Tooltip("011만 엇박자일 때 여기 값을 ±로 조절하세요. 0.25=4분의1박 이동")]
    public float meshPhaseOffset = 0f;

    [Header("Motion")]
    public float swingAngle = 30f;
    public float upDownAngle = 0f;

    [Header("Axis (Local)")]
    public Vector3 swingAxis = Vector3.right;
    public Vector3 upDownAxis = Vector3.forward;

    [Header("Options")]
    public bool mirrorRight = true;
    public bool useLateUpdate = true;

    readonly Dictionary<Transform, Quaternion> _baseRot = new Dictionary<Transform, Quaternion>();

    void Awake() => CacheBaseRotations();
    void OnEnable() => CacheBaseRotations();

    [ContextMenu("Cache Base Rotations")]
    public void CacheBaseRotations()
    {
        _baseRot.Clear();
        CacheList(leftTargets);
        CacheList(rightTargets);
        CacheList(meshTargets);
    }

    void CacheList(List<Transform> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null) continue;
            if (!_baseRot.ContainsKey(t))
                _baseRot.Add(t, t.localRotation);
        }
    }

    void Update()
    {
        if (!useLateUpdate) Tick();
    }

    void LateUpdate()
    {
        if (useLateUpdate) Tick();
    }

    void Tick()
    {
        float phase01 = GetPhase01(globalPhaseOffset);
        float meshPhase01 = GetPhase01(globalPhaseOffset + meshPhaseOffset); // ✅ 011만 보정

        // 스킨 패들(좌/우)
        ApplyList(leftTargets, phase01, isRight: false);
        ApplyList(rightTargets, phase01, isRight: true);

        // 011 등 일반 메쉬
        ApplyMeshList(meshTargets, meshPhase01);
    }

    void ApplyList(List<Transform> list, float phase01, bool isRight)
    {
        if (list == null) return;

        float s = Mathf.Sin(phase01 * Mathf.PI * 2f);
        float swing = s * swingAngle;
        float upDown = s * upDownAngle;

        if (isRight && mirrorRight) swing = -swing;

        Quaternion delta =
            Quaternion.AngleAxis(swing, swingAxis.normalized) *
            Quaternion.AngleAxis(upDown, upDownAxis.normalized);

        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null) continue;

            if (!_baseRot.TryGetValue(t, out var baseRot))
            {
                baseRot = t.localRotation;
                _baseRot[t] = baseRot;
            }

            t.localRotation = baseRot * delta;
        }
    }

    void ApplyMeshList(List<Transform> list, float phase01)
    {
        if (list == null) return;

        float s = Mathf.Sin(phase01 * Mathf.PI * 2f);
        float swing = s * swingAngle;
        float upDown = s * upDownAngle;

        Quaternion delta =
            Quaternion.AngleAxis(swing, swingAxis.normalized) *
            Quaternion.AngleAxis(upDown, upDownAxis.normalized);

        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null) continue;

            if (!_baseRot.TryGetValue(t, out var baseRot))
            {
                baseRot = t.localRotation;
                _baseRot[t] = baseRot;
            }

            t.localRotation = baseRot * delta;
        }
    }

    float GetPhase01(float phaseOffset01)
    {
        float t01;

        if (syncToAnimator && referenceAnimator != null && referenceAnimator.enabled)
        {
            var info = referenceAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
            t01 = info.normalizedTime;
        }
        else
        {
            t01 = Time.time;
        }

        t01 = (t01 * paddleSpeedMultiplier + phaseOffset01) % 1f;
        if (t01 < 0f) t01 += 1f;
        return t01;
    }
}
