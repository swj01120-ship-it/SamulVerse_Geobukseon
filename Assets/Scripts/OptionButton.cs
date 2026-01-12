using UnityEngine;
using UnityEngine.UI;

public class OptionButton : MonoBehaviour
{
    [Header("버튼 설정")]
    [SerializeField] private Button button;
    [SerializeField] private Image targetImage;

    [Header("색상 설정")]
    [Tooltip("선택되지 않았을 때 색상")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("선택되었을 때 색상")]
    [SerializeField] private Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private bool isSelected = false;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        // ❌ 여기서 onClick 등록하지 않음
        // 버튼 클릭은 OptionButtonGroup이 담당

        UpdateColor();
    }

    /// <summary>
    /// Group에서만 호출
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (targetImage != null)
        {
            targetImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    public bool IsSelected()
    {
        return isSelected;
    }
}
