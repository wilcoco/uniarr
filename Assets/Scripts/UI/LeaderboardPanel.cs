using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// 리더보드 패널 - 면적/시즌/누적 탭
    /// </summary>
    public class LeaderboardPanel : MonoBehaviour
    {
        public static LeaderboardPanel Instance { get; private set; }

        [Header("Root")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeBtn;
        [SerializeField] private Button backdropBtn;

        [Header("Tabs")]
        [SerializeField] private Button tabAreaBtn;     // 면적
        [SerializeField] private Button tabSeasonBtn;   // 시즌 승
        [SerializeField] private Button tabAllTimeBtn;  // 누적 승

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI headerSubtitle;
        [SerializeField] private TextMeshProUGUI myRankText;

        [Header("List")]
        [SerializeField] private RectTransform listContent;

        private string mode = "area";
        private List<LeaderboardRow> rows = new();
        private SeasonInfo currentSeason;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            BindListeners();
            if (panel != null) panel.SetActive(false);
        }

        void BindListeners()
        {
            if (closeBtn != null) { closeBtn.onClick.RemoveAllListeners(); closeBtn.onClick.AddListener(Hide); }
            if (backdropBtn != null) { backdropBtn.onClick.RemoveAllListeners(); backdropBtn.onClick.AddListener(Hide); }
            if (tabAreaBtn != null) { tabAreaBtn.onClick.RemoveAllListeners(); tabAreaBtn.onClick.AddListener(() => SetMode("area")); }
            if (tabSeasonBtn != null) { tabSeasonBtn.onClick.RemoveAllListeners(); tabSeasonBtn.onClick.AddListener(() => SetMode("current")); }
            if (tabAllTimeBtn != null) { tabAllTimeBtn.onClick.RemoveAllListeners(); tabAllTimeBtn.onClick.AddListener(() => SetMode("all-time")); }
        }

        public void Show()
        {
            BindListeners();
            if (panel != null)
            {
                panel.SetActive(true);
                panel.transform.SetAsLastSibling();
            }
            Fetch();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            Debug.Log("[LeaderboardPanel] Hide()");
        }

        void Update()
        {
            if (panel != null && panel.activeSelf &&
                UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
            }
        }

        void SetMode(string m)
        {
            mode = m;
            UpdateTabVisuals();
            Fetch();
        }

        void UpdateTabVisuals()
        {
            ApplyTabColor(tabAreaBtn,    mode == "area");
            ApplyTabColor(tabSeasonBtn,  mode == "current");
            ApplyTabColor(tabAllTimeBtn, mode == "all-time");
        }

        void ApplyTabColor(Button b, bool active)
        {
            if (b == null) return;
            var img = b.GetComponent<Image>();
            if (img != null) img.color = active ? new Color(0f, 1f, 0.53f) : new Color(0.13f, 0.13f, 0.13f);
            var label = b.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = active ? Color.black : new Color(0.7f, 0.7f, 0.7f);
        }

        void Fetch()
        {
            ApiManager.Instance.GetLeaderboard(mode, json =>
            {
                if (string.IsNullOrEmpty(json) || json[0] != '{')
                {
                    Debug.LogWarning($"[LeaderboardPanel] 비정상 응답: {Truncate(json)}");
                    rows = new List<LeaderboardRow>();
                    Render();
                    return;
                }
                try
                {
                    var resp = JsonUtility.FromJson<LeaderboardResponse>(json);
                    rows = resp?.leaderboard ?? new List<LeaderboardRow>();
                    currentSeason = resp?.season;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LeaderboardPanel] JSON parse 실패: {e.Message}\n응답: {Truncate(json)}");
                    rows = new List<LeaderboardRow>();
                }
                Render();
            },
            err => Debug.LogError($"[LeaderboardPanel] HTTP error: {err}"));
        }

        static string Truncate(string s) => string.IsNullOrEmpty(s) ? "(empty)" : (s.Length > 200 ? s.Substring(0, 200) + "..." : s);

        void Render()
        {
            if (headerSubtitle != null)
            {
                string label = mode switch
                {
                    "current"  => $"Season {currentSeason?.name ?? ""}",
                    "all-time" => "All-Time",
                    _          => "By Area"
                };
                headerSubtitle.text = label;
            }

            // 내 순위 찾기
            string myId = GameManager.Instance?.UserId;
            LeaderboardRow myRow = null;
            if (!string.IsNullOrEmpty(myId))
            {
                foreach (var r in rows)
                    if (r.userId == myId) { myRow = r; break; }
            }

            if (myRankText != null)
            {
                if (myRow != null)
                    myRankText.text = $"My Rank: <color=#ffd700>#{myRow.rank}</color>  -  {(mode == "current" ? $"Season Wins {myRow.seasonWins}" : $"Wins {myRow.battleWins}")}";
                else
                    myRankText.text = "My Rank: -";
            }

            // 리스트 렌더
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Destroy(listContent.GetChild(i).gameObject);

            foreach (var r in rows)
                CreateRow(r, r.userId == myId);
        }

        void CreateRow(LeaderboardRow row, bool isMe)
        {
            var go = new GameObject($"Row_{row.rank}");
            go.transform.SetParent(listContent, false);
            var img = go.AddComponent<Image>();
            img.sprite = UITheme.GetRoundedSprite();
            img.type   = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = isMe ? UITheme.AccentSoft : UITheme.SurfaceRaised;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 64);

            // 좌측: 메달/순위 원형
            var rankCell = new GameObject("RankCell");
            rankCell.transform.SetParent(go.transform, false);
            var rankImg = rankCell.AddComponent<Image>();
            rankImg.sprite = UITheme.GetCircleSprite();
            rankImg.color = row.rank switch
            {
                1 => new Color(1f, 0.84f, 0f),
                2 => new Color(0.75f, 0.75f, 0.78f),
                3 => new Color(0.80f, 0.50f, 0.20f),
                _ => UITheme.SurfaceSubtle
            };
            var rankRt = rankCell.GetComponent<RectTransform>();
            rankRt.anchorMin = new Vector2(0, 0.5f); rankRt.anchorMax = new Vector2(0, 0.5f);
            rankRt.pivot = new Vector2(0.5f, 0.5f);
            rankRt.sizeDelta = new Vector2(40, 40);
            rankRt.anchoredPosition = new Vector2(28, 0);

            var rankLabel = new GameObject("RankNum");
            rankLabel.transform.SetParent(rankCell.transform, false);
            var rankTmp = rankLabel.AddComponent<TextMeshProUGUI>();
            rankTmp.text = $"{row.rank}";
            rankTmp.fontSize = row.rank <= 3 ? 22 : 16;
            rankTmp.fontStyle = FontStyles.Bold;
            rankTmp.alignment = TextAlignmentOptions.Center;
            rankTmp.color = row.rank <= 3 ? Color.black : UITheme.TextPrimary;
            rankTmp.raycastTarget = false;
            var rlRt = rankTmp.GetComponent<RectTransform>();
            rlRt.anchorMin = Vector2.zero; rlRt.anchorMax = Vector2.one;
            rlRt.offsetMin = rlRt.offsetMax = Vector2.zero;

            // 중앙: 유저명 + 메타
            var infoGO = new GameObject("Info");
            infoGO.transform.SetParent(go.transform, false);
            var infoTmp = infoGO.AddComponent<TextMeshProUGUI>();
            infoTmp.fontSize = UITheme.FontBody;
            infoTmp.color = UITheme.TextPrimary;
            infoTmp.richText = true;
            infoTmp.raycastTarget = false;

            string layer = row.layer == "veteran"
                ? "<color=#ffd700>Veteran</color>"
                : "<color=#777>Beginner</color>";
            string winsLabel = mode == "current" ? $"Season wins {row.seasonWins}" : $"Wins {row.battleWins}";
            string me = isMe ? " <color=#00ff88>(you)</color>" : "";

            infoTmp.text = $"<b>{row.username}</b>{me}\n<size=11><color=#999>{layer} - {winsLabel}</color></size>";

            var infoRt = infoTmp.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0, 0); infoRt.anchorMax = new Vector2(1, 1);
            infoRt.offsetMin = new Vector2(64, 8); infoRt.offsetMax = new Vector2(-100, -8);

            // 우측: 면적 + 영역수
            var statsGO = new GameObject("Stats");
            statsGO.transform.SetParent(go.transform, false);
            var statsTmp = statsGO.AddComponent<TextMeshProUGUI>();
            statsTmp.text = $"<color=#00ff88><b>{FormatArea(row.totalArea)}</b></color>\n<size=10><color=#777>Territories {row.territoryCount}</color></size>";
            statsTmp.fontSize = UITheme.FontBody;
            statsTmp.color = UITheme.TextPrimary;
            statsTmp.alignment = TextAlignmentOptions.Right;
            statsTmp.richText = true;
            statsTmp.raycastTarget = false;
            var statsRt = statsTmp.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(1, 0); statsRt.anchorMax = new Vector2(1, 1);
            statsRt.pivot = new Vector2(1, 0.5f);
            statsRt.sizeDelta = new Vector2(96, 0);
            statsRt.anchoredPosition = new Vector2(-12, 0);
        }

        static string FormatArea(long m2)
        {
            if (m2 <= 0) return "0 m2";
            if (m2 >= 1_000_000) return $"{m2 / 1_000_000f:F1} km2";
            if (m2 >= 10_000)    return $"{m2 / 10_000f:F1}ha";
            return $"{m2} m2";
        }
    }
}
