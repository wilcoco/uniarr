using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Tower Defense Mega Pack 같은 3D 타워 프리팹들을
/// 탑다운/이소메트릭 PNG 스프라이트로 자동 렌더링.
///
/// 사용:
///   1) 메뉴 Guardian AR > Tower Sprite Renderer 열기
///   2) 클래스별로 프리팹 드래그 (arrow/cannon/magic/support/production/revenue)
///   3) "Render All" 클릭 → /Assets/Generated/Towers/*.png 생성
///   4) 그 PNG들을 client/public/assets/towers/ 폴더로 복사
///
/// 카메라:
///   - 30도 이소메트릭 뷰 (탑다운 + 약간 기울임 → 입체감)
///   - 투명 배경
///   - 256×256 권장
/// </summary>
public class TowerSpriteRenderer : EditorWindow
{
    // 13종 (Piloto Studio TowerDefenseStarterPack 매핑)
    // Lv1 프리팹만 슬롯에 넣으면, 자동으로 Lv2~Lv5 프리팹도 같은 폴더에서 찾음
    private GameObject genericLv1, balistaLv1, cannonLv1, assaultLv1, scifiLv1, fireLv1,
                       iceLv1, aquaLv1, electricLv1, natureLv1, venomLv1, arcaneLv1, crystalLv1;
    // 자동 탐지 모드 (체크 시 Piloto Studio 폴더에서 자동으로 모든 프리팹 찾음)
    private bool autoDetect = true;

    private int spriteSize = 256;
    private float cameraDistance = 5f;
    private float cameraAngle = 30f;     // 탑다운에서 약간 기울인 각도
    private bool renderAllTiers = true;  // 1~5 티어 모두 렌더 (스케일/색조 차이)

    private string outputDir = "Assets/Generated/Towers";

    [MenuItem("Guardian AR/Tower Sprite Renderer")]
    public static void OpenWindow()
    {
        GetWindow<TowerSpriteRenderer>("Tower Sprite Renderer").minSize = new Vector2(420, 480);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Piloto Studio TowerDefenseStarterPack → 2D 맵 스프라이트", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "13 클래스 × 5 레벨 = 65 PNG 자동 생성.\n" +
            "Auto-Detect ON: Piloto Studio 폴더에서 SM_TowerDefense_*_Lv1~5.prefab 자동 탐지\n" +
            "Auto-Detect OFF: 각 슬롯에 Lv1 프리팹 수동 드래그", MessageType.Info);

        GUILayout.Space(6);
        autoDetect = EditorGUILayout.Toggle("Auto-Detect Prefabs", autoDetect);

        if (!autoDetect)
        {
            GUILayout.Space(4);
            genericLv1  = (GameObject)EditorGUILayout.ObjectField("Generic Lv1",  genericLv1,  typeof(GameObject), false);
            balistaLv1  = (GameObject)EditorGUILayout.ObjectField("Balista Lv1",  balistaLv1,  typeof(GameObject), false);
            cannonLv1   = (GameObject)EditorGUILayout.ObjectField("Cannon Lv1",   cannonLv1,   typeof(GameObject), false);
            assaultLv1  = (GameObject)EditorGUILayout.ObjectField("Assault Lv1",  assaultLv1,  typeof(GameObject), false);
            scifiLv1    = (GameObject)EditorGUILayout.ObjectField("SciFi Lv1",    scifiLv1,    typeof(GameObject), false);
            fireLv1     = (GameObject)EditorGUILayout.ObjectField("Fire Lv1",     fireLv1,     typeof(GameObject), false);
            iceLv1      = (GameObject)EditorGUILayout.ObjectField("Ice Lv1",      iceLv1,      typeof(GameObject), false);
            aquaLv1     = (GameObject)EditorGUILayout.ObjectField("Aqua Lv1",     aquaLv1,     typeof(GameObject), false);
            electricLv1 = (GameObject)EditorGUILayout.ObjectField("Electric Lv1", electricLv1, typeof(GameObject), false);
            natureLv1   = (GameObject)EditorGUILayout.ObjectField("Nature Lv1",   natureLv1,   typeof(GameObject), false);
            venomLv1    = (GameObject)EditorGUILayout.ObjectField("Venom Lv1",    venomLv1,    typeof(GameObject), false);
            arcaneLv1   = (GameObject)EditorGUILayout.ObjectField("Arcane Lv1",   arcaneLv1,   typeof(GameObject), false);
            crystalLv1  = (GameObject)EditorGUILayout.ObjectField("Crystal Lv1",  crystalLv1,  typeof(GameObject), false);
        }

        GUILayout.Space(8);
        spriteSize       = EditorGUILayout.IntSlider("Sprite Size",   spriteSize, 64, 512);
        cameraAngle      = EditorGUILayout.Slider("Camera Angle (°)", cameraAngle, 0f, 90f);
        cameraDistance   = EditorGUILayout.Slider("Camera Distance",  cameraDistance, 1f, 20f);
        renderAllTiers   = EditorGUILayout.Toggle("Render All Tiers (T1-T5)", renderAllTiers);
        outputDir        = EditorGUILayout.TextField("Output Folder", outputDir);

        GUILayout.Space(12);
        if (GUILayout.Button("Render All", GUILayout.Height(36)))
            RenderAll();
        if (GUILayout.Button("Open Output Folder"))
            EditorUtility.RevealInFinder(outputDir);
    }

    static readonly string[] CLASS_NAMES = {
        "generic","balista","cannon","assault","scifi","fire","ice","aqua","electric","nature","venom","arcane","crystal"
    };
    static readonly string[] CLASS_PILOTO = {
        "Generic","Balista","Cannon","Assault","SciFi","Fire","Ice","Aqua","Electric","Nature","Venom","Arcane","Crystal"
    };

    GameObject FindPilotoPrefab(string pilotoName, int level)
    {
        // 표준 경로: Assets/Piloto Studio/TowerDefenseStarterPack/Prefabs/Towers/SM_TowerDefense_{name}_Lv{level}.prefab
        string path = $"Assets/Piloto Studio/TowerDefenseStarterPack/Prefabs/Towers/SM_TowerDefense_{pilotoName}_Lv{level}.prefab";
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    GameObject GetSlotPrefab(int idx)
    {
        switch (idx)
        {
            case 0: return genericLv1;
            case 1: return balistaLv1;
            case 2: return cannonLv1;
            case 3: return assaultLv1;
            case 4: return scifiLv1;
            case 5: return fireLv1;
            case 6: return iceLv1;
            case 7: return aquaLv1;
            case 8: return electricLv1;
            case 9: return natureLv1;
            case 10: return venomLv1;
            case 11: return arcaneLv1;
            case 12: return crystalLv1;
        }
        return null;
    }

    void RenderAll()
    {
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        int rendered = 0, missing = 0;

        for (int i = 0; i < CLASS_NAMES.Length; i++)
        {
            string clsName = CLASS_NAMES[i];
            int maxLevel = renderAllTiers ? 5 : 1;
            for (int lv = 1; lv <= maxLevel; lv++)
            {
                GameObject prefab = autoDetect
                    ? FindPilotoPrefab(CLASS_PILOTO[i], lv)
                    : GetSlotPrefab(i);  // 수동: Lv1만 사용 (스케일/색조로 대체)

                if (prefab == null) { missing++; continue; }
                int effectiveLevel = autoDetect ? 1 : lv;  // auto=실제 Lv 모델, manual=Lv1을 스케일링
                RenderOne(prefab, $"{outputDir}/{clsName}_t{lv}.png", effectiveLevel);
                rendered++;
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료",
            $"{rendered}개 스프라이트 생성, {missing}개 프리팹 누락\n경로: {outputDir}\n\n" +
            "이 PNG들을 client/public/assets/towers/ 로 복사하세요.", "OK");
    }

    void RenderOne(GameObject prefab, string path, int tier)
    {
        // 임시 씬 구성
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        // 티어별 스케일 (T1=0.85, T5=1.3)
        go.transform.localScale = Vector3.one * (0.85f + tier * 0.1f);

        // 티어별 색조 (T1 회색~T5 빨강) — Renderer.material 색 조절
        var renderers = go.GetComponentsInChildren<Renderer>();
        Color tierTint = tier switch {
            1 => new Color(0.7f, 0.7f, 0.7f, 1f),
            2 => new Color(0.4f, 0.85f, 0.6f, 1f),
            3 => new Color(0.65f, 0.55f, 0.95f, 1f),
            4 => new Color(0.96f, 0.62f, 0.04f, 1f),
            5 => new Color(0.95f, 0.27f, 0.27f, 1f),
            _ => Color.white
        };
        foreach (var r in renderers)
        {
            foreach (var m in r.materials)
            {
                if (m.HasProperty("_EmissionColor"))
                    m.SetColor("_EmissionColor", tierTint * 0.5f);
                m.color = Color.Lerp(m.color, tierTint, 0.3f);
            }
        }

        // 임시 카메라
        var camGO = new GameObject("RenderCam");
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); // 투명
        cam.orthographic = true;
        cam.orthographicSize = 1.5f;

        // 카메라 위치 (탑다운 + 기울임)
        var bounds = CalcBounds(go);
        Vector3 center = bounds.center;
        float rad = cameraAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(0, Mathf.Cos(rad) * cameraDistance, -Mathf.Sin(rad) * cameraDistance);
        camGO.transform.position = center + offset;
        camGO.transform.LookAt(center);

        // 라이트 (이미 씬에 있을 수 있지만 보장)
        var lightGO = new GameObject("RenderLight");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // 렌더 텍스처
        var rt = new RenderTexture(spriteSize, spriteSize, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        cam.Render();

        // PNG 저장
        RenderTexture.active = rt;
        var tex = new Texture2D(spriteSize, spriteSize, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, spriteSize, spriteSize), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null;

        // 정리
        DestroyImmediate(go);
        DestroyImmediate(camGO);
        DestroyImmediate(lightGO);
        DestroyImmediate(rt);
        DestroyImmediate(tex);

        Debug.Log($"[TowerSpriteRenderer] {path}");
    }

    Bounds CalcBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
