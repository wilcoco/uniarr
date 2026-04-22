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
        public string type; // defense, production
        public string owner;
        public string ownerId;
        public string territoryId;
        public LatLng position;
        public FixedGuardianStats stats;

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
}
