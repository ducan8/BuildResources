using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public class BuildTool : MonoBehaviour
{
    [MenuItem("Build Res/Change all Lua files into Text file")]
    static void ChangeLuaToTxt()
    {
        string path = EditorUtility.GetAssetPath(Selection.activeObject);
        foreach (string fileName in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
        {
            if (fileName.Contains(".meta"))
            {
                continue;
            }
            if (fileName.Contains(".txt"))
            {
                continue;
            }
            if (fileName.Contains(".lua"))
            {
                File.Move(fileName, fileName + ".txt");
            }
        }
    }

    /// <summary>
    /// Adds newly (if not already in the list) found assets.
    /// Returns how many found (not how many added)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="assetsFound">Adds to this list if it is not already there</param>
    /// <returns></returns>
    public static T[] GetAssetsFromPath<T>(string path) where T : Object
    {
        string[] filePaths = Directory.GetFiles(path);

        List<T> list = new List<T>();

        if (filePaths != null && filePaths.Length > 0)
        {
            for (int i = 0; i < filePaths.Length; i++)
            {
                /// Return if TPSheet file
                if (Path.GetExtension(filePaths[i]) == ".tpsheet")
                {
                    continue;
                }

                Object obj = AssetDatabase.LoadAssetAtPath(filePaths[i], typeof(T));
                if (obj is T asset)
                {
                    if (!list.Contains(asset))
                    {
                        list.Add(asset);
                    }
                }
            }
        }

        return list.ToArray();
    }

    #region Android
    [MenuItem("Build Res/Android - Change ETC2")]
    private static void AndroidChangeETC2()
    {
        ChangeTextureImportSettings.ChangeTextureFormat(TextureImporterFormat.ETC2_RGBA8Crunched, ChangeTextureImportSettings.Platform.Android);
    }

    [MenuItem("Build Res/Android - Build Video from selected")]
    static void AndroidBuildVideo()
    {
        // Bring up save panel
        var path = EditorUtility.SaveFilePanel("Save Resource", "", "New Resource", "unity3d");
        if (path.Length != 0)
        {
            // Build the resource file from the active selection.
            var selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
            BuildPipeline.BuildAssetBundle(Selection.activeObject, selection, path, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.Android);
        }
    }

    [MenuItem("Build Res/Android - Build All Videos inside selected folders")]
    static void AndroidBuildAllVideos()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Object[] selectedAssets = Selection.objects;
            foreach (Object asset in selectedAssets)
            {
                Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
                foreach (Object subAsset in subAssets)
                {
                    BuildPipeline.BuildAssetBundle(subAsset, null, outputFolder + "/" + subAsset.name + ".unity3d", BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.Android);
                }
            }
        }
    }

    [MenuItem("Build Res/Android - Build AssetBundle for shared assets")]
    static void AndroidBuildShared()
    {
        string folderPath = EditorUtility.OpenFolderPanel("Select Shared Assets Folder", Application.dataPath, "");
        string outputPath = "Assets/AssetsPackage/Android/Shared";
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        string[] assetPaths = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
        .Where(path => !path.EndsWith(".meta") && !Directory.Exists(path))
        .ToArray();

        string projectPath = Application.dataPath.Replace("Assets", "");
        Debug.Log("projectPath: " + projectPath);
        foreach (string assetPath in assetPaths)
        {
            string relativePath = assetPath.Replace("\\", "/").Replace(projectPath, "");
            Debug.Log("relativePath: " + relativePath);
            string bundleName = GetBundleName(relativePath, "Assets/Maps/Shared/Prefabs Objects/");
            var importer = AssetImporter.GetAtPath(relativePath);
            Debug.Log("importer: " + importer);
            if (importer != null)
            {
                importer.assetBundleName = bundleName.ToLower();
            }
            else
            {
                Debug.LogWarning($"Không thể lấy AssetImporter cho asset: {relativePath}");
            }
        }

        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        Debug.Log("Bundle count: " + allBundleNames.Length);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, BuildTarget.Android);
        Debug.Log("manifest: " + manifest);

    }

    static string GetBundleName(string fullPath, string rootPath)
    {
        Debug.Log($"Full path: {fullPath}, Root path: {rootPath}");
        string relativePath = fullPath.Replace(rootPath, "").Replace("(Clone)", "");
        string withoutExt = Path.ChangeExtension(relativePath, null);
        Debug.Log($"Bundle name: {withoutExt.ToLower()}");
        return withoutExt;
    }

    [MenuItem("Build Res/Android - Build AssetBundle for selected map")]
    static void AndroidBuildMap()
    {
        string folderPath = EditorUtility.OpenFolderPanel("Select Map Folder", Application.dataPath, "");
        if (string.IsNullOrEmpty(folderPath)) return;

        string outputPath = "Assets/AssetsPackage/Android/Maps";
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        string[] assetPaths = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".meta") && !Directory.Exists(path))
            .ToArray();

        foreach (string assetPath in assetPaths)
        {
            // Chuyển path tuyệt đối thành path tương đối với Assets
            string relativePath = "Assets" + assetPath.Replace(Application.dataPath, "").Replace("\\", "/");

            string bundleName = null;
            // Gán assetBundleName theo tên folder cha
            if (relativePath.Contains("atlastileshader"))
            {
                Debug.Log("Found shader: " + relativePath);
                string shaderName = Path.GetFileNameWithoutExtension(relativePath);
                bundleName = $"shader_{shaderName.ToLower()}";
            }
            else
            {
                // Mặc định bundle name theo cấu trúc thư mục
                bundleName = GetBundleName(relativePath, folderPath + "/");
                Debug.Log($"Default bundle name: {bundleName}");
            }
            Debug.Log($"bundle name: {bundleName}");
            var importer = AssetImporter.GetAtPath(relativePath);
            if (importer != null)
            {
                importer.assetBundleName = bundleName.ToLower();
                Debug.Log($"Set bundle name: {bundleName.ToLower()} for asset: {relativePath}");
            }
            else
            {
                Debug.LogWarning($"Không thể lấy AssetImporter cho asset: {relativePath}");
            }
        }

        //AssetDatabase.RemoveUnusedAssetBundleNames();

        // Build AssetBundle
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, BuildTarget.Android);
        //Debug.Log("Build manifest: " + (manifest != null ? "Success" : "Failed"));
        if (manifest != null)
        {
            string[] bundleNames = manifest.GetAllAssetBundles();
            foreach (string bundleName in bundleNames)
            {
                string oldPath = Path.Combine(outputPath, bundleName);
                string newPath = oldPath + ".unity3d";

                // Đổi tên nếu chưa có .unity3d
                if (File.Exists(oldPath) && !File.Exists(newPath))
                {
                    File.Move(oldPath, newPath);
                    //Debug.Log($"Renamed {bundleName} to {bundleName}.unity3d");
                }
            }
        }
    }


    [MenuItem("Build Res/Android - Build AssetBundle from selected")]
    static void AndroidBuildResource()
    {
        // Bring up save panel
        //var path = EditorUtility.SaveFilePanel("Save Resource", "", "New Resource", "unity3d");
        //if (path.Length != 0)
        //{
        //    // Build the resource file from the active selection.
        //    var selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        //    BuildPipeline.BuildAssetBundle(Selection.activeObject, selection, path, BuildAssetBundleOptions.CompleteAssets, BuildTarget.Android);
        //}

        // Bring up save panel
        var path = EditorUtility.SaveFilePanel("Save Resource", "", "New Resource", "unity3d");
        if (path.Length != 0)
        {
            // Build the resource file from the active selection.
            var selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);

            // Check if selection contains prefabs, then include dependencies
            bool containsPrefabs = false;
            foreach (Object obj in selection)
            {
                if (obj is GameObject go && PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                {
                    containsPrefabs = true;
                    break;
                }
            }

            Object[] finalSelection = selection;
            if (containsPrefabs)
            {
                // Include all dependencies to ensure meshes from .fbx are included
                finalSelection = EditorUtility.CollectDependencies(selection);
                Debug.Log($"Building with {finalSelection.Length} assets including dependencies");
            }

            BuildPipeline.BuildAssetBundle(Selection.activeObject, finalSelection, path, BuildAssetBundleOptions.CompleteAssets, BuildTarget.Android);
        }
    }


    [MenuItem("Build Res/Android - Build AssetBundle from one or many selected prefab")]
    static void AndroidBuildResources()
    {
        // Lọc ra GameObject là prefab (bỏ asset khác)
        var selectedPrefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets)
            .Where(go => PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
            .Distinct()
            .ToArray();

        if (selectedPrefabs.Length == 0)
        {
            EditorUtility.DisplayDialog("Build AssetBundle", "Hãy chọn ít nhất 1 prefab trong Project window.", "Ok");
            return;
        }

        if (selectedPrefabs.Length == 1)
        {
            var prefab = selectedPrefabs[0];
            string defaultName = SanitizeFileName(prefab.name);
            string path = EditorUtility.SaveFilePanel(
                "Save Resource",
                "",
                defaultName,
                "unity3d"
            );

            if (string.IsNullOrEmpty(path)) return;

            BuildOnePrefabWithDependencies(prefab, path);
            EditorUtility.DisplayDialog("Done", $"Built: {Path.GetFileName(path)}", "Ok");
        }
        else
        {
            // Nhiều prefab → chọn thư mục đích, mỗi prefab một file
            string folder = EditorUtility.SaveFolderPanel("Select Output Folder", "", "");
            if (string.IsNullOrEmpty(folder)) return;

            int success = 0, fail = 0;

            foreach (var prefab in selectedPrefabs)
            {
                try
                {
                    string fileName = SanitizeFileName(prefab.name) + ".unity3d";
                    string outPath = Path.Combine(folder, fileName);

                    // Nếu trùng tên, thêm số đếm
                    int counter = 1;
                    while (File.Exists(outPath))
                    {
                        fileName = $"{SanitizeFileName(prefab.name)}_{counter}.unity3d";
                        outPath = Path.Combine(folder, fileName);
                        counter++;
                    }

                    BuildOnePrefabWithDependencies(prefab, outPath);
                    success++;
                }
                catch
                {
                    fail++;
                }
            }

            EditorUtility.DisplayDialog("Done", $"Built {success} bundle(s), failed {fail}.", "Ok");
        }
    }

    static void BuildOnePrefabWithDependencies(GameObject prefab, string outPath)
    {
        // Thu thập dependencies để chắc chắn kéo theo mesh/material/texture/anim...
        // LƯU Ý: CollectDependencies sẽ trả về cả prefab, nhưng không sao
        Object[] deps = EditorUtility.CollectDependencies(new Object[] { prefab });

        // Một số project muốn loại bớt Editor-only assets:
        deps = deps.Where(o => AssetDatabase.Contains(o)).ToArray();

        // Tuỳ dự án bạn có thể thêm BuildAssetBundleOptions khác:
        var options = BuildAssetBundleOptions.CompleteAssets;

        // main asset = prefab, assets[] = deps
        bool ok = BuildPipeline.BuildAssetBundle(
            prefab,
            deps,
            outPath,
            options,
            BuildTarget.Android
        );

        if (!ok)
        {
            throw new System.Exception("BuildAssetBundle failed: " + outPath);
        }
        else
        {
            Debug.Log($"[AB] {prefab.name} -> {outPath} (deps: {deps.Length})");
        }
    }

    static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }



[MenuItem("Build Res/Android - Build All AssetBundles inside selected folders")]
    static void AndroidBuildAllResources()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Object[] selectedAssets = Selection.objects;
            foreach (Object asset in selectedAssets)
            {
                Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
                BuildPipeline.BuildAssetBundle(asset, subAssets, outputFolder + "/" + asset.name + ".unity3d", BuildAssetBundleOptions.CompleteAssets, BuildTarget.Android);
            }
        }
    }

    [MenuItem("Build Res/Android - Build All Assets inside selected folders individually (For build MAP)")]
    static void AndroidBuildAllResourcesInsideFolderIndividually()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Object[] selectedAssets = Selection.objects;
            foreach (Object asset in selectedAssets)
            {
                Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
                foreach (Object subAsset in subAssets)
                {
                    BuildPipeline.BuildAssetBundle(subAsset, null, outputFolder + "/" + subAsset.name + ".unity3d", BuildAssetBundleOptions.CompleteAssets, BuildTarget.Android);
                }
            }
        }
    }
    #endregion

    #region IOS
    [MenuItem("Build Res/IOS - Change ETC2")]
    private static void IOSChangeETC2()
    {
        ChangeTextureImportSettings.ChangeTextureFormat(TextureImporterFormat.ETC2_RGBA8Crunched, ChangeTextureImportSettings.Platform.iPhone);
    }

    [MenuItem("Build Res/IOS - Build Video from selected")]
    static void IOSBuildVideo()
    {
        // Bring up save panel
        var path = EditorUtility.SaveFilePanel("Save Resource", "", "New Resource", "unity3d");
        if (path.Length != 0)
        {
            // Build the resource file from the active selection.
            var selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
            BuildPipeline.BuildAssetBundle(Selection.activeObject, selection, path, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.iOS);
        }
    }

    [MenuItem("Build Res/IOS - Build All Videos inside selected folders")]
    static void IOSBuildAllVideos()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Object[] selectedAssets = Selection.objects;
            foreach (Object asset in selectedAssets)
            {
                Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
                foreach (Object subAsset in subAssets)
                {
                    BuildPipeline.BuildAssetBundle(subAsset, null, outputFolder + "/" + subAsset.name + ".unity3d", BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.iOS);
                }
            }
        }
    }

    [MenuItem("Build Res/IOS - Build AssetBundle from selected")]
    static void IOSBuildResource()
    {
        // Bring up save panel
        var path = EditorUtility.SaveFilePanel("Save Resource", "", "New Resource", "unity3d");
        if (path.Length != 0)
        {
            // Build the resource file from the active selection.
            var selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
            BuildPipeline.BuildAssetBundle(Selection.activeObject, selection, path, BuildAssetBundleOptions.CompleteAssets, BuildTarget.iOS);
        }
    }

    [MenuItem("Build Res/IOS - Build All AssetBundles inside selected folders")]
    static void IOSBuildAllResources()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Object[] selectedAssets = Selection.objects;
            foreach (Object asset in selectedAssets)
            {
                Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
                BuildPipeline.BuildAssetBundle(asset, subAssets, outputFolder + "/" + asset.name + ".unity3d", BuildAssetBundleOptions.CompleteAssets, BuildTarget.iOS);
            }
        }
    }

    [MenuItem("Build Res/IOS - Build All Assets inside selected folders individually (For build MAP)")]
    static void IOSBuildAllResourcesInsideFolderIndividually()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Object[] selectedAssets = Selection.objects;
            foreach (Object asset in selectedAssets)
            {
                Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
                foreach (Object subAsset in subAssets)
                {
                    BuildPipeline.BuildAssetBundle(subAsset, null, outputFolder + "/" + subAsset.name + ".unity3d", BuildAssetBundleOptions.CompleteAssets, BuildTarget.iOS);
                }
            }
        }
    }
    #endregion

    #region Standalone
    [MenuItem("Build Res/Windows - Build Video from selected")]
    static void WindowsBuildVideo()
    {
        // Bring up save panel
        var path = EditorUtility.SaveFilePanel("Save Resource", "", "New Resource", "unity3d");
        if (path.Length != 0)
        {
            // Build the resource file from the active selection.
            var selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
            BuildPipeline.BuildAssetBundle(Selection.activeObject, selection, path, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows);
        }
    }

    [MenuItem("Build Res/Windows - Build All Videos inside selected folders")]
    static void WindowsBuildAllVideos()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Object[] selectedAssets = Selection.objects;
            foreach (Object asset in selectedAssets)
            {
                Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
                foreach (Object subAsset in subAssets)
                {
                    BuildPipeline.BuildAssetBundle(subAsset, null, outputFolder + "/" + subAsset.name + ".unity3d", BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows);
                }
            }
        }
    }

    [MenuItem("Build Res/Windows - Build AssetBundle from selected")]
    static void WindowsBuildResource()
    {
        // Bring up save panel
        var path = EditorUtility.SaveFilePanel("Save Resource", "", "New Resource", "unity3d");
        if (path.Length != 0)
        {
            // Build the resource file from the active selection.
            var selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
            BuildPipeline.BuildAssetBundle(Selection.activeObject, selection, path, BuildAssetBundleOptions.CompleteAssets, BuildTarget.StandaloneWindows);
        }
    }

    [MenuItem("Build Res/Windows - Build All AssetBundles inside selected folders")]
    static void WindowsBuildAllResources()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Object[] selectedAssets = Selection.objects;
            foreach (Object asset in selectedAssets)
            {
                Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
                BuildPipeline.BuildAssetBundle(asset, subAssets, outputFolder + "/" + asset.name + ".unity3d", BuildAssetBundleOptions.CompleteAssets, BuildTarget.StandaloneWindows);
            }
        }
    }

    [MenuItem("Build Res/Windows - Build All Assets inside selected folders individually (For build MAP)")]
    static void WindowsBuildAllResourcesInsideFolderIndividually()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (!string.IsNullOrEmpty(outputFolder))
        {
            Object[] selectedAssets = Selection.objects;
            foreach (Object asset in selectedAssets)
            {
                Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
                foreach (Object subAsset in subAssets)
                {
                    BuildPipeline.BuildAssetBundle(subAsset, null, outputFolder + "/" + subAsset.name + ".unity3d", BuildAssetBundleOptions.CompleteAssets, BuildTarget.StandaloneWindows);
                }
            }
        }
    }
    #endregion
}
