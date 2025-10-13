using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable] class BonePack { public string prefab; public SmrEntry[] smrs; }
[System.Serializable] class SmrEntry { public string name; public string root; public string[] bones; }


public static class SkinnedPartBinder
{
    public static GameObject Attach(GameObject rigRoot, GameObject partPrefab)
    {
        var inst = UnityEngine.Object.Instantiate(partPrefab);
        inst.transform.SetParent(rigRoot.transform, false);

        //Debug.Log("prefab chứa " + inst.transform.GetChild(0).GetComponent<BoneNameMap>().rootBoneName);

        var bonesByName = rigRoot.GetComponentsInChildren<Transform>(true)
                                 .GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());

        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var map = smr.GetComponent<BoneNameMap>();
            Transform root = rigRoot.transform;
            if (map && !string.IsNullOrEmpty(map.rootBoneName) && bonesByName.TryGetValue(map.rootBoneName, out var rb))
                root = rb;

            Debug.Log("gắn rootBone: " + root.name + " cho " + smr.name);
            smr.rootBone = root;

            if (map?.boneNames != null && map.boneNames.Length > 0)
            {
                var newBones = new Transform[map.boneNames.Length];
                for (int i = 0; i < newBones.Length; i++)
                    newBones[i] = bonesByName.TryGetValue(map.boneNames[i], out var t) ? t : root;
                smr.bones = newBones;
            }
        }
        return inst;
    }

    // Gắn prefab part (chỉ có SMR) vào rig và rebind bones theo tên.
    // preferredRootBoneNames: ưu tiên theo thứ tự (ví dụ "w_Bip01", "Hips", "root")
    public static GameObject AttachAndRebind(GameObject rigRoot, AssetBundle bundle, GameObject partPrefab, params string[] preferredRoots)
    {
        var inst = UnityEngine.Object.Instantiate(partPrefab);
        inst.transform.SetParent(rigRoot.transform, false);

        // Tìm TextAsset JSON trong bundle
        var text = bundle.LoadAllAssets<TextAsset>()
                         .FirstOrDefault(t => t.name.EndsWith(".bones") || t.name.EndsWith(".bones.json"));
        if (text == null)
        {
            Debug.LogError("[AttachWithBonesJson] Không thấy *.bones.json trong bundle part.");
            return inst;
        }

        var pack = JsonUtility.FromJson<BonePack>(text.text);


        // Map tên -> bone trong rig
        var rigMap = rigRoot.GetComponentsInChildren<Transform>(true)
                            .GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());

        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var entry = pack.smrs.FirstOrDefault(e => e.name == smr.gameObject.name);
            if (entry == null) { Debug.LogWarning($"[Attach] Không có entry cho {smr.name}"); continue; }

            // Gán bones theo tên
            var bones = new Transform[entry.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                var name = entry.bones[i];
                if (!rigMap.TryGetValue(name, out var t))
                {
                    var loose = name.Replace("_", "").Replace(" ", "").ToLowerInvariant();
                    t = rigMap.FirstOrDefault(kv =>
                        kv.Key.Replace("_", "").Replace(" ", "").ToLowerInvariant() == loose).Value;
                }
                bones[i] = t ? t : rigRoot.transform;
            }
            smr.bones = bones;

            // rootBone
            Transform root = null;
            if (!rigMap.TryGetValue(entry.root, out root))
                rigMap.TryGetValue(preferredRoots[0], out root);
            smr.rootBone = root ? root : rigRoot.transform;

            // chống culling frame đầu
            smr.updateWhenOffscreen = true;
            smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 5f);
        }

        return inst;
    }

    // === Helpers ===
    static Dictionary<string, Transform> BuildBoneMap(Transform rigRoot)
    {
        var map = new Dictionary<string, Transform>(StringComparer.Ordinal);
        foreach (var t in rigRoot.GetComponentsInChildren<Transform>(true))
            if (!map.ContainsKey(t.name)) map.Add(t.name, t);
        return map;
    }

    static Transform FindByNameLoose(Dictionary<string, Transform> map, string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (map.TryGetValue(name, out var tExact)) return tExact;

        // so khớp "lỏng": bỏ "_" và khoảng trắng, lower-case
        string loose = Strip(name);
        foreach (var kv in map)
            if (Strip(kv.Key) == loose) return kv.Value;

        return null;
    }

    static string Strip(string s) => s.Replace("_", "").Replace(" ", "").ToLowerInvariant();
}
