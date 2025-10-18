using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


public class MyTools : EditorWindow
{
    string fbxFolder = "Assets\\test\\Player\\general"; // thư mục chứa FBX có clip
    string outFolder = "Assets\\test\\Player\\general\\Animations Clip";
    string animBundleName = "characters/base/anims/locomotion";
    bool includeSubFolders = true;
    bool setLoopIfNameMatches = true; // Idle/Walk/Run → loop

    [MenuItem("Tools/Animation/Extract Clips From FBX (Batch)")]
    static void ShowWin() => GetWindow<MyTools>("Extract FBX Clips");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Extract animation clips from FBX into standalone .anim", EditorStyles.wordWrappedLabel);
        fbxFolder = EditorGUILayout.TextField("FBX Folder", fbxFolder);
        outFolder = EditorGUILayout.TextField("Output Folder", outFolder);
        includeSubFolders = EditorGUILayout.Toggle("Include Subfolders", includeSubFolders);
        animBundleName = EditorGUILayout.TextField("Anim Bundle Name", animBundleName);
        setLoopIfNameMatches = EditorGUILayout.Toggle("Auto Loop Idle/Walk/Run", setLoopIfNameMatches);

        if (GUILayout.Button("Extract Now")) Extract();
    }

    void Extract()
    {
        if (!Directory.Exists(fbxFolder)) { Debug.LogError("FBX folder not found: " + fbxFolder); return; }
        if (!Directory.Exists(outFolder)) Directory.CreateDirectory(outFolder);

        var files = Directory.GetFiles(fbxFolder, "*.fbx",
            includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        int made = 0;
        foreach (var file in files)
        {
            var fbxPath = file.Replace("\\", "/");
            var all = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in all)
            {
                var clip = a as AnimationClip;
                if (!clip) continue;
                // Bỏ clip preview nội bộ Unity
                if (clip.name.StartsWith("__preview__")) continue;

                // Tạo .anim độc lập và copy dữ liệu
                var newClip = new AnimationClip();
                EditorUtility.CopySerialized(clip, newClip);

                // Tùy chọn set loop theo tên
                if (setLoopIfNameMatches)
                {
                    var lower = clip.name.ToLower();
                    if (lower.Contains("idle") || lower.Contains("loop") || lower.Contains("walk") || lower.Contains("run"))
                    {
                        SetClipLoop(newClip, true);
                    }
                }

                var safeName = Sanitize(clip.name);
                var outPath = $"{outFolder}/{safeName}.anim";
                outPath = AssetDatabase.GenerateUniqueAssetPath(outPath);
                AssetDatabase.CreateAsset(newClip, outPath);

                // Gán bundle name riêng cho clip → không bị gom vào prefab
                var imp = AssetImporter.GetAtPath(outPath);
                imp.assetBundleName = animBundleName;
                imp.SaveAndReimport();

                made++;
                Debug.Log($"Extracted {clip.name} → {outPath}");
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ Extracted {made} animation clips.");
    }

    static void SetClipLoop(AnimationClip clip, bool loop)
    {
        var so = new SerializedObject(clip);
        var settings = so.FindProperty("m_AnimationClipSettings");
        if (settings != null)
        {
            settings.FindPropertyRelative("m_LoopTime").boolValue = loop;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(clip);
    }

    static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

   


}



[System.Serializable] class BonePack { public string prefab; public SmrEntry[] smrs; }
[System.Serializable] class SmrEntry { public string name; public string root; public string[] bones; }


public class SkinnedPartExtractor : EditorWindow
{
    string fbxPartFolder = "Assets\\Characters\\Woman\\dress\\";
    string outPrefabPartFolder = "Assets\\Characters\\Woman\\dress\\";
    string outMatFolder = "Assets\\Characters\\Woman\\dress\\";
    string bundlePrefix = "characters/woman/"; // ví dụ: characters/woman/dress/dress01
    bool includeSub = true;
    bool duplicateMeshAsset = false; // bật nếu muốn tách mesh khỏi FBX

    [MenuItem("Tools/Characters/Extract SMR Part Prefabs")]
    static void ShowWin() => GetWindow<SkinnedPartExtractor>("Extract SMR Parts");

    void OnGUI()
    {
        fbxPartFolder = EditorGUILayout.TextField("FBX Folder", fbxPartFolder);
        outPrefabPartFolder = EditorGUILayout.TextField("Output Prefab Folder", outPrefabPartFolder);
        outMatFolder = EditorGUILayout.TextField("Output Material Folder", outMatFolder);
        bundlePrefix = EditorGUILayout.TextField("Bundle Prefix", bundlePrefix);
        includeSub = EditorGUILayout.Toggle("Include Subfolders", includeSub);
        duplicateMeshAsset = EditorGUILayout.Toggle("Duplicate Mesh Asset", duplicateMeshAsset);

        if (GUILayout.Button("Extract Now")) ExtractAll();
    }

    void ExtractAll()
    {
        if (!Directory.Exists(fbxPartFolder)) { Debug.LogError("FBX folder not found: " + fbxPartFolder); return; }
        Directory.CreateDirectory(outPrefabPartFolder);
        Directory.CreateDirectory(outMatFolder);

        var files = Directory.GetFiles(fbxPartFolder, "*.fbx",
            includeSub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        int made = 0;
        foreach (var fbxFile in files)
        {
            var fbxPath = fbxFile.Replace("\\", "/");
            var fbxGo = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (!fbxGo) { Debug.LogWarning("Cannot load FBX: " + fbxPath); continue; }

            var folderName = new DirectoryInfo(Path.GetDirectoryName(fbxFile)).Name;
            var partName = folderName;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxGo);
            var smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (smrs.Length == 0) { DestroyImmediate(instance); continue; }

            var root = new GameObject(partName);
            var partMatFolder = $"{outMatFolder}/{partName}";
            Directory.CreateDirectory(partMatFolder);

            var entries = new List<SmrEntry>();
            foreach (var src in smrs)
            {
                var child = new GameObject(src.gameObject.name);
                child.transform.SetParent(root.transform, false);

                var dst = child.AddComponent<SkinnedMeshRenderer>();

                // ===== Mesh handling =====
                if (!duplicateMeshAsset)
                {
                    // Giữ mesh trong FBX
                    dst.sharedMesh = src.sharedMesh;
                }
                else
                {
                    // CHANGED: Giữ nguyên tên mesh, KHÔNG thêm tiền tố partName_
                    var meshCopy = UnityEngine.Object.Instantiate(src.sharedMesh);
                    meshCopy.name = src.sharedMesh.name; // giữ đúng tên gốc

                    // Lưu file .asset dùng đúng tên mesh (đã sanitize), không prefix
                    var meshPath = $"{fbxPartFolder}/{Sanitize(src.sharedMesh.name)}.asset"; // CHANGED
                    meshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);                // tránh trùng → thêm hậu tố tự động
                    AssetDatabase.CreateAsset(meshCopy, meshPath);
                    dst.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                }

                // ===== Materials duplicate (không đổi tên, không prefix) =====
                var srcMats = src.sharedMaterials;
                var newMats = new Material[srcMats.Length];
                for (int i = 0; i < srcMats.Length; i++)
                {
                    var sm = srcMats[i];
                    if (!sm) continue;
                    var matCopy = new Material(sm.shader);
                    matCopy.CopyPropertiesFromMaterial(sm);
                    var matPath = $"{partMatFolder}/{Sanitize(sm.name)}.mat"; // đã giữ tên gốc (sanitize), không prefix
                    matPath = AssetDatabase.GenerateUniqueAssetPath(matPath);
                    AssetDatabase.CreateAsset(matCopy, matPath);
                    newMats[i] = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                }
                dst.sharedMaterials = newMats;

                // ===== Lưu tên xương & clear bone refs =====
                //var bn = child.AddComponent<BoneNameMap>();
                //bn.rootBoneName = src.rootBone ? src.rootBone.name : "Hips";
                //bn.boneNames = src.bones.Select(b => b ? b.name : "").ToArray();

                //// đảm bảo serialize
                //Undo.RegisterCreatedObjectUndo(child, "create part");
                //EditorUtility.SetDirty(bn);
                //EditorUtility.SetDirty(child);


                // 1) GHI LẠI THÔNG TIN XƯƠNG TỪ SMR GỐC (src)
                entries.Add(new SmrEntry
                {
                    name = child.name,                                         // TÊN OBJECT chứa SMR mới (để khớp)
                    root = src.rootBone ? src.rootBone.name : "w_Bip01",       // hoặc "Hips"/"root"
                    bones = src.bones.Select(b => b ? b.name : "").ToArray()
                });

                // 2) CLEAR THAM CHIẾU XƯƠNG Ở SMR MỚI (dst) ĐỂ PART KHÔNG DÍNH FBX
                dst.rootBone = null;
                dst.bones = new Transform[0];

                dst.updateWhenOffscreen = false;
                dst.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
                dst.skinnedMotionVectors = true;
            }

            var pack = new BonePack { prefab = partName, smrs = entries.ToArray() };
            var json = JsonUtility.ToJson(pack, true);

            var prefabPath = $"{outPrefabPartFolder}/{partName}.prefab";
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            // LƯU JSON CẠNH PREFAB
            var jsonPath = prefabPath.Replace(".prefab", ".bones.json");
            System.IO.File.WriteAllText(jsonPath, json);
            AssetDatabase.ImportAsset(jsonPath);                                // để Unity nhận TextAsset

            var importer = AssetImporter.GetAtPath(prefabPath);
            var ab = GuessBundleNameFromPath(prefabPath, bundlePrefix);
            importer.assetBundleName = ab;
            importer.SaveAndReimport();

            DestroyImmediate(root);
            DestroyImmediate(instance);
            made++;
            Debug.Log($"✅ Part prefab: {prefabPath}  (bundle: {ab})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Done. Created {made} part prefabs.");
    }

    static string GuessBundleNameFromPath(string prefabPath, string prefix)
    {
        var lower = prefabPath.ToLower();
        string type = lower.Contains("/dress/") ? "dress" :
                      lower.Contains("/face/") ? "face" :
                      lower.Contains("/hair/") ? "hair" : "parts";
        var folder = new DirectoryInfo(Path.GetDirectoryName(prefabPath)).Name.ToLower();
        return $"{prefix}/{type}/{folder}";
    }

    static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }
}



[CreateAssetMenu(menuName = "TL/Dali/WCollisionData")]
public class WCollisionData : ScriptableObject
{
    public Dictionary<Vector2Int, List<WcTri>> cells = new();
}
[System.Serializable] public struct WcTri { public Vector3 v1, v2, v3; }

public static class DaliWCollisionImporter
{
    [MenuItem("TL/Dali/Import WCollision")]
    public static void Import()
    {
        string path = EditorUtility.OpenFilePanel("dali.WCollision", "", "");
        if (string.IsNullOrEmpty(path)) return;

        var data = ScriptableObject.CreateInstance<WCollisionData>();
        using var br = new BinaryReader(File.OpenRead(path));
        uint version = br.ReadUInt32();        // theo BuildingCollisionMng.cpp
        int posCount = br.ReadInt32();
        for (int i = 0; i < posCount; i++)
        {
            int ix = br.ReadInt32(); int iz = br.ReadInt32();
            int triCount = br.ReadInt32();
            var key = new Vector2Int(ix, iz);
            var list = new List<WcTri>(triCount);
            for (int t = 0; t < triCount; t++)
            {
                var tri = new WcTri
                {
                    v1 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    v2 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                    v3 = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()),
                };
                list.Add(tri);
            }
            data.cells[key] = list;
        }
        AssetDatabase.CreateAsset(data, "Assets/Maps/dali/Dali_WCollision.asset");
        AssetDatabase.SaveAssets();
        Debug.Log($"WCollision imported: {data.cells.Count} cells");
    }
}

[CreateAssetMenu(menuName = "TL/Dali/RegionData")]
public class RegionData : ScriptableObject
{
    public List<Reg> regions = new();
}
[System.Serializable]
public class Reg
{
    public int id;
    public int passLevel;
    public Vector2[] pts; // theo thứ tự đỉnh trong file
}

public static class DaliRegionImporter
{
    [MenuItem("TL/Dali/Import Region")]
    public static void Import()
    {
        string path = EditorUtility.OpenFilePanel("dali.Region", "", "");
        if (string.IsNullOrEmpty(path)) return;

        var data = ScriptableObject.CreateInstance<RegionData>();
        using var br = new BinaryReader(File.OpenRead(path));
        int version = br.ReadInt32();               // theo Scene/Region.cpp
        int count = br.ReadInt32();                 // số region
        for (int i = 0; i < count; i++)
        {
            int id = br.ReadInt32();
            int pass = br.ReadInt32();
            int n = br.ReadInt32();
            var pts = new Vector2[n];
            for (int k = 0; k < n; k++) pts[k] = new Vector2(br.ReadSingle(), br.ReadSingle());
            data.regions.Add(new Reg { id = id, passLevel = pass, pts = pts });
        }
        AssetDatabase.CreateAsset(data, "Assets/Maps/dali/Dali_Region.asset");
        AssetDatabase.SaveAssets();
        Debug.Log($"Region imported: {data.regions.Count} polygons");
    }
}