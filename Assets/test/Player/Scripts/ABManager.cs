using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ABManager : MonoBehaviour
{
    public static ABManager Instance;
    void Awake() { if (!Instance) { Instance = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }

    readonly Dictionary<string, AssetBundle> _bundles = new();
    readonly Dictionary<string, int> _ref = new();

    public async Task<AssetBundle> LoadBundle(string url)
    {
        Debug.Log($"Loading bundle: {url}");
        if (_bundles.TryGetValue(url, out var cached)) { _ref[url]++; return cached; }
        var req = UnityWebRequestAssetBundle.GetAssetBundle(url);
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();
        if (req.result != UnityWebRequest.Result.Success) { Debug.LogError(req.error); return null; }
        var ab = DownloadHandlerAssetBundle.GetContent(req);
        if (!ab) { Debug.LogError($"Failed to load bundle from {url}"); return null; }
       
        _bundles[url] = ab; _ref[url] = 1;

        foreach(var bundle in _bundles.Values ) {
            Debug.Log("Cached bundle: " + bundle);
        }
        return ab;
    }

    public void Release(string url)
    {
        if (!_bundles.ContainsKey(url)) return;
        _ref[url]--;
        if (_ref[url] <= 0)
        {
            _bundles[url].Unload(false);
            _bundles.Remove(url);
            _ref.Remove(url);
        }
    }

    public async Task<GameObject> LoadMainPrefab(string url)
    {
        var ab = await LoadBundle(url);
        if (!ab) return null;
        var prefab = ab.LoadAsset<GameObject>(ab.GetAllAssetNames()[0]); // hoặc tên cụ thể
        return prefab;
    }
}
