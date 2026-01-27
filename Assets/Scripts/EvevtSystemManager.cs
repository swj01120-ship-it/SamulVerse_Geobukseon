using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemManager : MonoBehaviour
{
    public static EventSystemManager Instance { get; private set; }

    private StandaloneInputModule standaloneInput;
    private Component ovrInputModule;

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            standaloneInput = GetComponent<StandaloneInputModule>();

            // EventSystem 컴포넌트 가져오기
            EventSystem eventSystem = GetComponent<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = gameObject.AddComponent<EventSystem>();
            }

            Debug.Log("✓ EventSystemManager 초기화");
        }
        else
        {
            // 중복 제거
            Destroy(gameObject);
        }
    }

    public void SwitchToVR()
    {
        Debug.Log("━━━ EventSystem을 VR 모드로 전환 ━━━");

        if (standaloneInput != null)
        {
            standaloneInput.enabled = false;
        }

        Debug.Log("✓ VR Input Module 활성화 준비 완료");
    }

    public void SwitchToMouse()
    {
        Debug.Log("━━━ EventSystem을 마우스 모드로 전환 ━━━");

        if (ovrInputModule != null)
        {
            Destroy(ovrInputModule);
            ovrInputModule = null;
        }

        if (standaloneInput != null)
        {
            standaloneInput.enabled = true;
        }

        Debug.Log("✓ 마우스 Input Module 활성화 완료");
    }
}