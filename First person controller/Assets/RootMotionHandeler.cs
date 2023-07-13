using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionHandeler : MonoBehaviour
{
    Animator animator;
    [SerializeField] bool rootMotionEnabled;
    Vector3 animDeltaPos;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove() {
        if (rootMotionEnabled)
            animDeltaPos = animator.deltaPosition;
            //transform.parent.GetComponent<Rigidbody>().MovePosition(transform.parent.transform.position + animator.deltaPosition);
            //transform.parent.transform.position += animator.deltaPosition;

    }

    private void FixedUpdate() {
        transform.parent.GetComponent<Rigidbody>().MovePosition(transform.parent.transform.position + animDeltaPos);
    }
}
