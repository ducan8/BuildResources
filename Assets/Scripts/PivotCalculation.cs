using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

[ExecuteAlways]
public class PivotCalculation : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer SpriteRenderer;

    [SerializeField]
    private float Pivot_X;


    // Update is called once per frame
    void Update()
    {
        if (this.SpriteRenderer != null)
        {
            float pivot_x = this.SpriteRenderer.sprite.pivot.x;
            this.Pivot_X = pivot_x;
        }
        else
        {
            this.Pivot_X = 0;
        }
    }
}
