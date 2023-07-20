using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionHandeler : MonoBehaviour
{
    Animator animator;
    [SerializeField] bool rootMotionEnabled;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove() {
        if (rootMotionEnabled)
            transform.parent.transform.position += animator.deltaPosition;
    }
}
