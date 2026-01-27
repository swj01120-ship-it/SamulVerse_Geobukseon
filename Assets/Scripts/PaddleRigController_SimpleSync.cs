using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PaddleRigController_SimpleSync : MonoBehaviour
{
    [Header("Reference Animator (Rowing NPC)")]
    public Animator referenceAnimator;
    [Tooltip("Animator state 이름(네 경우: DogArmature). 비우면 현재 상태 사용")]
    public string rowingStateName = "DogArmature";

    [Header("Phase (Drag in Play Mode)")]
    [Range(0f, 1f)]
    public float globalPhaseOffset = 0.0f;

    [Header("Paddles")]
    public List<Transform> paddles = new List<Transform>();

    [Header("Auto Collect (optional)")]
    public bool autoCollectByName = true;
    public string nameContains = "paddle";

    [Header("Motion")]
    public float swingAngle = 22f;            // 앞뒤 젓기 각도
    public Vector3 rotationAxis = Vector3.right; // 로컬 X축 기준(필요시 바꿔)

    [Tooltip("좌/우를 반대로 움직이게(더 자연스러움)")]
    public bool mirrorLeftRight = true;

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

        float phase = u * Mathf.PI * 2f;

        float w = Mathf.Sin(phase); // 전부 동일한 위상(분산 0)
        float aBase = w * swingAngle;

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

        // state가 하나뿐이면 사실상 상관 없지만, 정확히 DogArmature일 때만 쓰고 싶으면:
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

