using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LoadAsset : MonoBehaviour
{
    string rootBundlePath => Path.Combine(Application.dataPath, "AssetsPackage/Android/Maps");

    // Sự kiện để theo dõi tiến độ tải (có thể kết nối với loading bar)
    public static event Action<float> OnProgressUpdate;

    // Tải map theo dependencies async
    public IEnumerator LoadMapWithDependencies(string mapName)
    {
        string mapBundleName = "assets/maps/dali/prefabs/" + mapName.ToLower();

        // Load main manifest async
        yield return AssetBundleLoader.LoadManifestAsync(rootBundlePath + "/Maps");

        // Tải bundle đệ quy async
        yield return AssetBundleLoader.LoadAssetBundleRecursivelyAsync(mapBundleName, rootBundlePath);

        AssetBundle map = AssetBundleLoader.GetLoadedBundle(mapBundleName);
        if (map == null)
        {
            Debug.LogError("Failed to load map bundle: " + mapBundleName);
            yield break;
        }
        else
        {
            Debug.Log("Successfully loaded map bundle: " + mapBundleName);
        }

        // Load the map prefab async
        string mapNames = map.GetAllAssetNames()[0];
        AssetBundleRequest assetRequest = map.LoadAssetAsync<GameObject>(mapNames);
        yield return assetRequest;

        GameObject mapPrefab = assetRequest.asset as GameObject;
        if (mapPrefab == null)
        {
            Debug.LogError("Failed to load map prefab from bundle: " + mapBundleName);
            yield break;
        }
        else
        {
            Instantiate(mapPrefab);
            Debug.Log("Successfully loaded map prefab: " + mapNames);

            Shader tilemapShader = Shader.Find("Custom/TwoLayerOgreShaderNoWhite");
            Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
            bool found = false;
            foreach (var mat in allMaterials)
            {
                if (mat == null) continue;

                if (mat.name.ToLower().Contains("shared_material"))
                {
                    mat.shader = tilemapShader;
                    found = true;
                    Debug.Log("Gán lại shader cho material: " + mat.name);
                }
            }
            if (!found)
            {
                Debug.LogWarning("Không tìm thấy material nào có tên chứa 'Shared' để gán shader.");
            }
        }


    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadMapWithDependencies("dali"));
    }

    // Update is called once per frame
    void Update()
    {

    }
}

public static class AssetBundleLoader
{
    private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private static AssetBundleManifest manifest;
    private static int totalBundlesToLoad = 0; // Để tính tiến độ
    private static int loadedBundleCount = 0; // Để tính tiến độ

    public static IEnumerator LoadManifestAsync(string manifestBundlePath)
    {
        if (manifest != null) yield break;

        var manifestBundleRequest = AssetBundle.LoadFromFileAsync(manifestBundlePath);
        yield return manifestBundleRequest;

        AssetBundle manifestBundle = manifestBundleRequest.assetBundle;
        if (manifestBundle == null)
        {
            Debug.LogError("Failed to load AssetBundleManifest bundle.");
            yield break;
        }

        var manifestRequest = manifestBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
        yield return manifestRequest;

        manifest = manifestRequest.asset as AssetBundleManifest;
        if (manifest != null)
        {
            Debug.Log("Loaded manifest successfully.");
        }

        manifestBundle.Unload(false); // Giữ manifest trong bộ nhớ
    }

    public static IEnumerator LoadAssetBundleRecursivelyAsync(string bundleName, string basePath)
    {
        if (loadedBundles.ContainsKey(bundleName)) yield break;

        if (manifest == null)
        {
            Debug.LogError("Manifest is null! Call LoadManifestAsync() first.");
            yield break;
        }

        // Lấy dependencies và tính tổng số bundle cần tải (cho tiến độ)
        string[] dependencies = manifest.GetAllDependencies(bundleName);
        totalBundlesToLoad += dependencies.Length + 1; // +1 cho bundle chính

        // Tải dependencies đệ quy async
        foreach (var dep in dependencies)
        {
            yield return LoadAssetBundleRecursivelyAsync(dep, basePath);
        }

        // Tải bundle chính async
        string bundlePath = Path.Combine(basePath, bundleName + ".unity3d");
        if (!File.Exists(bundlePath))
        {
            Debug.LogError("Bundle not found: " + bundlePath);
            yield break;
        }

        var request = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return request;

        AssetBundle bundle = request.assetBundle;
        if (bundle == null)
        {
            Debug.LogError("Failed to load bundle: " + bundlePath);
            yield break;
        }

        loadedBundles[bundleName] = bundle;
        loadedBundleCount++;
        Debug.Log("Loaded bundle: " + bundleName);

        // Cập nhật tiến độ (0-1)
        float progress = (float)loadedBundleCount / totalBundlesToLoad;
        //OnProgressUpdate?.Invoke(progress);
    }

    public static AssetBundle GetLoadedBundle(string bundleName)
    {
        loadedBundles.TryGetValue(bundleName, out AssetBundle bundle);
        return bundle;
    }

    public static void UnloadAll(bool unloadAssets = false)
    {
        foreach (var kv in loadedBundles)
        {
            kv.Value.Unload(unloadAssets);
        }
        loadedBundles.Clear();
        manifest = null;
        totalBundlesToLoad = 0;
        loadedBundleCount = 0;
    }
}

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using UnityEngine;

//public class LoadAsset : MonoBehaviour
//{
//    string rootBundlePath => Path.Combine(Application.dataPath, "AssetsPackage/Android/Maps");

//    // Tải map theo dependencies
//    public IEnumerator LoadMapWithDependencies(string mapName)
//    {
//        string mapBundleName = "assets/maps/dali/prefabs/" + mapName.ToLower();

//        // Load main manifest
//        AssetBundleLoader.LoadManifest(rootBundlePath + "/Maps");

//        AssetBundle map = AssetBundleLoader.LoadAssetBundleRecursively(mapBundleName, rootBundlePath);

//        if (map == null)
//        {
//            Debug.LogError("Failed to load map bundle: " + mapBundleName);
//            yield break;
//        }
//        else
//        {
//            Debug.Log("Successfully loaded map bundle: " + mapBundleName);
//        }
//        // Load the map prefab
//        string mapNames = map.GetAllAssetNames()[0];
//        GameObject mapPrefab = map.LoadAsset<GameObject>(mapNames);
//        if (mapPrefab == null)
//        {
//            Debug.LogError("Failed to load map prefab from bundle: " + mapBundleName);
//            yield break;
//        }
//        else
//        {
//            Instantiate(mapPrefab);
//            Debug.Log("Successfully loaded map prefab: " + mapNames);
//        }
//        // manifestBundle.Unload(false); // Optionally unload manifest bundle
//    }

//    // Start is called before the first frame update
//    void Start()
//    {
//        StartCoroutine(LoadMapWithDependencies("dali"));
//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }
//}

//public static class AssetBundleLoader
//{
//    private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
//    private static AssetBundleManifest manifest;

//    public static void LoadManifest(string manifestBundlePath)
//    {
//        if (manifest == null)
//        {
//            AssetBundle manifestBundle = AssetBundle.LoadFromFile(manifestBundlePath);
//            if (manifestBundle == null)
//            {
//                Debug.LogError("Failed to load AssetBundleManifest bundle.");
//                return;
//            }
//            manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
//            Debug.Log("Loaded manifest successfully.");
//        }
//    }

//    public static AssetBundle LoadAssetBundleRecursively(string bundleName, string basePath)
//    {
//        if (loadedBundles.ContainsKey(bundleName))
//        {
//            return loadedBundles[bundleName];
//        }

//        if (manifest == null)
//        {
//            Debug.LogError("Manifest is null! Call LoadManifest() first.");
//            return null;
//        }

//        string[] dependencies = manifest.GetAllDependencies(bundleName);
//        foreach (var dep in dependencies)
//        {
//            if (!loadedBundles.ContainsKey(dep))
//            {
//                LoadAssetBundleRecursively(dep, basePath);
//            }
//        }

//        string bundlePath = Path.Combine(basePath, bundleName + ".unity3d");
//        if (!File.Exists(bundlePath))
//        {
//            Debug.LogError("Bundle not found: " + bundlePath);
//            return null;
//        }

//        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
//        if (bundle == null)
//        {
//            Debug.LogError("Failed to load bundle: " + bundlePath);
//            return null;
//        }

//        loadedBundles[bundleName] = bundle;
//        Debug.Log("Loaded bundle: " + bundleName);
//        return bundle;
//    }

//    public static void UnloadAll(bool unloadAssets = false)
//    {
//        foreach (var kv in loadedBundles)
//        {
//            kv.Value.Unload(unloadAssets);
//        }
//        loadedBundles.Clear();
//        manifest = null;
//    }
//}

