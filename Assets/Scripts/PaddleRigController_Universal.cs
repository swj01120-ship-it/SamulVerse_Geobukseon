using System.Collections.Generic;
using UnityEngine;

public class PaddleRigController_Universal : MonoBehaviour
{
    [Header("Animator Sync")]
    public bool syncToAnimator = true;
    public Animator referenceAnimator;
    public int animatorLayer = 0;

    [Tooltip("노젓기 상태 이름(선택). 비워두면 현재 상태 normalizedTime 사용")]
    public string rowingStateName = "";
    public float paddleSpeedMultiplier = 1.0f;

    [Range(0f, 1f)]
    public float globalPhaseOffset = 0f;

    [Header("Paddle Roots (Drag paddles.006, paddles.011, ...)")]
    public List<Transform> paddleRoots = new List<Transform>();

    [Header("Skinned Paddle Bone Find")]
    [Tooltip("본 루트 이름(보통 base)")]
    public string boneRootName = "base";

    [Tooltip("본 이름 포함 문자열(보통 paddle_)")]
    public string boneNameContains = "paddle_";

    [Tooltip("이 문자열이 포함된 본은 제외(끝단 end_end... 제외용)")]
    public string excludeContains = "_end";

    [Header("Motion")]
    public float swingAngle = 20f;
    public float upDownAngle = 0f;
    public bool mirrorLeftRight = true;

    [Header("Axis (Local)")]
    public Vector3 swingAxis = Vector3.forward;
    public Vector3 upDownAxis = Vector3.right;

    [Header("Debug")]
    public bool logTargetCount = false;

    class Target
    {
        public Transform t;
        public Quaternion baseLocalRot; // 일반 메쉬용(011 같은 것)
        public bool isRight;
        public bool isBone;             // 스킨 본 여부
    }

    readonly List<Target> _targets = new List<Target>();
    bool _hasStateName;

    void Awake()
    {
        _hasStateName = !string.IsNullOrWhiteSpace(rowingStateName);
        RebuildTargets();
    }

    void OnEnable()
    {
        // Enable/Disable 반복 시에도 기본 포즈 재저장 필요할 수 있음
        RebuildTargets();
    }

    [ContextMenu("Rebuild Targets")]
    public void RebuildTargets()
    {
        _targets.Clear();

        // paddleRoots 비어있으면 자동 수집 (그래도 수동 드래그 추천)
        if (paddleRoots == null || paddleRoots.Count == 0)
        {
            paddleRoots = new List<Transform>();
            foreach (Transform child in transform)
            {
                string n = child.name.ToLowerInvariant();
                if (n.Contains("paddles") || n.Contains("paddle"))
                    paddleRoots.Add(child);
            }
        }

        foreach (var root in paddleRoots)
        {
            if (root == null) continue;

            // 1) 스킨 본 루트(base) 찾기
            Transform boneRoot = FindChildByName(root, boneRootName);

            if (boneRoot != null)
            {
                bool anyBone = TryCollectMainBones(boneRoot);
                if (anyBone) continue;
                // base가 있지만 조건에 맞는 본을 못 찾으면 일반 오브젝트로 처리
            }

            // 2) 일반 메쉬: root 자체를 회전
            _targets.Add(new Target
            {
                t = root,
                baseLocalRot = root.localRotation, // ✅ 011 등, 네가 맞춘 방향을 base로 저장
                isRight = root.name.ToLowerInvariant().Contains("_r"),
                isBone = false
            });
        }

        if (logTargetCount)
            Debug.Log($"[PaddleRig] Targets: {_targets.Count}");

        // 안전장치
        if (_targets.Count == 0)
        {
            _targets.Add(new Target
            {
                t = transform,
                baseLocalRot = transform.localRotation,
                isRight = false,
                isBone = false
            });
        }
    }

    bool TryCollectMainBones(Transform boneRoot)
    {
        string contains = (boneNameContains ?? "").ToLowerInvariant();
        string exclude = (excludeContains ?? "").ToLowerInvariant();

        bool any = false;

        foreach (var t in boneRoot.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();

            // paddle_ 포함
            if (!string.IsNullOrEmpty(contains) && !n.Contains(contains)) continue;

            // _end 같은 끝단 본 제외
            if (!string.IsNullOrEmpty(exclude) && n.Contains(exclude)) continue;

            // 메인 본만: 끝이 _l 또는 _r 인 것만(예: paddle_3_l / paddle_3_r)
            bool isL = n.EndsWith("_l") || n.Contains("_l");
            bool isR = n.EndsWith("_r") || n.Contains("_r");
            if (!isL && !isR) continue;

            _targets.Add(new Target
            {
                t = t,
                baseLocalRot = t.localRotation, // 본은 매 프레임 애니 포즈를 쓰므로 base는 참고용
                isRight = isR,
                isBone = true
            });

            any = true;
        }

        return any;
    }

    void LateUpdate()
    {
        float phase01 = GetPhase01();
        Apply(phase01);
    }

    float GetPhase01()
    {
        float t01;

        if (syncToAnimator && referenceAnimator != null && referenceAnimator.enabled)
        {
            var info = referenceAnimator.GetCurrentAnimatorStateInfo(animatorLayer);

            if (_hasStateName)
            {
                // 상태명이 다르면 그래도 현재 normalizedTime 사용(멈추는 것 방지)
                t01 = info.normalizedTime;
            }
            else
            {
                t01 = info.normalizedTime;
            }
        }
        else
        {
            // Animator가 없거나 꺼졌을 때도 테스트 가능하도록 시간 기반 fallback
            t01 = Time.time;
        }

        t01 = (t01 * paddleSpeedMultiplier + globalPhaseOffset) % 1f;
        if (t01 < 0f) t01 += 1f;
        return t01;
    }

    void Apply(float phase01)
    {
        // 왕복 리듬
        float s = Mathf.Sin(phase01 * Mathf.PI * 2f);

        float baseSwing = s * swingAngle;
        float baseUpDown = s * upDownAngle;

        Quaternion upDownRot = Quaternion.AngleAxis(baseUpDown, upDownAxis.normalized);

        for (int i = 0; i < _targets.Count; i++)
        {
            var trg = _targets[i];
            if (trg.t == null) continue;

            float swing = baseSwing;
            if (mirrorLeftRight && trg.isRight) swing = -baseSwing;

            Quaternion swingRot = Quaternion.AngleAxis(swing, swingAxis.normalized);
            Quaternion delta = swingRot * upDownRot;

            if (trg.isBone)
            {
                // ✅ 스킨 본: "이번 프레임의 애니 포즈" 위에 델타만 얹기 (Animator 덮어쓰기 방지)
                Quaternion animPose = trg.t.localRotation;
                trg.t.localRotation = animPose * delta;
            }
            else
            {
                // ✅ 일반 메쉬: 네가 맞춘 기본 방향(base) 유지 + 델타 적용 (011 방향 틀어짐 방지)
                trg.t.localRotation = trg.baseLocalRot * delta;
            }
        }
    }

    Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName)) return null;

        string tn = targetName.ToLowerInvariant();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLowerInvariant() == tn)
                return t;
        }
        return null;
    }
}
