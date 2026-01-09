using UnityEngine;

public class ObstacleBeatEmission : MonoBehaviour
{
    public AudioSource audioSource;
    public Renderer targetRenderer;

    public Color emissionColor = Color.red;
    public float intensityMultiplier = 5f;
    public float smoothSpeed = 10f;

    private Material mat;
    private float currentIntensity;

    void Start()
    {
        mat = targetRenderer.material;
        mat.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        float[] samples = new float[64];
        audioSource.GetOutputData(samples, 0);

        float volume = 0f;
        foreach (float s in samples)
        {
            volume += Mathf.Abs(s);
        }

        volume /= samples.Length;

        float targetIntensity = volume * intensityMultiplier;
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothSpeed);

        Color finalColor = emissionColor * currentIntensity;
        mat.SetColor("_EmissionColor", finalColor);
    }
}