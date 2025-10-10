using System.IO;
using UnityEngine;

[ExecuteAlways]
public class ExportSpriteToPNG : MonoBehaviour
{
    [SerializeField]
    private string _OutputDirectory;

    [SerializeField]
    private Sprite[] _Sprites;

    [SerializeField]
    private bool _MakeHorizontalFlip = false;

    [SerializeField]
    private bool _Start = false;

    private void Process()
    {
        if (!Directory.Exists(this._OutputDirectory))
        {
            Directory.CreateDirectory(this._OutputDirectory);
        }

        int autoID = -1;
        foreach (Sprite sprite in this._Sprites)
        {
            autoID++;
            string name = autoID.ToString();
            while (name.Length < 4)
            {
                name = "0" + name;
            }
            Texture2D texture = this.ExtractAndName(sprite);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(this._OutputDirectory, name + ".png"), texture.EncodeToPNG());
        }

        if (this._MakeHorizontalFlip)
        {
            int totalSprites = this._Sprites.Length;
            int spritesEachFrame = totalSprites / 8;

            for (int i = totalSprites - spritesEachFrame - 1; i >= spritesEachFrame; i--)
            {
                autoID++;
                string name = autoID.ToString();
                while (name.Length < 4)
                {
                    name = "0" + name;
                }
                Texture2D texture = this.ExtractAndName(this._Sprites[i]);
                texture = this.FlipTexture(texture);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(this._OutputDirectory, name + ".png"), texture.EncodeToPNG());
            }
        }
    }

    // Since a sprite may exist anywhere on a tex2d, this will crop out the sprite's claimed region and return a new, cropped, tex2d.
    private Texture2D ExtractAndName(Sprite sprite)
    {
        var output = new Texture2D((int) sprite.rect.width, (int) sprite.rect.height);
        var r = sprite.textureRect;
        var pixels = sprite.texture.GetPixels((int) r.x, (int) r.y, (int) r.width, (int) r.height);
        output.SetPixels(pixels);
        output.Apply();
        output.name = sprite.texture.name + " " + sprite.name;
        return output;
    }

    private Texture2D FlipTexture(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height);

        int xN = original.width;
        int yN = original.height;


        for (int i = 0; i < xN; i++)
        {
            for (int j = 0; j < yN; j++)
            {
                flipped.SetPixel(xN - i - 1, j, original.GetPixel(i, j));
            }
        }
        flipped.Apply();

        return flipped;
    }

    // Update is called once per frame
    void Update()
    {
        if (this._Start)
        {
            this.Process();
        }
        this._Start = false;
    }
}
