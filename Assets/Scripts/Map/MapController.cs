using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// 맵 화면 전체 컨트롤러 — 마커/영역 배치 및 터치 조작
    /// </summary>
    public class MapController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapTileManager tileManager;
        [SerializeField] private RectTransform markerContainer;   // 타일과 같은 좌표계
        [SerializeField] private RectTransform overlayContainer;  // UI 오버레이

        [Header("Prefabs")]
        [SerializeField] private GameObject myMarkerPrefab;
        [SerializeField] private GameObject guardianMarkerPrefab;
        [SerializeField] private GameObject otherPlayerPrefab;
        [SerializeField] private GameObject fixedGuardianPrefab;
        [SerializeField] private GameObject territoryCirclePrefab; // LineRenderer 포함

        [Header("AR 전환 버튼")]
        [SerializeField] private Button arModeButton;

        // 런타임 마커 인스턴스
        private GameObject myMarker;
        private Dictionary<string, GameObject> playerMarkers = new();
        private Dictionary<string, GameObject> fixedGuardianMarkers = new();
        private Dictionary<string, GameObject> territoryObjects = new();

        void Start()
        {
            var gm = GameManager.Instance;
            gm.OnTerritoriesChanged += RefreshTerritories;
            gm.OnNearbyChanged += RefreshNearby;

            var lm = LocationManager.Instance;
            lm.OnLocationUpdated += OnLocationUpdated;

            arModeButton.onClick.AddListener(SwitchToAR);

            tileManager.OnTilesLoaded += RefreshAllMarkers;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTerritoriesChanged -= RefreshTerritories;
                GameManager.Instance.OnNearbyChanged -= RefreshNearby;
            }
            if (LocationManager.Instance != null)
                LocationManager.Instance.OnLocationUpdated -= OnLocationUpdated;
        }

        private void OnLocationUpdated(LatLng loc)
        {
            tileManager.CenterOn(loc);
            PlaceMyMarker(loc);
        }

        // ─── 내 마커 ───────────────────────────────────────────────────
        private void PlaceMyMarker(LatLng loc)
        {
            if (myMarker == null)
                myMarker = Instantiate(myMarkerPrefab, markerContainer);

            myMarker.GetComponent<RectTransform>().anchoredPosition =
                tileManager.GPSToCanvasPosition(loc);
        }

        // ─── 전체 마커 새로고침 ────────────────────────────────────────
        private void RefreshAllMarkers()
        {
            if (LocationManager.Instance.HasLocation)
                PlaceMyMarker(LocationManager.Instance.CurrentLocation);
            RefreshTerritories();
            RefreshNearby();
        }

        // ─── 영역 ──────────────────────────────────────────────────────
        private void RefreshTerritories()
        {
            var gm = GameManager.Instance;
            var all = new List<Territory>();
            all.AddRange(gm.MyTerritories);
            all.AddRange(gm.NearbyTerritories);

            // 사라진 영역 제거
            var toRemove = new List<string>();
            foreach (var kv in territoryObjects)
            {
                if (!all.Exists(t => t.id == kv.Key))
                {
                    Destroy(kv.Value);
                    toRemove.Add(kv.Key);
                }
            }
            toRemove.ForEach(k => territoryObjects.Remove(k));

            // 추가/업데이트
            foreach (var t in all)
            {
                if (!territoryObjects.ContainsKey(t.id))
                {
                    var obj = Instantiate(territoryCirclePrefab, markerContainer);
                    territoryObjects[t.id] = obj;
                }

                var go = territoryObjects[t.id];
                go.GetComponent<RectTransform>().anchoredPosition =
                    tileManager.GPSToCanvasPosition(t.center);

                // 반경 픽셀 = 반경(m) / m-per-pixel
                float radiusPx = t.radius / tileManager.MetersPerPixel;
                go.GetComponent<TerritoryCircle>()?.SetCircle(radiusPx, t.isOwn);
            }
        }

        // ─── 주변 플레이어 / 고정 수호신 ──────────────────────────────
        private void RefreshNearby()
        {
            var gm = GameManager.Instance;

            // 플레이어 마커
            foreach (var p in gm.NearbyPlayers)
            {
                if (!playerMarkers.ContainsKey(p.id))
                {
                    var obj = Instantiate(otherPlayerPrefab, markerContainer);
                    // 클릭 이벤트 연결
                    var btn = obj.GetComponentInChildren<Button>();
                    var captured = p;
                    btn?.onClick.AddListener(() => GameManager.Instance.InitiatePlayerEncounter(captured));
                    playerMarkers[p.id] = obj;
                }

                playerMarkers[p.id].GetComponent<RectTransform>().anchoredPosition =
                    tileManager.GPSToCanvasPosition(p.location);

                // 이름 표시
                var label = playerMarkers[p.id].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = p.username;
            }

            // 고정 수호신 마커
            foreach (var fg in gm.NearbyFixedGuardians)
            {
                if (!fixedGuardianMarkers.ContainsKey(fg.id))
                {
                    var obj = Instantiate(fixedGuardianPrefab, markerContainer);
                    var btn = obj.GetComponentInChildren<Button>();
                    var captured = fg;
                    btn?.onClick.AddListener(() => GameManager.Instance.InitiateFixedGuardianAttack(captured));
                    fixedGuardianMarkers[fg.id] = obj;
                }

                fixedGuardianMarkers[fg.id].GetComponent<RectTransform>().anchoredPosition =
                    tileManager.GPSToCanvasPosition(fg.position);
            }
        }

        // ─── AR 전환 ───────────────────────────────────────────────────
        private void SwitchToAR()
        {
            ModeController.Instance.SwitchToAR();
        }
    }
}
