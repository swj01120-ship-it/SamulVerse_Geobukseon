using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PaddleRigController_CurveSync : MonoBehaviour
{
    [Header("Reference Animator (Rowing NPC)")]
    public Animator referenceAnimator;
    public string rowingStateName = "DogArmature";

    [Header("Phase (Drag in Play Mode)")]
    [Range(0f, 1f)]
    public float globalPhaseOffset = 0.0f;

    [Header("Paddles")]
    public List<Transform> paddles = new List<Transform>();

    [Header("Auto Collect (optional)")]
    public bool autoCollectByName = true;
    public string nameContains = "paddle";

    [Header("Motion (X Axis Only)")]
    public float swingAngle = 16f;                 // 최대 회전각
    public bool mirrorLeftRight = true;            // 좌/우 반대로 젓기(자연스러움)

    [Tooltip("로컬 X축만 회전")]
    public Vector3 rotationAxis = Vector3.right;

    [Header("Stroke Shape (Curve)")]
    [Tooltip("0~1 입력을 받아 -1~+1 출력이 되도록 커브를 구성하면 가장 편해요.")]
    public AnimationCurve strokeCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.5f, 0f),
            new Keyframe(0.75f, -1f),
            new Keyframe(1f, 0f)
        );

    [Tooltip("커브 출력값에 추가로 곱하는 강도(세밀 조정)")]
    public float curveAmplitude = 1.0f;

    [Tooltip("커브 출력값의 상한/하한을 제한(과도한 값 방지)")]
    public float curveClamp = 1.2f;

    struct PaddleState
    {
        public Transform t;
        public Quaternion startRot;
        public int side; // -1 / +1
    }
    readonly List<PaddleState> _states = new List<PaddleState>();

    void OnEnable()
    {
        BuildStates();
    }

    void OnValidate()
    {
        BuildStates();
    }

    void BuildStates()
    {
        _states.Clear();

        if ((paddles == null || paddles.Count == 0) && autoCollectByName)
        {
            paddles = new List<Transform>();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == null) continue;
                if (child.name.ToLower().Contains(nameContains.ToLower()))
                    paddles.Add(child);
            }
        }

        if (paddles == null) return;

        for (int i = 0; i < paddles.Count; i++)
        {
            var t = paddles[i];
            if (t == null) continue;

            int side = t.localPosition.x >= 0 ? 1 : -1;

            _states.Add(new PaddleState
            {
                t = t,
                startRot = t.localRotation,
                side = side
            });
        }
    }

    void Update()
    {
        float u = GetAnimatorNormalized01();
        u = Repeat01(u + globalPhaseOffset);

        // 커브는 0~1 입력
        float c = strokeCurve != null ? strokeCurve.Evaluate(u) : Mathf.Sin(u * Mathf.PI * 2f);

        // 커브 출력 안정화
        c *= curveAmplitude;
        c = Mathf.Clamp(c, -curveClamp, curveClamp);

        // 최종 각도(-angle ~ +angle)
        float aBase = c * swingAngle;

        for (int i = 0; i < _states.Count; i++)
        {
            var s = _states[i];
            if (s.t == null) continue;

            float a = aBase;
            if (mirrorLeftRight) a *= s.side;

            s.t.localRotation = s.startRot * Quaternion.AngleAxis(a, rotationAxis);
        }
    }

    float GetAnimatorNormalized01()
    {
        if (referenceAnimator == null)
            return Repeat01((float)Time.realtimeSinceStartup * 0.3f);

        var st = referenceAnimator.GetCurrentAnimatorStateInfo(0);

        if (!string.IsNullOrEmpty(rowingStateName) && !st.IsName(rowingStateName))
            return 0f;

        float nt = st.normalizedTime;
        return nt - Mathf.Floor(nt);
    }

    static float Repeat01(float v)
    {
        v = v - Mathf.Floor(v);
        return Mathf.Clamp01(v);
    }
}
