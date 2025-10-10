using System.Collections.Generic;
using UnityEngine;

public static class Rebind
{
    // Rebind bones của smr sang rigRoot (cây xương của body)
    public static void RebindByName(SkinnedMeshRenderer smr, Transform rigRoot, string preferredRootBoneName = "w_Bip01")
    {
        if (smr == null || rigRoot == null) return;

        // Lập map tên -> Transform trong rig mới
        var map = new Dictionary<string, Transform>(256);
        foreach (var t in rigRoot.GetComponentsInChildren<Transform>(true))
            if (!map.ContainsKey(t.name)) map.Add(t.name, t);

        // Gán lại bones theo tên
        var bones = smr.bones;
        int missing = 0;
        for (int i = 0; i < bones.Length; i++)
        {
            var old = bones[i];
            if (old == null) { missing++; continue; }
            if (!map.TryGetValue(old.name, out var nb))
            {
                // thử khớp lỏng: bỏ dấu '_' / khoảng trắng / lower-case
                string loose = old.name.Replace("_", "").Replace(" ", "").ToLower();
                Transform candidate = null;
                foreach (var kv in map)
                {
                    var k = kv.Key.Replace("_", "").Replace(" ", "").ToLower();
                    if (k == loose) { candidate = kv.Value; break; }
                }
                if (candidate == null) { missing++; continue; }
                nb = candidate;
            }
            bones[i] = nb;
        }
        smr.bones = bones;

        // rootBone: ưu tiên theo preferredRootBoneName, nếu không thì dùng xương cũ theo tên
        Transform newRoot = null;
        if (!string.IsNullOrEmpty(preferredRootBoneName))
            map.TryGetValue(preferredRootBoneName, out newRoot);
        if (newRoot == null && smr.rootBone != null)
            map.TryGetValue(smr.rootBone.name, out newRoot);
        if (newRoot == null)  // fallback hợp lý
            map.TryGetValue("w_Bip01", out newRoot);
        if (newRoot == null)
            map.TryGetValue("root", out newRoot);
        if (newRoot != null) smr.rootBone = newRoot;

        // Bound để tránh culling sai
        if (smr.sharedMesh != null) smr.sharedMesh.RecalculateBounds();

        if (missing > 0)
            Debug.LogWarning($"[RebindByName] {smr.name}: thiếu {missing} bone(s) khi map theo tên. Face lơ lửng thường do thiếu xương Head/Jaw/Eye.");
    }
}
