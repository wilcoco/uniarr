using System;
using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// React PWA를 WebView로 임베드. 모든 게임 로직은 web에서 동작.
    /// Unity는 AR 카메라 모드 진입 시에만 native로 전환.
    ///
    /// 메시지 프로토콜 (web → unity, JSON):
    ///   { "type": "ENTER_AR", "targetId": "...", "territoryId": "..." }
    ///   { "type": "EXIT_AR" }
    ///   { "type": "REQUEST_LOCATION" }
    ///
    /// 메시지 프로토콜 (unity → web, JSON):
    ///   { "type": "NATIVE_LOCATION", "lat": 37.4981, "lng": 127.0276 }
    ///   { "type": "AR_BATTLE_RESULT", ... }
    ///   { "type": "AR_MODE_CHANGED", "active": true|false }
    /// </summary>
    public class WebGameController : MonoBehaviour
    {
        public static WebGameController Instance { get; private set; }

        [Header("WebView 설정")]
        [SerializeField] private string webUrl = "https://arr-production.up.railway.app";

        private WebViewObject webView;
        private bool isWebReady = false;

        public event Action<string, string> OnEnterARRequest; // (targetId, territoryId)
        public event Action OnExitARRequest;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            // 서버 URL이 ApiManager에 있으면 그대로 사용
            if (ApiManager.Instance != null)
            {
                var fld = typeof(ApiManager).GetField("serverUrl",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var url = fld?.GetValue(ApiManager.Instance) as string;
                if (!string.IsNullOrEmpty(url)) webUrl = url;
            }

            CreateWebView();

            // 위치 정보가 들어오면 web으로 전달 (web GPS 대신)
            if (LocationManager.Instance != null)
                LocationManager.Instance.OnLocationUpdated += loc =>
                    PostToWeb($"{{\"type\":\"NATIVE_LOCATION\",\"lat\":{loc.lat},\"lng\":{loc.lng}}}");
        }

        void CreateWebView()
        {
            var go = new GameObject("WebViewObject");
            go.transform.SetParent(transform, false);
            webView = go.AddComponent<WebViewObject>();

            webView.Init(
                cb: msg => OnWebMessage(msg),
                err: e => Debug.LogError($"[WebGameController] WebView error: {e}"),
                started: u => Debug.Log($"[WebGameController] page started: {u}"),
                ld: u =>
                {
                    isWebReady = true;
                    Debug.Log($"[WebGameController] page loaded: {u}");

#if UNITY_EDITOR
                    // 에디터: 브라우저 GPS 권한 프롬프트 스킵 - 강남역 좌표 자동 반환
                    webView.EvaluateJS(@"
                        (function() {
                            var fake = { coords: { latitude: 37.4981, longitude: 127.0276, accuracy: 5, altitude: 0, heading: 0, speed: 0 }, timestamp: Date.now() };
                            window.__nativeLoc = fake;
                            if (navigator.geolocation) {
                                navigator.geolocation.getCurrentPosition = function(success) { try { success(window.__nativeLoc); } catch(e){} };
                                navigator.geolocation.watchPosition       = function(success) { try { success(window.__nativeLoc); } catch(e){} return 1; };
                                navigator.geolocation.clearWatch          = function() {};
                            }
                        })();
                    ");
#endif

                    // visitorId 자동 주입 (Unity 닉네임 → web localStorage)
                    var visitorId = GameManager.Instance?.VisitorId ?? PlayerPrefs.GetString("visitorId", "");
                    if (!string.IsNullOrEmpty(visitorId))
                        webView.EvaluateJS($"if(!window.localStorage.getItem('visitorId')){{window.localStorage.setItem('visitorId','{visitorId}');window.location.reload();}}");
                },
                enableWKWebView: true
            );

            webView.SetMargins(0, 0, 0, 0);
            webView.SetVisibility(true);
            webView.SetCameraAccess(false); // AR 모드는 native, web 카메라 불필요
            webView.LoadURL(webUrl);
        }

        // web → unity 메시지 수신
        void OnWebMessage(string raw)
        {
            Debug.Log($"[WebGameController] from web: {raw}");
            if (string.IsNullOrEmpty(raw)) return;

            try
            {
                var msg = JsonUtility.FromJson<WebMessage>(raw);
                if (msg == null || string.IsNullOrEmpty(msg.type)) return;

                switch (msg.type)
                {
                    case "SWITCH_TO_AR":   // 기존 React 메시지 호환
                    case "ENTER_AR":
                        OnEnterARRequest?.Invoke(msg.targetId, msg.territoryId);
                        SwitchToAR();
                        break;
                    case "EXIT_AR":
                    case "SWITCH_TO_MAP":
                        OnExitARRequest?.Invoke();
                        SwitchToMap();
                        break;
                    case "PLAYER_ENCOUNTER":
                    case "FIXED_GUARDIAN_ATTACK":
                        // 전투 대상이 들어옴 — AR 모드로 전환하면서 대상 정보 전달
                        OnEnterARRequest?.Invoke(msg.targetId, msg.territoryId);
                        SwitchToAR();
                        break;
                    case "LOCATION_UPDATE":
                        // Web GPS → Unity LocationManager 주입 (편의)
                        if (LocationManager.Instance != null)
                            LocationManager.Instance.InjectLocation(new LatLng(msg.lat, msg.lng));
                        break;
                    case "REQUEST_LOCATION":
                        var loc = LocationManager.Instance?.CurrentLocation;
                        if (loc != null)
                            PostToWeb($"{{\"type\":\"NATIVE_LOCATION\",\"lat\":{loc.lat},\"lng\":{loc.lng}}}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WebGameController] parse error: {e.Message}");
            }
        }

        public void PostToWeb(string json)
        {
            if (!isWebReady || webView == null) return;
            string escaped = json.Replace("\\", "\\\\").Replace("'", "\\'");
            // React unityBridge.js가 window.unityBridge.receive(msg) 형태로 받음
            webView.EvaluateJS(
                $"if(window.unityBridge && window.unityBridge.receive) window.unityBridge.receive('{escaped}');"
            );
        }

        public void SetWebViewVisible(bool visible)
        {
            if (webView != null) webView.SetVisibility(visible);
        }

        // AR 모드 진입: WebView 숨김 + ARModeRoot 활성화
        void SwitchToAR()
        {
            SetWebViewVisible(false);
            if (ModeController.Instance != null) ModeController.Instance.SwitchToAR();
            PostToWeb("{\"type\":\"AR_MODE_CHANGED\",\"active\":true}");
        }

        // AR 모드 이탈: WebView 표시 + ARModeRoot 비활성화
        public void SwitchToMap()
        {
            SetWebViewVisible(true);
            if (ModeController.Instance != null) ModeController.Instance.SwitchToMap();
            PostToWeb("{\"type\":\"AR_MODE_CHANGED\",\"active\":false}");
        }

        // 전투 결과를 web에 전달 (ARBattleManager가 호출)
        public void SendBattleResult(string winner, int attackerPower, int defenderPower)
        {
            PostToWeb($"{{\"type\":\"AR_BATTLE_RESULT\",\"winner\":\"{winner}\",\"attackerPower\":{attackerPower},\"defenderPower\":{defenderPower}}}");
            SwitchToMap();
        }

        [Serializable]
        class WebMessage
        {
            public string type;
            public string targetId;
            public string territoryId;
            public double lat;
            public double lng;
        }
    }
}
