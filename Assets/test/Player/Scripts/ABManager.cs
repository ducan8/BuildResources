using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ABManager : MonoBehaviour
{
    public static ABManager Instance;

    void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    readonly Dictionary<string, AssetBundle> _bundles = new();
    readonly Dictionary<string, int> _ref = new();

    public async Task<AssetBundle> LoadBundle(string url)
    {
        Debug.Log($"[ABManager] Loading bundle: {url}");

        if (_bundles.TryGetValue(url, out var cached))
        {
            _ref[url]++;
            Debug.Log($"[ABManager] Using cached bundle: {url}");
            return cached;
        }

        var req = UnityWebRequestAssetBundle.GetAssetBundle(url);
        var op = req.SendWebRequest();
        while (!op.isDone)
            await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ABManager] Failed to load bundle from {url}: {req.error}");
            return null;
        }

        var ab = DownloadHandlerAssetBundle.GetContent(req);
        if (!ab)
        {
            Debug.LogError($"[ABManager] Null AssetBundle from {url}");
            return null;
        }

        _bundles[url] = ab;
        _ref[url] = 1;

        // Log toàn bộ asset names trong bundle
        Debug.Log($"[ABManager] --- AssetBundle Loaded: {url} ---");
        var assets = ab.GetAllAssetNames();
        for (int i = 0; i < assets.Length; i++)
        {
            Debug.Log($"    [{i}] {assets[i]}");
        }
        Debug.Log($"[ABManager] --- End of list ({assets.Length} assets) ---");

        return ab;
    }

    public void Release(string url)
    {
        if (!_bundles.ContainsKey(url)) return;

        _ref[url]--;
        if (_ref[url] <= 0)
        {
            Debug.Log($"[ABManager] Unloading bundle: {url}");
            _bundles[url].Unload(false);
            _bundles.Remove(url);
            _ref.Remove(url);
        }
        else
        {
            Debug.Log($"[ABManager] Release ref: {url} ({_ref[url]} remaining)");
        }
    }

    public async Task<GameObject> LoadMainPrefab(string url)
    {
        var ab = await LoadBundle(url);
        if (!ab) return null;

        var names = ab.GetAllAssetNames();
        if (names.Length == 0)
        {
            Debug.LogWarning($"[ABManager] No assets found in bundle: {url}");
            return null;
        }

        var prefab = ab.LoadAsset<GameObject>(names[0]);
        Debug.Log($"[ABManager] Loaded main prefab: {names[0]}");
        return prefab;
    }

    // 🔹 Load cả prefab và controller, sau đó gán controller cho Animator
    public async Task<GameObject> LoadCharacterWithController(string prefabUrl, string controllerUrl)
    {
        // Load prefab bundle
        var prefabBundle = await LoadBundle(prefabUrl);
        if (!prefabBundle)
        {
            Debug.LogError($"[ABManager] Failed to load prefab bundle: {prefabUrl}");
            return null;
        }

        // Load controller bundle
        var controllerBundle = await LoadBundle(controllerUrl);
        if (!controllerBundle)
        {
            Debug.LogError($"[ABManager] Failed to load controller bundle: {controllerUrl}");
            return null;
        }

        // Lấy prefab
        var prefabNames = prefabBundle.GetAllAssetNames();
        if (prefabNames.Length == 0)
        {
            Debug.LogError($"[ABManager] Prefab bundle empty: {prefabUrl}");
            return null;
        }

        var prefab = prefabBundle.LoadAsset<GameObject>(prefabNames[0]);
        if (!prefab)
        {
            Debug.LogError($"[ABManager] Prefab not found in bundle: {prefabUrl}");
            return null;
        }

        // Lấy controller
        RuntimeAnimatorController controller = null;
        foreach (var name in controllerBundle.GetAllAssetNames())
        {
            if (name.EndsWith(".controller"))
            {
                controller = controllerBundle.LoadAsset<RuntimeAnimatorController>(name);
                break;
            }
        }

        if (controller == null)
        {
            Debug.LogError($"[ABManager] No controller found in bundle: {controllerUrl}");
            return null;
        }

        // Instantiate và gán controller
        var instance = Instantiate(prefab);
        var animator = instance.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[ABManager] Prefab has no Animator component.");
        }
        else
        {
            // Delay 1 frame để animator ổn định
            await Task.Yield();
            animator.runtimeAnimatorController = controller;
            Debug.Log($"[ABManager] Controller '{controller.name}' applied to {instance.name}");
        }

        return instance;
    }
}
