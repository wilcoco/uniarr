using UnityEditor;
using UnityEngine;
using GuardianAR;

/// <summary>
/// 개발 중 React PWA 소스를 빠르게 전환:
///   - Production:  https://arr-production.up.railway.app (배포된 PWA)
///   - LocalHost:   http://localhost:3000                  (Unity Editor에서 직접)
///   - LAN:         http://[PC_IP]:3000                    (실기기 디바이스에서 PC 접근)
///
/// React 개발 흐름:
///   1) cd arrrarr_project/client && npm run dev     (1초 핫리로드)
///   2) 브라우저 http://localhost:3000 으로 단독 테스트 (Unity 컴파일 0회)
///   3) Unity 테스트 시 메뉴 한 번 클릭으로 URL 전환
/// </summary>
public static class WebDevUrlSwitcher
{
    const string ProdUrl  = "https://arr-production.up.railway.app";
    const string LocalUrl = "http://localhost:3000";

    [MenuItem("Guardian AR/Web Source/Use PRODUCTION (railway)")]
    public static void UseProduction() => Apply(ProdUrl);

    [MenuItem("Guardian AR/Web Source/Use LOCALHOST :3000")]
    public static void UseLocalhost() => Apply(LocalUrl);

    [MenuItem("Guardian AR/Web Source/Use LAN (auto detect IP)")]
    public static void UseLanIp()
    {
        var ip = GetLocalIp();
        if (string.IsNullOrEmpty(ip))
        {
            EditorUtility.DisplayDialog("LAN", "LAN IP detection failed. Use Production or Localhost.", "OK");
            return;
        }
        Apply($"http://{ip}:3000");
    }

    static void Apply(string url)
    {
        int applied = 0;

        // 씬 내 모든 ApiManager / WebGameController에 적용
        foreach (var api in Object.FindObjectsByType<ApiManager>(FindObjectsSortMode.None))
        {
            SetField(api, "serverUrl", url);
            EditorUtility.SetDirty(api);
            applied++;
        }
        foreach (var web in Object.FindObjectsByType<WebGameController>(FindObjectsSortMode.None))
        {
            SetField(web, "webUrl", url);
            EditorUtility.SetDirty(web);
            applied++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[WebDevUrlSwitcher] Applied to {applied} component(s): {url}");
        EditorUtility.DisplayDialog("Web Source", $"URL: {url}\n\n적용된 컴포넌트: {applied}", "OK");
    }

    static string GetLocalIp()
    {
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                    var ip = addr.Address.ToString();
                    if (ip.StartsWith("127.")) continue;
                    if (!ip.StartsWith("192.168.") && !ip.StartsWith("10.") && !ip.StartsWith("172.")) continue;
                    return ip;
                }
            }
        }
        catch { }
        return null;
    }

    static void SetField(object target, string name, object value)
    {
        var t = target.GetType();
        while (t != null)
        {
            var f = t.GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (f != null) { f.SetValue(target, value); return; }
            t = t.BaseType;
        }
    }
}
