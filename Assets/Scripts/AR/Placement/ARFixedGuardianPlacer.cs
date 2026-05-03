using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// AR 평면을 감지해 13종 타워를 배치 (신 /api/towers/place)
    /// 흐름: 모드 진입 → 영역 선택 → 클래스 선택 → 티어 선택 → 바닥 탭 → API → 즉시 스폰
    /// </summary>
    public class ARFixedGuardianPlacer : MonoBehaviour
    {
        public static ARFixedGuardianPlacer Instance { get; private set; }

        [Header("AR Components")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Preview")]
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private GameObject placementIndicator;

        [Header("Piloto Studio Tower Prefabs")]
        [SerializeField] private bool autoLoadFromPiloto = true;

        [SerializeField] private GameObject genericTowerLv1, balistaTowerLv1, cannonTowerLv1, assaultTowerLv1,
                                             scifiTowerLv1, fireTowerLv1, iceTowerLv1, aquaTowerLv1,
                                             electricTowerLv1, natureTowerLv1, venomTowerLv1, arcaneTowerLv1, crystalTowerLv1;

        public GameObject GetTowerPrefab(string towerClass, int level = 1)
        {
            level = Mathf.Clamp(level, 1, 5);
            string pilotoName = towerClass switch
            {
                "balista"  => "Balista", "cannon"   => "Cannon",   "assault"  => "Assault",
                "scifi"    => "SciFi",   "fire"     => "Fire",     "ice"      => "Ice",
                "aqua"     => "Aqua",    "electric" => "Electric", "nature"   => "Nature",
                "venom"    => "Venom",   "arcane"   => "Arcane",   "crystal"  => "Crystal",
                _          => "Generic"
            };

#if UNITY_EDITOR
            if (autoLoadFromPiloto)
            {
                var path = $"Assets/Piloto Studio/TowerDefenseStarterPack/Prefabs/Towers/SM_TowerDefense_{pilotoName}_Lv{level}.prefab";
                var p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null) return p;
            }
#endif
            var res = Resources.Load<GameObject>($"Towers/SM_TowerDefense_{pilotoName}_Lv{level}");
            if (res != null) return res;
            res = Resources.Load<GameObject>($"Towers/SM_TowerDefense_{pilotoName}_Lv1");
            if (res != null) return res;

            GameObject manual = towerClass switch
            {
                "balista"  => balistaTowerLv1, "cannon"   => cannonTowerLv1,   "assault"  => assaultTowerLv1,
                "scifi"    => scifiTowerLv1,   "fire"     => fireTowerLv1,     "ice"      => iceTowerLv1,
                "aqua"     => aquaTowerLv1,    "electric" => electricTowerLv1, "nature"   => natureTowerLv1,
                "venom"    => venomTowerLv1,   "arcane"   => arcaneTowerLv1,   "crystal"  => crystalTowerLv1,
                _          => genericTowerLv1
            };
            return manual ?? previewPrefab;
        }

        // 13종 클래스 메타 (한글 라벨 + 비용 + 사거리 — UI 표시용)
        public class TowerClassInfo
        {
            public string key;
            public string label;
            public int costLv1;
            public int rangeLv1;
            public int dpsLv1; // 대략값
            public string desc;
        }

        // 라벨/설명은 ASCII — 기본 TMP 폰트(LiberationSans)에 한글 글리프 없음.
        // 한글 보고 싶으면 Korean TMP 폰트 추가 후 fallback 등록 필요.
        public static readonly TowerClassInfo[] CLASSES = new[]
        {
            new TowerClassInfo { key="generic",  label="Generic",  costLv1=30, rangeLv1=80,  dpsLv1=3,  desc="Balanced starter" },
            new TowerClassInfo { key="balista",  label="Balista",  costLv1=50, rangeLv1=150, dpsLv1=4,  desc="Long range, first shot +50%" },
            new TowerClassInfo { key="cannon",   label="Cannon",   costLv1=70, rangeLv1=100, dpsLv1=5,  desc="30m AOE blast" },
            new TowerClassInfo { key="assault",  label="Assault",  costLv1=50, rangeLv1=70,  dpsLv1=6,  desc="Rapid fire" },
            new TowerClassInfo { key="scifi",    label="SciFi",    costLv1=75, rangeLv1=130, dpsLv1=7,  desc="Pierce +30%" },
            new TowerClassInfo { key="fire",     label="Fire",     costLv1=55, rangeLv1=60,  dpsLv1=4,  desc="5s burn DoT" },
            new TowerClassInfo { key="ice",      label="Ice",      costLv1=60, rangeLv1=80,  dpsLv1=2,  desc="Vuln 10s" },
            new TowerClassInfo { key="aqua",     label="Aqua",     costLv1=60, rangeLv1=90,  dpsLv1=4,  desc="Vuln 5min" },
            new TowerClassInfo { key="electric", label="Electric", costLv1=65, rangeLv1=75,  dpsLv1=3,  desc="Chain +50%" },
            new TowerClassInfo { key="nature",   label="Nature",   costLv1=50, rangeLv1=50,  dpsLv1=0,  desc="Adjacent heal" },
            new TowerClassInfo { key="venom",    label="Venom",    costLv1=55, rangeLv1=65,  dpsLv1=2,  desc="Stacking poison" },
            new TowerClassInfo { key="arcane",   label="Arcane",   costLv1=70, rangeLv1=100, dpsLv1=4,  desc="Combine debuff" },
            new TowerClassInfo { key="crystal",  label="Crystal",  costLv1=80, rangeLv1=40,  dpsLv1=0,  desc="Ally synergy +10%" }
        };

        [Header("UI Panels")]
        [SerializeField] private GameObject scanHintPanel;
        [SerializeField] private GameObject territorySelectPanel;
        [SerializeField] private Transform territoryListContainer;
        [SerializeField] private GameObject territoryItemPrefab;

        [Header("Tower Class Picker (신규)")]
        [SerializeField] private GameObject classPickerPanel;
        [SerializeField] private Transform classListContainer;     // 13개 버튼 부모 (Grid Layout 권장)
        [SerializeField] private GameObject classItemPrefab;       // 1개 버튼 프리팹 (Image+Label)
        [SerializeField] private Slider tierSlider;                // 1-5
        [SerializeField] private TextMeshProUGUI tierLabel;
        [SerializeField] private TextMeshProUGUI selectedClassLabel;
        [SerializeField] private TextMeshProUGUI costLabel;
        [SerializeField] private TextMeshProUGUI energyLabel;
        [SerializeField] private Button classConfirmBtn;
        [SerializeField] private Button classCancelBtn;

        // 상태
        private bool isPlacementMode = false;
        private GameObject previewInstance;
        private Vector3 selectedWorldPos;
        private LatLng selectedGPSPos;
        private string selectedTerritoryId;
        private string selectedClass = "generic";
        private int selectedTier = 1;

        // grant (직접 침투 격파 후 무료 발판 — 옵션)
        private SlotGrant activeGrant;

        private List<ARRaycastHit> hits = new();

        void Awake()
        {
            Instance = this;
            if (scanHintPanel != null) scanHintPanel.SetActive(false);
            if (territorySelectPanel != null) territorySelectPanel.SetActive(false);
            if (classPickerPanel != null) classPickerPanel.SetActive(false);
            if (placementIndicator != null) placementIndicator.SetActive(false);
        }

        void Start()
        {
            if (classConfirmBtn != null) classConfirmBtn.onClick.AddListener(ConfirmClassAndStartScan);
            if (classCancelBtn  != null) classCancelBtn.onClick.AddListener(CancelPlacement);
            if (tierSlider != null)
            {
                tierSlider.minValue = 1; tierSlider.maxValue = 5; tierSlider.wholeNumbers = true;
                tierSlider.onValueChanged.AddListener(_ => UpdatePickerLabels());
            }
        }

        void Update()
        {
            if (!isPlacementMode || raycastManager == null) return;

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

                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    OnPositionSelected(hitPose.position);
                }
            }
            else if (placementIndicator != null)
            {
                placementIndicator.SetActive(false);
            }
        }

        // ─── 진입점 ────────────────────────────────────────────────────
        public void StartPlacementMode()
        {
            activeGrant = null;
            ShowTerritorySelect();
        }

        // 직접 침투 격파 후 발판 모드로 진입
        public void StartFootholdPlacement(SlotGrant grant)
        {
            activeGrant = grant;
            selectedTerritoryId = grant.territoryId;
            // grant는 Lv1 고정, 영역 선택 스킵 → 바로 클래스 picker
            ShowClassPicker();
        }

        // ─── 영역 선택 ─────────────────────────────────────────────────
        private void ShowTerritorySelect()
        {
            if (territorySelectPanel == null) return;
            territorySelectPanel.SetActive(true);

            foreach (Transform child in territoryListContainer)
                Destroy(child.gameObject);

            var territories = GameManager.Instance.MyTerritories;
            if (territories == null || territories.Count == 0)
            {
                Debug.LogWarning("내 영역이 없습니다. 먼저 확장하세요.");
                territorySelectPanel.SetActive(false);
                return;
            }

            foreach (var t in territories)
            {
                var item = Instantiate(territoryItemPrefab, territoryListContainer);
                var label = item.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = $"Zone {(int)t.radius}m";

                var btn = item.GetComponentInChildren<Button>();
                var capturedId = t.id;
                var capturedCenter = t.center;
                btn?.onClick.AddListener(() =>
                {
                    selectedTerritoryId = capturedId;
                    selectedGPSPos = capturedCenter;
                    territorySelectPanel.SetActive(false);
                    ShowClassPicker();
                });
            }
        }

        // ─── 클래스 / 티어 picker ──────────────────────────────────────
        private void ShowClassPicker()
        {
            if (classPickerPanel == null)
            {
                // UI 없으면 fallback: 제네릭 Lv1로 즉시 진행
                selectedClass = "generic";
                selectedTier = 1;
                BeginARScan();
                return;
            }

            classPickerPanel.SetActive(true);
            BuildClassList();
            UpdatePickerLabels();
        }

        private void BuildClassList()
        {
            if (classListContainer == null || classItemPrefab == null) return;

            foreach (Transform child in classListContainer)
                Destroy(child.gameObject);

            foreach (var info in CLASSES)
            {
                var item = Instantiate(classItemPrefab, classListContainer);
                var label = item.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = info.label;

                var img = item.GetComponentInChildren<Image>();
                // 클래스 컬러 힌트
                if (img != null) img.color = ClassTint(info.key);

                var btn = item.GetComponent<Button>();
                if (btn == null) btn = item.GetComponentInChildren<Button>();
                var capKey = info.key;
                if (btn != null)
                    btn.onClick.AddListener(() => { selectedClass = capKey; UpdatePickerLabels(); });
            }
        }

        private static Color ClassTint(string key) => key switch
        {
            "balista"  => new Color(0.85f, 0.78f, 0.42f),
            "cannon"   => new Color(0.42f, 0.42f, 0.42f),
            "assault"  => new Color(0.85f, 0.55f, 0.18f),
            "scifi"    => new Color(0.40f, 0.85f, 0.95f),
            "fire"     => new Color(0.95f, 0.36f, 0.18f),
            "ice"      => new Color(0.65f, 0.92f, 1.00f),
            "aqua"     => new Color(0.20f, 0.55f, 0.92f),
            "electric" => new Color(1.00f, 0.93f, 0.30f),
            "nature"   => new Color(0.40f, 0.82f, 0.40f),
            "venom"    => new Color(0.55f, 0.85f, 0.30f),
            "arcane"   => new Color(0.70f, 0.40f, 0.95f),
            "crystal"  => new Color(0.80f, 0.55f, 0.95f),
            _          => new Color(0.65f, 0.65f, 0.70f)
        };

        private void UpdatePickerLabels()
        {
            // foothold = Lv1 고정
            if (activeGrant != null) selectedTier = 1;
            else if (tierSlider != null) selectedTier = (int)tierSlider.value;
            selectedTier = Mathf.Clamp(selectedTier, 1, 5);

            var info = System.Array.Find(CLASSES, c => c.key == selectedClass) ?? CLASSES[0];

            // 비용 = Lv1 비용 × 1.4^(L-1) (서버 공식)
            int cost = activeGrant != null ? 0
                : Mathf.RoundToInt(info.costLv1 * Mathf.Pow(1.4f, selectedTier - 1));

            if (tierLabel != null) tierLabel.text = activeGrant != null ? "Lv1 (foothold)" : $"Lv{selectedTier}";
            if (selectedClassLabel != null) selectedClassLabel.text = $"{info.label} - {info.desc}";
            if (costLabel != null) costLabel.text = activeGrant != null ? "FREE (foothold)" : $"Cost {cost} energy";

            int energy = GameManager.Instance.Energy;
            if (energyLabel != null) energyLabel.text = $"Have {energy}";
            if (classConfirmBtn != null)
                classConfirmBtn.interactable = activeGrant != null || energy >= cost;
        }

        // ─── 클래스 확정 → AR 스캔 ─────────────────────────────────────
        private void ConfirmClassAndStartScan()
        {
            if (classPickerPanel != null) classPickerPanel.SetActive(false);
            BeginARScan();
        }

        private void BeginARScan()
        {
            isPlacementMode = true;
            if (planeManager != null) planeManager.enabled = true;
            if (scanHintPanel != null) scanHintPanel.SetActive(true);

            // 미리보기 — 선택한 타워 프리팹 사용
            if (previewInstance != null) Destroy(previewInstance);
            var prefab = GetTowerPrefab(selectedClass, selectedTier);
            if (prefab != null)
            {
                previewInstance = Instantiate(prefab);
                // 반투명 처리
                foreach (var rend in previewInstance.GetComponentsInChildren<Renderer>())
                {
                    foreach (var m in rend.materials)
                    {
                        if (m.HasProperty("_Color"))
                        {
                            var c = m.color; c.a = 0.55f; m.color = c;
                        }
                    }
                }
            }
        }

        // ─── 위치 확정 → 즉시 API ──────────────────────────────────────
        private void OnPositionSelected(Vector3 worldPos)
        {
            isPlacementMode = false;
            if (scanHintPanel != null) scanHintPanel.SetActive(false);
            if (placementIndicator != null) placementIndicator.SetActive(false);

            // GPS 좌표로 역변환 (foothold는 grant.position 강제)
            if (activeGrant != null)
            {
                selectedGPSPos = activeGrant.position;
            }
            else
            {
                var origin = ARModeController.Instance.AROrigin;
                if (origin != null)
                {
                    float dz = worldPos.z, dx = worldPos.x;
                    double latOffset = dz / 111320.0;
                    double lngOffset = dx / (111320.0 * System.Math.Cos(origin.lat * System.Math.PI / 180.0));
                    selectedGPSPos = new LatLng(origin.lat + latOffset, origin.lng + lngOffset);
                }
            }

            ApiManager.Instance.PlaceTower(
                GameManager.Instance.UserId,
                selectedTerritoryId,
                selectedClass,
                selectedTier,
                activeGrant?.id,
                json =>
                {
                    Debug.Log($"[AR] 타워 배치 성공: {json}");
                    SpawnPlacedTowerInAR(worldPos);
                    GameManager.Instance.LoadUserData();
                    if (previewInstance != null) { Destroy(previewInstance); previewInstance = null; }
                    if (planeManager != null) planeManager.enabled = false;
                    activeGrant = null;
                },
                err =>
                {
                    Debug.LogError($"[AR] 타워 배치 실패: {err}");
                    // 다시 스캔 모드로
                    isPlacementMode = true;
                    if (scanHintPanel != null) scanHintPanel.SetActive(true);
                });
        }

        private void SpawnPlacedTowerInAR(Vector3 worldPos)
        {
            var fg = new FixedGuardian
            {
                id = System.Guid.NewGuid().ToString(),
                type = "defense",
                owner = GameManager.Instance.VisitorId,
                position = selectedGPSPos,
                towerClass = selectedClass,
                tier = selectedTier
            };
            ARModeController.Instance.SpawnMyFixedGuardian(fg, worldPos);
        }

        public void CancelPlacement()
        {
            isPlacementMode = false;
            if (classPickerPanel != null) classPickerPanel.SetActive(false);
            if (scanHintPanel != null) scanHintPanel.SetActive(false);
            if (territorySelectPanel != null) territorySelectPanel.SetActive(false);
            if (previewInstance != null) { Destroy(previewInstance); previewInstance = null; }
            if (placementIndicator != null) placementIndicator.SetActive(false);
            if (planeManager != null) planeManager.enabled = false;
            activeGrant = null;
        }
    }

    [System.Serializable]
    public class SlotGrant
    {
        public string id;
        public string territoryId;
        public LatLng position;
        public string ownerName;
        public string expiresAt;
        public int secondsRemaining;
    }
}
