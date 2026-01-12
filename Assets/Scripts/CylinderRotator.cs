using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CylinderRotator : MonoBehaviour
{
    [Header("입력 모드 설정")]
    [SerializeField] private InputMode inputMode = InputMode.Mouse;

    [Header("회전 설정")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private bool enableRotation = true;

    [Header("UI 차단 설정")]
    [SerializeField] private bool blockUIInteraction = true;
    [SerializeField] private Canvas[] canvasesToBlock; // Options Canvas만 넣기

    [Header("VR 설정 (Meta Quest용)")]
    [SerializeField] private Transform vrControllerTransform;
    [SerializeField] private string vrGrabButton = "PrimaryIndexTrigger";

    private bool isDragging = false;
    private float lastInputX = 0f;
    private float currentRotationY = 0f;

    public enum InputMode
    {
        Mouse,
        VRController
    }

    void Update()
    {
        if (!enableRotation) return;

        switch (inputMode)
        {
            case InputMode.Mouse:
                HandleMouseInput();
                break;
            case InputMode.VRController:
                HandleVRInput();
                break;
        }
    }

    void HandleMouseInput()
    {
        /* =====================
         * 마우스 버튼 DOWN
         * ===================== */
        if (Input.GetMouseButtonDown(0))
        {
            // ★ UI 위에서 누르면 아예 시작 불가
            if (blockUIInteraction && IsPointerOverBlockedCanvas())
            {
                Debug.Log("⛔ UI 클릭 - 실린더 회전 차단");
                isDragging = false;
                return;
            }

            isDragging = true;
            lastInputX = Input.mousePosition.x;
            Debug.Log("🖱️ 실린더 드래그 시작");
        }

        /* =====================
         * 드래그 중
         * ===================== */
        if (isDragging && Input.GetMouseButton(0))
        {
            // ★ 드래그 중 UI에 닿아도 즉시 중단
            if (blockUIInteraction && IsPointerOverBlockedCanvas())
            {
                Debug.Log("⛔ 드래그 중 UI 진입 - 회전 중단");
                isDragging = false;
                return;
            }

            float mouseX = Input.mousePosition.x;
            float mouseDelta = mouseX - lastInputX;

            currentRotationY -= mouseDelta * rotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, currentRotationY, 0);

            lastInputX = mouseX;
        }

        /* =====================
         * 마우스 버튼 UP
         * ===================== */
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                Debug.Log($"🖱️ 드래그 종료 - 각도: {currentRotationY}");
            }
            isDragging = false;
        }

        /* =====================
         * 키보드 보조 회전
         * ===================== */
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            currentRotationY += 60f * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            currentRotationY -= 60f * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
        }
    }

    void HandleVRInput()
    {
        Debug.LogWarning("⚠️ VR 모드 활성화됨. Meta Quest SDK 연동 필요");
    }

    /// <summary>
    /// Inspector에 등록된 Canvas 위에 포인터가 있는지 검사
    /// </summary>
    private bool IsPointerOverBlockedCanvas()
    {
        if (EventSystem.current == null)
            return false;

        if (canvasesToBlock == null || canvasesToBlock.Length == 0)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            Canvas hitCanvas = result.gameObject.GetComponentInParent<Canvas>();
            if (hitCanvas == null) continue;

            foreach (Canvas blockedCanvas in canvasesToBlock)
            {
                if (hitCanvas == blockedCanvas)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /* =====================
     * 외부 제어용 메서드
     * ===================== */

    public void SetInputMode(InputMode mode)
    {
        inputMode = mode;
        isDragging = false;
        Debug.Log($"✓ 입력 모드 변경: {mode}");
    }

    public void SetRotationEnabled(bool enabled)
    {
        enableRotation = enabled;
        if (!enabled)
            isDragging = false;
    }

    public void SetRotation(float angle)
    {
        currentRotationY = angle;
        transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }

    public void RotateToNext()
    {
        currentRotationY -= 120f;
        transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }

    public void RotateToPrevious()
    {
        currentRotationY += 120f;
        transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }

    public float GetCurrentRotation()
    {
        return currentRotationY;
    }
}
