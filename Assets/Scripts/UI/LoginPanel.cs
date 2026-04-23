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
            startButton.onClick.AddListener(OnStartClicked);
            allowLocationButton.onClick.AddListener(OnAllowLocation);

            string saved = PlayerPrefs.GetString("visitorId", "");
            if (!string.IsNullOrEmpty(saved))
            {
                loginPanel.SetActive(false);
                ShowLocationPanel();
            }
            else
            {
                loginPanel.SetActive(true);
                locationPanel.SetActive(false);
            }

            LocationManager.Instance.OnLocationError += msg =>
            {
                locationErrorText.text = msg;
                locationErrorText.gameObject.SetActive(true);
                allowLocationButton.interactable = true;
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
            locationPanel.SetActive(true);
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
