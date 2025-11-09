using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlPlayer : MonoBehaviour
{
    public Animator Animator;

    private void Start()
    {
        Animator = this.GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            Animator.SetBool("isWalking", true);
            Debug.Log("Walking nhưng chả có gì xảy ra cả");
        }
        else
        {
            Animator.SetBool("isWalking", false);
            Debug.Log("Not Walking");
        }
    }
}
