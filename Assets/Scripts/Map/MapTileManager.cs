using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace GuardianAR
{
    /// <summary>
    /// OpenStreetMap 타일을 다운로드해 UI RawImage 그리드에 렌더링
    /// </summary>
    public class MapTileManager : MonoBehaviour
    {
        public static MapTileManager Instance { get; private set; }

        [Header("Tile Settings")]
        [SerializeField] private int zoom = 15;
        [SerializeField] private int tileGridSize = 3; // 3×3 그리드
        [SerializeField] private int tilePixelSize = 256;

        [Header("References")]
        [SerializeField] private RectTransform tileContainer;
        [SerializeField] private RawImage tilePrefab;

        // 타일 캐시 (재다운로드 방지)
        private Dictionary<string, Texture2D> tileCache = new();
        private RawImage[,] tileImages;

        // 현재 중심 타일 좌표
        private int centerTileX;
        private int centerTileY;

        // 지도 중심 GPS
        public LatLng MapCenter { get; private set; }

        public event Action OnTilesLoaded;

        public void ChangeZoom(int delta)
        {
            zoom = Mathf.Clamp(zoom + delta, 3, 19);
            centerTileX = 0;
            centerTileY = 0;
            if (MapCenter != null) CenterOn(MapCenter);
        }

        // 월드 좌표 ↔ 픽셀 변환용 스케일
        public float MetersPerPixel => (float)(MetersPerTile(MapCenter?.lat ?? 37.5, zoom) / tilePixelSize);

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            CreateTileGrid();
        }

        private void CreateTileGrid()
        {
            if (tilePrefab == null || tileContainer == null)
            {
                Debug.LogError("[MapTileManager] tilePrefab 또는 tileContainer가 연결되지 않았습니다");
                return;
            }

            tileImages = new RawImage[tileGridSize, tileGridSize];
            int half = tileGridSize / 2;

            for (int y = 0; y < tileGridSize; y++)
            {
                for (int x = 0; x < tileGridSize; x++)
                {
                    var img = Instantiate(tilePrefab, tileContainer);
                    img.gameObject.SetActive(true);              // 프리펩이 비활성이라 클론도 비활성 → 강제 활성화
                    img.color = new Color(0.15f, 0.15f, 0.18f);  // 로딩 전 회색 배경 (검은 화면 방지)
                    img.rectTransform.sizeDelta = new Vector2(tilePixelSize, tilePixelSize);
                    img.rectTransform.anchoredPosition = new Vector2(
                        (x - half) * tilePixelSize,
                        -(y - half) * tilePixelSize
                    );
                    tileImages[x, y] = img;
                }
            }
            Debug.Log($"[MapTileManager] 타일 그리드 생성 완료 ({tileGridSize}×{tileGridSize})");
        }

        public void CenterOn(LatLng location)
        {
            MapCenter = location;
            int tx = LngToTileX(location.lng, zoom);
            int ty = LatToTileY(location.lat, zoom);

            if (tx == centerTileX && ty == centerTileY) return;

            centerTileX = tx;
            centerTileY = ty;
            LoadTiles();
        }

        private void LoadTiles()
        {
            int half = tileGridSize / 2;
            for (int y = 0; y < tileGridSize; y++)
            {
                for (int x = 0; x < tileGridSize; x++)
                {
                    int tx = centerTileX + (x - half);
                    int ty = centerTileY + (y - half);
                    StartCoroutine(LoadTile(tx, ty, tileImages[x, y]));
                }
            }
        }

        private IEnumerator LoadTile(int x, int y, RawImage target)
        {
            string key = $"{zoom}/{x}/{y}";

            if (tileCache.TryGetValue(key, out var cached))
            {
                target.texture = cached;
                target.color = Color.white;
                yield break;
            }

            // 타일 좌표가 유효 범위를 벗어나면 스킵 (월드 끝)
            int max = (1 << zoom) - 1;
            if (x < 0 || y < 0 || x > max || y > max)
            {
                target.color = new Color(0.1f, 0.1f, 0.12f);
                yield break;
            }

            // OSM은 앱 사용 금지 → CartoDB Voyager 무료 타일 사용 (앱 허용, attribution 필요)
            string s = new[] { "a", "b", "c", "d" }[(x + y) % 4];
            string url = $"https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{zoom}/{x}/{y}.png";

            using var req = UnityWebRequestTexture.GetTexture(url);
            req.SetRequestHeader("User-Agent", "GuardianAR/1.0 (mobile-game)");
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var tex = DownloadHandlerTexture.GetContent(req);
                tileCache[key] = tex;
                if (target != null)
                {
                    target.texture = tex;
                    target.color = Color.white;
                }
                OnTilesLoaded?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[MapTileManager] 타일 로드 실패 {key}: {req.error} ({req.responseCode}) URL={url}");
                if (target != null) target.color = new Color(0.2f, 0.05f, 0.05f); // 빨간 회색 = 실패 표시
            }
        }

        // ─── GPS ↔ 타일/픽셀 변환 ─────────────────────────────────────

        public static int LngToTileX(double lng, int zoom)
            => (int)Math.Floor((lng + 180.0) / 360.0 * (1 << zoom));

        public static int LatToTileY(double lat, int zoom)
        {
            double rad = lat * Math.PI / 180.0;
            return (int)Math.Floor((1.0 - Math.Log(Math.Tan(rad) + 1.0 / Math.Cos(rad)) / Math.PI) / 2.0 * (1 << zoom));
        }

        // 타일 원점(좌상단)의 GPS
        public static double TileXToLng(int x, int zoom)
            => x / (double)(1 << zoom) * 360.0 - 180.0;

        public static double TileYToLat(int y, int zoom)
        {
            double n = Math.PI - 2.0 * Math.PI * y / (1 << zoom);
            return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
        }

        /// GPS 좌표를 tileContainer 내 앵커 픽셀 좌표로 변환
        public Vector2 GPSToCanvasPosition(LatLng loc)
        {
            if (MapCenter == null) return Vector2.zero;

            double centerLng = TileXToLng(centerTileX, zoom);
            double centerLat = TileYToLat(centerTileY, zoom);
            double nextLng = TileXToLng(centerTileX + 1, zoom);
            double nextLat = TileYToLat(centerTileY + 1, zoom);

            double pixelsPerLng = tilePixelSize / (nextLng - centerLng);
            double pixelsPerLat = tilePixelSize / (nextLat - centerLat);

            float px = (float)((loc.lng - centerLng) * pixelsPerLng);
            float py = (float)((loc.lat - centerLat) * pixelsPerLat);
            return new Vector2(px, py);
        }

        // 줌 레벨에서 타일 한 장의 실제 미터 크기
        private static double MetersPerTile(double lat, int zoom)
        {
            const double earthCircumference = 40075016.6856;
            return earthCircumference * Math.Cos(lat * Math.PI / 180.0) / (1 << zoom);
        }
    }
}
