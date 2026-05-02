using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using GuardianAR;

/// <summary>
/// Guardian AR 모든 프리팹 자동 생성
/// Unity 메뉴: Guardian AR → Create All Prefabs
/// </summary>
public class GuardianARPrefabCreator : Editor
{
    const string Root    = "Assets/Prefabs";
    const string MapDir  = "Assets/Prefabs/Map";
    const string ARDir   = "Assets/Prefabs/AR";
    const string TexDir  = "Assets/Prefabs/Textures";

    [MenuItem("Guardian AR/Create All Prefabs")]
    public static void CreateAllPrefabs()
    {
        EnsureFolders();

        var circle = GetOrCreateCircleSprite();

        // 맵 마커 프리팹
        CreateMyMarker(circle);
        CreateOtherPlayerMarker(circle);
        CreateFixedGuardianMarker(circle);
        CreateTerritoryCirclePrefab(circle);
        CreateMapTilePrefab();

        // AR 프리팹
        CreateARHPBarPrefab();
        CreateARDamageNumberPrefab();
        CreateGuardianARPrefab("GuardianARObject",      PrimitiveType.Cube,    false);
        CreateGuardianARPrefab("FixedGuardianARObject", PrimitiveType.Cube,    true);
        CreateTerritoryARPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ConnectToScene();

        EditorUtility.DisplayDialog("Done",
            "All prefabs created!\nCheck Assets/Prefabs/ folder.",
            "OK");
    }

    // ─── 폴더 생성 ────────────────────────────────────────────────────
    static void EnsureFolders()
    {
        foreach (var path in new[] { Root, MapDir, ARDir, TexDir })
        {
            if (AssetDatabase.IsValidFolder(path)) continue;
            var slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }

    // ─── 원형 스프라이트 ──────────────────────────────────────────────
    static Sprite GetOrCreateCircleSprite()
    {
        string pngPath = $"{TexDir}/Circle.png";

        if (!File.Exists(pngPath))
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var c = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
            float r = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), c) <= r
                        ? Color.white : Color.clear);
            tex.Apply();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(pngPath);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType  = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }

    // ─── 내 위치 마커 ─────────────────────────────────────────────────
    static void CreateMyMarker(Sprite circle)
    {
        var root = UIRoot("MyMarker", 44, 44);

        MakeImage("Ring", root.transform, circle, new Color(0.2f, 0.6f, 1f, 0.85f), 44, 44, 0, 0);
        MakeImage("Dot",  root.transform, circle, Color.white, 16, 16, 0, 0);

        Save(root, $"{MapDir}/MyMarker.prefab");
    }

    // ─── 다른 플레이어 마커 ───────────────────────────────────────────
    static void CreateOtherPlayerMarker(Sprite circle)
    {
        var root = UIRoot("OtherPlayerMarker", 60, 74);
        root.AddComponent<Button>();

        MakeImage("Icon", root.transform, circle, new Color(1f, 0.3f, 0.3f), 48, 48, 0, 13f);
        MakeTMPUGUI("Label", root.transform, "Player", 13f, new Vector2(84, 22), new Vector2(0, -24f));

        Save(root, $"{MapDir}/OtherPlayerMarker.prefab");
    }

    // ─── 고정 수호신 마커 ─────────────────────────────────────────────
    static void CreateFixedGuardianMarker(Sprite circle)
    {
        var root = UIRoot("FixedGuardianMarker", 60, 74);
        root.AddComponent<Button>();

        MakeImage("Icon",  root.transform, circle, new Color(1f, 0.75f, 0f), 48, 48, 0, 13f);
        var icon = MakeTMPUGUI("TypeLabel",  root.transform, "DEF",  18f, new Vector2(40, 40), new Vector2(0, 13f));
        icon.alignment = TextAlignmentOptions.Center;
        MakeTMPUGUI("OwnerLabel", root.transform, "", 11f, new Vector2(84, 20), new Vector2(0, -24f))
            .color = new Color(1f, 0.9f, 0.7f);

        Save(root, $"{MapDir}/FixedGuardianMarker.prefab");
    }

    // ─── 영역 원 ──────────────────────────────────────────────────────
    static void CreateTerritoryCirclePrefab(Sprite circle)
    {
        var root = UIRoot("TerritoryCircle", 100, 100);
        var tc   = root.AddComponent<TerritoryCircle>();

        var border = MakeImage("Border", root.transform, circle, new Color(0f, 1f, 0.53f, 0.9f), 100, 100, 0, 0);
        var fill   = MakeImage("Fill",   root.transform, circle, new Color(0f, 1f, 0.53f, 0.25f), 96,  96,  0, 0);

        SetField(tc, "borderImage", border);
        SetField(tc, "fillImage",   fill);

        Save(root, $"{MapDir}/TerritoryCircle.prefab");
    }

    // ─── 맵 타일 ──────────────────────────────────────────────────────
    static void CreateMapTilePrefab()
    {
        var root = UIRoot("MapTile", 256, 256);
        root.AddComponent<RawImage>().color = new Color(0.75f, 0.75f, 0.75f);
        root.SetActive(false);
        Save(root, $"{MapDir}/MapTile.prefab");
    }

    // ─── AR HP 바 ─────────────────────────────────────────────────────
    static void CreateARHPBarPrefab()
    {
        var root = new GameObject("ARHPBar");
        var bar  = root.AddComponent<ARHPBar>();

        // World Space Canvas (바 이미지)
        var canvasGO = new GameObject("BarCanvas");
        canvasGO.transform.SetParent(root.transform, false);
        canvasGO.transform.localPosition = new Vector3(0, 0.55f, 0);
        canvasGO.transform.localScale    = Vector3.one * 0.003f;
        var cvs = canvasGO.AddComponent<Canvas>();
        cvs.renderMode = RenderMode.WorldSpace;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 28);

        // 배경
        var bg = MakeImageWorld("BG", canvasGO.transform,
            new Color(0.1f, 0.1f, 0.1f, 0.85f), true);

        // HP Fill (상단 60%)
        var hpFillGO = new GameObject("HPFill");
        hpFillGO.transform.SetParent(canvasGO.transform, false);
        var hpFill = hpFillGO.AddComponent<Image>();
        hpFill.color = Color.green;
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillAmount = 1f;
        var hpRt = hpFillGO.GetComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0, 0.38f); hpRt.anchorMax = Vector2.one;
        hpRt.offsetMin = new Vector2(2, 0); hpRt.offsetMax = new Vector2(-2, -1);

        // Ult Charge (하단 38%)
        var ultGO = new GameObject("UltFill");
        ultGO.transform.SetParent(canvasGO.transform, false);
        var ultFill = ultGO.AddComponent<Image>();
        ultFill.color = new Color(1f, 0.8f, 0f);
        ultFill.type = Image.Type.Filled;
        ultFill.fillMethod = Image.FillMethod.Horizontal;
        ultFill.fillAmount = 0f;
        var ultRt = ultGO.GetComponent<RectTransform>();
        ultRt.anchorMin = Vector2.zero; ultRt.anchorMax = new Vector2(1, 0.38f);
        ultRt.offsetMin = new Vector2(2, 1); ultRt.offsetMax = new Vector2(-2, 0);

        // HP Text (3D TMP)
        var hpTextGO = new GameObject("HPText");
        hpTextGO.transform.SetParent(root.transform, false);
        hpTextGO.transform.localPosition = new Vector3(0, 0.62f, 0);
        hpTextGO.transform.localScale    = Vector3.one * 0.007f;
        var hpTmp = hpTextGO.AddComponent<TextMeshPro>();
        hpTmp.text = "100/100"; hpTmp.fontSize = 8f;
        hpTmp.alignment = TextAlignmentOptions.Center; hpTmp.color = Color.white;

        // Name Text (3D TMP)
        var nameGO = new GameObject("NameText");
        nameGO.transform.SetParent(root.transform, false);
        nameGO.transform.localPosition = new Vector3(0, 0.72f, 0);
        nameGO.transform.localScale    = Vector3.one * 0.007f;
        var nameTmp = nameGO.AddComponent<TextMeshPro>();
        nameTmp.text = "Guardian"; nameTmp.fontSize = 10f;
        nameTmp.alignment = TextAlignmentOptions.Center; nameTmp.color = Color.white;

        SetField(bar, "hpFill",      hpFill);
        SetField(bar, "ultChargeBar", ultFill);
        SetField(bar, "hpText",      hpTmp);
        SetField(bar, "nameText",    nameTmp);

        Save(root, $"{ARDir}/ARHPBar.prefab");
    }

    // ─── AR 데미지 숫자 ───────────────────────────────────────────────
    static void CreateARDamageNumberPrefab()
    {
        var root = new GameObject("ARDamageNumber");
        root.transform.localScale = Vector3.one * 0.01f;
        var tmp = root.AddComponent<TextMeshPro>();
        tmp.text = "0"; tmp.fontSize = 12f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        root.AddComponent<ARDamageNumber>();
        Save(root, $"{ARDir}/ARDamageNumber.prefab");
    }

    // ─── 수호신 AR 오브젝트 ───────────────────────────────────────────
    static void CreateGuardianARPrefab(string prefabName, PrimitiveType shape, bool isFixed)
    {
        var root = new GameObject(prefabName);
        root.transform.localScale = Vector3.one * 0.35f;

        // Body
        var body = GameObject.CreatePrimitive(shape);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.white;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.white * 0.5f);
        var renderer = body.GetComponent<Renderer>();
        renderer.material = mat;
        AssetDatabase.CreateAsset(mat, $"{ARDir}/{prefabName}_Mat.mat");

        // Labels (3D TMP)
        var nameLabelGO = MakeTMP3D("NameLabel", root.transform, "",   10f, new Vector3(0, 1.3f, 0));
        var typeLabelGO = MakeTMP3D("TypeLabel", root.transform, "?",  14f, new Vector3(0, 1.1f, 0));

        if (isFixed)
        {
            var obj = root.AddComponent<FixedGuardianARObject>();
            SetField(obj, "bodyRenderer", renderer);
            SetField(obj, "ownerLabel",   nameLabelGO);
            SetField(obj, "typeLabel",    typeLabelGO);
        }
        else
        {
            root.AddComponent<ARWorldAnchor>();
            var obj = root.AddComponent<GuardianARObject>();
            SetField(obj, "bodyRenderer", renderer);
            SetField(obj, "nameLabel",    nameLabelGO);
            SetField(obj, "typeLabel",    typeLabelGO);
        }

        Save(root, $"{ARDir}/{prefabName}.prefab");
    }

    // ─── 영역 AR 시각화 (반투명 원통) ────────────────────────────────
    static void CreateTerritoryARPrefab()
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        root.name = "TerritoryAR";
        Object.DestroyImmediate(root.GetComponent<Collider>());
        root.transform.localScale = new Vector3(10f, 0.02f, 10f);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0f, 1f, 0.53f, 0.3f);
        mat.SetFloat("_Surface", 1);  // Transparent
        mat.SetFloat("_Blend", 0);
        root.GetComponent<Renderer>().material = mat;
        AssetDatabase.CreateAsset(mat, $"{ARDir}/TerritoryAR_Mat.mat");

        Save(root, $"{ARDir}/TerritoryAR.prefab");
    }

    // ─── 씬 자동 연결 ─────────────────────────────────────────────────
    static void ConnectToScene()
    {
        // MapController
        foreach (var mc in Object.FindObjectsOfType<MapController>())
        {
            SetField(mc, "myMarkerPrefab",        Load($"{MapDir}/MyMarker.prefab"));
            SetField(mc, "otherPlayerPrefab",     Load($"{MapDir}/OtherPlayerMarker.prefab"));
            SetField(mc, "fixedGuardianPrefab",   Load($"{MapDir}/FixedGuardianMarker.prefab"));
            SetField(mc, "territoryCirclePrefab", Load($"{MapDir}/TerritoryCircle.prefab"));
            EditorUtility.SetDirty(mc);
        }

        // MapTileManager
        foreach (var tm in Object.FindObjectsOfType<MapTileManager>())
        {
            var raw = Load($"{MapDir}/MapTile.prefab")?.GetComponent<RawImage>();
            if (raw != null) SetField(tm, "tilePrefab", raw);
            EditorUtility.SetDirty(tm);
        }

        // ARBattleManager
        foreach (var bm in Object.FindObjectsOfType<ARBattleManager>())
        {
            var hpBar = Load($"{ARDir}/ARHPBar.prefab")?.GetComponent<ARHPBar>();
            SetField(bm, "hpBarPrefab",        hpBar);
            SetField(bm, "damageNumberPrefab", Load($"{ARDir}/ARDamageNumber.prefab"));
            EditorUtility.SetDirty(bm);
        }

        // ARModeController
        foreach (var ac in Object.FindObjectsOfType<ARModeController>())
        {
            var guardian = Load($"{ARDir}/GuardianARObject.prefab");
            var fixedG   = Load($"{ARDir}/FixedGuardianARObject.prefab");
            var terrAR   = Load($"{ARDir}/TerritoryAR.prefab");
            SetField(ac, "myGuardianARPrefab",       guardian);
            SetField(ac, "enemyGuardianARPrefab",    guardian);
            SetField(ac, "fixedGuardianARPrefab",    fixedG);
            SetField(ac, "myFixedGuardianARPrefab",  fixedG);
            SetField(ac, "territoryARPrefab",        terrAR);
            EditorUtility.SetDirty(ac);
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }

    // ─── 헬퍼 ─────────────────────────────────────────────────────────
    static GameObject UIRoot(string name, float w, float h)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        return go;
    }

    static Image MakeImage(string name, Transform parent, Sprite sprite, Color color,
        float w, float h, float ax, float ay)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color  = color;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(ax, ay);
        return img;
    }

    // World Space Canvas 용 이미지 (앵커 stretch)
    static Image MakeImageWorld(string name, Transform parent, Color color, bool stretch)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        if (stretch)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        return img;
    }

    static TextMeshProUGUI MakeTMPUGUI(string name, Transform parent, string text,
        float size, Vector2 sizeDelta, Vector2 pos)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;
        return tmp;
    }

    static TextMeshPro MakeTMP3D(string name, Transform parent, string text,
        float size, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * 0.3f;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text; tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static void Save(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void SetField(object target, string name, object value)
    {
        var type = target.GetType();
        while (type != null)
        {
            var f = type.GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null) { f.SetValue(target, value); return; }
            type = type.BaseType;
        }
        Debug.LogWarning($"[PrefabCreator] 필드 없음: {name}");
    }
}
