using System.Collections.Generic;
using UnityEngine;

public class PaddleRigController : MonoBehaviour
{
    [Header("Paddles")]
    public List<Transform> paddles = new List<Transform>();

    [Header("Sync (Recommended)")]
    public bool syncToAnimator = true;
    public Animator referenceAnimator;
    public string rowingStateName = "DogArmature"; // 네 상태 이름
    [Range(0f, 1f)] public float globalPhaseOffset = 0.0f;

    [Header("Motion")]
    [Tooltip("syncToAnimator=false 일 때만 의미 있음")]
    public float rowSpeed = 1.0f;

    public float swingAngle = 22f;   // 앞뒤 젓기 각도(주)
    public float upDownAngle = 0f;   // 필요 없으면 0 추천 (앞뒤만)
    public bool mirrorLeftRight = true;

    [Header("Axis")]
    public Vector3 rotationAxis = Vector3.right;     // 젓는 축
    public Vector3 secondaryAxis = Vector3.up;       // 보조축(안 쓰면 upDownAngle=0)

    [Header("Phase")]
    [Tooltip("0이면 모든 패들이 완전 동시. 분산을 원할 때만 0.05~0.2")]
    [Range(0f, 1f)] public float perPaddlePhaseOffset = 0.0f;

    [Header("Speed Tuning")]
    [Range(0.5f, 1.5f)]
    public float paddleSpeedMultiplier = 0.85f; // 0.85~0.95 추천

    struct PaddleState
    {
        public Transform t;
        public Quaternion startRot;
        public float phase01;
        public int side;
    }
    readonly List<PaddleState> _states = new List<PaddleState>();

    void Start()
    {
        BuildStates();
    }

    void BuildStates()
    {
        _states.Clear();

        // 비어 있으면 이름에 paddle 포함된 것 자동 수집 (기존 유지) :contentReference[oaicite:1]{index=1}
        if (paddles == null || paddles.Count == 0)
        {
            paddles = new List<Transform>();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains("paddle"))
                    paddles.Add(child);
            }
        }

        for (int i = 0; i < paddles.Count; i++)
        {
            if (paddles[i] == null) continue;

            int side = paddles[i].localPosition.x >= 0 ? 1 : -1;

            _states.Add(new PaddleState
            {
                t = paddles[i],
                startRot = paddles[i].localRotation,
                phase01 = (i * perPaddlePhaseOffset) % 1f,
                side = side
            });
        }
    }

    void Update()
    {
        float u01 = syncToAnimator ? Repeat01(GetAnimatorNormalized01() * paddleSpeedMultiplier)
                           : Repeat01(Time.time * rowSpeed);

        u01 = Repeat01(u01 + globalPhaseOffset);

        float basePhase = u01 * Mathf.PI * 2f;

        for (int i = 0; i < _states.Count; i++)
        {
            var s = _states[i];
            if (s.t == null) continue;

            float phase = basePhase + s.phase01 * Mathf.PI * 2f;

            float w = Mathf.Sin(phase);
            float a = w * swingAngle;

            if (mirrorLeftRight) a *= s.side;

            Quaternion rotA = Quaternion.AngleAxis(a, rotationAxis);

            Quaternion rotB = Quaternion.identity;
            if (upDownAngle != 0f)
            {
                float b = Mathf.Cos(phase) * upDownAngle;
                rotB = Quaternion.AngleAxis(b, secondaryAxis);
            }

            s.t.localRotation = s.startRot * rotA * rotB;
        }
    }

    float GetAnimatorNormalized01()
    {
        if (referenceAnimator == null) return 0f;

        var st = referenceAnimator.GetCurrentAnimatorStateInfo(0);

        // 상태가 하나뿐이면 사실상 상관 없지만, 안전하게 필터링
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
