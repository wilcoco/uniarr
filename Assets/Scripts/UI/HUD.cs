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

        void Start()
        {
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
                energyText.text = $"💎 {gm.Energy}";

            bool hasGuardian = gm.MyGuardian != null;
            createGuardianPanel.SetActive(!hasGuardian);
            openExpandBtn.gameObject.SetActive(hasGuardian);

            if (hasGuardian)
            {
                var g = gm.MyGuardian;
                guardianTypeText.text = g.type switch
                {
                    "animal" => "🦁 동물형",
                    "robot" => "🤖 로봇형",
                    "aircraft" => "✈ 비행체형",
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
    }
}
