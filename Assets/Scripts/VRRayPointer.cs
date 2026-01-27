using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VRRayPointer : MonoBehaviour
{
    [Header("Ray Settings")]
    public float rayLength = 10f;
    public Color rayColor = Color.cyan;
    public Color hitColor = Color.green;
    public float rayWidth = 0.01f;

    [Header("Controller")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Header("Physics Layer (Optional)")]
    public LayerMask physicsLayer = ~0; // Everything

    private LineRenderer lineRenderer;
    private OVRInputModule inputModule;

    private void Start()
    {
        SetupLineRenderer();
        SetupOVRInputModule();
    }

    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth * 0.5f;
        lineRenderer.positionCount = 2;

        // 런타임 안전한 기본 머티리얼
        if (lineRenderer.material == null)
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
    }

    private void SetupOVRInputModule()
    {
        inputModule = FindObjectOfType<OVRInputModule>();
        if (inputModule != null)
        {
            // OVRInputModule이 UI Ray를 쏘는 기준 Transform
            inputModule.rayTransform = transform;
            Debug.Log($"[VRRayPointer] OVRInputModule.rayTransform = {gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[VRRayPointer] OVRInputModule not found. (EventSystem에 OVR Input Module이 있어야 UI 클릭이 안정적)");
        }
    }

    private void Update()
    {
        DrawRay();
        CheckTrigger();
    }

    private void DrawRay()
    {
        if (lineRenderer == null) return;

        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;
        Vector3 endPos = startPos + direction * rayLength;

        bool hitSomething = false;

        // 1) UI Raycast (EventSystem)
        if (EventSystem.current != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                var pointerData = new PointerEventData(EventSystem.current)
                {
                    position = cam.WorldToScreenPoint(startPos)
                };

                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count > 0)
                {
                    hitSomething = true;
                    // worldPosition이 0일 수 있어 fallback 처리
                    endPos = results[0].worldPosition != Vector3.zero ? results[0].worldPosition : endPos;
                }
            }
        }

        // 2) Physics Raycast (선택)
        if (Physics.Raycast(startPos, direction, out RaycastHit hit, rayLength, physicsLayer))
        {
            hitSomething = true;
            endPos = hit.point;
        }

        Color c = hitSomething ? hitColor : rayColor;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }

    private void CheckTrigger()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller))
        {
            SimulateClick();
        }
    }

    private void SimulateClick()
    {
        if (EventSystem.current == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = cam.WorldToScreenPoint(transform.position)
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            GameObject hitObject = results[0].gameObject;
            ExecuteEvents.Execute(hitObject, pointerData, ExecuteEvents.pointerClickHandler);
        }
    }

    private void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if (lineRenderer != null) lineRenderer.enabled = true;
    }
}
