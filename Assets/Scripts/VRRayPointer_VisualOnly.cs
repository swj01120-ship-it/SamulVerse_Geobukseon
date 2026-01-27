using UnityEngine;

[DisallowMultipleComponent]
public class VRRayPointer_VisualOnly : MonoBehaviour
{
    [Header("Ray Visual Settings")]
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private float rayWidth = 0.01f;

    [Header("Runtime Control")]
    [Tooltip("씬 시작 시 레이를 숨길지 (Gate가 제어해도, 시작값으로 사용됨)")]
    [SerializeField] private bool startHidden = false;

    private LineRenderer lineRenderer;
    private bool _visible;

    public bool StartHidden => startHidden;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth * 0.5f;

        if (lineRenderer.material == null)
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        SetVisible(!startHidden);
    }

    private void Update()
    {
        if (!_visible) return;

        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * rayLength;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;
        if (lineRenderer != null) lineRenderer.enabled = visible;
    }
}
