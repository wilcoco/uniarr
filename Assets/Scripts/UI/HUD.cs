using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// 맵/AR 공통 HUD — 에너지, 수호신 정보, 모드 전환 버튼
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        [Header("알림 토스트")]
        [SerializeField] private GameObject notificationToast;
        [SerializeField] private TextMeshProUGUI notificationText;

        [Header("동맹 요청 팝업")]
        [SerializeField] private GameObject allianceRequestPanel;
        [SerializeField] private TextMeshProUGUI allianceRequesterText;
        [SerializeField] private Button allianceAcceptBtn;
        [SerializeField] private Button allianceDeclineBtn;

        [Header("상태 표시")]
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI guardianTypeText;
        [SerializeField] private TextMeshProUGUI guardianStatsText;
        [SerializeField] private TextMeshProUGUI nicknameText;

        [Header("주변 탐지")]
        [SerializeField] private GameObject detectBadge;       // 주변 타겟 있을 때 표시
        [SerializeField] private TextMeshProUGUI detectCount;

        [Header("수호신 생성 패널")]
        [SerializeField] private GameObject createGuardianPanel;
        [SerializeField] private Button createAnimalBtn;
        [SerializeField] private Button createRobotBtn;
        [SerializeField] private Button createAircraftBtn;

        [Header("영역 확장 패널")]
        [SerializeField] private GameObject expandPanel;
        [SerializeField] private Slider radiusSlider;
        [SerializeField] private TextMeshProUGUI radiusLabel;
        [SerializeField] private Button confirmExpandBtn;
        [SerializeField] private Button cancelExpandBtn;
        [SerializeField] private Button openExpandBtn;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (notificationToast != null) notificationToast.SetActive(false);
            if (allianceRequestPanel != null) allianceRequestPanel.SetActive(false);

            var gm = GameManager.Instance;
            gm.OnUserDataChanged += RefreshUserInfo;
            gm.OnNearbyChanged += RefreshDetectBadge;

            createAnimalBtn.onClick.AddListener(() => CreateGuardian("animal"));
            createRobotBtn.onClick.AddListener(() => CreateGuardian("robot"));
            createAircraftBtn.onClick.AddListener(() => CreateGuardian("aircraft"));

            openExpandBtn.onClick.AddListener(() => expandPanel.SetActive(true));
            cancelExpandBtn.onClick.AddListener(() => expandPanel.SetActive(false));
            confirmExpandBtn.onClick.AddListener(ConfirmExpand);

            radiusSlider.onValueChanged.AddListener(v =>
                radiusLabel.text = $"{(int)v}m");

            // 초기값
            radiusSlider.minValue = 50;
            radiusSlider.maxValue = 500;
            radiusSlider.value = 50;

            RefreshUserInfo();
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnUserDataChanged -= RefreshUserInfo;
                GameManager.Instance.OnNearbyChanged -= RefreshDetectBadge;
            }
        }

        private void RefreshUserInfo()
        {
            var gm = GameManager.Instance;

            if (nicknameText != null)
                nicknameText.text = gm.VisitorId ?? "";

            if (energyText != null)
                energyText.text = $"E:{gm.Energy}";

            bool hasGuardian = gm.MyGuardian != null;
            createGuardianPanel.SetActive(!hasGuardian);
            openExpandBtn.gameObject.SetActive(hasGuardian);

            if (hasGuardian)
            {
                var g = gm.MyGuardian;
                guardianTypeText.text = g.type switch
                {
                    "animal"   => "Animal",
                    "robot"    => "Robot",
                    "aircraft" => "Aircraft",
                    _ => g.type
                };
                guardianStatsText.text =
                    $"ATK:{g.stats.atk} DEF:{g.stats.def} HP:{g.stats.hp}\n" +
                    $"ABS:{g.stats.abs} PRD:{g.stats.prd} SPD:{g.stats.spd}";
            }
        }

        private void RefreshDetectBadge()
        {
            var gm = GameManager.Instance;
            int count = gm.NearbyPlayers.Count + gm.NearbyFixedGuardians.Count;
            detectBadge.SetActive(count > 0);
            if (detectCount != null) detectCount.text = count.ToString();
        }

        private void CreateGuardian(string type)
        {
            GameManager.Instance.CreateGuardian(type, success =>
            {
                if (!success) Debug.LogWarning("수호신 생성 실패");
            });
        }

        private void ConfirmExpand()
        {
            float radius = radiusSlider.value;
            GameManager.Instance.ExpandTerritory(radius, success =>
            {
                expandPanel.SetActive(false);
                if (!success) Debug.LogWarning("영역 확장 실패");
            });
        }

        // ─── 푸시 알림 UI ─────────────────────────────────────────────

        public void ShowNotification(string message)
        {
            if (notificationToast == null) return;
            StopCoroutine(nameof(HideToastAfterDelay));
            notificationText.text = message;
            notificationToast.SetActive(true);
            StartCoroutine(nameof(HideToastAfterDelay));
        }

        private IEnumerator HideToastAfterDelay()
        {
            yield return new WaitForSeconds(3f);
            if (notificationToast != null)
                notificationToast.SetActive(false);
        }

        public void ShowAllianceRequest(string requestId, string requesterId)
        {
            if (allianceRequestPanel == null) return;
            allianceRequesterText.text = $"Alliance Request: {requesterId}";
            allianceRequestPanel.SetActive(true);

            allianceAcceptBtn.onClick.RemoveAllListeners();
            allianceDeclineBtn.onClick.RemoveAllListeners();

            allianceAcceptBtn.onClick.AddListener(() =>
            {
                allianceRequestPanel.SetActive(false);
                ApiManager.Instance.RespondAlliance(requestId, true, _ =>
                    ShowNotification("Alliance accepted!"));
            });
            allianceDeclineBtn.onClick.AddListener(() =>
            {
                allianceRequestPanel.SetActive(false);
                ApiManager.Instance.RespondAlliance(requestId, false, _ =>
                    ShowNotification("Alliance request declined."));
            });
        }
    }
}
