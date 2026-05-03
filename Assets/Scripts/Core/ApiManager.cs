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
            => Post("/api/guardian/create", new ReqCreateGuardian { visitorId = visitorId, type = type }, cb, err);

        public void UpdateLocation(string visitorId, double lat, double lng, Action<string> cb = null)
            => Post("/api/guardian/location", new ReqLocation { visitorId = visitorId, lat = lat, lng = lng }, cb);

        public void GetNearbyPlayers(double lat, double lng, float radius, string excludeUserId, Action<string> cb, Action<string> err = null)
            => Get($"/api/guardian/nearby-players?lat={lat}&lng={lng}&radius={radius}&excludeUserId={excludeUserId}", cb, err);

        public void GetMyTerritories(string userId, Action<string> cb, Action<string> err = null)
            => Get($"/api/territory/my/{userId}", cb, err);

        public void GetNearbyTerritories(double lat, double lng, float radius, string excludeUserId, Action<string> cb, Action<string> err = null)
            => Get($"/api/territory/nearby?lat={lat}&lng={lng}&radius={radius}&excludeUserId={excludeUserId}", cb, err);

        public void GetNearbyFixedGuardians(double lat, double lng, float radius, string excludeUserId, Action<string> cb, Action<string> err = null)
            => Get($"/api/territory/nearby-fixed-guardians?lat={lat}&lng={lng}&radius={radius}&excludeUserId={excludeUserId}", cb, err);

        public void ExpandTerritory(string userId, double lat, double lng, float radius, Action<string> cb, Action<string> err = null)
            => Post("/api/territory/expand", new ReqExpand { userId = userId, lat = lat, lng = lng, radius = radius }, cb, err);

        public void CheckIntrusion(string userId, double lat, double lng, Action<string> cb, Action<string> err = null)
            => Post("/api/territory/check-intrusion", new ReqUserLocation { userId = userId, lat = lat, lng = lng }, cb, err);

        public void RequestBattle(string attackerId, string defenderId, string territoryId, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/request", new ReqBattleRequest { attackerId = attackerId, defenderId = defenderId, territoryId = territoryId }, cb, err);

        public void RequestPlayerBattle(string attackerId, string defenderId, string choice, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/request-player", new ReqRequestPlayer { attackerId = attackerId, defenderId = defenderId, choice = choice }, cb, err);

        // /execute는 attackerUltimate/defenderUltimate, /execute-player는 ultActivated
        public void ExecuteBattle(string battleId, bool arMode, bool ultActivated, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/execute", new ReqExecute { battleId = battleId, arMode = arMode, attackerUltimate = ultActivated, defenderUltimate = false }, cb, err);

        public void ExecutePlayerBattle(string battleId, bool arMode, bool ultActivated, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/execute-player", new ReqExecutePlayer { battleId = battleId, arMode = arMode, ultActivated = ultActivated }, cb, err);

        public void AttackFixedGuardian(string attackerId, string fixedGuardianId, bool arMode, bool ultActivated, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/attack-fixed-guardian", new ReqAttackFG { attackerId = attackerId, fixedGuardianId = fixedGuardianId, arMode = arMode, ultActivated = ultActivated }, cb, err);

        public void PlaceFixedGuardian(PlaceFixedGuardianRequest req, Action<string> cb, Action<string> err = null)
            => Post("/api/territory/place-guardian", req, cb, err);

        // 신 API — 13종 타워 배치 (towerClass + tier 모델)
        public void PlaceTower(string userId, string territoryId, string towerClass, int tier,
                               string grantId, Action<string> cb, Action<string> err = null)
            => Post("/api/towers/place",
                    new ReqPlaceTower {
                        userId = userId, territoryId = territoryId,
                        towerClass = towerClass, tier = tier, grantId = grantId
                    }, cb, err);

        public void GetTowerClasses(Action<string> cb, Action<string> err = null)
            => Get("/api/towers/classes", cb, err);

        public void GetSlotGrants(string userId, Action<string> cb, Action<string> err = null)
            => Get($"/api/towers/grants/{userId}", cb, err);

        public void GetMyTerritoryForPosition(string userId, double lat, double lng, Action<string> cb, Action<string> err = null)
            => Get($"/api/territory/my/{userId}", cb, err);

        public void UseUltimate(string visitorId, string battleId, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/ultimate", new ReqUltimate { visitorId = visitorId, battleId = battleId }, cb, err);

        // ─── 즉시 공격 (영역/플레이어) ────────────────────────────────
        public void Attack(string attackerId, string defenderId, string territoryId, bool arMode, bool ultActivated, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/attack", new ReqAttack { attackerId = attackerId, defenderId = defenderId, territoryId = territoryId, arMode = arMode, ultActivated = ultActivated }, cb, err);

        // ─── 동맹 요청/응답 ────────────────────────────────────────────
        public void RequestAlliance(string requesterId, string targetId, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/alliance-request", new ReqAlliance { requesterId = requesterId, targetId = targetId }, cb, err);

        public void RespondAlliance(string requestId, bool accept, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/alliance-respond", new ReqAllianceRespond { requestId = requestId, accept = accept }, cb, err);

        // ─── FCM 토큰 등록 ─────────────────────────────────────────────
        public void RegisterFcmToken(string userId, string fcmToken, Action<string> cb = null)
            => Post("/api/battle/fcm-token", new ReqFcm { userId = userId, fcmToken = fcmToken }, cb);

        // ─── 파츠 시스템 ──────────────────────────────────────────────
        public void GetParts(string userId, Action<string> cb, Action<string> err = null)
            => Get($"/api/parts/my/{userId}", cb, err);

        public void EquipPart(string userId, string partId, Action<string> cb, Action<string> err = null)
            => Post("/api/parts/equip", new ReqPart { userId = userId, partId = partId }, cb, err);

        public void UnequipPart(string userId, string partId, Action<string> cb, Action<string> err = null)
            => Post("/api/parts/unequip", new ReqPart { userId = userId, partId = partId }, cb, err);

        public void CombineParts(string userId, string[] partIds, Action<string> cb, Action<string> err = null)
            => Post("/api/parts/combine", new CombineRequest { userId = userId, partIds = partIds }, cb, err);

        // ─── 리더보드 ─────────────────────────────────────────────────
        // mode: "area" | "current" | "all-time"
        public void GetLeaderboard(string mode, Action<string> cb, Action<string> err = null)
            => Get($"/api/territory/leaderboard?season={mode}", cb, err);

        // ─── 활동/오프라인 요약 ───────────────────────────────────────
        public void GetActivitySummary(string userId, Action<string> cb, Action<string> err = null)
            => Get($"/api/activity/summary/{userId}", cb, err);

        public void ActivityPing(string userId, Action<string> cb = null)
            => Post("/api/activity/ping", new ReqUserId { userId = userId }, cb);

        // ─── 전투 프리뷰 ──────────────────────────────────────────────
        public void BattlePreview(string attackerId, string defenderId, string territoryId, bool arMode, bool ultActivated, Action<string> cb, Action<string> err = null)
            => Post("/api/battle/preview", new ReqAttack { attackerId = attackerId, defenderId = defenderId, territoryId = territoryId, arMode = arMode, ultActivated = ultActivated }, cb, err);
    }

    // ─── 요청 DTO (JsonUtility는 익명 타입 직렬화 불가, [Serializable] 클래스 필수) ──
    [System.Serializable] public class ReqCreateGuardian { public string visitorId; public string type; }
    [System.Serializable] public class ReqLocation       { public string visitorId; public double lat; public double lng; }
    [System.Serializable] public class ReqUserLocation   { public string userId;    public double lat; public double lng; }
    [System.Serializable] public class ReqExpand         { public string userId; public double lat; public double lng; public float radius; }
    [System.Serializable] public class ReqBattleRequest  { public string attackerId; public string defenderId; public string territoryId; }
    [System.Serializable] public class ReqRequestPlayer  { public string attackerId; public string defenderId; public string choice; }
    [System.Serializable] public class ReqExecute        { public string battleId;  public bool arMode; public bool attackerUltimate; public bool defenderUltimate; }
    [System.Serializable] public class ReqExecutePlayer  { public string battleId;  public bool arMode; public bool ultActivated; }
    [System.Serializable] public class ReqAttackFG       { public string attackerId; public string fixedGuardianId; public bool arMode; public bool ultActivated; }
    [System.Serializable] public class ReqUltimate       { public string visitorId; public string battleId; }
    [System.Serializable] public class ReqAttack         { public string attackerId; public string defenderId; public string territoryId; public bool arMode; public bool ultActivated; }
    [System.Serializable] public class ReqAlliance       { public string requesterId; public string targetId; }
    [System.Serializable] public class ReqAllianceRespond{ public string requestId; public bool accept; }
    [System.Serializable] public class ReqFcm            { public string userId; public string fcmToken; }
    [System.Serializable] public class ReqPart           { public string userId; public string partId; }
    [System.Serializable] public class ReqUserId         { public string userId; }
    [System.Serializable] public class CombineRequest    { public string userId; public string[] partIds; }
    [System.Serializable] public class ReqPlaceTower {
        public string userId; public string territoryId; public string towerClass;
        public int tier; public string grantId;
    }
}
