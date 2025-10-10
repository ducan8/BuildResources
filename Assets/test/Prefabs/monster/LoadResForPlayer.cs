using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LoadResForPlayer : MonoBehaviour
{
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        //string path3 = Path.Combine(Application.dataPath, "test/Prefabs/house.unity3d");
        string path3 = Path.Combine(Application.dataPath, "test/Prefabs/monster/桵須眸辦1.unity3d");

        AssetBundle tower = AssetBundle.LoadFromFile(path3);

        if (tower is null)
        {
            Debug.Log("Failed to load AssetBundle!");
            return;
        }
        else
        {
            Debug.Log("AssetBundle loaded successfully!");
        }

        //GameObject t = tower.LoadAsset<GameObject>("醫栠條窒");
        GameObject t = tower.LoadAsset<GameObject>("桵須眸辦1");
        GameObject tow = Instantiate(t);
        tow.transform.SetParent(this.transform, false);
        var matChilds = tow.GetComponentsInChildren<Renderer>();

        foreach (var m in matChilds)
        {
            m.material.shader = Shader.Find("Standard");
        }

        animator = tow.AddComponent<Animator>();
        var controller = Resources.Load<RuntimeAnimatorController>("PlayerController");
        if (controller is null)
        {
            Debug.Log("Failed to load AnimatorController!");
            return;
        }
        else
        {
            Debug.Log("AnimatorController loaded successfully!");
            animator.runtimeAnimatorController = controller;
        }
    }

    // Update is called once per frame
    void Update()
    {
        var vertical = Input.GetAxis("Vertical");
        var horizontal = Input.GetAxis("Horizontal");
        this.transform.position += new Vector3(horizontal, 0, vertical);

        if (vertical != 0f || horizontal != 0f)
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }
}
