using UnityEngine;

public class AnimatorDebugDump : MonoBehaviour
{
    [ContextMenu("Dump Animator Info")]
    public void Dump()
    {
        var anim = GetComponentInChildren<Animator>();
        if (!anim || anim.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[AnimatorDebugDump] Animator/controller null.");
            return;
        }

        var ctrl = anim.runtimeAnimatorController;
        Debug.Log($"[AnimatorDebugDump] Controller: {ctrl.name}");
        Debug.Log($"[AnimatorDebugDump] Layers: {anim.layerCount}");

        // Parameters
        foreach (var p in anim.parameters)
            Debug.Log($"[AnimatorDebugDump] Param: {p.type} {p.name}");

        // Clips
        var clips = ctrl.animationClips;
        Debug.Log($"[AnimatorDebugDump] Clips: {clips.Length}");
        foreach (var c in clips)
            Debug.Log($"    Clip: {c.name} len={c.length:0.00}s");
    }

    // Kiểm tra state tồn tại bằng hash (nếu bạn biết tên state)
    public bool HasState(int layer, string stateName)
    {
        var full = $"{GetComponentInChildren<Animator>().GetLayerName(layer)}.{stateName}";
        var hash = Animator.StringToHash(full);
        return GetComponentInChildren<Animator>().HasState(layer, hash);
    }
}
