using UnityEngine;
using UnityEditor;
using GuardianAR;

/// <summary>
/// Guardian AR 서버 주소 설정 창
/// Unity 메뉴: Guardian AR → Settings
/// </summary>
public class GuardianARSettings : EditorWindow
{
    private string serverUrl = "https://arr-production.up.railway.app";
    private const string KeyServer = "GuardianAR_ServerUrl";

    [MenuItem("Guardian AR/Settings")]
    public static void ShowWindow()
    {
        var win = GetWindow<GuardianARSettings>("Guardian AR Settings");
        win.minSize = new Vector2(420, 200);
    }

    void OnEnable()
    {
        serverUrl = EditorPrefs.GetString(KeyServer, serverUrl);
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Guardian AR - Server Settings", EditorStyles.boldLabel);
        GUILayout.Space(6);

        EditorGUILayout.HelpBox(
            "Click Apply to Scene after Save to update scene objects.",
            MessageType.Info);
        GUILayout.Space(10);

        serverUrl = EditorGUILayout.TextField("Server API URL", serverUrl);

        GUILayout.Space(16);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save", GUILayout.Height(36)))
        {
            EditorPrefs.SetString(KeyServer, serverUrl);
            Debug.Log("[GuardianAR] 설정 저장 완료");
        }

        if (GUILayout.Button("Apply to Scene", GUILayout.Height(36)))
        {
            ApplyToScene();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("If scene is not configured:", EditorStyles.miniLabel);
        if (GUILayout.Button("Run Setup Scene", GUILayout.Height(32)))
        {
            GuardianARSceneSetup.SetupScene();
            ApplyToScene();
        }
    }

    private void ApplyToScene()
    {
        int applied = 0;

        foreach (var api in FindObjectsOfType<ApiManager>())
        {
            SetField(api, "serverUrl", serverUrl);
            EditorUtility.SetDirty(api);
            applied++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Done",
            $"URL applied to {applied} component(s).\n\nServer: {serverUrl}",
            "OK");
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
