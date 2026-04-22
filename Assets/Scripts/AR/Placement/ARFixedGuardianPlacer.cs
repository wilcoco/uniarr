using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// AR 평면을 감지해 고정 수호신을 배치
    /// 흐름: 모드 진입 → 바닥 스캔 → 탭으로 위치 선택 → 타입/스탯 설정 → 확인 → API
    /// </summary>
    public class ARFixedGuardianPlacer : MonoBehaviour
    {
        public static ARFixedGuardianPlacer Instance { get; private set; }

        [Header("AR Components")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Preview")]
        [SerializeField] private GameObject previewPrefab;      // 반투명 배치 인디케이터
        [SerializeField] private GameObject placementIndicator; // 화살표/원형 가이드

        [Header("Setup Panel UI")]
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private Button defenseTypeBtn;
        [SerializeField] private Button productionTypeBtn;
        [SerializeField] private Slider atkSlider;
        [SerializeField] private Slider defSlider;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI atkLabel;
        [SerializeField] private TextMeshProUGUI defLabel;
        [SerializeField] private TextMeshProUGUI hpLabel;
        [SerializeField] private TextMeshProUGUI remainingStatsLabel;
        [SerializeField] private Button confirmBtn;
        [SerializeField] private Button cancelBtn;

        [Header("Scan Hint")]
        [SerializeField] private GameObject scanHintPanel;      // "바닥을 스캔하세요" 안내

        [Header("Territory Select Panel")]
        [SerializeField] private GameObject territorySelectPanel;
        [SerializeField] private Transform territoryListContainer;
        [SerializeField] private GameObject territoryItemPrefab;

        // 상태
        private bool isPlacementMode = false;
        private GameObject previewInstance;
        private Vector3 selectedWorldPos;
        private LatLng selectedGPSPos;
        private string selectedType = "defense";
        private string selectedTerritoryId;

        // 분배 가능한 스탯 (본체의 50%)
        private int maxAtk, maxDef, maxHp;

        private List<ARRaycastHit> hits = new();

        void Awake()
        {
            Instance = this;
            setupPanel.SetActive(false);
            scanHintPanel.SetActive(false);
            territorySelectPanel.SetActive(false);
            if (placementIndicator != null) placementIndicator.SetActive(false);
        }

        void Start()
        {
            defenseTypeBtn.onClick.AddListener(() => SelectType("defense"));
            productionTypeBtn.onClick.AddListener(() => SelectType("production"));
            confirmBtn.onClick.AddListener(ConfirmPlacement);
            cancelBtn.onClick.AddListener(CancelPlacement);

            atkSlider.onValueChanged.AddListener(_ => OnStatChanged());
            defSlider.onValueChanged.AddListener(_ => OnStatChanged());
            hpSlider.onValueChanged.AddListener(_ => OnStatChanged());
        }

        void Update()
        {
            if (!isPlacementMode) return;

            // 화면 중앙 또는 터치 위치에서 AR Raycast
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            {
                var hitPose = hits[0].pose;
                selectedWorldPos = hitPose.position;

                if (previewInstance != null)
                {
                    previewInstance.transform.position = hitPose.position;
                    previewInstance.transform.rotation = hitPose.rotation;
                }

                if (placementIndicator != null)
                {
                    placementIndicator.SetActive(true);
                    placementIndicator.transform.position = hitPose.position;
                }

                // 탭으로 위치 확정
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    OnPositionSelected(hitPose.position);
                }
            }
            else
            {
                if (placementIndicator != null) placementIndicator.SetActive(false);
            }
        }

        // ─── 배치 모드 시작 ────────────────────────────────────────────
        public void StartPlacementMode()
        {
            var myGuardian = GameManager.Instance.MyGuardian;
            if (myGuardian == null)
            {
                Debug.LogWarning("수호신이 없습니다");
                return;
            }

            // 분배 가능 최대치 (본체 스탯의 50%)
            maxAtk = myGuardian.stats.atk / 2;
            maxDef = myGuardian.stats.def / 2;
            maxHp = myGuardian.stats.hp / 2;

            // 영역 먼저 선택
            ShowTerritorySelect();
        }

        private void ShowTerritorySelect()
        {
            territorySelectPanel.SetActive(true);

            // 기존 목록 제거
            foreach (Transform child in territoryListContainer)
                Destroy(child.gameObject);

            // 내 영역 목록
            foreach (var t in GameManager.Instance.MyTerritories)
            {
                var item = Instantiate(territoryItemPrefab, territoryListContainer);
                var label = item.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = $"영역 ({t.radius}m)";

                var btn = item.GetComponentInChildren<Button>();
                var capturedId = t.id;
                var capturedCenter = t.center;
                btn?.onClick.AddListener(() =>
                {
                    selectedTerritoryId = capturedId;
                    selectedGPSPos = capturedCenter;
                    territorySelectPanel.SetActive(false);
                    BeginARPlacement();
                });
            }
        }

        private void BeginARPlacement()
        {
            isPlacementMode = true;
            planeManager.enabled = true;
            scanHintPanel.SetActive(true);

            if (previewPrefab != null)
                previewInstance = Instantiate(previewPrefab);
        }

        // ─── 위치 확정 ─────────────────────────────────────────────────
        private void OnPositionSelected(Vector3 worldPos)
        {
            // 선택 위치의 GPS 좌표 계산 (AR 원점 기준 역변환)
            var origin = ARModeController.Instance.AROrigin;
            if (origin != null)
            {
                float dz = worldPos.z;
                float dx = worldPos.x;
                double latOffset = dz / 111320.0;
                double lngOffset = dx / (111320.0 * System.Math.Cos(origin.lat * System.Math.PI / 180.0));
                selectedGPSPos = new LatLng(origin.lat + latOffset, origin.lng + lngOffset);
            }

            isPlacementMode = false;
            scanHintPanel.SetActive(false);
            if (placementIndicator != null) placementIndicator.SetActive(false);

            ShowSetupPanel();
        }

        // ─── 스탯 설정 패널 ────────────────────────────────────────────
        private void ShowSetupPanel()
        {
            setupPanel.SetActive(true);

            atkSlider.maxValue = maxAtk;
            defSlider.maxValue = maxDef;
            hpSlider.maxValue = maxHp;

            atkSlider.value = 0;
            defSlider.value = 0;
            hpSlider.value = 0;

            SelectType("defense");
            OnStatChanged();
        }

        private void SelectType(string type)
        {
            selectedType = type;

            // 버튼 시각 피드백
            var defColor = new Color(0.3f, 0.3f, 0.3f);
            var activeColor = type == "defense"
                ? new Color(0.27f, 0.53f, 1f)
                : new Color(1f, 0.85f, 0f);

            defenseTypeBtn.GetComponent<Image>().color = type == "defense" ? activeColor : defColor;
            productionTypeBtn.GetComponent<Image>().color = type == "production" ? activeColor : defColor;
        }

        private void OnStatChanged()
        {
            int usedAtk = (int)atkSlider.value;
            int usedDef = (int)defSlider.value;
            int usedHp = (int)hpSlider.value;

            atkLabel.text = $"ATK: {usedAtk}/{maxAtk}";
            defLabel.text = $"DEF: {usedDef}/{maxDef}";
            hpLabel.text = $"HP: {usedHp}/{maxHp}";

            int total = usedAtk + usedDef + usedHp;
            int maxTotal = maxAtk + maxDef + maxHp;
            remainingStatsLabel.text = $"남은 분배량: {maxTotal - total}";

            confirmBtn.interactable = total > 0;
        }

        // ─── 배치 확정 → API ───────────────────────────────────────────
        private void ConfirmPlacement()
        {
            if (string.IsNullOrEmpty(selectedTerritoryId)) return;

            int allocAtk = (int)atkSlider.value;
            int allocDef = (int)defSlider.value;
            int allocHp  = (int)hpSlider.value;

            var req = new PlaceFixedGuardianRequest
            {
                territoryId = selectedTerritoryId,
                userId = GameManager.Instance.UserId,
                lat = selectedGPSPos.lat,
                lng = selectedGPSPos.lng,
                stats = new PlaceStats { atk = allocAtk, def = allocDef, hp = allocHp },
                guardianType = selectedType
            };

            confirmBtn.interactable = false;

            ApiManager.Instance.PlaceFixedGuardian(req, json =>
            {
                setupPanel.SetActive(false);

                // AR 공간에 즉시 시각화
                SpawnPlacedGuardianInAR(req);

                // 본체 스탯 업데이트
                GameManager.Instance.LoadUserData();

                if (previewInstance != null)
                {
                    Destroy(previewInstance);
                    previewInstance = null;
                }
                planeManager.enabled = false;
            }, err =>
            {
                confirmBtn.interactable = true;
                Debug.LogError($"고정 수호신 배치 실패: {err}");
            });
        }

        private void SpawnPlacedGuardianInAR(PlaceFixedGuardianRequest req)
        {
            var fg = new FixedGuardian
            {
                id = System.Guid.NewGuid().ToString(),
                type = req.guardianType,
                owner = GameManager.Instance.VisitorId,
                position = new LatLng(req.lat, req.lng),
                stats = new FixedGuardianStats
                {
                    atk = req.stats.atk,
                    def = req.stats.def,
                    hp = req.stats.hp
                }
            };

            ARModeController.Instance.SpawnMyFixedGuardian(fg, selectedWorldPos);
        }

        private void CancelPlacement()
        {
            isPlacementMode = false;
            setupPanel.SetActive(false);
            scanHintPanel.SetActive(false);
            if (previewInstance != null) { Destroy(previewInstance); previewInstance = null; }
            if (placementIndicator != null) placementIndicator.SetActive(false);
            planeManager.enabled = false;
        }
    }
}
