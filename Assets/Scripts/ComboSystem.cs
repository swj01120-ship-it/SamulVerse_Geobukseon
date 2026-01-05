using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComboSystem : MonoBehaviour
{
    [Header("UI")]
    public Text comboText;
    public Text comboLabelText;

    [Header("NPC")]
    public Animator npcAnimator;
    public string speedParameterName = "Speed";

    [Header("Settings")]
    public int baseCombo = 0;
    public float minSpeed = 0.5f;
    public float maxSpeed = 2.0f;
    public int maxComboForSpeed = 50;

    [Header("Combo Particles")]
    public ParticleSystem combo10Particle;
    public ParticleSystem combo30Particle;
    public ParticleSystem combo50Particle;

    private int currentCombo = 0;

    void Start()
    {
        UpdateComboUI();
    }

    public void AddCombo()
    {
        currentCombo++;
        UpdateComboUI();
        UpdateNPCSpeed();

        // ¡Ú ÄÞº¸ ÆÄÆ¼Å¬ ¡Ú
        if (currentCombo == 10 && combo10Particle != null)
            combo10Particle.Play();
        else if (currentCombo == 30 && combo30Particle != null)
            combo30Particle.Play();
        else if (currentCombo == 50 && combo50Particle != null)
            combo50Particle.Play();
    }

    public void ResetCombo()
    {
        currentCombo = 0;
        UpdateComboUI();
        UpdateNPCSpeed();
    }

    void UpdateComboUI()
    {
        if (comboText != null)
        {
            if (currentCombo > 0)
            {
                comboText.text = currentCombo.ToString();

                // »ö»ó º¯°æ
                if (currentCombo >= 50)
                    comboText.color = Color.red;
                else if (currentCombo >= 30)
                    comboText.color = Color.yellow;
                else if (currentCombo >= 10)
                    comboText.color = Color.green;
                else
                    comboText.color = Color.white;
            }
            else
            {
                comboText.text = "";
            }
        }

        // COMBO ¶óº§ Ç¥½Ã/¼û±è
        if (comboLabelText != null)
        {
            comboLabelText.gameObject.SetActive(currentCombo > 0);
        }
    }

    void UpdateNPCSpeed()
    {
        if (npcAnimator != null)
        {
            float t = Mathf.Clamp01((float)currentCombo / maxComboForSpeed);
            float speed = Mathf.Lerp(minSpeed, maxSpeed, t);

            npcAnimator.SetFloat(speedParameterName, speed);
        }
    }

    public int GetCombo()
    {
        return currentCombo;
    }
}