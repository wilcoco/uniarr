using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Piloto Studio 65개 타워 프리팹을 Resources/Towers 로 복사 (Android 빌드 시 Resources.Load 가능하게).
///
/// 메뉴: Guardian AR > Copy Tower Prefabs to Resources
///
/// 동작:
///  - Assets/Piloto Studio/TowerDefenseStarterPack/Prefabs/Towers/SM_TowerDefense_*_Lv*.prefab 65개를
///    Assets/Resources/Towers/ 로 복사 (이름 그대로)
///  - 빌드 시 Resources.Load<GameObject>("Towers/SM_TowerDefense_Cannon_Lv3") 식으로 런타임 로드 가능
/// </summary>
public static class TowerResourcesCopier
{
    [MenuItem("Guardian AR/Copy Tower Prefabs to Resources")]
    public static void Copy()
    {
        const string srcDir = "Assets/Piloto Studio/TowerDefenseStarterPack/Prefabs/Towers";
        const string dstDir = "Assets/Resources/Towers";

        if (!AssetDatabase.IsValidFolder(srcDir))
        {
            EditorUtility.DisplayDialog("Error", $"소스 폴더 없음: {srcDir}", "OK");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dstDir))
            AssetDatabase.CreateFolder("Assets/Resources", "Towers");

        var guids = AssetDatabase.FindAssets("SM_TowerDefense t:Prefab", new[] { srcDir })
                                .Concat(AssetDatabase.FindAssets("SM_TowerDefense t:GameObject", new[] { srcDir }))
                                .Distinct().ToArray();
        int copied = 0;
        foreach (var guid in guids)
        {
            var src = AssetDatabase.GUIDToAssetPath(guid);
            if (!src.EndsWith(".prefab")) continue;
            var fname = Path.GetFileName(src);
            var dst = $"{dstDir}/{fname}";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) == null)
            {
                AssetDatabase.CopyAsset(src, dst);
                copied++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료",
            $"{copied}개 프리팹을 Resources/Towers 로 복사했습니다.\n" +
            "이제 안드로이드 빌드에서도 런타임 Resources.Load 작동.", "OK");
    }
}

