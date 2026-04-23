using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// 전투/동맹 모달
    /// - 플레이어 탭 → 공격 or 동맹 선택
    /// - 공격: 즉시 결과 표시 + 방어자는 푸시 알림 수신
    /// - 동맹: 요청 전송 + 상대 푸시 알림
    /// </summary>
    public class BattleModal : MonoBehaviour
    {
        public static BattleModal Instance { get; private set; }

        [Header("패널들")]
        [SerializeField] private GameObject encounterPanel;
        [SerializeField] private GameObject animatingPanel;
        [SerializeField] private GameObject resultPanel;

        [Header("Encounter 패널")]
        [SerializeField] private TextMeshProUGUI encounterTitle;
        [SerializeField] private TextMeshProUGUI encounterDesc;
        [SerializeField] private Button battleButton;
        [SerializeField] private Button allianceButton;
        [SerializeField] private Button closeButton;

        [Header("Animating 패널")]
        [SerializeField] private TextMeshProUGUI vs1Text;
        [SerializeField] private TextMeshProUGUI vs2Text;
        [SerializeField] private TextMeshProUGUI powerText;

        [Header("Result 패널")]
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI absorbText;
        [SerializeField] private Button resultCloseButton;

        private NearbyPlayer currentTarget;
        private Territory currentTerritory;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            battleButton.onClick.AddListener(OnAttack);
            allianceButton.onClick.AddListener(OnAllianceRequest);
            closeButton.onClick.AddListener(Hide);
            resultCloseButton.onClick.AddListener(Hide);
            gameObject.SetActive(false);
        }

        // ─── 플레이어 마커 탭 시 호출 ────────────────────────────────
        public void ShowPlayerMenu(NearbyPlayer player)
        {
            currentTarget    = player;
            currentTerritory = null;

            gameObject.SetActive(true);
            encounterPanel.SetActive(true);
            animatingPanel.SetActive(false);
            resultPanel.SetActive(false);

            string emoji = player.guardian?.type switch
            {
                "animal" => "🦁", "robot" => "🤖", "aircraft" => "✈", _ => "👤"
            };
            encounterTitle.text = $"{emoji} {player.username}";
            encounterDesc.text = player.guardian != null
                ? $"ATK:{player.guardian.stats?.atk}  DEF:{player.guardian.stats?.def}  HP:{player.guardian.stats?.hp}"
                : "No Guardian";
            allianceButton.gameObject.SetActive(true);
        }

        // ─── 고정 수호신 탭 시 호출 ──────────────────────────────────
        public void ShowFixedGuardianMenu(FixedGuardian fg)
        {
            currentTarget    = null;
            currentTerritory = null;

            gameObject.SetActive(true);
            encounterPanel.SetActive(true);
            animatingPanel.SetActive(false);
            resultPanel.SetActive(false);

            encounterTitle.text = $"{(fg.type == "production" ? "⚙" : "🛡")} {fg.owner}'s Guardian";
            encounterDesc.text  = $"ATK:{fg.Atk}  DEF:{fg.Def}  HP:{fg.Hp}";
            allianceButton.gameObject.SetActive(false);

            // 고정 수호신 공격은 기존 API 사용
            battleButton.onClick.RemoveAllListeners();
            battleButton.onClick.AddListener(() =>
            {
                ShowAnimating("Me", $"{fg.owner}'s Guardian");
                GameManager.Instance.InitiateFixedGuardianAttack(fg);
                GameManager.Instance.RespondToBattle("battle", result =>
                {
                    if (result != null) StartCoroutine(ShowResult(result));
                    else Hide();
                });
            });
        }

        // ─── 공격 선택 ────────────────────────────────────────────────
        private void OnAttack()
        {
            if (currentTarget == null) return;

            ShowAnimating("Me", currentTarget.username);

            GameManager.Instance.AttackPlayer(currentTarget, result =>
            {
                if (result != null) StartCoroutine(ShowResult(result));
                else Hide();
            });
        }

        // ─── 동맹 요청 ────────────────────────────────────────────────
        private void OnAllianceRequest()
        {
            if (currentTarget == null) return;

            allianceButton.interactable = false;
            GameManager.Instance.RequestAlliance(currentTarget, success =>
            {
                if (success)
                {
                    encounterTitle.text = "Alliance Requested!";
                    encounterDesc.text  = $"Alliance request sent to {currentTarget.username}.";
                }
                else
                {
                    encounterDesc.text = "Alliance request failed.";
                }
                allianceButton.interactable = true;
            });
        }

        // ─── 전투 연출 패널 ───────────────────────────────────────────
        private void ShowAnimating(string attacker, string defender)
        {
            encounterPanel.SetActive(false);
            animatingPanel.SetActive(true);
            resultPanel.SetActive(false);
            vs1Text.text   = attacker;
            vs2Text.text   = defender;
            powerText.text = "Fighting...";
        }

        // ─── 결과 표시 ────────────────────────────────────────────────
        private IEnumerator ShowResult(BattleResult result)
        {
            yield return new WaitForSeconds(2f);

            animatingPanel.SetActive(false);
            resultPanel.SetActive(true);

            bool iWon = result.winner == "attacker";
            winnerText.text  = iWon ? "Victory!" : "Defeat...";
            winnerText.color = iWon ? Color.green : Color.red;
            powerText.text   = $"Power {result.attackerPower} vs {result.defenderPower}";
            absorbText.text  = iWon && result.absorbed != null
                ? $"Absorbed: ATK+{result.absorbed.atk}  DEF+{result.absorbed.def}  HP+{result.absorbed.hp}"
                : "";
        }

        private void Hide()
        {
            currentTarget    = null;
            currentTerritory = null;
            gameObject.SetActive(false);

            // 버튼 리스너 원상복구
            battleButton.onClick.RemoveAllListeners();
            battleButton.onClick.AddListener(OnAttack);
        }
    }
}
