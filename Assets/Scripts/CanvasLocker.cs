using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas 전체를 잠금 처리 (Inspector에서 모든 설정 가능)
/// </summary>
public class CanvasLocker : MonoBehaviour
{
    [Header("잠금 설정")]
    [SerializeField] private bool isLocked = true;

    [Header("오버레이 설정")]
    [SerializeField] private bool showOverlay = true;
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.4f);

    [Header("자물쇠 아이콘 설정")]
    [SerializeField] private bool showLockIcon = true;
    [SerializeField] private Sprite lockIconSprite;
    [SerializeField] private Vector2 iconSize = new Vector2(100, 100);
    [SerializeField] private Vector2 iconPosition = Vector2.zero;
    [SerializeField] private Color iconColor = new Color(1, 1, 1, 0.8f);

    [Header("텍스트 설정")]
    [SerializeField] private bool showText = true;
    [SerializeField] private string lockText = "준비 중";
    [SerializeField] private int fontSize = 40;
    [SerializeField] private Color textColor = new Color(1, 1, 1, 0.8f);
    [SerializeField] private Vector2 textPosition = new Vector2(0, -100);
    [SerializeField] private Font customFont;

    private GameObject lockOverlay;
    private Canvas canvas;
    private Image overlayImage;
    private Image iconImage;
    private Text textComponent;

    void Start()
    {
        canvas = GetComponent<Canvas>();

        if (isLocked)
        {
            CreateLockOverlay();
        }
    }

    void CreateLockOverlay()
    {
        if (lockOverlay != null)
        {
            Destroy(lockOverlay);
        }

        // 오버레이 GameObject 생성
        lockOverlay = new GameObject("CanvasLockOverlay");
        lockOverlay.transform.SetParent(transform, false);

        // RectTransform 설정
        RectTransform rt = lockOverlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.SetAsLastSibling();

        // 오버레이 배경
        if (showOverlay)
        {
            overlayImage = lockOverlay.AddComponent<Image>();
            overlayImage.color = overlayColor;
            overlayImage.raycastTarget = true;
        }
        else
        {
            overlayImage = lockOverlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0);
            overlayImage.raycastTarget = true;
        }

        // 자물쇠 아이콘
        if (showLockIcon && lockIconSprite != null)
        {
            GameObject lockIcon = new GameObject("LockIcon");
            lockIcon.transform.SetParent(lockOverlay.transform, false);

            RectTransform iconRt = lockIcon.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = iconSize;
            iconRt.anchoredPosition = iconPosition;

            iconImage = lockIcon.AddComponent<Image>();
            iconImage.sprite = lockIconSprite;
            iconImage.color = iconColor;
        }

        // 텍스트
        if (showText)
        {
            GameObject textObj = new GameObject("LockText");
            textObj.transform.SetParent(lockOverlay.transform, false);

            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.sizeDelta = new Vector2(400, 100);
            textRt.anchoredPosition = textPosition;

            textComponent = textObj.AddComponent<Text>();
            textComponent.text = lockText;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = textColor;

            if (customFont != null)
            {
                textComponent.font = customFont;
            }
            else
            {
                textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        Debug.Log($"🔒 {gameObject.name} Canvas 잠금 생성");
    }

    public void Unlock()
    {
        isLocked = false;

        if (lockOverlay != null)
        {
            Destroy(lockOverlay);
            lockOverlay = null;
            Debug.Log($"🔓 {gameObject.name} Canvas 잠금 해제");
        }
    }

    public void Lock()
    {
        if (!isLocked)
        {
            isLocked = true;
            CreateLockOverlay();
        }
    }

    public bool IsLocked()
    {
        return isLocked;
    }

    public void SetLocked(bool locked)
    {
        if (locked && !isLocked)
        {
            Lock();
        }
        else if (!locked && isLocked)
        {
            Unlock();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && lockOverlay != null)
        {
            // 오버레이 업데이트
            if (overlayImage != null)
            {
                if (showOverlay)
                {
                    overlayImage.color = overlayColor;
                }
                else
                {
                    overlayImage.color = new Color(0, 0, 0, 0);
                }
            }

            // 아이콘 업데이트
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(showLockIcon && lockIconSprite != null);
                iconImage.sprite = lockIconSprite;
                iconImage.color = iconColor;
                RectTransform iconRt = iconImage.GetComponent<RectTransform>();
                iconRt.sizeDelta = iconSize;
                iconRt.anchoredPosition = iconPosition;
            }

            // 텍스트 업데이트
            if (textComponent != null)
            {
                textComponent.gameObject.SetActive(showText);
                textComponent.text = lockText;
                textComponent.fontSize = fontSize;
                textComponent.color = textColor;
                RectTransform textRt = textComponent.GetComponent<RectTransform>();
                textRt.anchoredPosition = textPosition;

                if (customFont != null)
                {
                    textComponent.font = customFont;
                }
            }

            // 잠금 상태 변경
            if (!isLocked && lockOverlay != null)
            {
                Unlock();
            }
            else if (isLocked && lockOverlay == null)
            {
                CreateLockOverlay();
            }
        }
    }
#endif
}