using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

namespace GuardianAR
{
    /// <summary>
    /// AR 모드 총괄 컨트롤러
    /// - GPS → AR 월드 좌표 변환
    /// - 수호신 / 영역 / 고정 수호신 AR 오브젝트 생명주기
    /// - ARBattleManager / ARFixedGuardianPlacer에 오브젝트 참조 제공
    /// </summary>
    public class ARModeController : MonoBehaviour
    {
        public static ARModeController Instance { get; private set; }

        [Header("AR Foundation")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARCameraManager arCameraManager;
        [SerializeField] private ARPlaneManager arPlaneManager;

        [Header("Prefabs")]
        [SerializeField] private GameObject myGuardianARPrefab;
        [SerializeField] private GameObject enemyGuardianARPrefab;
        [SerializeField] private GameObject fixedGuardianARPrefab;
        [SerializeField] private GameObject myFixedGuardianARPrefab;   // 내가 방금 배치한 것 (초록 발광)
        [SerializeField] private GameObject territoryARPrefab;

        [Header("AR UI")]
        [SerializeField] private Button backToMapButton;
        [SerializeField] private Button placeGuardianButton;            // 고정 수호신 배치 버튼
        [SerializeField] private Button arBattleSkipButton;             // 전투 없이 일반 모달로

        // AR 원점 GPS
        public LatLng AROrigin { get; private set; }
        private bool originSet = false;

        // 씬 오브젝트 추적
        private GameObject myGuardianObj;
        private readonly Dictionary<string, GameObject> arPlayers = new();
        private readonly Dictionary<string, GameObject> arFixedGuardians = new();
        private readonly Dictionary<string, GameObject> arMyTowers = new();  // 내 타워 (3D)
        private readonly Dictionary<string, GameObject> arMyFixedGuardians = new();
        private readonly Dictionary<string, GameObject> arTerritories = new();

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            backToMapButton.onClick.AddListener(() => ModeController.Instance.SwitchToMap());
            placeGuardianButton.onClick.AddListener(() => ARFixedGuardianPlacer.Instance.StartPlacementMode());

            var gm = GameManager.Instance;
            gm.OnNearbyChanged += RefreshEnemyObjects;
            gm.OnTerritoriesChanged += RefreshTerritories;

            LocationManager.Instance.OnLocationUpdated += OnLocationUpdated;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNearbyChanged -= RefreshEnemyObjects;
                GameManager.Instance.OnTerritoriesChanged -= RefreshTerritories;
            }
            if (LocationManager.Instance != null)
                LocationManager.Instance.OnLocationUpdated -= OnLocationUpdated;
        }

        // ─── 모드 진입/퇴출 ───────────────────────────────────────────
        public void EnterARMode()
        {
            arSession.enabled = true;

            if (LocationManager.Instance.HasLocation)
            {
                AROrigin = LocationManager.Instance.CurrentLocation;
                originSet = true;
            }

            // 최신 데이터 강제 새로고침 (web에서 새 타워 배치했을 가능성)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadUserData();  // MyTerritories + MyFixedGuardians 갱신
            }

            PlaceMyGuardian();
            RefreshEnemyObjects();
            RefreshTerritories();
            RefreshMyTowers();
        }

        public void ExitARMode()
        {
            arSession.enabled = false;
            arPlaneManager.enabled = false;
            ClearAll();
        }

        // ─── 내 수호신 (카메라 앞 고정, GPS 앵커 없음) ───────────────
        private void PlaceMyGuardian()
        {
            if (myGuardianObj != null || myGuardianARPrefab == null) return;

            myGuardianObj = Instantiate(myGuardianARPrefab);

            // 내 수호신은 ARWorldAnchor 없이 카메라 앞에 배치
            var anchor = myGuardianObj.GetComponent<ARWorldAnchor>();
            if (anchor != null) anchor.enabled = false;

            PositionInFront(myGuardianObj.transform, 1.5f);

            var g = GameManager.Instance.MyGuardian;
            if (g != null)
                myGuardianObj.GetComponent<GuardianARObject>()?.Setup(g, true);
        }

        private void OnLocationUpdated(LatLng loc)
        {
            if (!originSet) { AROrigin = loc; originSet = true; }

            // 내 수호신은 항상 카메라 1.5m 앞
            if (myGuardianObj != null)
                PositionInFront(myGuardianObj.transform, 1.5f);

            if (originSet)
            {
                RefreshEnemyObjects();
                RefreshTerritories();
                RefreshMyTowers();
            }
        }

        // ─── 내 타워 AR 스폰 (3D Piloto Studio 모델) ──────────────────
        private ARFixedGuardianPlacer _placerCache;
        private ARFixedGuardianPlacer Placer => _placerCache ??= FindObjectOfType<ARFixedGuardianPlacer>();

        private void RefreshMyTowers()
        {
            if (!originSet) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.MyFixedGuardians == null) return;

            // 신규/업데이트
            foreach (var fg in gm.MyFixedGuardians)
            {
                if (fg.position == null) continue;

                if (!arMyTowers.ContainsKey(fg.id))
                {
                    var prefab = Placer != null
                        ? Placer.GetTowerPrefab(fg.towerClass ?? "generic", Mathf.Clamp(fg.tier, 1, 5))
                        : myFixedGuardianARPrefab;
                    if (prefab == null) continue;

                    var obj = Instantiate(prefab);
                    arMyTowers[fg.id] = obj;

                    // 티어 시각 보정 (Lv1 모델 + 스케일/색조)
                    float scale = 1f + (Mathf.Clamp(fg.tier, 1, 5) - 1) * 0.12f;
                    obj.transform.localScale = Vector3.one * scale;

                    // 라벨 추가 (이름 + 티어)
                    var labelObj = new GameObject("Label");
                    labelObj.transform.SetParent(obj.transform, false);
                    labelObj.transform.localPosition = new Vector3(0, 1.2f * scale, 0);
                    var tmp = labelObj.AddComponent<TMPro.TextMeshPro>();
                    tmp.text = $"{(fg.towerClass ?? "tower").ToUpper()} L{fg.tier}";
                    tmp.fontSize = 1.5f;
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                    tmp.color = TierColor(fg.tier);
                }

                // 위치 갱신
                arMyTowers[fg.id].transform.position = GPSToWorld(fg.position) + Vector3.up * 0.05f;

                // 라벨이 카메라 향하도록
                var lbl = arMyTowers[fg.id].transform.Find("Label");
                if (lbl != null && Camera.main != null)
                    lbl.LookAt(Camera.main.transform);
            }

            // 사라진 타워 제거 (서버에서 격파/이동된 경우)
            var alive = new HashSet<string>();
            foreach (var fg in gm.MyFixedGuardians) alive.Add(fg.id);
            var toRemove = new List<string>();
            foreach (var kv in arMyTowers) if (!alive.Contains(kv.Key)) toRemove.Add(kv.Key);
            foreach (var k in toRemove) { Destroy(arMyTowers[k]); arMyTowers.Remove(k); }
        }

        static Color TierColor(int tier) => tier switch
        {
            1 => new Color(0.65f, 0.65f, 0.70f),
            2 => new Color(0.30f, 0.85f, 0.50f),
            3 => new Color(0.65f, 0.55f, 0.95f),
            4 => new Color(0.96f, 0.62f, 0.04f),
            5 => new Color(0.96f, 0.27f, 0.37f),
            _ => Color.white
        };

        // ─── 적 플레이어 / 고정 수호신 ────────────────────────────────
        private void RefreshEnemyObjects()
        {
            if (!originSet) return;
            var gm = GameManager.Instance;

            // 플레이어
            foreach (var p in gm.NearbyPlayers)
            {
                if (!arPlayers.ContainsKey(p.id))
                {
                    var obj = Instantiate(enemyGuardianARPrefab);
                    obj.GetComponent<GuardianARObject>()?.Setup(p);

                    var tap = obj.AddComponent<TapHandler>();
                    var cap = p;
                    tap.OnTapped += () => GameManager.Instance.InitiatePlayerEncounter(cap);
                    arPlayers[p.id] = obj;
                }

                if (p.location != null)
                    arPlayers[p.id].transform.position = GPSToWorld(p.location) + Vector3.up * 0.5f;
            }

            // 적 고정 수호신
            foreach (var fg in gm.NearbyFixedGuardians)
            {
                if (!arFixedGuardians.ContainsKey(fg.id))
                {
                    var obj = Instantiate(fixedGuardianARPrefab);
                    obj.GetComponent<FixedGuardianARObject>()?.Setup(fg);

                    var tap = obj.AddComponent<TapHandler>();
                    var cap = fg;
                    tap.OnTapped += () => GameManager.Instance.InitiateFixedGuardianAttack(cap);
                    arFixedGuardians[fg.id] = obj;
                }

                if (fg.position != null)
                    arFixedGuardians[fg.id].transform.position = GPSToWorld(fg.position) + Vector3.up * 0.3f;
            }
        }

        // ─── 영역 ──────────────────────────────────────────────────────
        private void RefreshTerritories()
        {
            if (!originSet) return;
            var gm = GameManager.Instance;

            var all = new List<Territory>();
            all.AddRange(gm.MyTerritories);
            all.AddRange(gm.NearbyTerritories);

            foreach (var t in all)
            {
                if (!arTerritories.ContainsKey(t.id) && territoryARPrefab != null)
                    arTerritories[t.id] = Instantiate(territoryARPrefab);

                if (!arTerritories.TryGetValue(t.id, out var go) || t.center == null) continue;

                go.transform.position = GPSToWorld(t.center);
                go.transform.localScale = new Vector3(t.radius * 2f, 0.05f, t.radius * 2f);

                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    bool vulnerable = IsVulnerable(t);
                    Color c;
                    if (vulnerable)        c = new Color(1f, 0.85f, 0f, 0.4f);  // 노란 발광
                    else if (t.isOwn)      c = new Color(0f, 1f, 0.53f, 0.3f);
                    else                   c = new Color(1f, 0.27f, 0.27f, 0.3f);
                    rend.material.color = c;
                    if (rend.material.HasProperty("_EmissionColor"))
                        rend.material.SetColor("_EmissionColor", c * (vulnerable ? 2f : 0.5f));
                }
            }
        }

        static bool IsVulnerable(Territory t)
        {
            if (string.IsNullOrEmpty(t.vulnerable_until)) return false;
            if (System.DateTime.TryParse(t.vulnerable_until, null,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var until))
                return until > System.DateTime.UtcNow;
            return false;
        }

        // ─── 내가 배치한 고정 수호신 즉시 AR 스폰 (ARFixedGuardianPlacer 호출) ──
        public void SpawnMyFixedGuardian(FixedGuardian fg, Vector3 worldPos)
        {
            if (myFixedGuardianARPrefab == null) return;

            var obj = Instantiate(myFixedGuardianARPrefab, worldPos, Quaternion.identity);
            obj.GetComponent<FixedGuardianARObject>()?.Setup(fg);

            // 내 고정 수호신은 탭 이벤트 없음 (자신은 공격 불가)
            arMyFixedGuardians[fg.id] = obj;
        }

        // ─── 외부 참조 제공 (ARBattleManager용) ───────────────────────
        public GameObject GetMyGuardianObject() => myGuardianObj;

        public GameObject GetPlayerObject(string playerId)
            => arPlayers.TryGetValue(playerId, out var go) ? go : null;

        public GameObject GetFixedGuardianObject(string fgId)
            => arFixedGuardians.TryGetValue(fgId, out var go) ? go : null;

        public GameObject GetNearestEnemyObject()
        {
            GameObject nearest = null;
            float minDist = float.MaxValue;
            var cam = Camera.main.transform.position;

            foreach (var kv in arPlayers)
            {
                float d = Vector3.Distance(kv.Value.transform.position, cam);
                if (d < minDist) { minDist = d; nearest = kv.Value; }
            }
            foreach (var kv in arFixedGuardians)
            {
                float d = Vector3.Distance(kv.Value.transform.position, cam);
                if (d < minDist) { minDist = d; nearest = kv.Value; }
            }
            return nearest;
        }

        // ─── GPS → AR 월드 좌표 ────────────────────────────────────────
        public Vector3 GPSToWorld(LatLng loc)
            => LocationManager.GPSToLocalOffset(loc, AROrigin);

        private void PositionInFront(Transform target, float distance)
        {
            var cam = Camera.main.transform;
            target.position = cam.position + cam.forward * distance;
            target.rotation = Quaternion.LookRotation(-cam.forward);
        }

        private void ClearAll()
        {
            foreach (var kv in arPlayers) Destroy(kv.Value);
            foreach (var kv in arFixedGuardians) Destroy(kv.Value);
            foreach (var kv in arMyFixedGuardians) Destroy(kv.Value);
            foreach (var kv in arMyTowers) Destroy(kv.Value);
            foreach (var kv in arTerritories) Destroy(kv.Value);
            if (myGuardianObj != null) Destroy(myGuardianObj);

            arPlayers.Clear();
            arFixedGuardians.Clear();
            arMyFixedGuardians.Clear();
            arMyTowers.Clear();
            arTerritories.Clear();
            myGuardianObj = null;
            originSet = false;
        }
    }
}
