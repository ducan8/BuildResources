using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class CharacterAssembler : MonoBehaviour
{
    [Header("Config")]
    public TextAsset configXml;
    CharactersRoot _cfg;

    [Header("Spawn character")]
    public Transform mount;

    [Header("Animator Defaults")]
    // Đặt đúng tên state Idle trong controller của bạn (sửa trên Inspector nếu khác)
    public string idleStateName = "root_空手站立01_stand";
    // Nếu giai đoạn init có Time.timeScale = 0 (màn loading), tạm dùng UnscaledTime
    public bool useUnscaledTimeDuringInit = true;

    GameObject _rigInst;
    readonly Dictionary<string, GameObject> _currentParts = new();
    string _baseUrl;

    public CharactersRoot Config => _cfg;
    public string BaseUrl => _baseUrl;

    void Awake()
    {
        _cfg = XmlUtil.LoadXmlFromTextAsset<CharactersRoot>(configXml);
        _baseUrl = _cfg.BaseUrl.TrimEnd('/') + "/";
    }

    public async Task InitBaseAsync()
    {
        // 1) Load base rig
        var rigAb = await ABManager.Instance.LoadBundle(_baseUrl + _cfg.Base.Rig);
        var rigPrefab = rigAb.LoadAsset<GameObject>(
            rigAb.GetAllAssetNames().First(n => n.EndsWith("prefab"))
        );
        _rigInst = Instantiate(rigPrefab, mount ? mount : transform);
        _rigInst.name = "character";

        // 2) Load + gán controller (bundle chứa .controller + .anim)
        await ApplyControllerFromBundle(_cfg.Base.Anims);
    }

    private async Task ApplyControllerFromBundle(string relativeBundlePath)
    {
        var url = _baseUrl + relativeBundlePath;
        var ab = await ABManager.Instance.LoadBundle(url);
        if (!ab) { Debug.LogError($"[CharacterAssembler] Load controller bundle fail: {url}"); return; }

        var ctrlPath = ab.GetAllAssetNames().FirstOrDefault(n => n.EndsWith(".controller"));
        if (string.IsNullOrEmpty(ctrlPath)) { Debug.LogError($"[CharacterAssembler] No .controller in: {url}"); return; }

        var ctrl = ab.LoadAsset<RuntimeAnimatorController>(ctrlPath);
        if (!ctrl) { Debug.LogError($"[CharacterAssembler] Load RAC fail: {ctrlPath}"); return; }

        var animator = _rigInst ? _rigInst.GetComponentInChildren<Animator>() : null;
        if (!animator) { Debug.LogWarning("[CharacterAssembler] No Animator on rig."); return; }

        // Đợi 1 frame để Animator khởi tạo nội bộ
        await Task.Yield();

        // Gán controller
        animator.runtimeAnimatorController = ctrl;

        // Cấu hình để chắc chắn Animator được evaluate
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (useUnscaledTimeDuringInit) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.speed = 1f;

        // Reset toàn bộ tham số & state về mặc định
        ResetAnimatorToDefaults(animator);

        // Ép về Idle (nếu biết tên)
        if (!string.IsNullOrEmpty(idleStateName))
        {
            try
            {
                animator.Play(idleStateName, 0, 0f); // layer 0, normalizedTime=0
                animator.Update(0f);                 // evaluate ngay frame hiện tại
            }
            catch
            {
                Debug.LogWarning($"[CharacterAssembler] Idle state not found: {idleStateName}");
            }
        }

        Debug.Log($"[CharacterAssembler] Controller '{ctrl.name}' applied & reset to Idle.");
    }

    private void ResetAnimatorToDefaults(Animator animator)
    {
        // Xóa mọi giá trị còn dính khiến AnyState/transition tự chạy
        foreach (var p in animator.parameters)
        {
            switch (p.type)
            {
                case AnimatorControllerParameterType.Bool: animator.SetBool(p.nameHash, false); break;
                case AnimatorControllerParameterType.Float: animator.SetFloat(p.nameHash, 0f); break;
                case AnimatorControllerParameterType.Int: animator.SetInteger(p.nameHash, 0); break;
                case AnimatorControllerParameterType.Trigger: animator.ResetTrigger(p.nameHash); break;
            }
        }

        // Rebind đưa Animator về default của controller (pose & params)
        animator.Rebind();
        animator.Update(0f); // Evaluate ngay lập tức
    }

    public async Task SetPartAsync(string type, VariantNode variant)
    {
        if (_currentParts.TryGetValue(type, out var old))
        {
            Destroy(old);
            _currentParts.Remove(type);
            Debug.Log($"Remove part: {old.name}");
        }

        var url = _baseUrl + variant.Bundle;
        var ab = await ABManager.Instance.LoadBundle(url);
        if (!ab) return;

        var prefab = ab.LoadAsset<GameObject>(ab.GetAllAssetNames().First(n => n.EndsWith("prefab")));
        var inst = SkinnedPartBinder.AttachAndRebind(_rigInst, ab, prefab, "w_Bip01", "root");
        inst.name = $"{variant.Code}";
        _currentParts[type] = inst;

        // Nếu controller phụ thuộc part (vd: weapon) thì có thể gọi lại ApplyControllerFromBundle tại đây
        // if (type == "weapon" && !string.IsNullOrEmpty(variant.ControllerBundle))
        //     await ApplyControllerFromBundle(variant.ControllerBundle);
    }
}
