using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PaddleRigControllerSyncToAnimator : MonoBehaviour
{
    [Header("Reference Animator (Rowing NPC)")]
    public Animator referenceAnimator;

    [Tooltip("노 젓는 애니메이션 State 이름. 비우면 현재 state의 normalizedTime 사용")]
    public string rowingStateName = "";

    [Header("Phase Tuning (Drag in Play Mode)")]
    [Range(0f, 1f)]
    public float globalPhaseOffset = 0.0f;  // ⭐ 이걸 드래그로 맞춘다

    [Tooltip("좌/우를 반대로 움직이게 할지")]
    public bool mirrorLeftRight = true;

    [Header("Paddles (optional)")]
    public List<Transform> paddles = new List<Transform>();

    [Header("Motion")]
    public float swingAngle = 25f;
    public float upDownAngle = 8f;

    [Tooltip("패들마다 위상 차이(0~1). 많을수록 순차적으로 젓는 느낌")]
    [Range(0f, 1f)]
    public float perPaddlePhaseOffset = 0.20f;

    [Header("Axis")]
    public Vector3 rotationAxis = Vector3.right;
    public Vector3 secondaryAxis = Vector3.up;

    [Header("Auto Collect")]
    public bool autoCollectByName = true;
    public string nameContains = "paddle"; // paddles.008 같은 것도 포함됨

    struct PaddleState
    {
        public Transform t;
        public Quaternion startRot;
        public float phase01;
        public int side; // -1 left, +1 right
    }
    readonly List<PaddleState> _states = new List<PaddleState>();
    int _stateHash;

    void OnEnable()
    {
        _stateHash = string.IsNullOrEmpty(rowingStateName) ? 0 : Animator.StringToHash(rowingStateName);
        BuildStates();
    }

    void OnValidate()
    {
        // 편집 중에도 바로 갱신
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

        for (int i = 0; i < paddles.Count; i++)
        {
            var t = paddles[i];
            if (t == null) continue;

            int side = t.localPosition.x >= 0 ? 1 : -1;

            _states.Add(new PaddleState
            {
                t = t,
                startRot = t.localRotation,
                phase01 = (i * perPaddlePhaseOffset) % 1f,
                side = side
            });
        }
    }

    void Update()
    {
        // normalized 0~1 진행도
        float u = GetAnimatorNormalized01();
        u = Repeat01(u + globalPhaseOffset);

        float basePhase = u * Mathf.PI * 2f;

        for (int i = 0; i < _states.Count; i++)
        {
            var s = _states[i];
            if (s.t == null) continue;

            float phase = basePhase + (s.phase01 * Mathf.PI * 2f);

            float dir = 1f;
            if (mirrorLeftRight) dir *= s.side;

            float w = Mathf.Sin(phase) * dir;
            float a = w * swingAngle;

            float b = Mathf.Cos(phase) * upDownAngle;

            Quaternion rotA = Quaternion.AngleAxis(a, rotationAxis);
            Quaternion rotB = Quaternion.AngleAxis(b, secondaryAxis);

            s.t.localRotation = s.startRot * rotA * rotB;
        }
    }

    float GetAnimatorNormalized01()
    {
        // Animator 없으면 시간 기반(테스트용)
        if (referenceAnimator == null)
            return Repeat01((float)Time.realtimeSinceStartup * 0.5f);

        var st = referenceAnimator.GetCurrentAnimatorStateInfo(0);

        // state 이름을 지정했다면 해당 state일 때만 사용 (아니면 현재 state 사용)
        // 정확히 필터링하고 싶으면 아래 if를 켜서 사용해도 됨.
        // if (_stateHash != 0 && !st.IsName(rowingStateName)) { }

        float nt = st.normalizedTime;
        return nt - Mathf.Floor(nt);
    }

    static float Repeat01(float v)
    {
        v = v - Mathf.Floor(v);
        return Mathf.Clamp01(v);
    }
}
