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

        [Header("Toast Notification")]
        [SerializeField] private GameObject notificationToast;
        [SerializeField] private TextMeshProUGUI notificationText;

        [Header("Alliance Request Popup")]
        [SerializeField] private GameObject allianceRequestPanel;
        [SerializeField] private TextMeshProUGUI allianceRequesterText;
        [SerializeField] private Button allianceAcceptBtn;
        [SerializeField] private Button allianceDeclineBtn;

        [Header("Status Display")]
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI guardianTypeText;
        [SerializeField] private TextMeshProUGUI guardianStatsText;
        [SerializeField] private TextMeshProUGUI nicknameText;

        [Header("Nearby Detection")]
        [SerializeField] private GameObject detectBadge;       // 주변 타겟 있을 때 표시
        [SerializeField] private TextMeshProUGUI detectCount;

        [Header("Create Guardian Panel")]
        [SerializeField] private GameObject createGuardianPanel;
        [SerializeField] private Button createAnimalBtn;
        [SerializeField] private Button createRobotBtn;
        [SerializeField] private Button createAircraftBtn;

        [Header("Expand Territory Panel")]
        [SerializeField] private GameObject expandPanel;
        [SerializeField] private Slider radiusSlider;
        [SerializeField] private TextMeshProUGUI radiusLabel;
        [SerializeField] private Button confirmExpandBtn;
        [SerializeField] private Button cancelExpandBtn;
        [SerializeField] private Button openExpandBtn;

        [Header("v2 New Panel Buttons")]
        [SerializeField] private Button openPartsBtn;
        [SerializeField] private Button openLeaderboardBtn;

        void Awake()
        {
            // 중복 Canvas 자살 방지 — 새 Instance가 그대로 유지
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

            if (openPartsBtn != null)       openPartsBtn.onClick.AddListener      (() => PartsPanel.Instance?.Show());
            if (openLeaderboardBtn != null) openLeaderboardBtn.onClick.AddListener(() => LeaderboardPanel.Instance?.Show());

            radiusSlider.onValueChanged.AddListener(v =>
                radiusLabel.text = $"{(int)v}m");

            // 초기값
            radiusSlider.minValue = 50;
            radiusSlider.maxValue = 500;
            radiusSlider.value = 50;

            RefreshUserInfo();

            // 사용자 데이터 로드 후 오프라인 요약 1회 호출 + 주기 ping
            gm.OnUserDataChanged += FetchOfflineSummaryOnce;
            InvokeRepeating(nameof(SendActivityPing), 60f, 60f);
        }

        private bool _summaryFetched = false;
        void FetchOfflineSummaryOnce()
        {
            if (_summaryFetched) return;
            var userId = GameManager.Instance?.UserId;
            if (string.IsNullOrEmpty(userId)) return;
            _summaryFetched = true;

            ApiManager.Instance.GetActivitySummary(userId, json =>
            {
                if (string.IsNullOrEmpty(json) || json.Length == 0 || json[0] != '{')
                {
                    Debug.LogWarning($"[HUD] activity summary 비정상 응답: {(string.IsNullOrEmpty(json) ? "(empty)" : json.Substring(0, System.Math.Min(200, json.Length)))}");
                    return;
                }
                ActivitySummaryResponse resp = null;
                try { resp = JsonUtility.FromJson<ActivitySummaryResponse>(json); }
                catch (System.Exception e) { Debug.LogError($"[HUD] activity parse 실패: {e.Message}"); return; }
                if (resp == null || !resp.success || !resp.hasContent || resp.summary == null) return;

                var s = resp.summary;
                var parts = new System.Collections.Generic.List<string>();
                if (s.partsCount > 0)     parts.Add($"{s.partsCount} parts received");
                if (s.attackedCount > 0)  parts.Add($"Attacked {s.attackedCount}x (W {s.attackedWon}/L {s.attackedLost})");
                if (s.defeated)           parts.Add("Guardian defeated - territories vulnerable");
                if (s.vulnerableCount > 0) parts.Add($"{s.vulnerableCount} vulnerable territories");
                if (s.currentRank > 0)    parts.Add($"Rank #{s.currentRank}");
                if (parts.Count > 0) ShowNotification("Welcome back!\n" + string.Join(" - ", parts));
            },
            err => Debug.LogWarning($"[HUD] activity HTTP error: {err}"));
        }

        void SendActivityPing()
        {
            var userId = GameManager.Instance?.UserId;
            if (!string.IsNullOrEmpty(userId)) ApiManager.Instance.ActivityPing(userId);
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
