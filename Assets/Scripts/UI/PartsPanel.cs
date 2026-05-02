using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuardianAR
{
    /// <summary>
    /// 파츠 인벤토리 + 합성 패널
    /// React PartsPanel.jsx와 기능 동일
    /// </summary>
    public class PartsPanel : MonoBehaviour
    {
        public static PartsPanel Instance { get; private set; }

        // 슬롯/티어 메타
        static readonly string[] Slots = { "head", "body", "arms", "legs", "core" };
        static readonly Dictionary<string, string> SlotLabels = new()
        {
            ["head"] = "Head", ["body"] = "Body", ["arms"] = "Arms",
            ["legs"] = "Legs", ["core"] = "Core"
        };
        static readonly Dictionary<int, int> CombineRates = new()
        {
            [1] = 70, [2] = 55, [3] = 40, [4] = 25
        };

        [Header("Root")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeBtn;
        [SerializeField] private Button backdropBtn; // 백드롭 클릭으로도 닫기

        [Header("Filter")]
        [SerializeField] private Button filterAllBtn;
        [SerializeField] private Button[] filterSlotBtns; // 5개

        [Header("Hint / Result")]
        [SerializeField] private TextMeshProUGUI combineHintText;
        [SerializeField] private GameObject resultBanner;
        [SerializeField] private TextMeshProUGUI resultBannerText;

        [Header("Card List")]
        [SerializeField] private RectTransform listContent;       // VerticalLayoutGroup
        [SerializeField] private GameObject partCardPrefab;       // 동적 생성용 (없으면 코드로 생성)

        [Header("Combine Action")]
        [SerializeField] private Button combineBtn;
        [SerializeField] private TextMeshProUGUI combineBtnText;

        // 런타임 상태
        private List<Part> myParts = new();
        private readonly HashSet<string> selected = new();
        private string filter = "all";
        private bool busy = false;

        void Awake()
        {
            // Instance 중복 시 새 것을 우선시 (Destroy(gameObject)는 Canvas 통째로 날려버려 위험)
            Instance = this;
        }

        void Start()
        {
            BindListeners();
            if (panel != null) panel.SetActive(false);
            if (resultBanner != null) resultBanner.SetActive(false);
        }

        // 리스너를 한 곳에서 안전하게 바인딩 (재진입 시 중복 등록 방지)
        void BindListeners()
        {
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(Hide);
            }
            if (backdropBtn != null)
            {
                backdropBtn.onClick.RemoveAllListeners();
                backdropBtn.onClick.AddListener(Hide);
            }
            if (filterAllBtn != null)
            {
                filterAllBtn.onClick.RemoveAllListeners();
                filterAllBtn.onClick.AddListener(() => SetFilter("all"));
            }
            if (filterSlotBtns != null)
            {
                for (int i = 0; i < filterSlotBtns.Length && i < Slots.Length; i++)
                {
                    if (filterSlotBtns[i] == null) continue;
                    int idx = i;
                    filterSlotBtns[i].onClick.RemoveAllListeners();
                    filterSlotBtns[i].onClick.AddListener(() => SetFilter(Slots[idx]));
                }
            }
            if (combineBtn != null)
            {
                combineBtn.onClick.RemoveAllListeners();
                combineBtn.onClick.AddListener(OnCombine);
            }
        }

        public void Show()
        {
            BindListeners(); // 안전 재바인딩
            if (panel != null)
            {
                panel.SetActive(true);
                panel.transform.SetAsLastSibling();
            }
            FetchParts();
        }

        public void Hide()
        {
            Debug.Log("[PartsPanel] Hide() called");
            if (panel != null) panel.SetActive(false);
            selected.Clear();
            if (resultBanner != null) resultBanner.SetActive(false);
        }

        void Update()
        {
            // ESC 키로도 닫기 (안전 fallback)
            if (panel != null && panel.activeSelf &&
                UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
            }
        }

        // 내부 패널 클릭이 백드롭에 전파되지 않도록 빈 핸들러 (Inner panel button)
        public void OnPanelClicked() { /* swallow */ }

        void SetFilter(string f)
        {
            filter = f;
            Render();
        }

        void FetchParts()
        {
            var userId = GameManager.Instance?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            ApiManager.Instance.GetParts(userId, json =>
            {
                if (string.IsNullOrEmpty(json) || json[0] != '{')
                {
                    Debug.LogWarning($"[PartsPanel] 비정상 응답: {Trunc(json)}");
                    myParts = new List<Part>();
                    Render();
                    return;
                }
                try
                {
                    var resp = JsonUtility.FromJson<PartsResponse>(json);
                    myParts = resp?.parts ?? new List<Part>();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[PartsPanel] JSON parse 실패: {e.Message}\n응답: {Trunc(json)}");
                    myParts = new List<Part>();
                }
                Render();
            },
            err => Debug.LogError($"[PartsPanel] HTTP error: {err}"));
        }

        static string Trunc(string s) => string.IsNullOrEmpty(s) ? "(empty)" : (s.Length > 200 ? s.Substring(0, 200) + "..." : s);

        void Render()
        {
            if (listContent == null) return;

            // 기존 카드 제거
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Destroy(listContent.GetChild(i).gameObject);

            // 필터링
            var filtered = filter == "all" ? myParts : myParts.Where(p => p.slot == filter).ToList();

            if (filtered.Count == 0)
            {
                var emptyGO = new GameObject("Empty");
                emptyGO.transform.SetParent(listContent, false);
                var tmp = emptyGO.AddComponent<TextMeshProUGUI>();
                tmp.text = "No parts - owning territories drops parts hourly";
                tmp.fontSize = 14;
                tmp.color = new Color(0.5f, 0.5f, 0.5f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
            }

            foreach (var p in filtered)
                CreatePartCard(p);

            UpdateCombineHint();
        }

        // 티어별 색상
        static readonly Color[] TierColors = {
            Color.white,
            new Color(0.65f, 0.65f, 0.70f),    // T1 회색
            new Color(0.30f, 0.85f, 0.80f),    // T2 청록
            new Color(0.65f, 0.55f, 0.95f),    // T3 보라
            new Color(0.96f, 0.62f, 0.04f),    // T4 주황
            new Color(0.96f, 0.27f, 0.37f)     // T5 빨강
        };
        static readonly Dictionary<string, string> SlotIcons = new()
        {
            ["head"] = "H", ["body"] = "B", ["arms"] = "A",
            ["legs"] = "L", ["core"] = "C"
        };

        void CreatePartCard(Part p)
        {
            var cardGO = new GameObject($"Card_{p.id}");
            cardGO.transform.SetParent(listContent, false);
            var img = cardGO.AddComponent<Image>();
            img.sprite = UITheme.GetRoundedSprite();
            img.type   = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            bool isSel = selected.Contains(p.id);
            img.color = isSel
                ? new Color(0.18f, 0.14f, 0.06f)
                : (p.equipped ? new Color(0.06f, 0.16f, 0.10f) : UITheme.SurfaceRaised);

            // 선택/장착 시 외곽선 효과
            if (isSel || p.equipped)
            {
                var outline = cardGO.AddComponent<Outline>();
                outline.effectColor = isSel ? UITheme.Gold : UITheme.Accent;
                outline.effectDistance = new Vector2(2, -2);
            }

            var rt = cardGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 80);

            // 좌측: 슬롯 아이콘 (원형)
            var iconCell = new GameObject("Icon");
            iconCell.transform.SetParent(cardGO.transform, false);
            var iconImg = iconCell.AddComponent<Image>();
            iconImg.sprite = UITheme.GetCircleSprite();
            iconImg.color = TierColors[Mathf.Clamp(p.tier, 1, 5)];
            var iconRt = iconCell.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f); iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(48, 48);
            iconRt.anchoredPosition = new Vector2(36, 0);

            var iconLabel = new GameObject("IconLabel");
            iconLabel.transform.SetParent(iconCell.transform, false);
            var iconTmp = iconLabel.AddComponent<TextMeshProUGUI>();
            iconTmp.text = SlotIcons.TryGetValue(p.slot, out var ic) ? ic : "?";
            iconTmp.fontSize = 22;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color = Color.black;
            iconTmp.raycastTarget = false;
            var ilRt = iconTmp.GetComponent<RectTransform>();
            ilRt.anchorMin = Vector2.zero; ilRt.anchorMax = Vector2.one;
            ilRt.offsetMin = ilRt.offsetMax = Vector2.zero;

            // 중앙: 슬롯 + 티어 + 보너스
            var info = new GameObject("Info");
            info.transform.SetParent(cardGO.transform, false);
            var infoTmp = info.AddComponent<TextMeshProUGUI>();
            string stars = new string('*', p.tier);
            string tierColor = ColorUtility.ToHtmlStringRGB(TierColors[Mathf.Clamp(p.tier, 1, 5)]);
            string bonus = "";
            if (p.stat_bonuses != null)
            {
                var b = p.stat_bonuses;
                var bs = new List<string>();
                if (b.atk > 0) bs.Add($"ATK+{b.atk}");
                if (b.def > 0) bs.Add($"DEF+{b.def}");
                if (b.hp  > 0) bs.Add($"HP+{b.hp}");
                if (b.abs > 0) bs.Add($"ABS+{b.abs}");
                if (b.prd > 0) bs.Add($"PRD+{b.prd}");
                if (b.spd > 0) bs.Add($"SPD+{b.spd}");
                if (b.rng > 0) bs.Add($"RNG+{b.rng}");
                if (b.ter > 0) bs.Add($"TER+{b.ter}");
                bonus = string.Join(" / ", bs);
            }
            string passives = (p.passives != null && p.passives.Count > 0)
                ? $"\n<color=#a78bfa>* {string.Join(", ", p.passives)}</color>"
                : "";
            infoTmp.text = $"<b>{SlotLabels[p.slot]}</b>  <color=#{tierColor}><b>T{p.tier} {stars}</b></color>\n<color=#00ff88>{bonus}</color>{passives}";
            infoTmp.fontSize = UITheme.FontCaption;
            infoTmp.color = UITheme.TextPrimary;
            infoTmp.richText = true;
            infoTmp.raycastTarget = false;
            var infoRt = info.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0, 0); infoRt.anchorMax = new Vector2(1, 1);
            infoRt.offsetMin = new Vector2(72, 8); infoRt.offsetMax = new Vector2(-92, -8);

            // 카드 클릭으로 선택 토글
            var cardBtn = cardGO.AddComponent<Button>();
            cardBtn.targetGraphic = img;
            cardBtn.onClick.AddListener(() => ToggleSelect(p.id));

            // 우측: 장착/해제 버튼 (둥근)
            var actionGO = new GameObject("Action");
            actionGO.transform.SetParent(cardGO.transform, false);
            var actionImg = actionGO.AddComponent<Image>();
            actionImg.sprite = UITheme.GetRoundedSprite();
            actionImg.type   = Image.Type.Sliced;
            actionImg.pixelsPerUnitMultiplier = 1f;
            actionImg.color = p.equipped
                ? new Color(0.35f, 0.10f, 0.12f)
                : new Color(0.05f, 0.22f, 0.12f);
            var actionRt = actionGO.GetComponent<RectTransform>();
            actionRt.anchorMin = new Vector2(1, 0.5f); actionRt.anchorMax = new Vector2(1, 0.5f);
            actionRt.pivot = new Vector2(1, 0.5f);
            actionRt.sizeDelta = new Vector2(72, 40);
            actionRt.anchoredPosition = new Vector2(-12, 0);
            var actionBtn = actionGO.AddComponent<Button>();
            actionBtn.targetGraphic = actionImg;
            actionBtn.onClick.AddListener(() => OnEquipToggle(p));

            var actionLabel = new GameObject("Label");
            actionLabel.transform.SetParent(actionGO.transform, false);
            var labelTmp = actionLabel.AddComponent<TextMeshProUGUI>();
            labelTmp.text = p.equipped ? "Unequip" : "Equip";
            labelTmp.fontSize = 13;
            labelTmp.color = p.equipped ? UITheme.Danger : UITheme.Accent;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.raycastTarget = false;
            var labelRt = actionLabel.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        }

        void ToggleSelect(string partId)
        {
            if (selected.Contains(partId)) selected.Remove(partId);
            else if (selected.Count < 3) selected.Add(partId);
            Render();
        }

        void UpdateCombineHint()
        {
            if (combineHintText == null || combineBtn == null) return;

            var selParts = myParts.Where(p => selected.Contains(p.id)).ToList();
            var tiers = selParts.Select(p => p.tier).Distinct().ToList();
            var slots = selParts.Select(p => p.slot).Distinct().ToList();
            int? tier = (tiers.Count == 1) ? tiers[0] : (int?)null;
            bool valid = selected.Count == 3 && tiers.Count == 1 && slots.Count == 1 && tier < 5;

            if (valid && tier.HasValue)
            {
                int rate = CombineRates.TryGetValue(tier.Value, out var r) ? r : 50;
                combineHintText.text = $"T{tier} -> T{tier + 1} combine, {rate}% success, fail returns T{Mathf.Max(1, tier.Value - 1)} salvage";
                combineBtnText.text = busy ? "Combining..." : $"Combine ({rate}%)";
                combineBtn.gameObject.SetActive(true);
                combineBtn.interactable = !busy;
            }
            else
            {
                combineHintText.text = $"Select 3 parts to combine to higher tier ({selected.Count}/3)";
                combineBtn.gameObject.SetActive(selected.Count == 3);
                if (selected.Count == 3)
                {
                    combineBtnText.text = "Need 3 of same slot & tier";
                    combineBtn.interactable = false;
                }
            }
        }

        void OnEquipToggle(Part p)
        {
            if (busy) return;
            busy = true;
            var userId = GameManager.Instance.UserId;
            System.Action<string> cb = _ => { busy = false; FetchParts(); };
            if (p.equipped) ApiManager.Instance.UnequipPart(userId, p.id, cb);
            else            ApiManager.Instance.EquipPart  (userId, p.id, cb);
        }

        void OnCombine()
        {
            if (busy || selected.Count != 3) return;
            busy = true;
            var userId = GameManager.Instance.UserId;
            ApiManager.Instance.CombineParts(userId, selected.ToArray(), json =>
            {
                var resp = JsonUtility.FromJson<CombineResponse>(json);
                ShowResult(resp);
                selected.Clear();
                busy = false;
                FetchParts();
            });
        }

        void ShowResult(CombineResponse resp)
        {
            if (resultBanner == null || resultBannerText == null) return;
            resultBanner.SetActive(true);
            bool success = resp != null && resp.result == "success";
            resultBannerText.text = !string.IsNullOrEmpty(resp?.message)
                ? resp.message
                : (success
                    ? $"Combine success! T{resp?.part?.tier} part obtained"
                    : $"Combine failed - T{resp?.part?.tier} salvage returned");
            var img = resultBanner.GetComponent<Image>();
            if (img != null) img.color = success
                ? new Color(0.1f, 0.23f, 0.16f)
                : new Color(0.23f, 0.16f, 0.1f);
        }
    }
}
