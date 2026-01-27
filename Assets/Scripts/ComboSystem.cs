using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ComboSystem : MonoBehaviour
{
    [Header("UI")]
    public Text comboText;

    [Header("Speed Curve")]
    public float minSpeed = 0.8f;
    public float maxSpeed = 1.6f;
    public int maxComboForSpeed = 50;

    // =========================
    // Combo Particles
    // =========================
    [Header("Combo Particles (Prefab or Scene ParticleSystem)")]
    [Tooltip("씬에 있는 ParticleSystem을 넣어도 되고, 프리팹을 넣어도 됩니다.")]
    public ParticleSystem combo10Particle;
    public ParticleSystem combo30Particle;
    public ParticleSystem combo50Particle;

    [Header("Combo Particle Spawn Points")]
    [Tooltip("10콤보 파티클이 나갈 위치(Transform)")]
    public Transform combo10SpawnPoint;
    [Tooltip("30콤보 파티클이 나갈 위치(Transform)")]
    public Transform combo30SpawnPoint;
    [Tooltip("50콤보 파티클이 나갈 위치(Transform)")]
    public Transform combo50SpawnPoint;

    [Header("Combo Particle Spawn Options")]
    [Tooltip("체크하면 파티클을 스폰포인트 위치에 '생성(Instantiate)'해서 재생합니다. (프리팹일 때 추천)")]
    public bool instantiateParticleOnPlay = true;

    [Tooltip("생성한 파티클을 몇 초 후 자동 파괴(메모리 누수 방지). 0이면 파괴 안 함")]
    public float destroySpawnedParticleAfter = 5f;

    // =========================
    // Targets - NPC
    // =========================
    [Header("Targets - NPC (Animators)")]
    [Tooltip("양쪽/여러 NPC Animator를 전부 넣어주면 일괄 적용")]
    public List<Animator> npcAnimators = new List<Animator>();

    [Tooltip("Animator.speed를 직접 적용")]
    public bool driveNpcAnimatorSpeed = true;

    [Tooltip("Animator 파라미터로도 속도 적용(컨트롤러가 파라미터 기반일 때만)")]
    public bool driveNpcAnimatorParameter = false;

    [Tooltip("파라미터 기반 속도를 쓸 때의 파라미터 이름")]
    public string speedParameterName = "Speed";

    // =========================
    // Targets - Paddle Rig (NO hard type)
    // =========================
    [Header("Targets - Paddle Rig")]
    [Tooltip("PaddleRigController_Manual / PaddleRigController_Universal 등, 패들 제어 스크립트 컴포넌트를 드래그")]
    public MonoBehaviour paddleRigBehaviour;

    public bool drivePaddleSpeed = true;

    [Tooltip("콤보 0일 때 패들 멀티플라이어 기본값")]
    public float basePaddleMultiplier = 0.85f;

    [Tooltip("콤보 속도(speedMul)에 추가로 곱해지는 스케일")]
    public float paddleExtraScale = 1.0f;

    [Tooltip("PaddleRig가 syncToAnimator=true 일 때도 추가 가속을 줄지(기본 OFF 추천)")]
    public bool alsoScalePaddleWhenSyncToAnimator = false;

    // =========================
    // Targets - World / Boat Speed
    // =========================
    [Header("Targets - Boat / World Speed (Generic)")]
    public List<MonoBehaviour> worldSpeedComponents = new List<MonoBehaviour>();

    public string[] fieldNameCandidates = new string[]
    {
        "speed", "moveSpeed", "scrollSpeed", "worldSpeed", "rowSpeed", "rotationSpeed"
    };

    public float worldSpeedMultiplier = 1.0f;

    // =========================
    // Runtime
    // =========================
    private int currentCombo = 0;
    private bool combo10Achieved = false;
    private bool combo30Achieved = false;
    private bool combo50Achieved = false;

    public float CurrentSpeedMul { get; private set; } = 1f;

    void Start()
    {
        UpdateComboUI();
        StopAllComboParticles();
        ApplySpeedFromCombo();
    }

    // ---- Public API ----
    public void AddCombo()
    {
        currentCombo++;
        UpdateComboUI();
        ApplySpeedFromCombo();
        CheckComboMilestones();
    }

    public void ResetCombo()
    {
        currentCombo = 0;
        combo10Achieved = combo30Achieved = combo50Achieved = false;
        UpdateComboUI();
        ApplySpeedFromCombo();
    }

    public int GetCurrentCombo() => currentCombo;

    // ---- Internals ----
    private void UpdateComboUI()
    {
        if (comboText != null)
            comboText.text = currentCombo.ToString();
    }

    private void StopAllComboParticles()
    {
        // 씬에 있는 파티클이면 정리. (프리팹 참조면 여기서 Stop해도 상관없음)
        StopAndClear(combo10Particle);
        StopAndClear(combo30Particle);
        StopAndClear(combo50Particle);
    }

    private void StopAndClear(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void CheckComboMilestones()
    {
        if (currentCombo == 10 && !combo10Achieved)
        {
            PlayComboParticle(combo10Particle, combo10SpawnPoint);
            combo10Achieved = true;
        }
        else if (currentCombo == 30 && !combo30Achieved)
        {
            PlayComboParticle(combo30Particle, combo30SpawnPoint);
            combo30Achieved = true;
        }
        else if (currentCombo == 50 && !combo50Achieved)
        {
            PlayComboParticle(combo50Particle, combo50SpawnPoint);
            combo50Achieved = true;
        }
    }

    private void PlayComboParticle(ParticleSystem ps, Transform spawnPoint)
    {
        if (ps == null)
        {
            Debug.LogWarning("[ComboSystem] ParticleSystem reference is NULL.");
            return;
        }

        // 스폰포인트 없으면: 그냥 해당 파티클 위치에서라도 재생
        if (spawnPoint == null)
        {
            Debug.LogWarning("[ComboSystem] SpawnPoint is NULL. Playing particle at its current position.");
            StopAndClear(ps);
            ps.Play();
            return;
        }

        if (instantiateParticleOnPlay)
        {
            // ps가 프리팹이든 씬 오브젝트든 상관없이 "새로 복제해서" 스폰 위치에서 재생
            ParticleSystem spawned = Instantiate(ps, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            spawned.transform.localScale = Vector3.one;

            spawned.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            spawned.Play();

            if (destroySpawnedParticleAfter > 0f)
                Destroy(spawned.gameObject, destroySpawnedParticleAfter);
        }
        else
        {
            // 씬에 있는 파티클을 스폰 위치로 옮겨서 재생 (한 개만 재사용)
            ps.transform.position = spawnPoint.position;
            ps.transform.rotation = spawnPoint.rotation;

            StopAndClear(ps);
            ps.Play();
        }
    }

    private void ApplySpeedFromCombo()
    {
        float t = (maxComboForSpeed <= 0) ? 0f : Mathf.Clamp01((float)currentCombo / maxComboForSpeed);
        float speedMul = Mathf.Lerp(minSpeed, maxSpeed, t);
        CurrentSpeedMul = speedMul;

        ApplyNpcSpeed(speedMul);
        ApplyPaddleSpeed(speedMul);
        ApplyWorldSpeed(speedMul);
    }

    private void ApplyNpcSpeed(float speedMul)
    {
        if (npcAnimators == null) return;

        for (int i = 0; i < npcAnimators.Count; i++)
        {
            var a = npcAnimators[i];
            if (a == null) continue;

            if (driveNpcAnimatorSpeed) a.speed = speedMul;

            if (driveNpcAnimatorParameter && !string.IsNullOrEmpty(speedParameterName))
                a.SetFloat(speedParameterName, speedMul);
        }
    }

    private void ApplyPaddleSpeed(float speedMul)
    {
        if (!drivePaddleSpeed) return;
        if (paddleRigBehaviour == null) return;

        bool syncToAnimator = GetBoolMember(paddleRigBehaviour, "syncToAnimator", defaultValue: false);

        float mul = basePaddleMultiplier;
        if (!syncToAnimator || alsoScalePaddleWhenSyncToAnimator)
            mul *= (speedMul * paddleExtraScale);

        SetFloatMember(paddleRigBehaviour, "paddleSpeedMultiplier", mul);

        if (!syncToAnimator)
            SetFloatMember(paddleRigBehaviour, "rowSpeed", speedMul);
    }

    private void ApplyWorldSpeed(float speedMul)
    {
        if (worldSpeedComponents == null || worldSpeedComponents.Count == 0) return;

        float v = speedMul * worldSpeedMultiplier;

        for (int i = 0; i < worldSpeedComponents.Count; i++)
        {
            var mb = worldSpeedComponents[i];
            if (mb == null) continue;

            ApplyFloatByCandidates(mb, fieldNameCandidates, v);
        }
    }

    // =========================
    // Reflection helpers
    // =========================
    private static bool GetBoolMember(MonoBehaviour mb, string name, bool defaultValue)
    {
        var type = mb.GetType();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        var f = type.GetField(name, flags);
        if (f != null && f.FieldType == typeof(bool))
            return (bool)f.GetValue(mb);

        var p = type.GetProperty(name, flags);
        if (p != null && p.PropertyType == typeof(bool) && p.CanRead)
            return (bool)p.GetValue(mb, null);

        return defaultValue;
    }

    private static void SetFloatMember(MonoBehaviour mb, string name, float value)
    {
        var type = mb.GetType();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        var f = type.GetField(name, flags);
        if (f != null && f.FieldType == typeof(float))
        {
            f.SetValue(mb, value);
            return;
        }

        var p = type.GetProperty(name, flags);
        if (p != null && p.PropertyType == typeof(float) && p.CanWrite)
        {
            p.SetValue(mb, value, null);
            return;
        }
    }

    private static void ApplyFloatByCandidates(MonoBehaviour mb, string[] candidates, float value)
    {
        if (candidates == null) return;

        var type = mb.GetType();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        for (int i = 0; i < candidates.Length; i++)
        {
            string name = candidates[i];
            if (string.IsNullOrEmpty(name)) continue;

            var f = type.GetField(name, flags);
            if (f != null && f.FieldType == typeof(float))
            {
                f.SetValue(mb, value);
                return;
            }

            var p = type.GetProperty(name, flags);
            if (p != null && p.PropertyType == typeof(float) && p.CanWrite)
            {
                p.SetValue(mb, value, null);
                return;
            }
        }
    }
}
