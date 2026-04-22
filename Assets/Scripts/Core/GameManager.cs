using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// 전역 게임 상태 관리 (웹의 Zustand gameStore에 대응)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ─── 유저 상태 ─────────────────────────────────────────────────
        public string VisitorId { get; private set; }
        public string UserId { get; private set; }
        public int Energy { get; private set; } = 100;

        // ─── 수호신 ────────────────────────────────────────────────────
        public Guardian MyGuardian { get; private set; }

        // ─── 영역 ──────────────────────────────────────────────────────
        public List<Territory> MyTerritories { get; private set; } = new();
        public List<Territory> NearbyTerritories { get; private set; } = new();

        // ─── 주변 ──────────────────────────────────────────────────────
        public List<NearbyPlayer> NearbyPlayers { get; private set; } = new();
        public List<FixedGuardian> NearbyFixedGuardians { get; private set; } = new();

        // ─── 전투 ──────────────────────────────────────────────────────
        public CurrentBattle ActiveBattle { get; private set; }
        public string LastIntrudedTerritoryId { get; private set; }

        // ─── 이벤트 ────────────────────────────────────────────────────
        public event Action OnUserDataChanged;
        public event Action OnTerritoriesChanged;
        public event Action OnNearbyChanged;
        public event Action<CurrentBattle> OnBattleTriggered;
        public event Action OnBattleEnded;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            VisitorId = PlayerPrefs.GetString("visitorId", null);
        }

        void Start()
        {
            LocationManager.Instance.OnLocationUpdated += HandleLocationUpdate;

            if (!string.IsNullOrEmpty(VisitorId))
                LoadUserData();
        }

        // ─── 로그인 ────────────────────────────────────────────────────
        public void SetVisitorId(string id)
        {
            VisitorId = id;
            PlayerPrefs.SetString("visitorId", id);
            PlayerPrefs.Save();
            LoadUserData();
        }

        // ─── 데이터 로드 ───────────────────────────────────────────────
        public void LoadUserData()
        {
            if (string.IsNullOrEmpty(VisitorId)) return;

            ApiManager.Instance.GetGuardian(VisitorId, json =>
            {
                var resp = JsonUtility.FromJson<GuardianResponse>(json);
                if (resp.guardian != null)
                {
                    MyGuardian = resp.guardian;
                    UserId = resp.userId;
                    Energy = resp.energy;
                    OnUserDataChanged?.Invoke();

                    if (!string.IsNullOrEmpty(UserId))
                        LoadMyTerritories();
                }
            });
        }

        private void LoadMyTerritories()
        {
            ApiManager.Instance.GetMyTerritories(UserId, json =>
            {
                var resp = JsonUtility.FromJson<TerritoriesResponse>(json);
                MyTerritories = resp.territories ?? new List<Territory>();
                foreach (var t in MyTerritories) t.isOwn = true;
                OnTerritoriesChanged?.Invoke();
            });
        }

        // ─── 위치 업데이트 ─────────────────────────────────────────────
        private void HandleLocationUpdate(LatLng loc)
        {
            ApiManager.Instance.UpdateLocation(VisitorId, loc.lat, loc.lng);

            if (string.IsNullOrEmpty(UserId)) return;

            RefreshNearby(loc);
            CheckIntrusion(loc);
        }

        private void RefreshNearby(LatLng loc)
        {
            ApiManager.Instance.GetNearbyTerritories(loc.lat, loc.lng, 1000f, UserId, json =>
            {
                var resp = JsonUtility.FromJson<TerritoriesResponse>(json);
                NearbyTerritories = resp.territories ?? new List<Territory>();
                OnTerritoriesChanged?.Invoke();
            });

            ApiManager.Instance.GetNearbyPlayers(loc.lat, loc.lng, 1000f, UserId, json =>
            {
                var resp = JsonUtility.FromJson<PlayersResponse>(json);
                NearbyPlayers = resp.players ?? new List<NearbyPlayer>();
                OnNearbyChanged?.Invoke();
            });

            ApiManager.Instance.GetNearbyFixedGuardians(loc.lat, loc.lng, 1000f, UserId, json =>
            {
                var resp = JsonUtility.FromJson<FixedGuardiansResponse>(json);
                NearbyFixedGuardians = resp.fixedGuardians ?? new List<FixedGuardian>();
                OnNearbyChanged?.Invoke();
            });
        }

        private void CheckIntrusion(LatLng loc)
        {
            ApiManager.Instance.CheckIntrusion(UserId, loc.lat, loc.lng, json =>
            {
                var resp = JsonUtility.FromJson<IntrusionResponse>(json);
                if (resp.intruded && resp.territory != null)
                {
                    if (resp.territory.id == LastIntrudedTerritoryId) return;

                    LastIntrudedTerritoryId = resp.territory.id;
                    TriggerBattle(new CurrentBattle
                    {
                        status = BattleStatus.IntrusionDetected,
                        territory = resp.territory
                    });
                }
                else
                {
                    LastIntrudedTerritoryId = null;
                }
            });
        }

        // ─── 수호신 생성 ───────────────────────────────────────────────
        public void CreateGuardian(string type, Action<bool> callback = null)
        {
            ApiManager.Instance.CreateGuardian(VisitorId, type, json =>
            {
                var resp = JsonUtility.FromJson<CreateGuardianResponse>(json);
                if (resp.success)
                {
                    MyGuardian = resp.guardian;
                    UserId = resp.userId;
                    OnUserDataChanged?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        // ─── 전투 ──────────────────────────────────────────────────────
        public void TriggerBattle(CurrentBattle battle)
        {
            ActiveBattle = battle;
            OnBattleTriggered?.Invoke(battle);
        }

        public void InitiatePlayerEncounter(NearbyPlayer player)
        {
            TriggerBattle(new CurrentBattle
            {
                status = BattleStatus.PlayerEncounter,
                targetPlayer = player
            });
        }

        public void InitiateFixedGuardianAttack(FixedGuardian fg)
        {
            TriggerBattle(new CurrentBattle
            {
                status = BattleStatus.FixedGuardianAttack,
                targetFixedGuardian = fg
            });
        }

        public void RespondToBattle(string choice, Action<BattleResult> callback)
        {
            var battle = ActiveBattle;
            if (battle == null) return;

            if (battle.status == BattleStatus.PlayerEncounter)
            {
                ApiManager.Instance.RequestPlayerBattle(UserId, battle.targetPlayer.id, choice, reqJson =>
                {
                    var req = JsonUtility.FromJson<BattleRequestResponse>(reqJson);
                    if (!req.success) { callback?.Invoke(null); return; }

                    if (choice == "alliance") { EndBattle(); callback?.Invoke(null); return; }

                    ApiManager.Instance.ExecutePlayerBattle(req.battleId, execJson =>
                    {
                        var result = JsonUtility.FromJson<BattleResult>(execJson);
                        ActiveBattle.result = result;
                        callback?.Invoke(result);
                        LoadUserData();
                    });
                });
            }
            else if (battle.status == BattleStatus.FixedGuardianAttack)
            {
                ApiManager.Instance.AttackFixedGuardian(UserId, battle.targetFixedGuardian.id, execJson =>
                {
                    var result = JsonUtility.FromJson<BattleResult>(execJson);
                    ActiveBattle.result = result;
                    callback?.Invoke(result);
                    LoadUserData();
                    if (LocationManager.Instance.HasLocation)
                        HandleLocationUpdate(LocationManager.Instance.CurrentLocation);
                });
            }
            else if (battle.status == BattleStatus.IntrusionDetected)
            {
                ApiManager.Instance.RequestBattle(UserId, battle.territory.userId, battle.territory.id, reqJson =>
                {
                    var req = JsonUtility.FromJson<BattleRequestResponse>(reqJson);
                    if (!req.success) { callback?.Invoke(null); return; }

                    ApiManager.Instance.ExecuteBattle(req.battleId, execJson =>
                    {
                        var result = JsonUtility.FromJson<BattleResult>(execJson);
                        ActiveBattle.result = result;
                        callback?.Invoke(result);
                        LoadUserData();
                    });
                });
            }
        }

        public void EndBattle()
        {
            ActiveBattle = null;
            OnBattleEnded?.Invoke();
        }

        // ─── 영역 확장 ─────────────────────────────────────────────────
        public void ExpandTerritory(float radius, Action<bool> callback = null)
        {
            var loc = LocationManager.Instance.CurrentLocation;
            if (loc == null) { callback?.Invoke(false); return; }

            ApiManager.Instance.ExpandTerritory(UserId, loc.lat, loc.lng, radius, json =>
            {
                var resp = JsonUtility.FromJson<ExpandTerritoryResponse>(json);
                if (resp.success)
                {
                    resp.territory.isOwn = true;
                    MyTerritories.Add(resp.territory);
                    Energy -= GetTerritoryEnergyCost(radius);
                    OnTerritoriesChanged?.Invoke();
                    OnUserDataChanged?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        private int GetTerritoryEnergyCost(float radius)
        {
            if (radius <= 50) return 10;
            if (radius <= 100) return 25;
            if (radius <= 200) return 60;
            if (radius <= 300) return 120;
            return 250;
        }

        // ─── JSON 응답 래퍼 (JsonUtility 한계 대응) ────────────────────
        [Serializable] private class GuardianResponse
        {
            public Guardian guardian;
            public string userId;
            public int energy;
        }
        [Serializable] private class TerritoriesResponse
        {
            public List<Territory> territories;
        }
        [Serializable] private class PlayersResponse
        {
            public List<NearbyPlayer> players;
        }
        [Serializable] private class FixedGuardiansResponse
        {
            public List<FixedGuardian> fixedGuardians;
        }
        [Serializable] private class IntrusionResponse
        {
            public bool intruded;
            public Territory territory;
        }
        [Serializable] private class CreateGuardianResponse
        {
            public bool success;
            public Guardian guardian;
            public string userId;
        }
        [Serializable] private class BattleRequestResponse
        {
            public bool success;
            public string battleId;
            public string error;
        }
        [Serializable] private class ExpandTerritoryResponse
        {
            public bool success;
            public Territory territory;
            public string error;
        }
    }
}
