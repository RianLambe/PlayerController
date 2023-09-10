using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RootMotionHandeler : MonoBehaviour
{
    Animator animator;
    [SerializeField] bool rootMotionEnabled;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove() {
        if (rootMotionEnabled) {
            //Vector3 slope = transform.parent.GetComponentInParent<CharacterController>().gcHit.normal;

            transform.parent.transform.position += Vector3.ProjectOnPlane(animator.deltaPosition, Physics.gravity);


        }
        //transform.parent.transform.position += animator.deltaPosition;
    }
}
