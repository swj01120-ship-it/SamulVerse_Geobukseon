using UnityEngine;

public class RowSpeedDirector : MonoBehaviour
{
    public ComboSystem comboSystem;

    [Header("Speed")]
    public float baseSpeed = 1.0f;
    public float tier1Speed = 1.15f;
    public float tier2Speed = 1.35f;
    public float tier3Speed = 1.60f;

    public int tier1 = 10;
    public int tier2 = 30;
    public int tier3 = 50;

    [Header("Targets")]
    public Animator[] npcAnimators;
    public PaddleRigController paddleController;

    void Awake()
    {
        if (comboSystem == null) comboSystem = FindObjectOfType<ComboSystem>();
    }

    void Update()
    {
        float speed = baseSpeed * GetTierMul();

        // NPC 애니메이션 속도
        if (npcAnimators != null)
        {
            for (int i = 0; i < npcAnimators.Length; i++)
            {
                if (npcAnimators[i] == null) continue;
                npcAnimators[i].speed = speed;
            }
        }

        // 패들 속도
        if (paddleController != null)
            paddleController.rowSpeed = speed;
    }

    float GetTierMul()
    {
        int combo = comboSystem != null ? comboSystem.GetCurrentCombo() : 0;

        if (combo >= tier3) return tier3Speed;
        if (combo >= tier2) return tier2Speed;
        if (combo >= tier1) return tier1Speed;
        return 1.0f;
    }
}
