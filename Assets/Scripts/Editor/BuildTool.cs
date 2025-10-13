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

    //[MenuItem("Build Res/Android - Build AssetBundle for shared assets")]
    //static void AndroidBuildShared()
    //{
    //    string folderPath = EditorUtility.OpenFolderPanel("Select Shared Assets Folder", Application.dataPath, "");
    //    string outputPath = "Assets/AssetsPackage/Android/Shared";
    //    if (!Directory.Exists(outputPath))
    //    {
    //        Directory.CreateDirectory(outputPath);
    //    }

    //    string[] assetPaths = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
    //    .Where(path => !path.EndsWith(".meta") && !Directory.Exists(path))
    //    .ToArray();

    //    string projectPath = Application.dataPath.Replace("Assets", "");
    //    Debug.Log("projectPath: " + projectPath);
    //    foreach (string assetPath in assetPaths)
    //    {
    //        string relativePath = assetPath.Replace("\\", "/").Replace(projectPath, "");
    //        Debug.Log("relativePath: " + relativePath);
    //        string bundleName = GetBundleName(relativePath, "Assets/Maps/Shared/Prefabs Objects/");
    //        var importer = AssetImporter.GetAtPath(relativePath);
    //        Debug.Log("importer: " + importer);
    //        if (importer != null)
    //        {
    //            importer.assetBundleName = bundleName.ToLower();
    //        }
    //        else
    //        {
    //            Debug.LogWarning($"Không thể lấy AssetImporter cho asset: {relativePath}");
    //        }
    //    }

    //    string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
    //    Debug.Log("Bundle count: " + allBundleNames.Length);

    //    AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, BuildTarget.Android);
    //    Debug.Log("manifest: " + manifest);

    //}

    //static string GetBundleName(string fullPath, string rootPath)
    //{
    //    Debug.Log($"Full path: {fullPath}, Root path: {rootPath}");
    //    string relativePath = fullPath.Replace(rootPath, "").Replace("(Clone)", "");
    //    string withoutExt = Path.ChangeExtension(relativePath, null);
    //    Debug.Log($"Bundle name: {withoutExt.ToLower()}");
    //    return withoutExt;
    //}

    //[MenuItem("Build Res/Android - Build AssetBundle for selected map")]
    //static void AndroidBuildMap()
    //{
    //    string folderPath = EditorUtility.OpenFolderPanel("Select Map Folder", Application.dataPath, "");
    //    if (string.IsNullOrEmpty(folderPath)) return;

    //    string outputPath = "Assets/AssetsPackage/Android/Maps";
    //    if (!Directory.Exists(outputPath))
    //    {
    //        Directory.CreateDirectory(outputPath);
    //    }

    //    string[] assetPaths = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
    //        .Where(path => !path.EndsWith(".meta") && !Directory.Exists(path))
    //        .ToArray();

    //    foreach (string assetPath in assetPaths)
    //    {
    //        // Chuyển path tuyệt đối thành path tương đối với Assets
    //        string relativePath = "Assets" + assetPath.Replace(Application.dataPath, "").Replace("\\", "/");

    //        string bundleName = null;
    //        // Gán assetBundleName theo tên folder cha
    //        if (relativePath.Contains("atlastileshader"))
    //        {
    //            Debug.Log("Found shader: " + relativePath);
    //            string shaderName = Path.GetFileNameWithoutExtension(relativePath);
    //            bundleName = $"shader_{shaderName.ToLower()}";
    //        }
    //        else
    //        {
    //            // Mặc định bundle name theo cấu trúc thư mục
    //            bundleName = GetBundleName(relativePath, folderPath + "/");
    //            Debug.Log($"Default bundle name: {bundleName}");
    //        }
    //        Debug.Log($"bundle name: {bundleName}");
    //        var importer = AssetImporter.GetAtPath(relativePath);
    //        if (importer != null)
    //        {
    //            importer.assetBundleName = bundleName.ToLower();
    //            Debug.Log($"Set bundle name: {bundleName.ToLower()} for asset: {relativePath}");
    //        }
    //        else
    //        {
    //            Debug.LogWarning($"Không thể lấy AssetImporter cho asset: {relativePath}");
    //        }
    //    }

        //AssetDatabase.RemoveUnusedAssetBundleNames();

        // Build AssetBundle
    //    AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, BuildTarget.Android);
    //    //Debug.Log("Build manifest: " + (manifest != null ? "Success" : "Failed"));
    //    if (manifest != null)
    //    {
    //        string[] bundleNames = manifest.GetAllAssetBundles();
    //        foreach (string bundleName in bundleNames)
    //        {
    //            string oldPath = Path.Combine(outputPath, bundleName);
    //            string newPath = oldPath + ".unity3d";

    //            // Đổi tên nếu chưa có .unity3d
    //            if (File.Exists(oldPath) && !File.Exists(newPath))
    //            {
    //                File.Move(oldPath, newPath);
    //                //Debug.Log($"Renamed {bundleName} to {bundleName}.unity3d");
    //            }
    //        }
    //    }
    //}


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


    // build asset bundle từ 1 hoặc nhiều asset (không giới hạn GameObject)
    [MenuItem("Build Res/Android - Build AssetBundle from 1 or many asset")]
    static void AndroidBuildResource_AnyAsset()
    {
        // Lấy toàn bộ asset (không giới hạn GameObject)
        var selection = Selection.GetFiltered<Object>(SelectionMode.Assets)
                                 .Where(IsBuildableAsset)
                                 .Distinct()
                                 .ToArray();

        if (selection.Length == 0)
        {
            EditorUtility.DisplayDialog("Build AssetBundle", "Hãy chọn ít nhất 1 asset hợp lệ trong Project window (prefab, animation clip, controller, material...)", "Ok");
            return;
        }

        if (selection.Length == 1)
        {
            var asset = selection[0];
            string defaultName = SanitizeFileName(asset.name);
            string path = EditorUtility.SaveFilePanel("Save Resource", "", defaultName, "unity3d");
            if (string.IsNullOrEmpty(path)) return;

            BuildOneAssetWithDependencies(asset, path);
            EditorUtility.DisplayDialog("Done", $"Built: {Path.GetFileName(path)}", "Ok");
        }
        else
        {
            string folder = EditorUtility.SaveFolderPanel("Select Output Folder", "", "");
            if (string.IsNullOrEmpty(folder)) return;

            int success = 0, fail = 0;

            foreach (var asset in selection)
            {
                try
                {
                    string fileName = SanitizeFileName(asset.name) + ".unity3d";
                    string outPath = Path.Combine(folder, fileName);

                    // Xử lý trùng tên
                    int counter = 1;
                    while (File.Exists(outPath))
                    {
                        fileName = $"{SanitizeFileName(asset.name)}_{counter}.unity3d";
                        outPath = Path.Combine(folder, fileName);
                        counter++;
                    }

                    BuildOneAssetWithDependencies(asset, outPath);
                    success++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[AB] Fail: {asset.name} -> {ex.Message}");
                    fail++;
                }
            }

            EditorUtility.DisplayDialog("Done", $"Built {success} bundle(s), failed {fail}.", "Ok");
        }
    }

    static void BuildOneAssetWithDependencies(Object mainAsset, string outPath)
    {
        // Thu thập dependencies (kéo theo clip, texture, mesh, materials, controller…)
        Object[] deps = EditorUtility.CollectDependencies(new Object[] { mainAsset });

        // Giữ lại những asset thật sự thuộc Project, bỏ script, editor-only, null
        deps = deps.Where(o =>
        {
            if (o == null) return false;
            if (!AssetDatabase.Contains(o)) return false;

            // Loại script
            if (o is MonoScript) return false;

            // Loại folder (DefaultAsset có thể là folder)
            string p = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(p)) return false;
            if (AssetDatabase.IsValidFolder(p)) return false;

            // Loại asset trong thư mục Editor
            if (p.Contains("/Editor/")) return false;

            return true;
        }).ToArray();

        var options = BuildAssetBundleOptions.CompleteAssets;

        bool ok = BuildPipeline.BuildAssetBundle(
            mainAsset,   // main asset có thể là Prefab, AnimationClip, AnimatorController, Material, v.v.
            deps,
            outPath,
            options,
            BuildTarget.Android
        );

        if (!ok)
            throw new System.Exception("BuildAssetBundle failed: " + outPath);

        Debug.Log($"[AB] {mainAsset.name} -> {outPath} (deps: {deps.Length})");
    }

    static bool IsBuildableAsset(Object obj)
    {
        if (obj == null) return false;

        // Không build script hoặc asset ảo
        if (obj is MonoScript) return false;

        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return false;

        // Bỏ folder
        if (AssetDatabase.IsValidFolder(path)) return false;

        // Có thể bỏ luôn Editor-only nếu muốn
        if (path.Contains("/Editor/")) return false;

        // Còn lại: ok (Prefab, AnimationClip, AnimatorController, Material, Texture, FBX sub-asset, Shader, etc.)
        return true;
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
        // Chọn thư mục trong Project window (có thể chọn nhiều)
        var selected = Selection.GetFiltered<Object>(SelectionMode.Assets)
                                .Where(o => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o)))
                                .ToArray();
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Build AssetBundle", "Hãy chọn ít nhất 1 folder trong Project window.", "OK");
            return;
        }

        string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
        if (string.IsNullOrEmpty(outputFolder)) return;

        int total = 0, okCnt = 0, failCnt = 0;

        foreach (var sel in selected)
        {
            string selPath = AssetDatabase.GetAssetPath(sel);
            if (!AssetDatabase.IsValidFolder(selPath))
            {
                // Bỏ qua asset lẻ, chỉ nhận folder
                continue;
            }

            total++;
            string bundleName = SanitizeFileName(Path.GetFileName(selPath)) + ".unity3d";
            string outPath = Path.Combine(outputFolder, bundleName);

            try
            {
                // 1) Gom toàn bộ asset trong folder (đệ quy) + toàn bộ sub-assets (clip nằm trong FBX, v.v.)
                var allAssets = GetAllAssetsInFolderRecursive(selPath);

                if (allAssets.Count == 0)
                {
                    Debug.LogWarning($"[AB] Folder rỗng: {selPath}");
                    continue;
                }

                // 2) CollectDependencies trên TOÀN BỘ tập để kéo theo clip mà AnimatorController tham chiếu
                Object[] deps = EditorUtility.CollectDependencies(allAssets.ToArray());

                // 3) Lọc: chỉ giữ asset thật trong Project, bỏ script, folder, Editor-only
                deps = deps.Where(IsBuildableAsset).ToArray();

                // 4) Chọn 1 mainAsset (không quan trọng, vì ta đã cung cấp đầy đủ 'deps')
                Object mainAsset = deps.FirstOrDefault() ?? allAssets[0];

                // 5) Build
                var opts = BuildAssetBundleOptions.CompleteAssets;
                bool ok = BuildPipeline.BuildAssetBundle(mainAsset, deps, outPath, opts, BuildTarget.Android);
                if (!ok)
                    throw new System.Exception("BuildAssetBundle failed");

                Debug.Log($"[AB] Folder '{selPath}' -> {outPath} (assets: {allAssets.Count}, deps: {deps.Length})");
                okCnt++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AB][FAIL] {selPath} -> {ex.Message}");
                failCnt++;
            }
        }

        EditorUtility.DisplayDialog("Done", $"Built {okCnt}/{total} bundle(s). Failed: {failCnt}.", "OK");
    }

    // --- Helpers ---

    /// <summary>
    /// Lấy toàn bộ asset trong folder (đệ quy), bao gồm sub-assets trong cùng file (ví dụ FBX chứa nhiều AnimationClip).
    /// </summary>
    static List<Object> GetAllAssetsInFolderRecursive(string folderPath)
    {
        var results = new List<Object>();
        var guidArray = AssetDatabase.FindAssets("", new[] { folderPath }); // rỗng = lấy tất cả

        foreach (string guid in guidArray)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path)) continue; // bỏ folder

            // Main asset
            var main = AssetDatabase.LoadMainAssetAtPath(path);
            if (main != null && IsBuildableAsset(main))
                results.Add(main);

            // Sub-assets (AnimationClip trong FBX, materials embedded, v.v.)
            var subs = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var sub in subs)
            {
                if (sub != null && IsBuildableAsset(sub))
                    results.Add(sub);
            }
        }

        // Loại trùng
        return results.Distinct().ToList();
    }



    //[MenuItem("Build Res/Android - Build All AssetBundles inside selected folders")]
    //static void AndroidBuildAllResources()
    //{
    //    string outputFolder = EditorUtility.SaveFolderPanel("Save Resources", "Build - Android", "");
    //    if (!string.IsNullOrEmpty(outputFolder))
    //    {
    //        Object[] selectedAssets = Selection.objects;
    //        foreach (Object asset in selectedAssets)
    //        {
    //            Object[] subAssets = BuildTool.GetAssetsFromPath<Object>(AssetDatabase.GetAssetPath(asset));
    //            BuildPipeline.BuildAssetBundle(asset, subAssets, outputFolder + "/" + asset.name + ".unity3d", BuildAssetBundleOptions.CompleteAssets, BuildTarget.Android);
    //        }
    //    }
    //}

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
