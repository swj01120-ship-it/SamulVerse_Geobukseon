using UnityEngine;

public class ComboVfxBooster : MonoBehaviour
{
    [Header("References")]
    public ComboSystem comboSystem;                    // 가능하면 인스펙터로 연결
    public JudgementVfxController judgementVfx;        // Hit_04 루트에 붙은 컴포넌트
    public DrumHit drumHit;                            // 같은 오브젝트의 DrumHit

    [Header("Combo Tier Settings (match your ComboSystem milestones)")]
    public int tier1 = 10;
    public int tier2 = 30;
    public int tier3 = 50;

    [Header("VFX Strength Multipliers")]
    public float tier0Vfx = 1.00f;
    public float tier1Vfx = 1.15f;
    public float tier2Vfx = 1.35f;
    public float tier3Vfx = 1.60f;

    [Header("Wire Flash Multipliers")]
    public float tier0Wire = 1.00f;
    public float tier1Wire = 1.15f;
    public float tier2Wire = 1.35f;
    public float tier3Wire = 1.60f;

    void Awake()
    {
        if (comboSystem == null) comboSystem = FindObjectOfType<ComboSystem>();
        if (drumHit == null) drumHit = GetComponent<DrumHit>();
    }

    public int GetCombo()
    {
        return comboSystem != null ? comboSystem.GetCurrentCombo() : 0; // :contentReference[oaicite:3]{index=3}
    }

    public float GetVfxMultiplier()
    {
        int c = GetCombo();
        if (c >= tier3) return tier3Vfx;
        if (c >= tier2) return tier2Vfx;
        if (c >= tier1) return tier1Vfx;
        return tier0Vfx;
    }

    public float GetWireMultiplier()
    {
        int c = GetCombo();
        if (c >= tier3) return tier3Wire;
        if (c >= tier2) return tier2Wire;
        if (c >= tier1) return tier1Wire;
        return tier0Wire;
    }

    // VFX 파티클 강도(Quest-friendly): StartSize / EmissionRate / BurstCount를 “안전 범위”에서만 스케일
    public void ApplyToParticleSystems(ParticleSystem[] systems)
    {
        if (systems == null) return;

        float m = GetVfxMultiplier();

        foreach (var ps in systems)
        {
            if (ps == null) continue;

            // Main: StartSize 스케일 (너무 과하면 화면 가림)
            var main = ps.main;
            main.startSizeMultiplier = Mathf.Clamp(main.startSizeMultiplier * m, 0.05f, 3.0f);

            // Emission: Rate를 조금만 스케일
            var em = ps.emission;
            if (em.enabled)
            {
                var rate = em.rateOverTime;
                if (rate.mode == ParticleSystemCurveMode.Constant)
                {
                    float v = rate.constant;
                    rate.constant = Mathf.Clamp(v * m, 0f, 200f);
                    em.rateOverTime = rate;
                }

                // Burst도 있으면 살짝 스케일 (Quest라서 상한 걸어둠)
                int burstCount = em.burstCount;
                if (burstCount > 0)
                {
                    ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[burstCount];
                    em.GetBursts(bursts);

                    for (int i = 0; i < bursts.Length; i++)
                    {
                        // 최소/최대 모두 스케일
                        float min = bursts[i].minCount;
                        float max = bursts[i].maxCount;
                        bursts[i].minCount = (short)Mathf.Clamp(Mathf.RoundToInt(min * m), 0, 80);
                        bursts[i].maxCount = (short)Mathf.Clamp(Mathf.RoundToInt(max * m), 0, 120);
                    }
                    em.SetBursts(bursts);
                }
            }
        }
    }
}
