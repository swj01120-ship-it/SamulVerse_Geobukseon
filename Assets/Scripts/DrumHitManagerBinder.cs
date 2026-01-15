using UnityEngine;

public class DrumHitManagerBinder : MonoBehaviour
{
    [Tooltip("DrumHit 컴포넌트(프로젝트에 있는 실제 스크립트)")]
    public MonoBehaviour drumHit;

    void Awake()
    {
        if (drumHit == null) drumHit = GetComponent<MonoBehaviour>(); // 실수 방지용(직접 넣는 걸 권장)
    }

    void Start()
    {
        // RhythmGameManager.Instance가 살아있는지 확인
        if (RhythmGameManager.Instance == null)
        {
            var mgr = FindObjectOfType<RhythmGameManager>();
            if (mgr != null && RhythmGameManager.Instance == null)
            {
                // Awake에서 Instance 설정이 됐어야 하지만, 혹시 비활성/순서 이슈면 여기서 보장
                // (RhythmGameManager 코드 자체는 건드리지 않음)
            }
        }

        // DrumHit 쪽에 "gameManager" 같은 참조 필드가 있으면 자동으로 꽂아주기(리플렉션)
        if (drumHit != null && RhythmGameManager.Instance != null)
        {
            var t = drumHit.GetType();
            var f = t.GetField("gameManager", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(RhythmGameManager))
            {
                f.SetValue(drumHit, RhythmGameManager.Instance);
            }

            var p = t.GetProperty("gameManager", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p != null && p.CanWrite && p.PropertyType == typeof(RhythmGameManager))
            {
                p.SetValue(drumHit, RhythmGameManager.Instance, null);
            }
        }
    }
}
