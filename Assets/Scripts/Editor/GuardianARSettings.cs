using UnityEngine;
using UnityEditor;
using GuardianAR;

/// <summary>
/// Guardian AR 서버/앱 주소 설정 창
/// Unity 메뉴: Guardian AR → Settings
/// </summary>
public class GuardianARSettings : EditorWindow
{
    private string webAppUrl    = "https://your-app.vercel.app";
    private string localUrl     = "http://localhost:5173";
    private string serverUrl    = "https://arr-production.up.railway.app";

    private const string KeyWeb    = "GuardianAR_WebAppUrl";
    private const string KeyLocal  = "GuardianAR_LocalUrl";
    private const string KeyServer = "GuardianAR_ServerUrl";

    [MenuItem("Guardian AR/Settings")]
    public static void ShowWindow()
    {
        var win = GetWindow<GuardianARSettings>("Guardian AR Settings");
        win.minSize = new Vector2(420, 260);
    }

    void OnEnable()
    {
        webAppUrl  = EditorPrefs.GetString(KeyWeb,    webAppUrl);
        localUrl   = EditorPrefs.GetString(KeyLocal,  localUrl);
        serverUrl  = EditorPrefs.GetString(KeyServer, serverUrl);
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Guardian AR — URL 설정", EditorStyles.boldLabel);
        GUILayout.Space(6);

        EditorGUILayout.HelpBox(
            "Save 후 Apply to Scene을 눌러야 씬 오브젝트에 반영됩니다.",
            MessageType.Info);
        GUILayout.Space(10);

        webAppUrl  = EditorGUILayout.TextField("배포 웹앱 URL",   webAppUrl);
        localUrl   = EditorGUILayout.TextField("로컬 테스트 URL", localUrl);
        serverUrl  = EditorGUILayout.TextField("서버 API URL",    serverUrl);

        GUILayout.Space(16);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save", GUILayout.Height(36)))
        {
            EditorPrefs.SetString(KeyWeb,    webAppUrl);
            EditorPrefs.SetString(KeyLocal,  localUrl);
            EditorPrefs.SetString(KeyServer, serverUrl);
            Debug.Log("[GuardianAR] 설정 저장 완료");
        }

        if (GUILayout.Button("Apply to Scene", GUILayout.Height(36)))
        {
            ApplyToScene();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("씬 구성이 안 되어 있다면:", EditorStyles.miniLabel);
        if (GUILayout.Button("Setup Scene 실행", GUILayout.Height(32)))
        {
            GuardianARSceneSetup.SetupScene();
            ApplyToScene();
        }
    }

    private void ApplyToScene()
    {
        int applied = 0;

        // MapWebViewController 업데이트
        foreach (var mvc in FindObjectsOfType<MapWebViewController>())
        {
            SetField(mvc, "webAppUrl", webAppUrl);
            SetField(mvc, "localUrl",  localUrl);
            EditorUtility.SetDirty(mvc);
            applied++;
        }

        // ApiManager 업데이트
        foreach (var api in FindObjectsOfType<ApiManager>())
        {
            SetField(api, "serverUrl", serverUrl);
            EditorUtility.SetDirty(api);
            applied++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("적용 완료",
            $"{applied}개 컴포넌트에 URL이 적용되고 씬이 저장되었습니다.\n\n" +
            $"웹앱: {webAppUrl}\n로컬: {localUrl}\n서버: {serverUrl}",
            "확인");
    }

    static void SetField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);
        field?.SetValue(target, value);
    }
}
