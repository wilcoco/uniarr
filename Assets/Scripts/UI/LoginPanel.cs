using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// 닉네임 입력 패널 — visitorId 설정 후 GPS 요청
    /// </summary>
    public class LoginPanel : MonoBehaviour
    {
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button startButton;
        [SerializeField] private TextMeshProUGUI errorText;

        [SerializeField] private GameObject locationPanel;
        [SerializeField] private Button allowLocationButton;
        [SerializeField] private TextMeshProUGUI locationErrorText;

        void Start()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);

#if UNITY_EDITOR
            // 에디터: 위치 권한 UI/Allow 버튼 자체를 비활성화하고 GameObject도 숨김
            if (locationPanel != null) locationPanel.SetActive(false);
            if (allowLocationButton != null) allowLocationButton.gameObject.SetActive(false);
#else
            if (allowLocationButton != null) allowLocationButton.onClick.AddListener(OnAllowLocation);
            if (locationPanel != null) locationPanel.SetActive(false);
#endif

            string saved = PlayerPrefs.GetString("visitorId", "");
            if (!string.IsNullOrEmpty(saved))
            {
                if (loginPanel != null) loginPanel.SetActive(false);
                ShowLocationPanel();
            }
            else
            {
                if (loginPanel != null) loginPanel.SetActive(true);
            }

            LocationManager.Instance.OnLocationError += msg =>
            {
#if !UNITY_EDITOR
                if (locationErrorText != null)
                {
                    locationErrorText.text = msg;
                    locationErrorText.gameObject.SetActive(true);
                }
                if (allowLocationButton != null) allowLocationButton.interactable = true;
#endif
            };

            LocationManager.Instance.OnLocationUpdated += _ => HidePanels();
        }

        private void OnStartClicked()
        {
            string nick = nicknameInput.text.Trim();
            if (string.IsNullOrEmpty(nick))
            {
                errorText.text = "Please enter a nickname";
                return;
            }
            GameManager.Instance.SetVisitorId(nick);
            loginPanel.SetActive(false);
            ShowLocationPanel();
        }

        private void ShowLocationPanel()
        {
#if UNITY_EDITOR
            // 에디터: 권한 팝업 스킵하고 즉시 추적 시작 (자동 GPS 주입됨)
            LocationManager.Instance.StartTracking();
#else
            locationPanel.SetActive(true);
#endif
        }

        private void OnAllowLocation()
        {
            allowLocationButton.interactable = false;
            locationErrorText.gameObject.SetActive(false);
            LocationManager.Instance.StartTracking();
        }

        private void HidePanels()
        {
            loginPanel.SetActive(false);
            locationPanel.SetActive(false);
        }
    }
}
