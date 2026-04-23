using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// AR 전투 오케스트레이터
    /// ① 수호신 배치 → ② 턴제 연출 → ③ 결과 표시
    /// </summary>
    public class ARBattleManager : MonoBehaviour
    {
        public static ARBattleManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private ARHPBar hpBarPrefab;
        [SerializeField] private GameObject damageNumberPrefab;

        [Header("Effects")]
        [SerializeField] private ARAttackEffect attackEffect;

        [Header("Battle UI (World Space Canvas)")]
        [SerializeField] private Canvas battleCanvas;
        [SerializeField] private TextMeshProUGUI battleStatusText;  // "전투 시작!" 등
        [SerializeField] private TextMeshProUGUI ultimatePromptText;// "궁극기 사용 가능!"
        [SerializeField] private Button ultButton;
        [SerializeField] private Button skipButton;

        [Header("Result UI")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitle;
        [SerializeField] private TextMeshProUGUI resultDetail;
        [SerializeField] private Button resultCloseButton;

        // 전투 중인 오브젝트 참조
        private GameObject attackerObj;
        private GameObject defenderObj;
        private ARHPBar attackerHPBar;
        private ARHPBar defenderHPBar;

        // 전투 상태
        private bool isBattleRunning;
        private bool playerUsedUltimate;
        private bool skipRequested;

        // 전투 포지션 (카메라 기준)
        private Vector3 AttackerPos => Camera.main.transform.position
                                       + Camera.main.transform.forward * 1.2f
                                       + Camera.main.transform.right * -0.5f;
        private Vector3 DefenderPos => Camera.main.transform.position
                                       + Camera.main.transform.forward * 1.2f
                                       + Camera.main.transform.right * 0.5f;

        void Awake()
        {
            Instance = this;
            battleCanvas.gameObject.SetActive(false);
            resultPanel.SetActive(false);
        }

        void Start()
        {
            GameManager.Instance.OnBattleTriggered += OnBattleTriggered;

            ultButton.onClick.AddListener(() =>
            {
                playerUsedUltimate = true;
                ultButton.interactable = false;
                ultimatePromptText.gameObject.SetActive(false);
            });

            skipButton.onClick.AddListener(() => skipRequested = true);

            resultCloseButton.onClick.AddListener(() =>
            {
                resultPanel.SetActive(false);
                GameManager.Instance.EndBattle();
            });
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBattleTriggered -= OnBattleTriggered;
        }

        // ─── 전투 트리거 ───────────────────────────────────────────────
        private void OnBattleTriggered(CurrentBattle battle)
        {
            if (ModeController.Instance.CurrentMode != ModeController.GameMode.AR) return;
            if (isBattleRunning) return;

            StartCoroutine(RunBattleSequence(battle));
        }

        // ─── 메인 전투 시퀀스 ─────────────────────────────────────────
        private IEnumerator RunBattleSequence(CurrentBattle battle)
        {
            isBattleRunning = true;
            skipRequested = false;
            playerUsedUltimate = false;

            // 1. 내 수호신 AR 오브젝트 가져오기
            attackerObj = ARModeController.Instance.GetMyGuardianObject();
            defenderObj = GetDefenderObject(battle);

            if (attackerObj == null || defenderObj == null)
            {
                // AR 오브젝트 없으면 일반 모달로 fallback
                isBattleRunning = false;
                yield break;
            }

            // 2. UI 활성화
            battleCanvas.gameObject.SetActive(true);
            resultPanel.SetActive(false);

            // 3. 수호신들을 전투 포지션으로 이동
            yield return StartCoroutine(MoveToPosition(attackerObj.transform, AttackerPos, 0.5f));
            yield return StartCoroutine(MoveToPosition(defenderObj.transform, DefenderPos, 0.5f));

            // 4. HP 바 생성
            var myGuardian = GameManager.Instance.MyGuardian;
            attackerHPBar = Instantiate(hpBarPrefab);
            attackerHPBar.Init(
                GameManager.Instance.VisitorId,
                myGuardian?.stats?.hp ?? 100,
                myGuardian?.stats?.ult_charge ?? 0
            );

            defenderHPBar = Instantiate(hpBarPrefab);
            SetDefenderHPBar(battle);

            // 5. 궁극기 버튼 상태
            bool canUseUlt = (myGuardian?.stats?.ult_charge ?? 0) >= 100;
            ultButton.gameObject.SetActive(canUseUlt);
            ultimatePromptText.gameObject.SetActive(canUseUlt);

            // 6. 카운트다운
            yield return StartCoroutine(Countdown());

            // 7. API에서 실제 결과 가져오기 (궁극기 여부와 함께)
            BattleResult result = null;
            bool apiDone = false;

            GameManager.Instance.RespondToBattle("battle", r =>
            {
                result = r;
                apiDone = true;
            });

            // 잠깐 대기 (궁극기 사용 시간)
            float waitTime = 0f;
            while (!apiDone && waitTime < 5f)
            {
                waitTime += Time.deltaTime;
                yield return null;
            }

            if (result == null)
            {
                yield return StartCoroutine(ShowBattleStatus("Server Error"));
                CleanupBattle();
                yield break;
            }

            // 8. 결과 기반 턴 연출 시뮬레이션
            yield return StartCoroutine(PlayBattleAnimation(result, myGuardian));

            // 9. 결과 패널
            ShowResult(result, myGuardian);

            isBattleRunning = false;
        }

        // ─── 전투 애니메이션 ───────────────────────────────────────────
        private IEnumerator PlayBattleAnimation(BattleResult result, Guardian myGuardian)
        {
            // 결과로부터 턴 흐름 역산
            var frames = SimulateBattleFrames(result, myGuardian);

            int attackerHpCurrent = myGuardian?.stats?.hp ?? 100;
            int defenderHpCurrent = GetDefenderInitialHp(GameManager.Instance.ActiveBattle);

            foreach (var frame in frames)
            {
                if (skipRequested) break;

                Transform attTr = frame.isAttackerTurn ? attackerObj.transform : defenderObj.transform;
                Transform defTr = frame.isAttackerTurn ? defenderObj.transform : attackerObj.transform;

                // 궁극기 연출
                if (frame.attackerUsedUlt && frame.isAttackerTurn)
                {
                    yield return StartCoroutine(ShowBattleStatus("⚡ Ultimate!"));
                    yield return StartCoroutine(
                        attackEffect.PlayUltimate(attTr, myGuardian?.type ?? "animal"));
                }

                // 공격 연출
                yield return StartCoroutine(
                    attackEffect.PlayAttack(attTr, defTr, frame.isCritical));

                // 데미지 숫자
                if (damageNumberPrefab != null)
                    ARDamageNumber.Spawn(damageNumberPrefab, defTr.position, frame.damage, frame.isCritical);

                // HP 바 업데이트
                if (frame.isAttackerTurn)
                {
                    defenderHpCurrent = frame.defenderHpAfter;
                    defenderHPBar?.SetHP(defenderHpCurrent);
                }
                else
                {
                    attackerHpCurrent = frame.attackerHpAfter;
                    attackerHPBar?.SetHP(attackerHpCurrent);
                }

                yield return new WaitForSeconds(0.8f);
            }

            // 패배한 쪽 쓰러지는 연출
            bool iWon = result.winner == "attacker";
            yield return StartCoroutine(DefeatAnimation(iWon ? defenderObj.transform : attackerObj.transform));
        }

        // 결과를 턴별 프레임으로 역산 (3~5턴 시뮬)
        private List<BattleFrame> SimulateBattleFrames(BattleResult result, Guardian myGuardian)
        {
            var frames = new List<BattleFrame>();
            bool iWon = result.winner == "attacker";

            int myHp = myGuardian?.stats?.hp ?? 100;
            int enemyHp = GetDefenderInitialHp(GameManager.Instance.ActiveBattle);

            int totalTurns = UnityEngine.Random.Range(3, 6);
            int attackerTurns = iWon ? (totalTurns / 2 + 1) : (totalTurns / 2);

            int myDmgPerTurn = Mathf.Max(1, result.attackerPower / attackerTurns);
            int enemyDmgPerTurn = Mathf.Max(1, result.defenderPower / (totalTurns - attackerTurns + 1));

            bool ultUsed = false;
            for (int i = 0; i < totalTurns; i++)
            {
                bool isMyTurn = (i % 2 == 0);
                bool crit = UnityEngine.Random.value < 0.2f;
                bool useUlt = !ultUsed && isMyTurn && playerUsedUltimate && i == 0;
                if (useUlt) ultUsed = true;

                int dmg = isMyTurn
                    ? (int)(myDmgPerTurn * (crit ? 1.5f : 1f) * (useUlt ? 1.5f : 1f))
                    : (int)(enemyDmgPerTurn * (crit ? 1.5f : 1f));

                if (isMyTurn) enemyHp = Mathf.Max(0, enemyHp - dmg);
                else myHp = Mathf.Max(0, myHp - dmg);

                frames.Add(new BattleFrame
                {
                    isAttackerTurn = isMyTurn,
                    damage = dmg,
                    isCritical = crit,
                    attackerUsedUlt = useUlt,
                    attackerHpAfter = myHp,
                    defenderHpAfter = enemyHp
                });

                if (myHp <= 0 || enemyHp <= 0) break;
            }

            return frames;
        }

        private int GetDefenderInitialHp(CurrentBattle battle)
        {
            if (battle == null) return 100;
            return battle.status switch
            {
                BattleStatus.PlayerEncounter => battle.targetPlayer?.guardian?.stats?.hp ?? 100,
                BattleStatus.FixedGuardianAttack => battle.targetFixedGuardian?.Hp ?? 50,
                BattleStatus.IntrusionDetected => 100,
                _ => 100
            };
        }

        // ─── 결과 UI ──────────────────────────────────────────────────
        private void ShowResult(BattleResult result, Guardian myGuardian)
        {
            battleCanvas.gameObject.SetActive(false);
            resultPanel.SetActive(true);

            bool iWon = result.winner == "attacker";
            resultTitle.text = iWon ? "🎉 Victory!" : "💀 Defeat...";
            resultTitle.color = iWon ? Color.green : Color.red;

            string detail = $"My Power: {result.attackerPower}  vs  Enemy: {result.defenderPower}\n";
            if (iWon && result.absorbed != null)
                detail += $"Absorbed → ATK+{result.absorbed.atk}  DEF+{result.absorbed.def}  HP+{result.absorbed.hp}";
            resultDetail.text = detail;

            CleanupBattle();
        }

        // ─── 유틸 ──────────────────────────────────────────────────────
        private IEnumerator Countdown()
        {
            for (int i = 3; i >= 1; i--)
            {
                battleStatusText.text = i.ToString();
                yield return new WaitForSeconds(0.8f);
            }
            battleStatusText.text = "Fight!";
            yield return new WaitForSeconds(0.5f);
            battleStatusText.text = "";
        }

        private IEnumerator ShowBattleStatus(string msg)
        {
            battleStatusText.text = msg;
            yield return new WaitForSeconds(1f);
            battleStatusText.text = "";
        }

        private IEnumerator MoveToPosition(Transform target, Vector3 dest, float duration)
        {
            Vector3 start = target.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.position = Vector3.Lerp(start, dest, elapsed / duration);
                yield return null;
            }
            target.position = dest;
        }

        private IEnumerator DefeatAnimation(Transform loser)
        {
            float dur = 0.8f;
            float elapsed = 0f;
            Vector3 startPos = loser.position;
            Quaternion startRot = loser.rotation;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                loser.position = startPos + Vector3.down * (t * 0.5f);
                loser.rotation = startRot * Quaternion.Euler(0, 0, t * 90f);
                yield return null;
            }
            loser.gameObject.SetActive(false);
        }

        private void SetDefenderHPBar(CurrentBattle battle)
        {
            switch (battle.status)
            {
                case BattleStatus.PlayerEncounter:
                    var p = battle.targetPlayer;
                    defenderHPBar.Init(p.username, p.guardian?.stats?.hp ?? 100, 0);
                    break;
                case BattleStatus.FixedGuardianAttack:
                    var fg = battle.targetFixedGuardian;
                    defenderHPBar.Init($"{fg.owner}'s Fixed Guardian", fg.Hp, 0);
                    break;
                case BattleStatus.IntrusionDetected:
                    defenderHPBar.Init("Defender", 100, 0);
                    break;
            }
        }

        private GameObject GetDefenderObject(CurrentBattle battle)
        {
            return battle.status switch
            {
                BattleStatus.PlayerEncounter =>
                    ARModeController.Instance.GetPlayerObject(battle.targetPlayer.id),
                BattleStatus.FixedGuardianAttack =>
                    ARModeController.Instance.GetFixedGuardianObject(battle.targetFixedGuardian.id),
                _ => ARModeController.Instance.GetNearestEnemyObject()
            };
        }

        private void CleanupBattle()
        {
            if (attackerHPBar != null) Destroy(attackerHPBar.gameObject);
            if (defenderHPBar != null) Destroy(defenderHPBar.gameObject);
            attackerHPBar = null;
            defenderHPBar = null;
        }
    }
}
