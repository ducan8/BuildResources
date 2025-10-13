using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TMPro; // <-- quan trọng: dùng TMP_Dropdown

public class CustomizeUI : MonoBehaviour
{
    public CharacterAssembler assembler;      // script loader/assembler ở bước trước
    public TMP_Dropdown dressDD;              // kéo TMP_Dropdown từ Canvas vào đây
    public TMP_Dropdown faceDD;
    public TMP_Dropdown hairDD;

    // Cache các Part để tra nhanh
    PartNode _dress, _face, _hair;

    async void Start()
    {
        // 1) Load base rig + locomotion trước
        await assembler.InitBaseAsync();

        // 2) Lấy ra 3 nhóm part
        var cfg = assembler.Config;
        _dress = cfg.Parts.FirstOrDefault(p => p.Type == "dress");
        _face = cfg.Parts.FirstOrDefault(p => p.Type == "face");
        _hair = cfg.Parts.FirstOrDefault(p => p.Type == "hair");

        // 3) Gắn dropdown với danh sách chỉ-text
        SetupTMPDropdown(dressDD, _dress, "Áo");
        SetupTMPDropdown(faceDD, _face, "Mặt");
        SetupTMPDropdown(hairDD, _hair, "Tóc");

        // 4) Áp mặc định
        await ApplyDefaultAsync();
    }

    void SetupTMPDropdown(TMP_Dropdown dd, PartNode part, string labelPrefix)
    {
        dd.onValueChanged.RemoveAllListeners();      // tránh đăng ký lặp

        dd.options.Clear();
        if (part == null || part.Variants.Count == 0)
        {
            dd.options.Add(new TMP_Dropdown.OptionData("(trống)"));
            dd.interactable = false;
            dd.RefreshShownValue();
            return;
        }

        // Hiển thị: "Mặt 1", "Tóc 2", "Áo 3"...
        for (int i = 0; i < part.Variants.Count; i++)
        {
            dd.options.Add(new TMP_Dropdown.OptionData($"{labelPrefix} {i + 1}"));
        }
        dd.interactable = true;
        dd.RefreshShownValue();

        // Đăng ký handler (gọi async an toàn)
        dd.onValueChanged.AddListener(i => _ = OnSelectAsync(part, i));
    }

    async Task OnSelectAsync(PartNode part, int index)
    {
        if (part == null || index < 0 || index >= part.Variants.Count) return;
        var v = part.Variants[index];                // v.Code → "face01"/"hair02"/"dress03"…
        await assembler.SetPartAsync(part.Type, v);  // load bundle theo đường dẫn & rebind xương
    }

    async Task ApplyDefaultAsync()
    {
        // Dress
        if (_dress != null && _dress.Variants.Count > 0)
        {
            Debug.Log("Applying default dress 01");
            var v = FindDefault(_dress);
            dressDD.value = _dress.Variants.IndexOf(v);
            dressDD.RefreshShownValue();
            await assembler.SetPartAsync("dress", v);
        }
        // Face
        if (_face != null && _face.Variants.Count > 0)
        {
            Debug.Log("Applying default face 01");
            var v = FindDefault(_face);
            faceDD.value = _face.Variants.IndexOf(v);
            faceDD.RefreshShownValue();
            await assembler.SetPartAsync("face", v);
        }
        // Hair
        if (_hair != null && _hair.Variants.Count > 0)
        {
            Debug.Log("Applying default hair 01");
            var v = FindDefault(_hair);
            hairDD.value = _hair.Variants.IndexOf(v);
            hairDD.RefreshShownValue();
            await assembler.SetPartAsync("hair", v);
        }
    }

    static VariantNode FindDefault(PartNode part)
    {
        if (!string.IsNullOrEmpty(part.Default))
        {
            var byCode = part.Variants.FirstOrDefault(x => x.Code == part.Default);
            if (byCode != null) return byCode;
        }
        return part.Variants[0];
    }
}
