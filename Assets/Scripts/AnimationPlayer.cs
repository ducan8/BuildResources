using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationPlayer : MonoBehaviour
{
    [SerializeField]
    private Sprite[] Sprites;

    [SerializeField]
    private float Duration = 0.5f;

    private new SpriteRenderer renderer;

    private void Awake()
    {
        this.renderer = this.GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        this.StartCoroutine(this.Play());
    }

    private IEnumerator Play()
    {
        float timeEachFrame = this.Duration / this.Sprites.Length;
        int idx = -1;
        while (true)
        {
            idx++;
            if (idx >= this.Sprites.Length)
            {
                idx = 0;
            }
            this.renderer.sprite = this.Sprites[idx];
            this.renderer.drawMode = SpriteDrawMode.Sliced;
            this.renderer.size = this.Sprites[idx].rect.size;
            yield return new WaitForSeconds(timeEachFrame);
        }
    }
}
