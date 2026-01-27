using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개발 중 잠금 처리를 위한 옵션 버튼
/// Inspector에서 isLocked만 체크 해제하면 즉시 사용 가능
/// </summary>
public class LockedOptionButton : MonoBehaviour
{
    [Header("잠금 설정")]
    [SerializeField] private bool isLocked = true;

    [Header("잠금 UI")]
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Sprite lockIconSprite;

    [Header("오버레이 색상")]
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.3f); // ★ 0.5에서 0.3으로 변경 (더 투명)

    [Header("자동 참조")]
    private Button button;
    private OptionButton optionButton;
    private Image overlayImage;

    private void Awake()
    {
        button = GetComponent<Button>();
        optionButton = GetComponent<OptionButton>();

        // 잠금 오버레이 자동 생성
        if (lockOverlay == null && isLocked)
        {
            CreateLockOverlay();
        }

        ApplyLockState();
    }

    private void CreateLockOverlay()
    {
        // 오버레이 GameObject 생성
        lockOverlay = new GameObject("LockOverlay");
        lockOverlay.transform.SetParent(transform, false);

        // RectTransform 설정
        RectTransform rt = lockOverlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        // Image 추가 (더 투명한 검은색 배경)
        overlayImage = lockOverlay.AddComponent<Image>();
        overlayImage.color = overlayColor; // ★ Inspector에서 조절 가능
        overlayImage.raycastTarget = true; // 클릭 차단

        // 자물쇠 아이콘 생성 (선택사항)
        if (lockIconSprite != null)
        {
            GameObject lockIcon = new GameObject("LockIcon");
            lockIcon.transform.SetParent(lockOverlay.transform, false);

            RectTransform iconRt = lockIcon.AddComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(40, 40);

            Image iconImage = lockIcon.AddComponent<Image>();
            iconImage.sprite = lockIconSprite;
            iconImage.color = new Color(1, 1, 1, 0.9f); // ★ 더 선명한 아이콘
        }

        Debug.Log($"✓ {gameObject.name}에 잠금 오버레이 생성");
    }

    private void ApplyLockState()
    {
        if (isLocked)
        {
            // 버튼 비활성화
            if (button != null)
            {
                button.interactable = false;
            }

            // 오버레이 표시
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(true);

                // ★ 오버레이 색상 적용
                if (overlayImage != null)
                {
                    overlayImage.color = overlayColor;
                }
            }

            Debug.Log($"🔒 {gameObject.name} 잠금 상태");
        }
        else
        {
            // 버튼 활성화
            if (button != null)
            {
                button.interactable = true;
            }

            // 오버레이 숨김
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(false);
            }

            Debug.Log($"🔓 {gameObject.name} 잠금 해제");
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        ApplyLockState();
    }

    public bool IsLocked()
    {
        return isLocked;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyLockState();
        }
    }
#endif
}