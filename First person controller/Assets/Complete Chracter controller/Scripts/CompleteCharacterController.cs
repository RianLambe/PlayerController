using Cinemachine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Plane = UnityEngine.Plane;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class CompleteCharacterController : MonoBehaviour
{
    //Input variables
    public PlayerInput pi;
    public Vector2 movementInput { get; private set;}
    Vector3 moveDirection;
    Vector2 mousePosition;

    //Camera settings
    public float lookSpeed = 0.2f;
    public Vector2 cameraAngleLimits = new Vector2(-80, 80);
    public AnimationCurve cameraFOVCurve;
    public float cameraFOVAdjustSpeed = 0.1f;
    public float cameraOffset = 0.2f;
    public float cameraArmLength = 2f;
    Vector3 cameraStartingPos;
    public bool useHeadBobCurves;
    public float headBobFrequency;
    public float headBobAmplitude;
    public AnimationCurve headBobFrequencyCurve;
    public AnimationCurve headBobAmplitudeCurve;
    public enum cameraStyles {Standard, Locked}
    public cameraStyles cameraStyle;
    public float TPRotationSpeed = 500;
    public Transform cameraTarget;

    //Moving variables
    public float walkSpeed = 5;
    public float sprintSpeed = 8;
    public float walkCrouchSpeed = 1;
    public float sprintCrouchSpeed = 2.5f;
    public bool disableMovementWhenGrounded = false;
    float moveSpeed = 5;
    public float acceleration = 40;
    public float playerDrag = 10;
    public float maxStepHeight = 0.5f;
    public float stepSmoothingSpeed = 10;
    public float maxSlopeAngle = 80;
    public bool smoothStep = true;
    float playerHeight;
    public float walkingHeight = 1.8f;
    public float crouchingHeight = 0.9f;
    public float crouchSpeed = 5;
    public bool dynamicCrouch;
    public float dynamicCrouchOffset = 0.1f;
    float maxUncrouchDistance;
    bool canUncrouch;
    RaycastHit stepRay;


    //Jumping variables
    public enum jumpModes {Standard, Charge, Hold}
    public jumpModes jumpMode;
    public float jumpHeight = 1;
    public AnimationCurve chargeCurve;
    public int maxJumps = 1;
    public float coyoteTime = 0;
    public enum jumpBufferModes {None, Single, Continuous};
    public jumpBufferModes jumpBufferMode;
    public float maxJumpBuffer;
    float jumpBufferTime;
    bool withinCoyoteTime;
    bool performJump = false;
    bool jumping;

    //Gravity variables
    float gravity = 9.81f;
    private float timeFell;
    public float timeSinceFall = 0;
    public bool landed;
    public Vector3 gravityDirection;
    public Transform attractor;
    public float dynamicGravityLimit = 100;
    public float gravityChangeSpeed = 10;

    //Ground check variables
    public float groundCheckOffset = 0.15f;
    public float groundCheckSize = 0.25f;
    public float horizontalCheckDistance = 0.5f;
    public float verticalCheckDistance = 2f;
    public float groundCheckCoolDown = .1f;
    public LayerMask groundLayer;
    public LayerMask dynamicGravityLayer;

    //Audio variables
    public AudioSource aSource;
    public bool autoFootstepSounds = true;
    public bool autoJumpSounds = true;
    public bool autoLandSounds = true;
    public float walkStepTime = 0.5f;
    float stepTime;
    public bool enableDefaultSounds;
    [SerializeField] public List<AudioSettings> footstepAudioClips;
    [SerializeField] public List<AudioSettings> jumpingAudioClips;
    [SerializeField] public List<AudioSettings> landingAudioClips;


    //Object assignment
    public Transform playerObject;
    Animator animator;
    public CinemachineVirtualCamera playerCamera;
    public CapsuleCollider stepCollider;
    public Rigidbody rb;


    //misc game variables
    float upAngle;
    float sideAngle;
    public bool sprinting { get; private set;}
    public bool crouching { get; private set; }
    public bool grounded { get; private set; }
    int numberOfJumps;
    Quaternion targetRotation = Quaternion.identity;
    Vector3 horizontalVelocity;
    public Vector3 localVelocty { get; private set; }
    Vector3 cachedMoveDirection;
    Vector2 inputCache;
    RaycastHit gcHit;
    Vector3 slopeAngle;
    Vector3 cachedSlopeAngle;
    public float jumpHoldTime { get; private set; }
    Matrix4x4 predictiveOrientation;
    Vector3 floorPos;
    bool attemptStep;
    bool debugMode = false;

    //Called on script load
    private void Awake() {
        //Get references to GameObjects 
        playerCamera = playerCamera.GetComponentInChildren<CinemachineVirtualCamera>();
        rb = GetComponent<Rigidbody>();
        pi = gameObject.GetComponent<PlayerInput>();
        animator = playerObject == null ? null : playerObject.GetComponent<Animator>();

        //Set default values
        UpdateEditor();
        //cameraStartingPos = playerCamera.transform.localPosition;
        predictiveOrientation.SetTRS(Vector3.zero, Quaternion.identity, Vector3.one);
        stepTime = walkStepTime;

        //Set cursor lock mode
        Cursor.lockState = CursorLockMode.Locked;
    }

    //Gets all the inputs from the player
    private void GetInput() {
        //Gets sprint and crouch input
        sprinting = pi.actions.FindAction("Sprint").IsPressed();
        crouching = pi.actions.FindAction("Crouch").IsPressed();

        //Sets players movment speed 
        if (sprinting && crouching) moveSpeed = sprintCrouchSpeed;
        else if (sprinting) moveSpeed = sprintSpeed;
        else if (crouching) moveSpeed = walkCrouchSpeed;
        else moveSpeed = walkSpeed;

        if (disableMovementWhenGrounded) moveSpeed = grounded ? 0 : moveSpeed;

        //Gets axis inputs from the player
        mousePosition = pi.actions.FindAction("Look").ReadValue<Vector2>() * lookSpeed; //Mouse inputs
        mousePosition.y = Mathf.Clamp(mousePosition.y, cameraAngleLimits.x, cameraAngleLimits.y);

        movementInput = pi.actions.FindAction("Move").ReadValue<Vector2>(); //Movement inputs, Taking into acount camera direction
        moveDirection = playerCamera.transform.forward * movementInput.y + playerCamera.transform.right * movementInput.x;
        moveDirection = Vector3.ProjectOnPlane(moveDirection, transform.up).normalized;

        if (movementInput != Vector2.zero) {
            inputCache = movementInput; //The saved direction from the last time the player moved
            cachedSlopeAngle = slopeAngle;
        }
        cachedMoveDirection = playerCamera.transform.forward * inputCache.y + playerCamera.transform.right * inputCache.x;
        cachedMoveDirection = Vector3.ProjectOnPlane(cachedMoveDirection, transform.up).normalized;
        slopeAngle = moveDirection;
    }

    //Makes the player jump
    public void OnJump(InputAction.CallbackContext context) {

        //Pressed
        if (context.performed && numberOfJumps <= maxJumps && (withinCoyoteTime || maxJumps > 1)) {
            switch (jumpMode) {
                case jumpModes.Standard:
                    ExecuteJump(jumpHeight);
                    if(animator != null) animator.SetTrigger("jump");
                    break;

                case jumpModes.Charge:
                    jumpHoldTime = Time.time;

                    if (animator != null)  {
                        animator.SetFloat("jumpCharge", 0);
                        animator.SetBool("chargingJump", true);
                    } 
                    break;

                case jumpModes.Hold:
                    jumpHoldTime = Time.time;
                    if (animator != null) animator.SetTrigger("jump");
                    break;
            }
        }
        else if (context.performed && !grounded && jumpBufferMode == jumpBufferModes.Single || jumpBufferMode == jumpBufferModes.Continuous) {
            jumpBufferTime = Time.time;
            performJump = true;
        }

        //Released
        if (context.canceled) {          
            performJump = false;

            if (jumpMode == jumpModes.Charge) {
                if (playerObject != null) playerObject.GetComponent<Animator>().SetBool("chargingJump", false);
                ExecuteJump(chargeCurve.Evaluate(Time.time - jumpHoldTime));
            }
        }
    }

    //Performs the jump with the calculated power
    void ExecuteJump(float power) {
        timeSinceFall = Time.time;
        //landed = false;
        jumping = true;
        grounded = false;
        attemptStep = false;

        //Apply velocity to player
        rb.velocity = transform.up * Mathf.Sqrt(2.0f * gravity * (power));

        //SetGravityDirection(gravity, transform.up, false);
        numberOfJumps++;

        //if(autoJumpSounds) PlaySounds(jumpingAudioClips);
    }

    //Checks to see if the player is grounded as well as the angle of the ground
    void GroundChecks() {
        float hitAngle;
        string hitLayer;
        targetRotation = transform.rotation;

        //Checks if the player is able to step up
        StepCheck();

        //Sets the players gravity to false if they are grounded to avoid jittering
        rb.useGravity = grounded? false : true;

        ////-----Attractor-----////
        if (attractor != null) {
            SetGravityDirection(gravity, (transform.position - attractor.transform.position).normalized, true);
        }

        ////-----Horizontal checks-----////
        else if (Physics.Raycast(transform.position + (transform.up * groundCheckOffset), Vector3.ProjectOnPlane(moveDirection, predictiveOrientation * Vector3.up), out gcHit, horizontalCheckDistance, groundLayer)) {   
            
            hitAngle = Vector3.Angle(gcHit.normal, transform.up);
            hitLayer = LayerMask.LayerToName(gcHit.transform.gameObject.layer);
        
            //Checks if the player is standing on something that has dynamic gravity
            if ((hitLayer == "Dynamic gravity" || hitLayer == "Dynamic gravity + Moving platform")) {
                slopeAngle = moveDirection;
                SetGravityDirection(gravity, gcHit.normal, true);
            }
        
            //Checks if the player is standing on a slope 
            else if (grounded && hitAngle < maxSlopeAngle) {
                slopeAngle = Vector3.ProjectOnPlane(moveDirection, gcHit.normal).normalized;
                SetGravityDirection(gravity, gcHit.normal, false);
            }
        }
        
        ////-----Vertical checks-----////
        else if (Physics.Raycast(transform.position + (transform.up * groundCheckOffset), -(predictiveOrientation.rotation * Vector3.up), out gcHit, verticalCheckDistance)) {     
            hitLayer = LayerMask.LayerToName(gcHit.transform.gameObject.layer);
            hitAngle = Vector3.Angle(gcHit.normal, predictiveOrientation * Vector3.up);

            //Checks if the player is standing on something that has dynamic gravity
            if ((hitLayer == "Dynamic gravity" || hitLayer == "Dynamic gravity + Moving platform") && (hitAngle <= dynamicGravityLimit)) {
                slopeAngle = moveDirection; 
                SetGravityDirection(gravity, gcHit.normal, true);        
            }
            //Checks if the player is standing on a slope 
            else if (grounded) {
                slopeAngle = Vector3.ProjectOnPlane(moveDirection, gcHit.normal).normalized;
                SetGravityDirection(gravity, gcHit.normal, false);
            }
        }
        
        //Deal with jump buffer and coyote time
        if (grounded) {
            //Play sound once on landing
            if (!landed) {
                if (autoLandSounds) PlaySounds(landingAudioClips);
                landed = true;
                jumping = false;

                //Jump once landed it jump buffer meets requirements
                if (Time.time - jumpBufferTime <= maxJumpBuffer && performJump || jumpBufferMode == jumpBufferModes.Continuous && performJump) {
                    ExecuteJump(jumpHeight);
                    if (jumpBufferMode == jumpBufferModes.Single) performJump = false;
                }
            }
            timeFell = Time.time;
        }
        else {
            SetGravityDirection(gravity, transform.up, false);

            landed = false;
        }

        withinCoyoteTime = (Time.time - timeFell) <= coyoteTime;
    }

    //Sets the height of the player and the size of the main collider
    private void SetHeight() {
        stepCollider.height = playerHeight - maxStepHeight ;
        stepCollider.center = new Vector3(0, (playerHeight - maxStepHeight) / 2 + maxStepHeight, 0);
    }

    //Checks if the player is able to step up on something
    private void StepCheck() {

        float checkDistance = attemptStep ? playerHeight + maxStepHeight - groundCheckSize : playerHeight - groundCheckSize;
        if (Physics.BoxCast(transform.position + (transform.up * playerHeight), new Vector3(groundCheckSize, groundCheckSize, groundCheckSize), -transform.up, out stepRay, Quaternion.identity, checkDistance, groundLayer)) {
            if (!jumping) {
                floorPos = transform.position + (transform.up * playerHeight) - transform.up * (stepRay.distance + groundCheckSize);
    
                //Move the player and the camera to the new step height position
                transform.position = Vector3.Lerp(transform.position, floorPos, smoothStep ? stepSmoothingSpeed * Time.deltaTime : 1);
                cameraStartingPos = Vector3.Lerp(cameraStartingPos, new Vector3(0, playerHeight - cameraOffset, 0), stepSmoothingSpeed * Time.deltaTime);
    
                string stepLayer = LayerMask.LayerToName(stepRay.transform.gameObject.layer);
                transform.SetParent(stepLayer == "Moving platform" || stepLayer == "Dynamic gravity + Moving platform" ? stepRay.transform : null, true);
                attemptStep = true;
            }
            grounded = true;
        }
        else {
            attemptStep = false;
            grounded = false;
            floorPos = transform.position - transform.up * (playerHeight / 2);
            transform.SetParent(null);
        }
    }

    //Sets the direction of the gravity
    public void SetGravityDirection(float newGrvaityStrenght, Vector3 upVector, bool adjustRotation) {
        if (adjustRotation) {
            targetRotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;

        }

        predictiveOrientation.SetTRS(floorPos, targetRotation, Vector3.one);
        
        gravity = newGrvaityStrenght;
        Physics.gravity = upVector.normalized * -newGrvaityStrenght;
    }

    //Checks if the extent that the player is able to uncrouch
    private void GetCrouchLimits() {
        RaycastHit unCrouchHit;

        //Checks if there is something blocking the player from uncrouching
        if (Physics.BoxCast(floorPos + (transform.up * groundCheckOffset), new Vector3(groundCheckSize, groundCheckSize, groundCheckSize), transform.up, out unCrouchHit, Quaternion.identity, walkingHeight, groundLayer)) {
            maxUncrouchDistance = unCrouchHit.distance + groundCheckSize;
            canUncrouch = false;
        }
        else {
            maxUncrouchDistance = walkingHeight;
            canUncrouch = true;
        }

        //Sets the clamped player height
        if (dynamicCrouch) {
            playerHeight = Mathf.Lerp(playerHeight, Mathf.Clamp(crouching ? crouchingHeight : walkingHeight, crouchingHeight, maxUncrouchDistance - dynamicCrouchOffset + groundCheckOffset), crouchSpeed * Time.deltaTime);
        }
        else {
            playerHeight = Mathf.Lerp(playerHeight, !crouching && canUncrouch ? walkingHeight : crouchingHeight, crouchSpeed * Time.deltaTime);
        }

        SetHeight();
    }

    //Limit movement speed
    public void LimitPlayerSpeed() {

        //Sets player drag depending on weather they are grounded or not
        if (grounded && ((Time.time - timeSinceFall) > .2f)) {
            rb.drag = playerDrag;
            numberOfJumps = 0;
        }
        else {
            rb.drag = 0;
        }
        if (grounded) moveSpeed *= movementInput.magnitude;
        
        localVelocty = transform.InverseTransformDirection(rb.velocity);

        //Convert world global velocity to relative velocity and remove vertical component
        Matrix4x4 limitTransform = Matrix4x4.identity;
        if(cachedSlopeAngle.sqrMagnitude > 0) limitTransform.SetTRS(Vector3.zero, Quaternion.LookRotation(cachedSlopeAngle, Physics.gravity), Vector3.one);
        Vector3 convertedVelocity = limitTransform.inverse.MultiplyVector(rb.velocity);
        horizontalVelocity = new Vector3(convertedVelocity.x, 0, convertedVelocity.z);
        Vector3 limitedVal = horizontalVelocity.normalized * moveSpeed;

        //Limit speed when over threshhold
        if (horizontalVelocity.magnitude > moveSpeed) {
            rb.velocity = limitTransform.MultiplyVector(new Vector3(limitedVal.x, convertedVelocity.y, limitedVal.z));

        }
    }

    //Rotates the players camera
    void RotatePlayer() {
        Quaternion angle = Quaternion.identity;

        //Set target angle to rotate towards
        if (cachedMoveDirection != Vector3.zero) {
            switch (cameraStyle) {
                case cameraStyles.Standard:
                    angle = Quaternion.LookRotation(cachedMoveDirection, transform.up);
                    break;

                case cameraStyles.Locked:
                    angle = Quaternion.LookRotation(Vector3.ProjectOnPlane(playerCamera.transform.forward, transform.up), transform.up);
                    break;
            }
        }

        //Rotate player object
        if (movementInput != Vector2.zero && playerObject != null) {
            //playerObject.transform.localRotation = angle;
            playerObject.rotation = Quaternion.RotateTowards(playerObject.rotation, angle, TPRotationSpeed * Time.deltaTime);
        }
 

        //Camera rotation
        if (cameraTarget == null) {
            upAngle = Mathf.Clamp(mousePosition.y + upAngle, cameraAngleLimits.x, cameraAngleLimits.y);
            sideAngle = mousePosition.x + sideAngle;
            playerCamera.transform.localRotation = Quaternion.Euler(-upAngle, sideAngle, 0);
        }
        else {
            playerCamera.transform.LookAt(cameraTarget);
        }

        //Rotates the player to the new desired rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, predictiveOrientation.rotation, gravityChangeSpeed * Time.deltaTime);
    }

    //Applies certain camera effeects 
    private void CameraEffects() {
        //Set camera FOV
        playerCamera.m_Lens.FieldOfView = Mathf.Lerp(playerCamera.m_Lens.FieldOfView, cameraFOVCurve.Evaluate(rb.velocity.magnitude), cameraFOVAdjustSpeed);

        //Camera collision
        RaycastHit cameraCollision;
        if (Physics.Raycast(transform.TransformPoint(cameraStartingPos), -playerCamera.transform.forward, out cameraCollision, cameraArmLength, groundLayer)) {
            //playerCamera.transform.position = cameraCollision.point;
            SetCameraPosition(cameraCollision.point);
        }
        else {
            //playerCamera.transform.position = transform.TransformPoint(cameraStartingPos) - playerCamera.transform.forward * cameraArmLength;
            SetCameraPosition(transform.TransformPoint(cameraStartingPos) - playerCamera.transform.forward * cameraArmLength);
        }

        //Head bob
        if (!grounded) return;
        if (useHeadBobCurves) {
            playerCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = headBobFrequencyCurve.Evaluate(rb.velocity.magnitude);
            playerCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = headBobAmplitudeCurve.Evaluate(rb.velocity.magnitude);
        }
        else {
            playerCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = rb.velocity.magnitude * headBobFrequency;
            playerCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = rb.velocity.magnitude * headBobAmplitude;
        }
    }

    private void SetCameraPosition(Vector3 pos) {
        playerCamera.transform.position = pos;
    }

    //Plays the footstep sounds
    void FootstepSounds() { 
        if (!grounded || movementInput.magnitude == 0) return;

        stepTime += Time.deltaTime;

        if (stepTime >= walkStepTime * (walkSpeed / rb.velocity.magnitude)) {
            stepTime = 0;

            PlaySounds(footstepAudioClips);
        }
    }

    //Plays a random sound effect from a list of audio clips
    public void PlaySounds(List<AudioSettings> clips) {
        if (grounded == false) return;

        Debug.Log("test");

        foreach (AudioSettings soundClip in clips) {
            if (stepRay.transform.tag == soundClip.tag) {
                aSource.PlayOneShot(soundClip.sounds[Random.Range(0, soundClip.sounds.Count)]);
                return;
            }
        }
        if (enableDefaultSounds && clips.Count > 0) aSource.PlayOneShot(clips[0].sounds[Random.Range(0, clips[0].sounds.Count)]);
    }

    //Moves the player along the desired plane
    void MovePlayer() {
        if (rb != null) rb.AddForce(slopeAngle * moveSpeed * acceleration, ForceMode.Force);
    }

    //Called every frame
    private void Update() {

        //Gets the players input
        GetInput();

        //Does a series of tests to determine if the player is grounded or if there is a wall in front of them
        GroundChecks();

        //Handels crouching 
        GetCrouchLimits();
        
        //Makes sure the player does not exceed a certain speed
        LimitPlayerSpeed();

        //Handels all forms of rotation for the player including camera effect related 
        RotatePlayer();

        //Handles all camera related effects
        CameraEffects();

        //Handles playing all the footstep sounds
        if (autoFootstepSounds)FootstepSounds();
    }

    //Called every fixed framerate frame
    private void FixedUpdate() {
        //Moves the player 
        MovePlayer();

        if (jumpMode == jumpModes.Hold && pi.actions.FindAction("Jump").IsPressed()) ExecuteJump(jumpHeight * (Time.time - jumpHoldTime));
    }

    //Draws debug visuals if enabled
    private void OnDrawGizmos() {
        if (debugMode) {
            //Draw shapes
            Gizmos.DrawCube(floorPos, new Vector3(.5f, 0, .5f));

            //Debug rays
            if (Application.isPlaying) {
                Debug.DrawRay(transform.position, Physics.gravity.normalized * 10, Color.yellow, Time.deltaTime);
                Debug.DrawRay(transform.position + (transform.up * .15f), cachedSlopeAngle * 10, Color.red);
            }
        }
    }

    //Update character in editor
    public void UpdateEditor() {
        playerHeight = walkingHeight;
        cameraStartingPos = new Vector3(0, playerHeight - cameraOffset, 0);
        SetCameraPosition(transform.TransformPoint(cameraStartingPos) - playerCamera.transform.forward * cameraArmLength);
        SetHeight();
    }
}
