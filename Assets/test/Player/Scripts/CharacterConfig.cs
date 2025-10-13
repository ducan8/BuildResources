using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

[XmlRoot("Characters")]
public class CharactersRoot
{
    [XmlAttribute("baseUrl")] public string BaseUrl;
    public BaseNode Base;
    [XmlElement("Part")] public List<PartNode> Parts = new();
}

public class BaseNode
{
    [XmlAttribute("rig")] public string Rig;     // "characters/base"
    [XmlAttribute("anims")] public string Anims;   // "characters/base/anims"
}

public class PartNode
{
    [XmlAttribute("type")] public string Type;     // "dress" | "face" | "hair"
    [XmlAttribute("category")] public string Category; // "female"
    [XmlAttribute("default")] public string Default;  // "dress01"
    [XmlElement("Variant")] public List<VariantNode> Variants = new();
}

public class VariantNode
{
    [XmlAttribute("id")] public string Id;     // "1"
    [XmlAttribute("code")] public string Code;   // "dress01"
    [XmlAttribute("bundle")] public string Bundle; // "characters/woman/dress/dress01"
    //[XmlAttribute("hash")] public string Hash;   // from manifest (optional in sandbox)
    //[XmlAttribute("size")] public long Size;     // optional
    //[XmlAttribute("preview")] public string Preview; // img url (optional)
}
public static class XmlUtil
{
    public static T LoadXmlFromTextAsset<T>(UnityEngine.TextAsset ta)
    {
        var xs = new XmlSerializer(typeof(T));
        using var sr = new StringReader(ta.text);
        return (T)xs.Deserialize(sr);
    }
}