using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GuardianAR
{
    public class ApiManager : MonoBehaviour
    {
        public static ApiManager Instance { get; private set; }

        // Inspector에서 서버 주소 설정 (예: https://your-app.railway.app)
        [SerializeField] private string serverUrl = "https://arr-production.up.railway.app";

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ─── GET ───────────────────────────────────────────────────────
        public void Get(string path, Action<string> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(GetCoroutine(serverUrl + path, onSuccess, onError));
        }

        private IEnumerator GetCoroutine(string url, Action<string> onSuccess, Action<string> onError)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(req.downloadHandler.text);
            else
                onError?.Invoke(req.error);
        }

        // ─── POST ──────────────────────────────────────────────────────
        public void Post(string path, object body, Action<string> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(PostCoroutine(serverUrl + path, JsonUtility.ToJson(body), onSuccess, onError));
        }

        private IEnumerator PostCoroutine(string url, string json, Action<string> onSuccess, Action<string> onError)
        {
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(req.downloadHandler.text);
            else
                onError?.Invoke(req.error);
        }

        // ─── API 메서드들 ──────────────────────────────────────────────

        public void GetGuardian(string visitorId, Action<string> cb, Action<string> err = null)
            => Get($"/api/guardian/{visitorId}", cb, err);

        public void CreateGuardian(string visitorId, string type, Action<string> cb, Action<string> err = null)
            => Post("/api/guardian/create", new { visitorId, type, parts = new { } }, cb, err);

        public void UpdateLocation(string visitorId, double lat, double lng, Action<string> cb = null)
            => Post("/api/guardian/location", new { visitorId, lat, lng }, cb);

        public void GetNearbyPlayers(double lat, double lng, float radius, string excludeUserId, Action<string> cb, Action<string> err = null)
            => Get($"/api/guardian/nearby-players?lat={lat}&lng={lng}&radius={radius}&excludeUserId={excludeUserId}", cb, err);

        public void GetMyTerritories(string userId, Action<string> cb, Action<string> err = null)
            => Get($"/api/territory/my/{userId}", cb, err);

        public void GetNearbyTerritories(double lat, double lng, float radius, string excludeUserId, Action<string> cb, Action<string> err = null)
            => Get($"/api/territory/nearby?lat={lat}&lng={lng}&radius={radius}&excludeUserId={excludeUserId}", cb, err);

        public void GetNearbyFixedGuardians(double lat, double lng, float radius, string excludeUserId, Action<string> cb, Action<string> err = null)
            => Get($"/api/territory/nearby-fixed-guardians?lat={lat}&lng={lng}&radius={radius}&excludeUserId={excludeUserId}", cb, err);

        public void ExpandTerritory(string userId, double lat, double lng, float radius, Action<string> cb, Action<string> err = null)
            => Post("/api/territory/expand", new { userId, lat, lng, radius }, cb, err);

        public void CheckIntrusion(string userId, double lat, double lng, Action<string> cb, Action<string> err = null)
            => Post("/api/territory/check-intrusion", new { userId, lat, lng }, cb, err);

        public void RequestBattle(string attackerId, string defenderId, string territoryId, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/request", new { attackerId, defenderId, territoryId }, cb, err);

        public void RequestPlayerBattle(string attackerId, string defenderId, string choice, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/request-player", new { attackerId, defenderId, choice }, cb, err);

        public void ExecuteBattle(string battleId, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/execute", new { battleId }, cb, err);

        public void ExecutePlayerBattle(string battleId, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/execute-player", new { battleId }, cb, err);

        public void AttackFixedGuardian(string attackerId, string fixedGuardianId, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/attack-fixed-guardian", new { attackerId, fixedGuardianId }, cb, err);

        public void PlaceFixedGuardian(PlaceFixedGuardianRequest req, Action<string> cb, Action<string> err = null)
            => Post("/api/territory/place-guardian", req, cb, err);

        public void GetMyTerritoryForPosition(string userId, double lat, double lng, Action<string> cb, Action<string> err = null)
            => Get($"/api/territory/my/{userId}", cb, err);

        public void UseUltimate(string visitorId, string battleId, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/ultimate", new { visitorId, battleId }, cb, err);
    }
}
