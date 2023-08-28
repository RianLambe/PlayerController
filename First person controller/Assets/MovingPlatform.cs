using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    Animator animator;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    //private void OnAnimatorMove() {
    //    if (transform.Find("TP Character") != null) transform.Find("TP Character").transform.rotation *= animator.deltaRotation;
    //
    //    //transform.parent.transform.position += animator.deltaPosition;
    //}

    private void LateUpdate() {
        if (transform.Find("TP Character") != null) transform.Find("TP Character").transform.rotation *= animator.deltaRotation;
    }
}
