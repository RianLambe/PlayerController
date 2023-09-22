using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static CompleteCharacterController;

public class RootMotionHandeler : MonoBehaviour
{
    Animator animator;
    [SerializeField] bool rootMotionEnabled;
    CompleteCharacterController characterController;

    [SerializeField] string inputMagnitude;
    [SerializeField] string verticalVelocity;
    [SerializeField] string falling;
    [SerializeField] string jumpCharge;
    [SerializeField] string sprinting;
    [SerializeField] string vInput;
    [SerializeField] string hInput;




    private void Awake() {
        animator = GetComponent<Animator>();
        characterController = GetComponentInParent<CompleteCharacterController>();
    }

    private void OnAnimatorMove() {
        if (rootMotionEnabled) {
            //Vector3 slope = transform.parent.GetComponentInParent<CharacterController>().gcHit.normal;

            transform.parent.transform.position += Vector3.ProjectOnPlane(animator.deltaPosition, Physics.gravity);


        }
        //transform.parent.transform.position += animator.deltaPosition;
    }

    void OnFootstep() {
        characterController.PlaySounds(characterController.footstepAudioClips);
    }

    void OnJump() {
        characterController.PlaySounds(characterController.jumpingAudioClips);
    }

    void OnLand() {
        characterController.PlaySounds(characterController.landingAudioClips);
    }

    private void Update() {
        //Animate character
        if (animator != null) {
            animator.SetFloat(inputMagnitude, characterController.movementInput.magnitude, .1f, Time.deltaTime);
            animator.SetBool(falling, !characterController.grounded);
            animator.SetBool(sprinting, characterController.sprinting);

            ///-----Other useful animation parameters you could use-----///
            //if (!characterController.grounded) animator.SetFloat(verticalVelocity, characterController.localVelocty.y, .1f, Time.deltaTime); //--Players vertical velocity--//
            //animator.SetFloat(jumpCharge, (Time.time - characterController.jumpHoldTime) / characterController.jumpHeight, .1f, Time.deltaTime); //--For charged jumps--//
            //if (characterController.cameraStyle == cameraStyles.Standard) {
            //    animator.SetFloat(vInput, characterController.movementInput.magnitude, .1f, Time.deltaTime); //--For animation graphs with movement in both x and y directions--//
            //
            //}
            //else {
            //    animator.SetFloat(vInput, characterController.movementInput.y, .1f, Time.deltaTime); //--For animation graphs with movement in both x and y directions--//
            //    animator.SetFloat(hInput, characterController.movementInput.x, .1f, Time.deltaTime); //--For animation graphs with movement in both x and y directions--//
            //}
        }
    }
}
