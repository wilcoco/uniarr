using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using TMPro;
using GuardianAR;

/// <summary>
/// AR 타워 배치 picker UI를 코드로 일괄 생성 + ARFixedGuardianPlacer에 자동 연결
///
/// 메뉴: Guardian AR/Setup AR Tower Picker UI
///
/// 생성하는 것:
///   - Canvas "ARTowerPickerCanvas" (Screen Space - Overlay, sortOrder 100)
///   - ClassPickerPanel
///     ├ Title
///     ├ ClassListContainer (Grid 5×3)
///     │   └ 13 × ClassItemPrefab (런타임 인스턴스화용)
///     ├ TierSlider (1-5)
///     ├ TierLabel / SelectedClassLabel / CostLabel / EnergyLabel
///     └ ConfirmBtn / CancelBtn
///   - ClassItemPrefab — 실제 prefab은 안 만들고 비활성 자식으로 두고 Inspector에 자기 자신 참조
/// </summary>
public class SetupARTowerPickerUI : Editor
{
    [MenuItem("Guardian AR/Setup AR Tower Picker UI")]
    public static void Setup()
    {
        var placer = Object.FindFirstObjectByType<ARFixedGuardianPlacer>(FindObjectsInactive.Include);
        bool created = false;
        if (placer == null)
        {
            // 자동 생성 — ARModeRoot 또는 AR Session Origin을 부모로 사용
            var arRoot = GameObject.Find("ARModeRoot");
            Transform parent = arRoot != null ? arRoot.transform : null;
            if (parent == null)
            {
                var origin = Object.FindFirstObjectByType<ARSessionOrigin>(FindObjectsInactive.Include);
                if (origin != null) parent = origin.transform.parent ?? origin.transform;
            }
            if (parent == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "ARModeRoot / ARSessionOrigin을 씬에서 찾을 수 없습니다.\n먼저 Guardian AR > Setup Scene을 실행하세요.", "OK");
                return;
            }

            var placerGO = new GameObject("ARFixedGuardianPlacer");
            placerGO.transform.SetParent(parent, false);
            placer = placerGO.AddComponent<ARFixedGuardianPlacer>();

            // raycastManager / planeManager 자동 연결
            var rcm = Object.FindFirstObjectByType<ARRaycastManager>(FindObjectsInactive.Include);
            var pm  = Object.FindFirstObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
            if (rcm != null) SetField(placer, "raycastManager", rcm);
            if (pm  != null) SetField(placer, "planeManager", pm);

            created = true;
            Debug.Log("[Setup AR Picker] ARFixedGuardianPlacer 자동 생성됨 (부모: " + parent.name + ")");
        }

        if (!EditorUtility.DisplayDialog("Setup AR Tower Picker UI",
            (created ? "ARFixedGuardianPlacer를 새로 생성했습니다.\n\n" : "") +
            "AR 타워 배치 UI를 생성합니다.\n기존 ARTowerPickerCanvas가 있으면 삭제됩니다.",
            "Create", "Cancel")) return;

        // 기존 Canvas 정리
        var existing = GameObject.Find("ARTowerPickerCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        // ─── Canvas ─────────────────────────────────────────────────
        var canvasGO = new GameObject("ARTowerPickerCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
        canvasGO.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ─── Panel ─────────────────────────────────────────────────
        var panel = CreateUI("ClassPickerPanel", canvasGO.transform);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0);
        panelRT.anchorMax = new Vector2(0.5f, 0);
        panelRT.pivot = new Vector2(0.5f, 0);
        panelRT.sizeDelta = new Vector2(900, 1100);
        panelRT.anchoredPosition = new Vector2(0, 80);

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

        var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(20, 20, 20, 20);
        panelLayout.spacing = 12;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childControlWidth = true;

        // 제목 — 기본 TMP 폰트(LiberationSans)에 한글/이모지 글리프 없음 → ASCII
        AddText(panel.transform, "Title", "TOWER SELECT", 36, FontStyles.Bold, TextAlignmentOptions.Center, 60);

        // ─── Class List Container (Grid 5×3) ───────────────────────
        var listGO = CreateUI("ClassListContainer", panel.transform);
        var listLE = listGO.AddComponent<LayoutElement>();
        listLE.preferredHeight = 360;
        listLE.flexibleHeight = 0;
        var grid = listGO.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160, 110);
        grid.spacing = new Vector2(8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.MiddleCenter;

        // ─── Class Item Prefab (씬 안에 비활성 템플릿) ─────────────
        var prefabHolder = CreateUI("ClassItemPrefabTemplate", canvasGO.transform);
        prefabHolder.SetActive(false);
        var prefabRT = prefabHolder.GetComponent<RectTransform>();
        prefabRT.sizeDelta = new Vector2(160, 110);

        var prefabImg = prefabHolder.AddComponent<Image>();
        prefabImg.color = new Color(0.65f, 0.65f, 0.70f, 1f);
        var prefabBtn = prefabHolder.AddComponent<Button>();
        var btnColors = prefabBtn.colors;
        btnColors.highlightedColor = new Color(1, 1, 1, 1);
        btnColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1);
        prefabBtn.colors = btnColors;

        var prefabLabel = AddText(prefabHolder.transform, "Label", "Class", 22,
            FontStyles.Bold, TextAlignmentOptions.Center, 0);
        var labelRT = prefabLabel.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
        prefabLabel.color = new Color(0, 0, 0, 0.95f);

        // ─── Territory Select Panel (영역 선택 — 클래스 picker 이전 단계) ───
        var terrPanel = CreateUI("TerritorySelectPanel", canvasGO.transform);
        var terrRT = terrPanel.GetComponent<RectTransform>();
        terrRT.anchorMin = new Vector2(0.5f, 0.5f); terrRT.anchorMax = new Vector2(0.5f, 0.5f);
        terrRT.pivot = new Vector2(0.5f, 0.5f);
        terrRT.sizeDelta = new Vector2(800, 900);
        terrRT.anchoredPosition = Vector2.zero;
        var terrImg = terrPanel.AddComponent<Image>();
        terrImg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        var terrLayout = terrPanel.AddComponent<VerticalLayoutGroup>();
        terrLayout.padding = new RectOffset(20, 20, 20, 20);
        terrLayout.spacing = 10;
        terrLayout.childForceExpandWidth = true;
        AddText(terrPanel.transform, "Title", "SELECT TERRITORY", 36,
            FontStyles.Bold, TextAlignmentOptions.Center, 60);

        var terrList = CreateUI("TerritoryListContainer", terrPanel.transform);
        var terrListLE = terrList.AddComponent<LayoutElement>(); terrListLE.flexibleHeight = 1;
        var terrListLayout = terrList.AddComponent<VerticalLayoutGroup>();
        terrListLayout.spacing = 6; terrListLayout.childForceExpandWidth = true;

        // territoryItemPrefab 템플릿
        var terrItemTpl = CreateUI("TerritoryItemPrefabTemplate", canvasGO.transform);
        terrItemTpl.SetActive(false);
        var terrItemRT = terrItemTpl.GetComponent<RectTransform>();
        terrItemRT.sizeDelta = new Vector2(0, 100);
        var terrItemImg = terrItemTpl.AddComponent<Image>();
        terrItemImg.color = new Color(0.15f, 0.18f, 0.25f);
        var terrItemBtn = terrItemTpl.AddComponent<Button>();
        var terrItemLE = terrItemTpl.AddComponent<LayoutElement>();
        terrItemLE.preferredHeight = 100;
        var terrItemLabel = AddText(terrItemTpl.transform, "Label", "Zone", 28,
            FontStyles.Bold, TextAlignmentOptions.Center, 0);
        var terrItemLabelRT = terrItemLabel.GetComponent<RectTransform>();
        terrItemLabelRT.anchorMin = Vector2.zero; terrItemLabelRT.anchorMax = Vector2.one;
        terrItemLabelRT.offsetMin = Vector2.zero; terrItemLabelRT.offsetMax = Vector2.zero;

        terrPanel.SetActive(false);

        // ─── Scan Hint Panel (바닥 스캔 안내) ─────────────────────────
        var scanPanel = CreateUI("ScanHintPanel", canvasGO.transform);
        var scanRT = scanPanel.GetComponent<RectTransform>();
        scanRT.anchorMin = new Vector2(0.5f, 0); scanRT.anchorMax = new Vector2(0.5f, 0);
        scanRT.pivot = new Vector2(0.5f, 0);
        scanRT.sizeDelta = new Vector2(700, 120);
        scanRT.anchoredPosition = new Vector2(0, 200);
        var scanImg = scanPanel.AddComponent<Image>();
        scanImg.color = new Color(0, 0, 0, 0.75f);
        AddText(scanPanel.transform, "Hint", "Scan floor and tap to place", 28,
            FontStyles.Bold, TextAlignmentOptions.Center, 0)
            .GetComponent<RectTransform>().sizeDelta = new Vector2(680, 100);
        scanPanel.SetActive(false);

        // ─── Place Tower 트리거 버튼 (우측 하단) ─────────────────────
        var triggerBtn = CreateUI("PlaceTowerTriggerBtn", canvasGO.transform);
        var triggerRT = triggerBtn.GetComponent<RectTransform>();
        triggerRT.anchorMin = new Vector2(1, 0); triggerRT.anchorMax = new Vector2(1, 0);
        triggerRT.pivot = new Vector2(1, 0);
        triggerRT.sizeDelta = new Vector2(220, 90);
        triggerRT.anchoredPosition = new Vector2(-30, 30);
        var trigImg = triggerBtn.AddComponent<Image>();
        trigImg.color = new Color(1f, 0.55f, 0.2f);
        var trigBtn = triggerBtn.AddComponent<Button>();
        AddText(triggerBtn.transform, "Label", "PLACE TOWER", 26,
            FontStyles.Bold, TextAlignmentOptions.Center, 0)
            .GetComponent<RectTransform>().sizeDelta = new Vector2(200, 70);
        // 런타임 헬퍼 컴포넌트가 onClick을 자동 wiring (PersistentListener는 직렬화 깨짐)
        triggerBtn.AddComponent<ARPlaceTriggerButton>();

        // ─── Tier Slider 영역 ─────────────────────────────────────
        var tierBox = CreateUI("TierBox", panel.transform);
        var tierBoxLE = tierBox.AddComponent<LayoutElement>();
        tierBoxLE.preferredHeight = 100;
        var tierLayout = tierBox.AddComponent<VerticalLayoutGroup>();
        tierLayout.spacing = 4; tierLayout.childForceExpandWidth = true;
        var tierLabelTMP = AddText(tierBox.transform, "TierLabel", "Lv1", 32,
            FontStyles.Bold, TextAlignmentOptions.Center, 40);

        var sliderGO = CreateUI("TierSlider", tierBox.transform);
        var sliderLE = sliderGO.AddComponent<LayoutElement>(); sliderLE.preferredHeight = 50;
        var slider = BuildSlider(sliderGO);

        // ─── 정보 라벨들 ──────────────────────────────────────────
        var selClassTMP = AddText(panel.transform, "SelectedClassLabel",
            "Generic - balanced starter", 24, FontStyles.Normal, TextAlignmentOptions.Center, 40);
        selClassTMP.color = new Color(1f, 0.85f, 0.4f);

        var costTMP = AddText(panel.transform, "CostLabel",
            "Cost 30 energy", 28, FontStyles.Bold, TextAlignmentOptions.Center, 50);
        costTMP.color = new Color(1f, 0.55f, 0.2f);

        var energyTMP = AddText(panel.transform, "EnergyLabel",
            "Have 0", 22, FontStyles.Normal, TextAlignmentOptions.Center, 36);
        energyTMP.color = new Color(0.7f, 0.7f, 0.7f);

        // ─── 버튼들 ──────────────────────────────────────────────
        var btnRow = CreateUI("BtnRow", panel.transform);
        var btnRowLE = btnRow.AddComponent<LayoutElement>(); btnRowLE.preferredHeight = 90;
        var btnLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 12; btnLayout.childForceExpandWidth = true; btnLayout.childForceExpandHeight = true;

        var cancelBtn = BuildButton(btnRow.transform, "CancelBtn", "CANCEL",
            new Color(0.25f, 0.25f, 0.25f));
        var confirmBtn = BuildButton(btnRow.transform, "ConfirmBtn", "CONFIRM",
            new Color(1f, 0.55f, 0.2f));

        // ─── ARFixedGuardianPlacer에 reflection으로 연결 ───────────
        SetField(placer, "classPickerPanel", panel);
        SetField(placer, "classListContainer", listGO.transform);
        SetField(placer, "classItemPrefab", prefabHolder);
        SetField(placer, "tierSlider", slider);
        SetField(placer, "tierLabel", tierLabelTMP);
        SetField(placer, "selectedClassLabel", selClassTMP);
        SetField(placer, "costLabel", costTMP);
        SetField(placer, "energyLabel", energyTMP);
        SetField(placer, "classConfirmBtn", confirmBtn);
        SetField(placer, "classCancelBtn", cancelBtn);
        SetField(placer, "territorySelectPanel", terrPanel);
        SetField(placer, "territoryListContainer", terrList.transform);
        SetField(placer, "territoryItemPrefab", terrItemTpl);
        SetField(placer, "scanHintPanel", scanPanel);

        // 시작 시 비활성
        panel.SetActive(false);

        EditorUtility.SetDirty(placer);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Done",
            "AR 타워 picker UI 생성 완료!\n\n" +
            (created ? "• ARFixedGuardianPlacer 자동 생성\n" : "") +
            "• 🏰 타워 배치 트리거 버튼 (우측 하단)\n" +
            "• 영역 선택 패널 (TerritorySelectPanel)\n" +
            "• 13개 클래스 그리드 + Tier 1-5 슬라이더\n" +
            "• 바닥 스캔 안내 패널\n" +
            "• ARFixedGuardianPlacer Inspector에 모두 자동 연결\n\n" +
            "AR 모드 진입 → 우측 하단 '🏰 타워 배치' → 영역 → 클래스/티어 → 바닥 탭",
            "OK");

        Selection.activeGameObject = canvasGO;
    }

    static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI AddText(Transform parent, string name, string text,
        float size, FontStyles style, TextAlignmentOptions align, float preferredHeight)
    {
        var go = CreateUI(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = Color.white;
        if (preferredHeight > 0)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
        }
        return tmp;
    }

    static Button BuildButton(Transform parent, string name, string label, Color bg)
    {
        var go = CreateUI(name, parent);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        var tmp = AddText(go.transform, "Label", label, 30,
            FontStyles.Bold, TextAlignmentOptions.Center, 0);
        var rt = tmp.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return btn;
    }

    static Slider BuildSlider(GameObject sliderGO)
    {
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 1; slider.maxValue = 5; slider.wholeNumbers = true; slider.value = 1;

        // Background
        var bgGO = CreateUI("Background", sliderGO.transform);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.4f); bgRT.anchorMax = new Vector2(1, 0.6f);
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.20f);

        // Fill Area + Fill
        var fillArea = CreateUI("FillArea", sliderGO.transform);
        var fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.4f); fillAreaRT.anchorMax = new Vector2(1, 0.6f);
        fillAreaRT.offsetMin = new Vector2(10, 0); fillAreaRT.offsetMax = new Vector2(-10, 0);

        var fill = CreateUI("Fill", fillArea.transform);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.55f, 0.2f);
        slider.fillRect = fillRT;

        // Handle Slide Area + Handle
        var slideArea = CreateUI("HandleSlideArea", sliderGO.transform);
        var slideRT = slideArea.GetComponent<RectTransform>();
        slideRT.anchorMin = new Vector2(0, 0); slideRT.anchorMax = new Vector2(1, 1);
        slideRT.offsetMin = new Vector2(10, 0); slideRT.offsetMax = new Vector2(-10, 0);

        var handle = CreateUI("Handle", slideArea.transform);
        var handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(40, 50);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;

        return slider;
    }

    static void SetField(object target, string name, object value)
    {
        var type = target.GetType();
        while (type != null)
        {
            var f = type.GetField(name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (f != null) { f.SetValue(target, value); return; }
            type = type.BaseType;
        }
        Debug.LogWarning($"[SetupARTowerPickerUI] Field not found: {name}");
    }
}
