using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using GuardianAR;

/// <summary>
/// Guardian AR 씬 자동 구성 (v3 - WebView 기반)
///
/// 설계:
///   - 모든 게임 로직/UI는 React PWA(서버에 호스팅)에서 동작
///   - Unity는 WebView 임베드 + AR 카메라만 담당
///   - 핵심 매니저: ApiManager, GameManager, LocationManager, WebGameController
///   - AR: ARSession + ARModeController + ARBattleManager
///
/// Unity 메뉴: Guardian AR → Setup Scene
/// </summary>
public class GuardianARSceneSetup : Editor
{
    [MenuItem("Guardian AR/Setup Scene")]
    public static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("Setup Scene",
            "Build Guardian AR scene structure (WebView + AR).\nExisting objects will be removed.\nContinue?",
            "Create", "Cancel")) return;

        // ─── 0. 기존 오브젝트 전체 정리 ──────────────────────────────
        DestroyAllOfType<HUD>();
        DestroyAllOfType<BattleModal>();
        DestroyAllOfType<LoginPanel>();
        DestroyAllOfType<PartsPanel>();
        DestroyAllOfType<LeaderboardPanel>();
        DestroyAllOfType<ModeController>();
        DestroyAllOfType<ARModeController>();
        DestroyAllOfType<ARBattleManager>();
        DestroyAllOfType<MapController>();
        DestroyAllOfType<MapTileManager>();
        DestroyAllOfType<MapInputHandler>();
        DestroyAllOfType<ARFixedGuardianPlacer>();
        DestroyAllOfType<GameManager>();
        DestroyAllOfType<ApiManager>();
        DestroyAllOfType<LocationManager>();
        DestroyAllOfType<MainThreadDispatcher>();
        DestroyAllOfType<FirebaseManager>();
        DestroyAllOfType<AppBootstrap>();
        DestroyAllOfType<EditorGPSDebug>();
        DestroyAllOfType<WebGameController>();

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (canvas != null && canvas.transform.parent == null) DestroyImmediate(canvas.gameObject);
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            if (es != null) DestroyImmediate(es.gameObject);
        foreach (var name in new[] { "Bootstrap", "Managers", "ModeController", "MapModeRoot", "ARModeRoot", "WebRoot" })
        {
            GameObject obj;
            while ((obj = GameObject.Find(name)) != null) DestroyImmediate(obj);
        }

        // ─── 1. EventSystem ─────────────────────────────────────────
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ─── 2. Managers (DontDestroyOnLoad 가능) ─────────────────────
        var managers = new GameObject("Managers");
        var apiMgr = new GameObject("ApiManager");      apiMgr.transform.SetParent(managers.transform);     apiMgr.AddComponent<ApiManager>();
        var locMgr = new GameObject("LocationManager"); locMgr.transform.SetParent(managers.transform);     locMgr.AddComponent<LocationManager>();
        var gmMgr  = new GameObject("GameManager");     gmMgr.transform.SetParent(managers.transform);      gmMgr.AddComponent<GameManager>();
        var dispMgr= new GameObject("MainThreadDispatcher"); dispMgr.transform.SetParent(managers.transform); dispMgr.AddComponent<MainThreadDispatcher>();
        var fbMgr  = new GameObject("FirebaseManager"); fbMgr.transform.SetParent(managers.transform);     fbMgr.AddComponent<FirebaseManager>();

        // ─── 3. WebRoot (PWA 임베드) ──────────────────────────────────
        var webRoot = new GameObject("WebRoot");
        webRoot.AddComponent<WebGameController>();

        // ─── 4. ARModeRoot (AR 모드 전용) ─────────────────────────────
        var arRoot = new GameObject("ARModeRoot");
        arRoot.SetActive(false); // 시작 시 비활성, AR 진입 시 활성화

        var arSessionGO = new GameObject("AR Session"); arSessionGO.transform.SetParent(arRoot.transform);
        var arSession = arSessionGO.AddComponent<ARSession>();

        var arOriginGO = new GameObject("AR Session Origin"); arOriginGO.transform.SetParent(arRoot.transform);
        var arOrigin = arOriginGO.AddComponent<ARSessionOrigin>();
        var arPlaneMgr = arOriginGO.AddComponent<ARPlaneManager>();
        arOriginGO.AddComponent<ARRaycastManager>();

        var arCamGO = new GameObject("AR Camera"); arCamGO.transform.SetParent(arOriginGO.transform);
        var arCam = arCamGO.AddComponent<Camera>();
        arCam.clearFlags = CameraClearFlags.Color;
        arCam.tag = "MainCamera";
        var arCamMgr = arCamGO.AddComponent<ARCameraManager>();
        arCamGO.AddComponent<ARCameraBackground>();
        arOrigin.camera = arCam;

        var arCtrlGO = new GameObject("ARModeController"); arCtrlGO.transform.SetParent(arRoot.transform);
        var arCtrl = arCtrlGO.AddComponent<ARModeController>();
        SetField(arCtrl, "arSession", arSession);
        SetField(arCtrl, "arCameraManager", arCamMgr);
        SetField(arCtrl, "arPlaneManager", arPlaneMgr);

        var arBattleGO = new GameObject("ARBattleManager"); arBattleGO.transform.SetParent(arRoot.transform);
        arBattleGO.AddComponent<ARBattleManager>();

        // ─── 5. ModeController (Map ↔ AR 전환 — 이번엔 Web ↔ AR) ─────
        var modeGO = new GameObject("ModeController");
        var mode = modeGO.AddComponent<ModeController>();
        SetField(mode, "arModeRoot", arRoot);
        SetField(mode, "arController", arCtrl);

        // ─── 6. AR ↔ Web 연결: AR 진입/이탈 시 WebView 토글 ──────────
        // 런타임에 WebGameController가 ENTER_AR 메시지 수신 → arRoot.SetActive(true)
        // (구체 연결은 WebGameController가 ModeController를 호출하는 형태로 처리)

        // ─── 7. EditorGPSDebug ────────────────────────────────────────
#if UNITY_EDITOR
        var dbgGO = new GameObject("EditorGPSDebug");
        dbgGO.AddComponent<GuardianAR.EditorGPSDebug>();
#endif

        // ─── 씬 저장 ───────────────────────────────────────────────────
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Done",
            "WebView-based scene setup complete!\n\n" +
            "All map/HUD/parts/leaderboard logic runs in React PWA (WebView).\n" +
            "AR mode activates only when web requests it.\n\n" +
            "Check: Guardian AR > Settings - server URL (web is loaded from same URL)",
            "OK");

        Debug.Log("[GuardianAR] Scene setup v3 complete (WebView + AR).");
    }

    static void DestroyAllOfType<T>() where T : Component
    {
        foreach (var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null) continue;
            var root = c.transform.root.gameObject;
            if (root != null) DestroyImmediate(root);
        }
    }

    static void SetField(object target, string name, object value)
    {
        var type = target.GetType();
        while (type != null)
        {
            var f = type.GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (f != null) { f.SetValue(target, value); return; }
            type = type.BaseType;
        }
    }
}
