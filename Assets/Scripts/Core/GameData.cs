using System;
using System.Collections.Generic;

namespace GuardianAR
{
    [Serializable]
    public class LatLng
    {
        public double lat;
        public double lng;

        public LatLng(double lat, double lng) { this.lat = lat; this.lng = lng; }
    }

    [Serializable]
    public class GuardianStats
    {
        public int atk;
        public int def;
        public int hp;
        public int abs;
        public int prd;
        public int spd;
        public int rng;
        public int ter;
        public int ult_charge;
    }

    [Serializable]
    public class Guardian
    {
        public string id;
        public string type; // animal, robot, aircraft
        public GuardianStats stats;
    }

    [Serializable]
    public class Territory
    {
        public string id;
        public string userId;
        public LatLng center;
        public float radius;
        public bool isOwn;
        public string vulnerable_until;  // ISO timestamp; null/expired면 정상
        public string tower_type;        // 'normal' | 'revenue'
    }

    [Serializable]
    public class NearbyPlayer
    {
        public string id;
        public string username;
        public LatLng location;
        public bool isOnline;
        public Guardian guardian;
    }

    [Serializable]
    public class FixedGuardianStats
    {
        public int atk;
        public int def;
        public int hp;
    }

    [Serializable]
    public class FixedGuardian
    {
        public string id;
        public string type;       // defense, production
        public string owner;
        public string ownerId;
        public string territoryId;
        public LatLng position;
        public FixedGuardianStats stats;
        public string towerClass; // generic/balista/cannon/assault/scifi/fire/ice/aqua/electric/nature/venom/arcane/crystal
        public int    tier;       // 1-5
        public int    range;      // m

        // 편의 프로퍼티
        public int Atk => stats?.atk ?? 0;
        public int Def => stats?.def ?? 0;
        public int Hp  => stats?.hp  ?? 0;
    }

    [Serializable]
    public class BattleResult
    {
        public bool success;
        public string winner; // attacker, defender
        public int attackerPower;
        public int defenderPower;
        public AbsorbedStats absorbed;
        public string battleId;
        public BattleDetails battleDetails;
        public string error;
    }

    [Serializable]
    public class AbsorbedStats
    {
        public int atk;
        public int def;
        public int hp;
    }

    [Serializable]
    public class BattleDetails
    {
        public BattleSide attacker;
        public BattleSide defender;
        public bool isJointDefense;
        public bool isFixedGuardianBattle;
    }

    [Serializable]
    public class BattleSide
    {
        public string name;
        public string type;
        public GuardianStats stats;
    }

    // 전투 트리거 상황
    public enum BattleStatus
    {
        None,
        IntrusionDetected,
        PlayerEncounter,
        FixedGuardianAttack
    }

    public class CurrentBattle
    {
        public BattleStatus status;
        public Territory territory;
        public NearbyPlayer targetPlayer;
        public FixedGuardian targetFixedGuardian;
        public BattleResult result;
    }

    // 고정 수호신 배치 요청
    [Serializable]
    public class PlaceFixedGuardianRequest
    {
        public string territoryId;
        public string userId;
        public double lat;
        public double lng;
        public PlaceStats stats;
        public string guardianType; // defense, production
    }

    [Serializable]
    public class PlaceStats
    {
        public int atk;
        public int def;
        public int hp;
    }

    // AR 전투 연출용 프레임 데이터
    public class BattleFrame
    {
        public bool isAttackerTurn;
        public int damage;
        public bool isCritical;
        public bool attackerUsedUlt;
        public bool defenderUsedUlt;
        public int attackerHpAfter;
        public int defenderHpAfter;
    }

    // ─── v2: 파츠 시스템 ─────────────────────────────────────────
    [Serializable]
    public class PartStatBonuses
    {
        public int atk;
        public int def;
        public int hp;
        public int abs;
        public int prd;
        public int spd;
        public int rng;
        public int ter;
    }

    [Serializable]
    public class Part
    {
        public string id;
        public string slot;          // head, body, arms, legs, core
        public int    tier;          // 1~5
        public string guardian_type;
        public PartStatBonuses stat_bonuses;
        public List<string> passives; // ["regenerate", "fortify", ...]
        public bool   equipped;
    }

    [Serializable]
    public class PartsResponse
    {
        public bool success;
        public List<Part> parts;
        public string error;
    }

    [Serializable]
    public class CombineResponse
    {
        public bool   success;
        public string result;  // "success" | "fail"
        public Part   part;    // 합성 결과 또는 잔해
        public int    successRate;
        public string message;
    }

    // ─── v2: 리더보드 ───────────────────────────────────────────
    [Serializable]
    public class LeaderboardRow
    {
        public int    rank;
        public string userId;
        public string username;
        public string layer;
        public int    battleWins;
        public int    seasonWins;
        public int    territoryCount;
        public long   totalArea;
        public int    revenueTowers;
    }

    [Serializable]
    public class SeasonInfo
    {
        public int    id;
        public string name;
        public string started_at;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public string mode;
        public SeasonInfo season;
        public List<LeaderboardRow> leaderboard;
    }

    // ─── v2: 활동 요약 / 오프라인 리포트 ────────────────────────
    [Serializable]
    public class ActivityPartTier
    {
        public int tier;
        public int count;
    }

    [Serializable]
    public class ActivitySummary
    {
        public int    partsCount;
        public List<ActivityPartTier> partsByTier;
        public int    attackedCount;
        public int    attackedWon;
        public int    attackedLost;
        public bool   defeated;
        public int    vulnerableCount;
        public int    currentRank;
    }

    [Serializable]
    public class ActivitySummaryResponse
    {
        public bool   success;
        public string lastSeen;
        public bool   hasContent;
        public ActivitySummary summary;
    }

    // ─── v2: 전투 프리뷰 ────────────────────────────────────────
    [Serializable]
    public class BattlePreview
    {
        public bool   success;
        public int    attackerPower;
        public int    defenderPower;
        public int    winChance;       // 0~100
        public bool   vulnerable;
        public float  typeAdvantage;   // 1.0 / 1.15 / 0.87
        public string attackerType;
        public string defenderType;
        public string error;
    }

    // 영역에 vulnerable_until 추가
    [Serializable]
    public class TerritoryV2
    {
        public string id;
        public string userId;
        public LatLng center;
        public float  radius;
        public bool   isOwn;
        public string vulnerable_until;
        public string tower_type;
    }
}
