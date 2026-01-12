using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OptionButtonGroup : MonoBehaviour
{
    [Header("그룹 내 버튼들")]
    [SerializeField] private List<OptionButton> buttons = new List<OptionButton>();

    [Header("설정")]
    [Tooltip("true면 여러 개 선택 가능, false면 하나만 선택")]
    [SerializeField] private bool allowMultipleSelection = false;

    [Tooltip("기본 선택 인덱스 (-1이면 기본 선택 없음)")]
    [SerializeField] private int defaultSelectedIndex = 0;

    private void Awake()
    {
        // 자식 OptionButton 자동 수집
        if (buttons == null || buttons.Count == 0)
        {
            buttons = new List<OptionButton>(GetComponentsInChildren<OptionButton>());
        }

        // 버튼 클릭 리스너 연결 (Group이 전담)
        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            Button unityButton = buttons[i].GetComponent<Button>();

            if (unityButton != null)
            {
                unityButton.onClick.RemoveAllListeners();
                unityButton.onClick.AddListener(() => OnButtonClicked(index));
            }
        }
    }

    private void Start()
    {
        // 기본 선택 처리
        ApplyDefaultSelection();
    }

    private void ApplyDefaultSelection()
    {
        if (defaultSelectedIndex < 0 || defaultSelectedIndex >= buttons.Count)
        {
            ClearAllSelections();
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].SetSelected(i == defaultSelectedIndex);
        }
    }

    private void OnButtonClicked(int clickedIndex)
    {
        // 🔊 클릭 사운드
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSound();
        }

        if (clickedIndex < 0 || clickedIndex >= buttons.Count)
            return;

        if (!allowMultipleSelection)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].SetSelected(i == clickedIndex);
            }
        }
        else
        {
            bool newState = !buttons[clickedIndex].IsSelected();
            buttons[clickedIndex].SetSelected(newState);
        }
    }

    public int GetSelectedIndex()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].IsSelected())
                return i;
        }
        return -1;
    }

    public List<int> GetSelectedIndices()
    {
        List<int> result = new List<int>();
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].IsSelected())
                result.Add(i);
        }
        return result;
    }

    public void SelectButton(int index)
    {
        OnButtonClicked(index);
    }

    public void ClearAllSelections()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].SetSelected(false);
        }
    }
}
