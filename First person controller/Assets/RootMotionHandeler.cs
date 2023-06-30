using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionHandeler : MonoBehaviour
{
    Animator animator;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove() {
        transform.position += animator.deltaPosition;
    }
}
