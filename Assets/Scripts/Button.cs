using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class VRUIButtonFocusEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISubmitHandler
{
    [Header("Target")]
    public Image targetImage;

    [Header("Color")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.4f);
    public Color focusColor = new Color(1f, 1f, 1f, 1f);
    public Color pressedColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Scale")]
    public float normalScale = 1.0f;
    public float focusScale = 1.15f;
    public float pressedScale = 1.05f;

    [Header("Animation")]
    public float animationSpeed = 10f;

    private Coroutine scaleCoroutine;

    void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponentInChildren<Image>();
    }

    void Start()
    {
        SetNormalImmediate();
    }

    // 🔹 VR 레이 Hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        SetFocus();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetNormal();
    }

    // 🔹 VR 트리거 누를 때
    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressed();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetFocus();
    }

    // 🔹 XR Submit (컨트롤러 Select)
    public void OnSubmit(BaseEventData eventData)
    {
        SetPressed();
    }

    void SetFocus()
    {
        if (targetImage == null) return;
        targetImage.color = focusColor;
        AnimateScale(focusScale);
    }

    void SetPressed()
    {
        if (targetImage == null) return;
        targetImage.color = pressedColor;
        AnimateScale(pressedScale);
    }

    void SetNormal()
    {
        if (targetImage == null) return;
        targetImage.color = normalColor;
        AnimateScale(normalScale);
    }

    void SetNormalImmediate()
    {
        if (targetImage == null) return;
        targetImage.color = normalColor;
        transform.localScale = Vector3.one * normalScale;
    }

    void AnimateScale(float target)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleTo(target));
    }

    IEnumerator ScaleTo(float target)
    {
        Vector3 targetScale = Vector3.one * target;

        while (Vector3.Distance(transform.localScale, targetScale) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * animationSpeed
            );
            yield return null;
        }

        transform.localScale = targetScale;
    }
}