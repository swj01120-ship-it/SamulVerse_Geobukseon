using UnityEngine;
using UnityEngine.EventSystems;

public class VRRayPointer : MonoBehaviour
{
    [Header("Ray Settings")]
    public float rayLength = 10f;
    public Color rayColor = Color.cyan;
    public float rayWidth = 0.01f;

    [Header("Controller")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    private LineRenderer lineRenderer;
    private OVRInputModule inputModule;

    void Start()
    {
        // Line Renderer 생성
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth * 0.5f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
        lineRenderer.positionCount = 2;

        // OVR Input Module 찾아서 Ray Transform 설정
        inputModule = FindObjectOfType<OVRInputModule>();
        if (inputModule != null)
        {
            inputModule.rayTransform = transform;
            Debug.Log("✅ Ray Transform set to: " + gameObject.name);
        }
        else
        {
            Debug.LogWarning("⚠️ OVRInputModule not found!");
        }
    }

    void Update()
    {
        DrawRay();
    }

    void DrawRay()
    {
        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;
        Vector3 endPos = startPos + direction * rayLength;

        // Raycast로 UI 감지
        RaycastHit hit;
        int layerMask = 1 << LayerMask.NameToLayer("UI");

        if (Physics.Raycast(startPos, direction, out hit, rayLength, layerMask))
        {
            endPos = hit.point;
            lineRenderer.startColor = Color.green; // 히트 시 초록색
            lineRenderer.endColor = Color.green;
        }
        else
        {
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayColor;
        }

        // 레이저 그리기
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }

    void OnDisable()
    {
        // 레이저 숨기기
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    void OnEnable()
    {
        // 레이저 보이기
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }
}
