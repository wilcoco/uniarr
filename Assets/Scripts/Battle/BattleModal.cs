using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// 전투/동맹 모달 — 맵·AR 공통 오버레이
    /// </summary>
    public class BattleModal : MonoBehaviour
    {
        [Header("패널들")]
        [SerializeField] private GameObject encounterPanel;   // 선택 단계
        [SerializeField] private GameObject animatingPanel;  // 전투 연출
        [SerializeField] private GameObject resultPanel;     // 결과

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

        void Start()
        {
            GameManager.Instance.OnBattleTriggered += OnBattleTriggered;
            GameManager.Instance.OnBattleEnded += () => gameObject.SetActive(false);

            battleButton.onClick.AddListener(() => OnChoice("battle"));
            allianceButton.onClick.AddListener(() => OnChoice("alliance"));
            closeButton.onClick.AddListener(OnClose);
            resultCloseButton.onClick.AddListener(OnClose);

            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBattleTriggered -= OnBattleTriggered;
        }

        private void OnBattleTriggered(CurrentBattle battle)
        {
            gameObject.SetActive(true);
            ShowEncounterPanel(battle);
        }

        private void ShowEncounterPanel(CurrentBattle battle)
        {
            encounterPanel.SetActive(true);
            animatingPanel.SetActive(false);
            resultPanel.SetActive(false);

            switch (battle.status)
            {
                case BattleStatus.IntrusionDetected:
                    encounterTitle.text = "영역 침입!";
                    encounterDesc.text = $"{battle.territory?.userId ?? "적"}의 영역에 침입했습니다.";
                    allianceButton.gameObject.SetActive(true);
                    break;

                case BattleStatus.PlayerEncounter:
                    var p = battle.targetPlayer;
                    string emoji = p.guardian?.type switch
                    {
                        "animal" => "🦁", "robot" => "🤖", "aircraft" => "✈", _ => "👤"
                    };
                    encounterTitle.text = $"{emoji} {p.username}";
                    encounterDesc.text = $"ATK:{p.guardian?.stats?.atk} DEF:{p.guardian?.stats?.def} HP:{p.guardian?.stats?.hp}";
                    allianceButton.gameObject.SetActive(true);
                    break;

                case BattleStatus.FixedGuardianAttack:
                    var fg = battle.targetFixedGuardian;
                    encounterTitle.text = $"{(fg.type == "production" ? "⚙" : "🛡")} {fg.owner}의 수호신";
                    encounterDesc.text = $"ATK:{fg.Atk} DEF:{fg.Def} HP:{fg.Hp}";
                    allianceButton.gameObject.SetActive(false); // 고정 수호신은 동맹 불가
                    break;
            }
        }

        private void OnChoice(string choice)
        {
            encounterPanel.SetActive(false);
            animatingPanel.SetActive(true);

            var battle = GameManager.Instance.ActiveBattle;
            if (battle != null)
            {
                string attName = GameManager.Instance.VisitorId;
                string defName = battle.status == BattleStatus.PlayerEncounter
                    ? battle.targetPlayer.username
                    : battle.targetFixedGuardian?.owner ?? "방어자";

                vs1Text.text = attName;
                vs2Text.text = defName;
                powerText.text = "전투 중...";
            }

            GameManager.Instance.RespondToBattle(choice, result =>
            {
                if (result == null)
                {
                    // 동맹 또는 실패
                    GameManager.Instance.EndBattle();
                    return;
                }
                StartCoroutine(ShowResult(result));
            });
        }

        private IEnumerator ShowResult(BattleResult result)
        {
            // 4초 연출 대기
            yield return new WaitForSeconds(4f);

            animatingPanel.SetActive(false);
            resultPanel.SetActive(true);

            bool iWon = result.winner == "attacker";
            winnerText.text = iWon ? "🎉 승리!" : "💀 패배...";
            winnerText.color = iWon ? Color.green : Color.red;

            powerText.text = $"내 전투력: {result.attackerPower}  vs  상대: {result.defenderPower}";

            if (result.absorbed != null && iWon)
                absorbText.text = $"흡수: ATK+{result.absorbed.atk} DEF+{result.absorbed.def} HP+{result.absorbed.hp}";
            else
                absorbText.text = "";
        }

        private void OnClose()
        {
            GameManager.Instance.EndBattle();
        }
    }
}
