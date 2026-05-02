using UnityEditor;
using UnityEngine;

/// <summary>
/// WebViewObject가 Legacy Input.mousePosition을 사용하므로
/// Active Input Handling을 "Both"로 강제 설정.
/// 메뉴: Guardian AR > Fix Input System (Legacy + New)
/// </summary>
public static class InputSystemBothEnabler
{
    [MenuItem("Guardian AR/Fix Input System (Legacy + New)")]
    public static void EnableBoth()
    {
        // PlayerSettings의 activeInputHandler는 SerializedObject로만 접근 가능
        var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (ps == null || ps.Length == 0)
        {
            Debug.LogError("[InputSystemBothEnabler] ProjectSettings.asset not found");
            return;
        }
        var so = new SerializedObject(ps[0]);
        var prop = so.FindProperty("activeInputHandler");
        if (prop == null)
        {
            Debug.LogError("[InputSystemBothEnabler] activeInputHandler property not found");
            return;
        }
        prop.intValue = 2; // 0=Old, 1=New, 2=Both
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Input System",
            "Active Input Handling = Both 적용됨.\nUnity가 재시작될 수 있습니다.",
            "OK");
        Debug.Log("[InputSystemBothEnabler] Active Input Handling set to Both");
    }
}
