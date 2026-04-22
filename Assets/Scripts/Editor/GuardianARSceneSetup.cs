using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using GuardianAR;

/// <summary>
/// Guardian AR 씬 자동 구성 에디터 스크립트
/// Unity 메뉴: Guardian AR → Setup Scene
/// </summary>
public class GuardianARSceneSetup : Editor
{
    [MenuItem("Guardian AR/Setup Scene")]
    public static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("씬 구성",
            "현재 씬에 Guardian AR 전체 구조를 생성합니다.\n기존 동명 오브젝트는 삭제됩니다.\n계속하시겠습니까?",
            "생성", "취소")) return;

        // 기존 루트 오브젝트 정리
        foreach (var name in new[] {
            "Bootstrap", "Managers", "ModeController",
            "MapModeRoot", "ARModeRoot", "HUD" })
        {
            var existing = GameObject.Find(name);
            if (existing != null) DestroyImmediate(existing);
        }

        // ── 1. Managers ──────────────────────────────────────────────
        var managers = CreateEmpty("Managers");
        var apiManagerGO   = CreateEmpty("ApiManager",   managers); apiManagerGO.AddComponent<ApiManager>();
        var locationMgrGO  = CreateEmpty("LocationManager", managers); locationMgrGO.AddComponent<LocationManager>();
        var gameMgrGO      = CreateEmpty("GameManager",  managers); gameMgrGO.AddComponent<GameManager>();

        // ── 2. Bootstrap ─────────────────────────────────────────────
        var bootstrap = CreateEmpty("Bootstrap");
        var bs = bootstrap.AddComponent<AppBootstrap>();
        SetPrivateField(bs, "apiManagerPrefab",      apiManagerGO.GetComponent<ApiManager>());
        SetPrivateField(bs, "locationManagerPrefab", locationMgrGO.GetComponent<LocationManager>());
        SetPrivateField(bs, "gameManagerPrefab",     gameMgrGO.GetComponent<GameManager>());

        // ── 3. MapModeRoot ────────────────────────────────────────────
        var mapRoot = CreateEmpty("MapModeRoot");

        // 맵 Canvas
        var mapCanvas = CreateUICanvas("MapCanvas", mapRoot);

        // 타일 컨테이너 (타일 이미지들이 여기에 생성됨)
        var tileContainerGO = new GameObject("TileContainer");
        tileContainerGO.transform.SetParent(mapCanvas.transform, false);
        var tileContainerRt = tileContainerGO.AddComponent<RectTransform>();
        StretchFill(tileContainerRt);

        // 마커 컨테이너 (플레이어/수호신 마커)
        var markerContainerGO = new GameObject("MarkerContainer");
        markerContainerGO.transform.SetParent(mapCanvas.transform, false);
        var markerContainerRt = markerContainerGO.AddComponent<RectTransform>();
        StretchFill(markerContainerRt);

        // 오버레이 컨테이너 (UI 버튼)
        var overlayGO = new GameObject("Overlay");
        overlayGO.transform.SetParent(mapCanvas.transform, false);
        var overlayRt = overlayGO.AddComponent<RectTransform>();
        StretchFill(overlayRt);

        // AR 모드 전환 버튼
        var arModeBtn = CreateButton("ARModeButton", overlayRt, "📷 AR 모드");
        PlaceBottom(arModeBtn.GetComponent<RectTransform>(), 150f, 50f, 0f);
        arModeBtn.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.6f, 1f);

        // 타일 프리팹 (비활성 RawImage GO)
        var tilePrefabGO = new GameObject("TilePrefab");
        tilePrefabGO.transform.SetParent(mapRoot.transform, false);
        tilePrefabGO.SetActive(false);
        var tileRawImage = tilePrefabGO.AddComponent<UnityEngine.UI.RawImage>();

        // 맵 매니저 GO
        var mapManagersGO = CreateEmpty("MapManagers", mapRoot);
        var tileManager   = mapManagersGO.AddComponent<MapTileManager>();
        var mapController = mapManagersGO.AddComponent<MapController>();
        var mapInput      = mapManagersGO.AddComponent<MapInputHandler>();

        SetPrivateField(tileManager, "tileContainer", tileContainerRt);
        SetPrivateField(tileManager, "tilePrefab",    tileRawImage);

        SetPrivateField(mapController, "tileManager",       tileManager);
        SetPrivateField(mapController, "markerContainer",   markerContainerRt);
        SetPrivateField(mapController, "overlayContainer",  overlayRt);
        SetPrivateField(mapController, "arModeButton",      arModeBtn.GetComponent<UnityEngine.UI.Button>());

        SetPrivateField(mapInput, "tileManager",      tileManager);
        SetPrivateField(mapInput, "tileContainer",    tileContainerRt);
        SetPrivateField(mapInput, "markerContainer",  markerContainerRt);

        // ── 4. ARModeRoot ─────────────────────────────────────────────
        var arRoot = CreateEmpty("ARModeRoot");

        // AR Session
        var arSessionGO = CreateEmpty("AR Session", arRoot);
        var arSession   = arSessionGO.AddComponent<ARSession>();

        // AR Session Origin + Camera
        var arOriginGO = CreateEmpty("AR Session Origin", arRoot);
        var arOrigin   = arOriginGO.AddComponent<ARSessionOrigin>();
        arOriginGO.AddComponent<ARPlaneManager>();
        arOriginGO.AddComponent<ARRaycastManager>();

        var arCamGO  = CreateEmpty("AR Camera", arOriginGO);
        var arCam    = arCamGO.AddComponent<Camera>();
        arCam.clearFlags = CameraClearFlags.Color;
        arCam.tag = "MainCamera";
        arCamGO.AddComponent<ARCameraManager>();
        arCamGO.AddComponent<ARCameraBackground>();
        arOrigin.camera = arCam;

        // ARModeController
        var arCtrlGO = CreateEmpty("ARModeController", arRoot);
        var arCtrl   = arCtrlGO.AddComponent<ARModeController>();
        SetPrivateField(arCtrl, "arSession",        arSession);
        SetPrivateField(arCtrl, "arCameraManager",  arCamGO.GetComponent<ARCameraManager>());
        SetPrivateField(arCtrl, "arPlaneManager",   arOriginGO.GetComponent<ARPlaneManager>());

        // ARModeController UI 버튼
        var arUICanvas  = CreateUICanvas("AR_UI_Canvas", arCtrlGO.transform);
        var backBtn     = CreateButton("BackToMapButton", arUICanvas.transform, "◀ 지도");
        var placeBtn    = CreateButton("PlaceGuardianButton", arUICanvas.transform, "🛡 고정 수호신 배치");
        PlaceTopLeft(backBtn.GetComponent<RectTransform>(),  120f, 44f, 10f, -10f);
        PlaceTopRight(placeBtn.GetComponent<RectTransform>(), 180f, 44f, -10f, -10f);
        SetPrivateField(arCtrl, "backToMapButton",     backBtn.GetComponent<Button>());
        SetPrivateField(arCtrl, "placeGuardianButton", placeBtn.GetComponent<Button>());

        // ARBattleManager
        var battleMgrGO    = CreateEmpty("ARBattleManager", arRoot);
        var battleMgr      = battleMgrGO.AddComponent<ARBattleManager>();
        var attackEffectGO = CreateEmpty("ARAttackEffect",  battleMgrGO.transform);
        var attackEffect   = attackEffectGO.AddComponent<ARAttackEffect>();
        // LineRenderer (빔)
        var lr = attackEffectGO.AddComponent<LineRenderer>();
        lr.startWidth = 0.02f; lr.endWidth = 0.02f;
        lr.positionCount = 2;
        lr.enabled = false;
        SetPrivateField(attackEffect, "beamLine", lr);
        SetPrivateField(battleMgr,   "attackEffect", attackEffect);

        // Battle UI Canvas (World Space)
        var battleCanvas2 = CreateUICanvas("BattleCanvas", battleMgrGO.transform, RenderMode.WorldSpace);
        battleCanvas2.transform.localPosition = new Vector3(0, 1.5f, 2f);
        battleCanvas2.transform.localScale    = Vector3.one * 0.003f;
        var battleRt = battleCanvas2.GetComponent<RectTransform>();
        battleRt.sizeDelta = new Vector2(400, 300);

        var battleStatusLabel  = CreateTMPLabel("BattleStatusText", battleCanvas2.transform, "", 40, TextAlignmentOptions.Center);
        PlaceCenter(battleStatusLabel.GetComponent<RectTransform>(), 300f, 60f, 0f, 60f);

        var ultPromptLabel = CreateTMPLabel("UltimatePrompt", battleCanvas2.transform, "⚡ 궁극기 사용 가능!", 22, TextAlignmentOptions.Center);
        PlaceCenter(ultPromptLabel.GetComponent<RectTransform>(), 250f, 40f, 0f, -80f);
        ultPromptLabel.color = Color.yellow;

        var ultBtn  = CreateButton("UltimateButton",  battleCanvas2.transform, "⚡ 궁극기");
        var skipBtn = CreateButton("SkipButton",       battleCanvas2.transform, "건너뛰기");
        PlaceCenter(ultBtn.GetComponent<RectTransform>(),  130f, 44f, -75f, -130f);
        PlaceCenter(skipBtn.GetComponent<RectTransform>(), 100f, 44f,  75f, -130f);

        SetPrivateField(battleMgr, "battleCanvas",       battleCanvas2);
        SetPrivateField(battleMgr, "battleStatusText",   battleStatusLabel);
        SetPrivateField(battleMgr, "ultimatePromptText", ultPromptLabel);
        SetPrivateField(battleMgr, "ultButton",          ultBtn.GetComponent<Button>());
        SetPrivateField(battleMgr, "skipButton",         skipBtn.GetComponent<Button>());

        // Battle Result Panel (Screen Space)
        var resultCanvas = CreateUICanvas("ResultCanvas", battleMgrGO.transform);
        var resultPanel  = CreatePanel("ResultPanel", resultCanvas.transform, new Color(0f, 0f, 0f, 0.9f));
        StretchFill(resultPanel.GetComponent<RectTransform>(), 80f, 80f, -80f, -80f);
        var resultTitle  = CreateTMPLabel("ResultTitle",  resultPanel.transform, "결과",    48, TextAlignmentOptions.Center);
        var resultDetail = CreateTMPLabel("ResultDetail", resultPanel.transform, "",         22, TextAlignmentOptions.Center);
        var resultClose  = CreateButton("ResultCloseButton", resultPanel.transform, "확인");
        PlaceCenter(resultTitle.GetComponent<RectTransform>(),  300f, 60f,  0f,  60f);
        PlaceCenter(resultDetail.GetComponent<RectTransform>(), 280f, 80f,  0f,  -10f);
        PlaceCenter(resultClose.GetComponent<RectTransform>(),  140f, 44f,  0f, -80f);
        resultPanel.SetActive(false);

        SetPrivateField(battleMgr, "resultPanel",       resultPanel);
        SetPrivateField(battleMgr, "resultTitle",       resultTitle);
        SetPrivateField(battleMgr, "resultDetail",      resultDetail);
        SetPrivateField(battleMgr, "resultCloseButton", resultClose.GetComponent<Button>());

        // ARFixedGuardianPlacer
        var placerGO = CreateEmpty("ARFixedGuardianPlacer", arRoot);
        var placer   = placerGO.AddComponent<ARFixedGuardianPlacer>();
        SetPrivateField(placer, "raycastManager", arOriginGO.GetComponent<ARRaycastManager>());
        SetPrivateField(placer, "planeManager",   arOriginGO.GetComponent<ARPlaneManager>());
        BuildPlacerUI(placer, placerGO);

        // ── 5. HUD (항상 표시) ────────────────────────────────────────
        var hudRoot   = CreateUICanvas("HUD");
        var hud       = hudRoot.gameObject.AddComponent<HUD>();
        BuildHUDUI(hud, hudRoot.transform);

        // BattleModal (맵 모드용)
        var battleModalCanvas = CreateUICanvas("BattleModal");
        var battleModal = battleModalCanvas.gameObject.AddComponent<BattleModal>();
        BuildBattleModalUI(battleModal, battleModalCanvas.transform);
        battleModalCanvas.gameObject.SetActive(false);

        // ── 6. ModeController ─────────────────────────────────────────
        var modeCtrlGO = CreateEmpty("ModeController");
        var modeCtrl   = modeCtrlGO.AddComponent<ModeController>();
        SetPrivateField(modeCtrl, "mapModeRoot",     mapRoot);
        SetPrivateField(modeCtrl, "arModeRoot",      arRoot);
        SetPrivateField(modeCtrl, "arController",    arCtrl);
        SetPrivateField(modeCtrl, "hudRoot",         hudRoot.gameObject);
        SetPrivateField(modeCtrl, "battleModalRoot", battleModalCanvas.gameObject);

        // ── 씬 저장 ───────────────────────────────────────────────────
        EditorUtility.SetDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            .GetRootGameObjects()[0]);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("완료",
            "씬 구성이 완료되었습니다!\n\n남은 작업:\n" +
            "① Guardian AR → Settings 에서 서버 URL 확인\n" +
            "② AR 수호신 Prefab 연결 (ARModeController)\n" +
            "③ 마커 Prefab 연결 (MapController)\n" +
            "④ HP Bar / 데미지 숫자 Prefab 연결 (ARBattleManager)",
            "확인");

        Debug.Log("[GuardianAR] 씬 구성 완료!");
    }

    // ─────────────────────────────────────────────────────────────────
    // ARFixedGuardianPlacer UI 구성
    // ─────────────────────────────────────────────────────────────────
    static void BuildPlacerUI(ARFixedGuardianPlacer placer, GameObject parent)
    {
        var canvas = CreateUICanvas("PlacerCanvas", parent.transform);

        // 영역 선택 패널
        var terrPanel = CreatePanel("TerritorySelectPanel", canvas.transform, new Color(0,0,0,0.85f));
        StretchFill(terrPanel.GetComponent<RectTransform>(), 40f, 100f, -40f, -100f);
        var terrTitle = CreateTMPLabel("Title", terrPanel.transform, "배치할 영역 선택", 24, TextAlignmentOptions.Center);
        PlaceTop(terrTitle.GetComponent<RectTransform>(), 300f, 40f, -10f);
        var scrollView = CreateScrollView("TerritoryList", terrPanel.transform);
        StretchFill(scrollView.GetComponent<RectTransform>(), 10f, 55f, -10f, -10f);
        terrPanel.SetActive(false);

        // 스캔 안내 패널
        var scanPanel = CreatePanel("ScanHintPanel", canvas.transform, new Color(0,0,0,0.7f));
        PlaceBottom(scanPanel.GetComponent<RectTransform>(), 340f, 70f, -120f);
        CreateTMPLabel("ScanText", scanPanel.transform,
            "📱 카메라를 바닥으로 향해 스캔하세요\n바닥 감지 후 화면을 탭하여 위치를 선택합니다",
            18, TextAlignmentOptions.Center);
        scanPanel.SetActive(false);

        // 배치 설정 패널
        var setupPanel = CreatePanel("SetupPanel", canvas.transform, new Color(0,0,0,0.9f));
        StretchFill(setupPanel.GetComponent<RectTransform>(), 20f, 80f, -20f, -80f);
        BuildSetupPanelContent(setupPanel.transform, placer);
        setupPanel.SetActive(false);

        SetPrivateField(placer, "setupPanel",              setupPanel);
        SetPrivateField(placer, "scanHintPanel",           scanPanel);
        SetPrivateField(placer, "territorySelectPanel",    terrPanel);
        SetPrivateField(placer, "territoryListContainer",  scrollView.transform.Find("Viewport/Content"));
    }

    static void BuildSetupPanelContent(Transform parent, ARFixedGuardianPlacer placer)
    {
        CreateTMPLabel("Title", parent, "고정 수호신 설정", 24, TextAlignmentOptions.Center)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 140f);

        // 타입 버튼
        var defBtn  = CreateButton("DefenseTypeBtn",    parent, "🛡 방어형");
        var prodBtn = CreateButton("ProductionTypeBtn", parent, "⚙ 생산형");
        PlaceCenter(defBtn.GetComponent<RectTransform>(),  120f, 44f, -70f, 90f);
        PlaceCenter(prodBtn.GetComponent<RectTransform>(), 120f, 44f,  70f, 90f);

        // 스탯 슬라이더
        var atkLabel = CreateTMPLabel("AtkLabel", parent, "ATK: 0/0", 18, TextAlignmentOptions.Left);
        var defLabel = CreateTMPLabel("DefLabel", parent, "DEF: 0/0", 18, TextAlignmentOptions.Left);
        var hpLabel  = CreateTMPLabel("HpLabel",  parent, "HP: 0/0",  18, TextAlignmentOptions.Left);
        PlaceCenter(atkLabel.GetComponent<RectTransform>(), 200f, 24f, -50f,  30f);
        PlaceCenter(defLabel.GetComponent<RectTransform>(), 200f, 24f, -50f,  -10f);
        PlaceCenter(hpLabel.GetComponent<RectTransform>(),  200f, 24f, -50f,  -50f);

        var atkSlider = CreateSlider("AtkSlider", parent);
        var defSlider = CreateSlider("DefSlider", parent);
        var hpSlider  = CreateSlider("HpSlider",  parent);
        PlaceCenter(atkSlider.GetComponent<RectTransform>(), 260f, 24f, 30f,  30f);
        PlaceCenter(defSlider.GetComponent<RectTransform>(), 260f, 24f, 30f, -10f);
        PlaceCenter(hpSlider.GetComponent<RectTransform>(),  260f, 24f, 30f, -50f);

        var remainLabel = CreateTMPLabel("RemainingLabel", parent, "남은 분배량: 0", 16, TextAlignmentOptions.Center);
        PlaceCenter(remainLabel.GetComponent<RectTransform>(), 280f, 30f, 0f, -90f);

        // 확인/취소
        var confirmBtn = CreateButton("ConfirmButton", parent, "배치 확정");
        var cancelBtn  = CreateButton("CancelButton",  parent, "취소");
        PlaceCenter(confirmBtn.GetComponent<RectTransform>(), 130f, 44f, -75f, -135f);
        PlaceCenter(cancelBtn.GetComponent<RectTransform>(),  100f, 44f,  65f, -135f);
        confirmBtn.GetComponent<Image>().color = new Color(0f, 0.8f, 0.4f);

        SetPrivateField(placer, "defenseTypeBtn",      defBtn.GetComponent<Button>());
        SetPrivateField(placer, "productionTypeBtn",   prodBtn.GetComponent<Button>());
        SetPrivateField(placer, "atkSlider",           atkSlider.GetComponent<Slider>());
        SetPrivateField(placer, "defSlider",           defSlider.GetComponent<Slider>());
        SetPrivateField(placer, "hpSlider",            hpSlider.GetComponent<Slider>());
        SetPrivateField(placer, "atkLabel",            atkLabel);
        SetPrivateField(placer, "defLabel",            defLabel);
        SetPrivateField(placer, "hpLabel",             hpLabel);
        SetPrivateField(placer, "remainingStatsLabel", remainLabel);
        SetPrivateField(placer, "confirmBtn",          confirmBtn.GetComponent<Button>());
        SetPrivateField(placer, "cancelBtn",           cancelBtn.GetComponent<Button>());
    }

    // ─────────────────────────────────────────────────────────────────
    // HUD UI 구성
    // ─────────────────────────────────────────────────────────────────
    static void BuildHUDUI(HUD hud, Transform parent)
    {
        // 상단 바
        var topBar = CreatePanel("TopBar", parent, new Color(0,0,0,0.6f));
        PlaceTop(topBar.GetComponent<RectTransform>(), 0f, 60f, 0f, true);

        var nicknameLabel = CreateTMPLabel("NicknameText", topBar.transform, "닉네임", 18, TextAlignmentOptions.Left);
        PlaceLeft(nicknameLabel.GetComponent<RectTransform>(), 160f, 40f, 16f);

        var energyLabel = CreateTMPLabel("EnergyText", topBar.transform, "💎 100", 18, TextAlignmentOptions.Right);
        PlaceRight(energyLabel.GetComponent<RectTransform>(), 120f, 40f, -16f);

        var guardianTypeLabel = CreateTMPLabel("GuardianTypeText", topBar.transform, "", 20, TextAlignmentOptions.Center);
        PlaceCenter(guardianTypeLabel.GetComponent<RectTransform>(), 140f, 30f, 0f, 8f);

        var guardianStatsLabel = CreateTMPLabel("GuardianStatsText", topBar.transform, "", 12, TextAlignmentOptions.Center);
        PlaceCenter(guardianStatsLabel.GetComponent<RectTransform>(), 280f, 20f, 0f, -12f);
        guardianStatsLabel.fontSize = 12f;

        // 탐지 뱃지
        var detectBadge = CreatePanel("DetectBadge", parent, new Color(1f, 0.27f, 0.27f, 0.9f));
        PlaceTop(detectBadge.GetComponent<RectTransform>(), 80f, 36f, -16f, false, true);
        var detectCount = CreateTMPLabel("DetectCount", detectBadge.transform, "0", 18, TextAlignmentOptions.Center);
        StretchFill(detectCount.GetComponent<RectTransform>());
        detectBadge.SetActive(false);

        // 수호신 생성 패널
        var createPanel = CreatePanel("CreateGuardianPanel", parent, new Color(0,0,0,0.85f));
        StretchFill(createPanel.GetComponent<RectTransform>(), 60f, 120f, -60f, -120f);
        CreateTMPLabel("CreateTitle", createPanel.transform, "수호신을 선택하세요", 22, TextAlignmentOptions.Center)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80f);

        var animalBtn  = CreateButton("CreateAnimalBtn",   createPanel.transform, "🦁 동물형\nATK↑ SPD↑");
        var robotBtn   = CreateButton("CreateRobotBtn",    createPanel.transform, "🤖 로봇형\nATK↑ DEF↑");
        var aircraftBtn= CreateButton("CreateAircraftBtn", createPanel.transform, "✈ 비행체형\nRNG↑ TER↑");
        PlaceCenter(animalBtn.GetComponent<RectTransform>(),   140f, 80f, -110f, 0f);
        PlaceCenter(robotBtn.GetComponent<RectTransform>(),    140f, 80f,    0f, 0f);
        PlaceCenter(aircraftBtn.GetComponent<RectTransform>(), 140f, 80f,  110f, 0f);

        // 영역 확장 패널
        var expandPanel = CreatePanel("ExpandPanel", parent, new Color(0,0,0,0.9f));
        StretchFill(expandPanel.GetComponent<RectTransform>(), 40f, 80f, -40f, -80f);
        CreateTMPLabel("ExpandTitle", expandPanel.transform, "영역 확장", 22, TextAlignmentOptions.Center)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100f);
        var radiusSlider = CreateSlider("RadiusSlider", expandPanel.transform);
        PlaceCenter(radiusSlider.GetComponent<RectTransform>(), 280f, 30f, 0f, 20f);
        var sld = radiusSlider.GetComponent<Slider>();
        sld.minValue = 50; sld.maxValue = 500; sld.value = 50;
        var radiusLabel   = CreateTMPLabel("RadiusLabel",   expandPanel.transform, "50m", 20, TextAlignmentOptions.Center);
        PlaceCenter(radiusLabel.GetComponent<RectTransform>(), 100f, 30f, 0f, 60f);
        var confirmExpBtn = CreateButton("ConfirmExpandBtn", expandPanel.transform, "확장");
        var cancelExpBtn  = CreateButton("CancelExpandBtn",  expandPanel.transform, "취소");
        PlaceCenter(confirmExpBtn.GetComponent<RectTransform>(), 120f, 44f, -75f, -50f);
        PlaceCenter(cancelExpBtn.GetComponent<RectTransform>(),  100f, 44f,  65f, -50f);
        confirmExpBtn.GetComponent<Image>().color = new Color(0f, 0.8f, 0.4f);
        expandPanel.SetActive(false);

        // 영역 확장 열기 버튼 (하단)
        var openExpandBtn = CreateButton("OpenExpandButton", parent, "➕ 영역 확장");
        PlaceBottom(openExpandBtn.GetComponent<RectTransform>(), 150f, 44f, -20f);
        openExpandBtn.GetComponent<Image>().color = new Color(0f, 0.6f, 1f);

        SetPrivateField(hud, "energyText",        energyLabel);
        SetPrivateField(hud, "guardianTypeText",  guardianTypeLabel);
        SetPrivateField(hud, "guardianStatsText", guardianStatsLabel);
        SetPrivateField(hud, "nicknameText",      nicknameLabel);
        SetPrivateField(hud, "detectBadge",       detectBadge);
        SetPrivateField(hud, "detectCount",       detectCount);
        SetPrivateField(hud, "createGuardianPanel", createPanel);
        SetPrivateField(hud, "createAnimalBtn",   animalBtn.GetComponent<Button>());
        SetPrivateField(hud, "createRobotBtn",    robotBtn.GetComponent<Button>());
        SetPrivateField(hud, "createAircraftBtn", aircraftBtn.GetComponent<Button>());
        SetPrivateField(hud, "expandPanel",       expandPanel);
        SetPrivateField(hud, "radiusSlider",      sld);
        SetPrivateField(hud, "radiusLabel",       radiusLabel);
        SetPrivateField(hud, "confirmExpandBtn",  confirmExpBtn.GetComponent<Button>());
        SetPrivateField(hud, "cancelExpandBtn",   cancelExpBtn.GetComponent<Button>());
        SetPrivateField(hud, "openExpandBtn",     openExpandBtn.GetComponent<Button>());
    }

    // ─────────────────────────────────────────────────────────────────
    // BattleModal UI 구성
    // ─────────────────────────────────────────────────────────────────
    static void BuildBattleModalUI(BattleModal modal, Transform parent)
    {
        var bg = CreatePanel("BG", parent, new Color(0,0,0,0.5f));
        StretchFill(bg.GetComponent<RectTransform>());

        var card = CreatePanel("Card", bg.transform, new Color(0.08f, 0.08f, 0.1f, 0.97f));
        StretchFill(card.GetComponent<RectTransform>(), 30f, 120f, -30f, -120f);

        // Encounter 패널
        var encPanel = CreatePanel("EncounterPanel", card.transform, Color.clear);
        StretchFill(encPanel.GetComponent<RectTransform>());
        var encTitle = CreateTMPLabel("EncounterTitle", encPanel.transform, "조우!", 32, TextAlignmentOptions.Center);
        var encDesc  = CreateTMPLabel("EncounterDesc",  encPanel.transform, "",      20, TextAlignmentOptions.Center);
        PlaceCenter(encTitle.GetComponent<RectTransform>(), 300f, 50f, 0f,  60f);
        PlaceCenter(encDesc.GetComponent<RectTransform>(),  300f, 40f, 0f,  10f);

        var battleBtn   = CreateButton("BattleButton",   encPanel.transform, "⚔ 전투");
        var allianceBtn = CreateButton("AllianceButton", encPanel.transform, "🤝 동맹");
        var closeBtn    = CreateButton("CloseButton",    encPanel.transform, "✕");
        PlaceCenter(battleBtn.GetComponent<RectTransform>(),   120f, 44f, -75f, -60f);
        PlaceCenter(allianceBtn.GetComponent<RectTransform>(), 120f, 44f,  75f, -60f);
        PlaceCenter(closeBtn.GetComponent<RectTransform>(),    80f,  36f,  0f,  -110f);
        battleBtn.GetComponent<Image>().color   = new Color(0.8f, 0.2f, 0.2f);
        allianceBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f);

        // Animating 패널
        var animPanel = CreatePanel("AnimatingPanel", card.transform, Color.clear);
        StretchFill(animPanel.GetComponent<RectTransform>());
        var vs1 = CreateTMPLabel("VS1Text", animPanel.transform, "나",   26, TextAlignmentOptions.Center);
        var vs2 = CreateTMPLabel("VS2Text", animPanel.transform, "상대", 26, TextAlignmentOptions.Center);
        var pw  = CreateTMPLabel("PowerText", animPanel.transform, "전투 중...", 20, TextAlignmentOptions.Center);
        CreateTMPLabel("VSText", animPanel.transform, "VS", 36, TextAlignmentOptions.Center);
        PlaceCenter(vs1.GetComponent<RectTransform>(), 120f, 40f, -100f, 20f);
        PlaceCenter(vs2.GetComponent<RectTransform>(), 120f, 40f,  100f, 20f);
        PlaceCenter(pw.GetComponent<RectTransform>(),  280f, 30f,    0f, -40f);
        animPanel.SetActive(false);

        // Result 패널
        var resultPanel2 = CreatePanel("ResultPanel", card.transform, Color.clear);
        StretchFill(resultPanel2.GetComponent<RectTransform>());
        var winnerLabel = CreateTMPLabel("WinnerText",  resultPanel2.transform, "결과!", 40, TextAlignmentOptions.Center);
        var absorbLabel = CreateTMPLabel("AbsorbText",  resultPanel2.transform, "",      18, TextAlignmentOptions.Center);
        var resClose    = CreateButton("ResultCloseButton", resultPanel2.transform, "확인");
        PlaceCenter(winnerLabel.GetComponent<RectTransform>(), 280f, 60f, 0f,  60f);
        PlaceCenter(absorbLabel.GetComponent<RectTransform>(), 280f, 50f, 0f,   0f);
        PlaceCenter(resClose.GetComponent<RectTransform>(),    130f, 44f, 0f, -70f);
        resultPanel2.SetActive(false);

        SetPrivateField(modal, "encounterPanel",     encPanel);
        SetPrivateField(modal, "animatingPanel",     animPanel);
        SetPrivateField(modal, "resultPanel",        resultPanel2);
        SetPrivateField(modal, "encounterTitle",     encTitle);
        SetPrivateField(modal, "encounterDesc",      encDesc);
        SetPrivateField(modal, "battleButton",       battleBtn.GetComponent<Button>());
        SetPrivateField(modal, "allianceButton",     allianceBtn.GetComponent<Button>());
        SetPrivateField(modal, "closeButton",        closeBtn.GetComponent<Button>());
        SetPrivateField(modal, "vs1Text",            vs1);
        SetPrivateField(modal, "vs2Text",            vs2);
        SetPrivateField(modal, "powerText",          pw);
        SetPrivateField(modal, "winnerText",         winnerLabel);
        SetPrivateField(modal, "absorbText",         absorbLabel);
        SetPrivateField(modal, "resultCloseButton",  resClose.GetComponent<Button>());
    }

    // ─────────────────────────────────────────────────────────────────
    // 헬퍼: 오브젝트 생성
    // ─────────────────────────────────────────────────────────────────
    static GameObject CreateEmpty(string name, GameObject parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent.transform, false);
        return go;
    }

    static GameObject CreateEmpty(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static Canvas CreateUICanvas(string name, Transform parent = null, RenderMode mode = RenderMode.ScreenSpaceOverlay)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = mode;
        if (mode != RenderMode.WorldSpace)
        {
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            go.AddComponent<GraphicRaycaster>();
        }
        return canvas;
    }

    static Canvas CreateUICanvas(string name, GameObject parent, RenderMode mode = RenderMode.ScreenSpaceOverlay)
        => CreateUICanvas(name, parent?.transform, mode);

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return go;
    }

    static TextMeshProUGUI CreateTMPLabel(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static GameObject CreateButton(string name, Transform parent, string label)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f);
        go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        StretchFill(textGO.GetComponent<RectTransform>());
        return go;
    }

    static GameObject CreateSlider(string name, Transform parent)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Slider>();
        var bg = new GameObject("Background"); bg.transform.SetParent(go.transform, false);
        bg.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
        StretchFill(bg.GetComponent<RectTransform>());
        var fill = new GameObject("Fill"); fill.transform.SetParent(go.transform, false);
        fill.AddComponent<Image>().color = new Color(0f, 0.8f, 0.4f);
        StretchFill(fill.GetComponent<RectTransform>());
        go.GetComponent<Slider>().fillRect = fill.GetComponent<RectTransform>();
        return go;
    }

    static GameObject CreateScrollView(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        var sv = go.AddComponent<ScrollRect>();

        var vp = new GameObject("Viewport"); vp.transform.SetParent(go.transform, false);
        vp.AddComponent<Image>().color = Color.clear;
        vp.AddComponent<Mask>().showMaskGraphic = false;
        StretchFill(vp.GetComponent<RectTransform>());

        var content = new GameObject("Content"); content.transform.SetParent(vp.transform, false);
        var contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0,1); contentRt.anchorMax = new Vector2(1,1);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0, 0);
        content.AddComponent<VerticalLayoutGroup>().spacing = 8f;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sv.viewport = vp.GetComponent<RectTransform>();
        sv.content  = contentRt;
        sv.horizontal = false;
        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // RectTransform 헬퍼
    // ─────────────────────────────────────────────────────────────────
    static void StretchFill(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(r, t);
    }

    static void PlaceCenter(RectTransform rt, float w, float h, float x, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    static void PlaceTop(RectTransform rt, float w, float h, float x, bool stretch = false, bool right = false)
    {
        if (stretch) { rt.anchorMin = new Vector2(0,1); rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(0,-h); rt.offsetMax = Vector2.zero; }
        else { rt.anchorMin = rt.anchorMax = new Vector2(right ? 1f : 0.5f, 1f); rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, -h/2); }
    }

    static void PlaceBottom(RectTransform rt, float w, float h, float x)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, 20f);
    }

    static void PlaceLeft(RectTransform rt, float w, float h, float x)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    static void PlaceRight(RectTransform rt, float w, float h, float x)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    static void PlaceTopLeft(RectTransform rt, float w, float h, float x, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    static void PlaceTopRight(RectTransform rt, float w, float h, float x, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    // ─────────────────────────────────────────────────────────────────
    // private 필드 강제 설정 (SerializeField)
    // ─────────────────────────────────────────────────────────────────
    static void SetPrivateField(object target, string fieldName, object value)
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (field != null) { field.SetValue(target, value); return; }
            type = type.BaseType;
        }
        Debug.LogWarning($"[SceneSetup] 필드 없음: {fieldName}");
    }
}
