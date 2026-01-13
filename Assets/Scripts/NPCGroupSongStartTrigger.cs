using UnityEngine;

public class NPCGroupSongStartTrigger : MonoBehaviour
{
    [Header("Animator")]
    public string startTriggerName = "Start";

    [Header("Auto Collect")]
    public bool autoCollectAnimators = true;
    public Animator[] animators;

    [Header("Optional: Enable Components On Start")]
    public Behaviour[] enableOnStart; // NavMeshAgent, AI 스크립트 등

    private void Awake()
    {
        if (autoCollectAnimators)
            animators = GetComponentsInChildren<Animator>(true);

        // ✅ Start 전 완전 정지: Animator 자체 비활성화(가장 확실)
        if (animators != null)
        {
            for (int i = 0; i < animators.Length; i++)
                if (animators[i] != null)
                    animators[i].enabled = false;
        }

        // AI/이동 등도 시작 전 OFF
        if (enableOnStart != null)
        {
            for (int i = 0; i < enableOnStart.Length; i++)
                if (enableOnStart[i] != null)
                    enableOnStart[i].enabled = false;
        }
    }

    private void OnEnable()
    {
        // ✅ A 방식 이벤트로 변경
        MainGameAutoStartController.OnSongStart += HandleSongStart;
    }

    private void OnDisable()
    {
        MainGameAutoStartController.OnSongStart -= HandleSongStart;
    }

    private void HandleSongStart()
    {
        // Animator 전부 시작
        if (animators != null)
        {
            for (int i = 0; i < animators.Length; i++)
            {
                var a = animators[i];
                if (a == null) continue;

                a.enabled = true;   // ✅ 여기서 켬
                a.speed = 1f;

                if (!string.IsNullOrEmpty(startTriggerName))
                    a.SetTrigger(startTriggerName);
            }
        }

        // AI/이동 등 스크립트 ON
        if (enableOnStart != null)
        {
            for (int i = 0; i < enableOnStart.Length; i++)
                if (enableOnStart[i] != null)
                    enableOnStart[i].enabled = true;
        }
    }
}
