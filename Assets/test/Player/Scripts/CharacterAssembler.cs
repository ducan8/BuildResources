using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class CharacterAssembler : MonoBehaviour
{
    [Header("Config")]
    public TextAsset configXml; // kéo file XML vào
    CharactersRoot _cfg;

    [Header("Spawn character")]
    public Transform mount; // vị trí spawn base

    GameObject _rigInst;
    readonly Dictionary<string, GameObject> _currentParts = new(); // type->inst
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
        // Load shaders nếu tách riêng (optional)
        // await ABManager.I.LoadBundle(_baseUrl + "core/shaders");

        // Load base rig
        var rigAb = await ABManager.Instance.LoadBundle(_baseUrl + _cfg.Base.Rig);
        var rigPrefab = rigAb.LoadAsset<GameObject>(rigAb.GetAllAssetNames().Where(rig => rig.EndsWith("prefab")).ToArray()[0]);
        _rigInst = Instantiate(rigPrefab, mount ? mount : transform);
        _rigInst.name = "character";

        // Load locomotion (controller + clips)
        await ABManager.Instance.LoadBundle(_baseUrl + _cfg.Base.Anims);
        // Nếu base chưa gán controller, bạn có thể lấy từ bundle và gán ở đây.
    }

    public async Task SetPartAsync(string type, VariantNode variant)
    {
        // Hủy part cũ
        if (_currentParts.TryGetValue(type, out var old))
        {
            Destroy(old);
            _currentParts.Remove(type);
            Debug.Log($"Loại bỏ bộ phận cũ: {old.name}");
        }

        // Load part bundle
        var url = _baseUrl + variant.Bundle;
        Debug.Log($"Tải bộ phận mới: {type} từ {url}");
        var ab = await ABManager.Instance.LoadBundle(url);
        if (!ab) return;

        // Lấy prefab (giả sử prefab đầu tiên)
        var prefab = ab.LoadAsset<GameObject>(ab.GetAllAssetNames().Where(rig => rig.EndsWith("prefab")).ToArray()[0]);
        //var inst = SkinnedPartBinder.Attach(_rigInst, prefab);
        var inst = SkinnedPartBinder.AttachAndRebind(_rigInst,ab, prefab, "w_Bip01", "root");
        inst.name = $"{variant.Code}";
        _currentParts[type] = inst;
    }
}
