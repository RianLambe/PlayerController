using Cinemachine;
using System;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEditor;
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

public class CharacterController : MonoBehaviour
{
    //Camera settings
    public float lookSpeed = .2f;
    public Vector2 cameraAngleLimits = new Vector2(-80, 80);
    public float cameraOffset;
    public AnimationCurve cameraFOVCurve;
    Vector3 cameraStartingPos;
    public float cameraFOVAdjustSpeed = .1f;
    public bool useHeadBobCurves;
    public float headBobFrequency;
    public float headBobAmplitude;
    public AnimationCurve headBobFrequencyCurve;
    public AnimationCurve headBobAmplitudeCurve;
    public enum cameraStyles {Standard, Locked}
    public cameraStyles cameraStyle;
    public float TPRotationSpeed = 500;
    public Transform cameraTarget;
    public float cameraArmLenght = 2f;

    //Moving variables
    public float walkSpeed = 5;
    public float sprintSpeed = 8;
    public float walkCrouchSpeed;
    public float sprintCrouchSpeed;
    float moveSpeed = 5;
    public float acceleration = 10;
    public float playerDrag = 1;
    public float maxSlopeAngle;
    public float maxStepHeight = 0.5f;
    public float stepSmoothingSpeed = 9;
    public bool smoothStep;
    float playerHeight;
    public float walkingHeight;
    public float crouchingHeight;
    RaycastHit stepRay;
    public bool dynamicCrouch;
    public float dynamicCrouchOffset = 0.1f;


    //Jumping variables
    public float jumpHeight = 1;
    public int maxJumps = 1;
    public float coyoteTime;
    bool withinCoyoteTime;
    public enum jumpBufferModes {None, Single, Continuous};
    public jumpBufferModes jumpBufferMode;
    public float maxJumpBuffer;
    float jumpBufferTime;
    bool performJump = false;
    public enum jumpModes {Standard, Charge, Hold}
    public jumpModes jumpMode;
    public AnimationCurve chargeCurve;


    //Ground check variables
    public Vector3 groundCheckOrigin;
    public float groundCheckDistance = .1f;
    public Vector3 groundAngleCheckOrigin;
    public float groundAngleCheckDistance = .7f;
    public float groundCheckCoolDown = .1f;
    public LayerMask groundLayer;
    public LayerMask dynamicGravityLayer;

    //Gravity variables
    float gravity = 9.81f;
    private float timeFell;
    public float timeSinceFall = 0;
    [HideInInspector] public bool isFalling; // can most likley remove
    bool landed;
    public Vector3 gravityDirection;
    public Transform attractor;
    [Range(0,100)]public float gravityChangeSpeed = .1f;
    public float maxGravityChange;

    //Audio variables
    public AudioSource aSource;
    public float walkStepTime = 0.5f;
    float stepTime = 0.5f;
    public bool enableDefaultSounds;
    [SerializeField] public List<AudioSettings> footstepAudioClips;
    [SerializeField] public List<AudioSettings> jumpingAudioClips;
    [SerializeField] public List<AudioSettings> landingAudioClips;


    //Object assignment
    public CapsuleCollider stepCollider;
    public Rigidbody rb;
    public Transform playerObject;
    public CinemachineVirtualCamera playerCamera;

    //Input variables
    PlayerInput pi;
    Vector2 movementInput;
    Vector3 moveDirection;
    Vector2 mousePosition;

    //misc game variables
    private float upAngle = 0;
    private float sideAngle = 0;
    private bool sprinting;
    private bool crouching;
    private bool grounded;
    private int numberOfJumps;
    public Quaternion targetRotation = Quaternion.identity;
    private Vector3 horizontalVelocity;
    private Vector3 localVelocty;
    private Vector3 cachedMoveDirection;
    private Vector2 inputCache;
    public RaycastHit gcHit;
    public Vector3 slopeAngle;
    public Vector3 cachedSlopeAngle;
    public bool overideGravity;
    float jumpHoldTime;
    Animator animator;

    public GameObject testingTransform;
    bool jumping;
    public float maxUncrouchDistance;
    bool canUncrouch;

    Vector3 floorPos;

    //Called on script load
    private void Awake() {
        //Get references to GaemObjects 
        playerCamera = playerCamera.GetComponentInChildren<CinemachineVirtualCamera>();
        rb = GetComponent<Rigidbody>();
        pi = GetComponent<PlayerInput>();
        animator = playerObject == null ? null :playerObject.GetComponent<Animator>();
        cameraStartingPos = playerCamera.transform.localPosition;

        //Set cursor lock mode
        Cursor.lockState = CursorLockMode.Locked;
    }

    //Makes the player jump
    public void OnJump(InputAction.CallbackContext context) {

        //Pressed
        if (context.performed && numberOfJumps <= maxJumps && (withinCoyoteTime || maxJumps > 1)) {
            switch (jumpMode) {
                case jumpModes.Standard:
                    grounded = false;
                    ExecuteJump(jumpHeight);
                    if(animator != null) animator.SetTrigger("jump");
                    break;

                case jumpModes.Charge:
                    jumpHoldTime = Time.time;
                    Debug.Log("jpht = " + jumpHoldTime);

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
                grounded = false;
                //if (numberOfJumps < maxJumps) ExecuteJump(jumpHeight * chargeCurve.Evaluate(jumpHoldTime));

                playerObject.GetComponent<Animator>().SetBool("chargingJump", false);
                Debug.Log(Time.time - jumpHoldTime);
                ExecuteJump(chargeCurve.Evaluate(Time.time - jumpHoldTime));
            }
        }
    }

    //Performs the jump with the calculated power
    void ExecuteJump(float power) {
        timeSinceFall = Time.time;
        jumping = true;

        //Cancel out vertical velocity
        SetGravityDirection(gravity, transform.up, false);
        localVelocty = transform.InverseTransformDirection(rb.velocity);
        localVelocty = new Vector3(localVelocty.x, 0, localVelocty.z);

        //Apply velocity to player
        rb.velocity = transform.up * Mathf.Sqrt(2.0f * gravity * (power -  playerHeight / 2));

        numberOfJumps++;

        PlaySounds(jumpingAudioClips);
    }

    //Moves rhe player along the desired plane
    void MovePlayer() {
        if (rb != null) rb.AddForce(slopeAngle * moveSpeed * acceleration, ForceMode.Force);
    }

    //Limit movement speed
    public void LimitPlayerSpeed() {

        //Convert world global velocity to relative velocity and remove vertical component
        Matrix4x4 matrix4x4 = Matrix4x4.identity;
        if (cachedSlopeAngle.sqrMagnitude > 0) matrix4x4.SetTRS(Vector3.zero, Quaternion.LookRotation(cachedSlopeAngle), Vector3.one);
        Vector3 convertedVelocity = matrix4x4.inverse.MultiplyVector(rb.velocity);
        horizontalVelocity = new Vector3(convertedVelocity.x, 0, convertedVelocity.z);
        Vector3 limitedVal = horizontalVelocity.normalized * moveSpeed;

        //Limit speed when over threshhold
        if (horizontalVelocity.magnitude > moveSpeed) {
            rb.velocity = matrix4x4.MultiplyVector(new Vector3(limitedVal.x, convertedVelocity.y, limitedVal.z));
        }
    }

    //Sets the direction of the gravity
    public void SetGravityDirection(float newGrvaityStrenght, Vector3 upVector, bool adjustRotation) {
        if (adjustRotation) {
            targetRotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;

        }

        testingTransform.transform.rotation = targetRotation;
        testingTransform.transform.position = floorPos;
        
        gravity = newGrvaityStrenght;
        Physics.gravity = upVector.normalized * -newGrvaityStrenght;
    }

    //Rotates the players camera
    void RotatePlayer() {
        Quaternion angle = Quaternion.identity;

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

        //Set camera FOV
        //playerCamera.m_Lens.FieldOfView = Mathf.Lerp(playerCamera.m_Lens.FieldOfView, cameraFOVCurve.Evaluate(rb.velocity.magnitude), cameraFOVAdjustSpeed);

        RaycastHit cameraCollision;
        if (Physics.Raycast(transform.TransformPoint(cameraStartingPos), -playerCamera.transform.forward, out cameraCollision, cameraArmLenght, groundLayer)) {
            playerCamera.transform.position = cameraCollision.point;
        }
        else {
            playerCamera.transform.position = transform.TransformPoint(cameraStartingPos) - playerCamera.transform.forward * cameraArmLenght;
        }

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

    //Checks to see if the player is grounded as well as the angle of the ground
    void GroundChecks() {
        float hitAngle;
        targetRotation = transform.rotation;

        string hitLayer;

        Vector3 target = transform.position;

        //Checks if the player is able to step up
        StepCheck();

        //Sets the players gravity to false if they are grounded to avoid jittering
        rb.useGravity = grounded? false : true;

        //-----Overide gravity-----//
        if (overideGravity) {
            Debug.Log("Using overided gravity");
        }

        //-----Attractor-----//
        else if (attractor != null) {
            //SetGravityDirection(gravity, (transform.position - attractor.transform.position).normalized, true);
            SetGravityDirection(gravity, (transform.position - attractor.transform.position).normalized, true);
        }

        ////-----Horizontal checks-----//
        else if (Physics.Raycast(transform.position + (transform.up * .15f), Vector3.ProjectOnPlane(moveDirection, testingTransform.transform.up), out gcHit, groundAngleCheckDistance, groundLayer)) {   
            
            hitAngle = Vector3.Angle(gcHit.normal, transform.up);
            hitLayer = LayerMask.LayerToName(gcHit.transform.gameObject.layer);
    
            //Checks if the player is standing on something that has dynamic gravity
            if ((hitLayer == "Dynamic gravity" || hitLayer == "Dynamic gravity + Moving platform") && (hitAngle <= maxGravityChange)) {
                slopeAngle = moveDirection;
                SetGravityDirection(gravity, gcHit.normal, true);
                Debug.Log("Horizontal");
            }

            //slopeAngle = Vector3.ProjectOnPlane(moveDirection, gcHit.normal).normalized;
            //SetGravityDirection(gravity, gcHit.normal, false);
            //
            ////Checks if the objects the player is walking into is on the "Dynamic gravity" layer meaning the player will stick to it
            //if ((hitLayer == "Dynamic gravity" || hitLayer == "Dynamic gravity + Moving platform") && (hitAngle <= maxGravityChange)) {
            //    SetGravityDirection(gravity, gcHit.normal, true);
            //    Debug.Log("1");
            //}
        }
        
        //-----Vertical checks-----//
        else if (Physics.Raycast(transform.position + (transform.up * .15f), -testingTransform.transform.up, out gcHit, 2)) {     
            hitLayer = LayerMask.LayerToName(gcHit.transform.gameObject.layer);
            hitAngle = Vector3.Angle(gcHit.normal, testingTransform.transform.up);
        
            //Checks if the player is standing on something that has dynamic gravity
            if ((hitLayer == "Dynamic gravity" || hitLayer == "Dynamic gravity + Moving platform") && (hitAngle <= maxGravityChange)) {
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
                PlaySounds(landingAudioClips);
                landed = true;
                jumping = false;
            }
            //Jump once landed it jump buffer meets requirements
            if (Time.time - jumpBufferTime <= maxJumpBuffer && performJump || jumpBufferMode == jumpBufferModes.Continuous && performJump) {
                ExecuteJump(jumpHeight);
                if (jumpBufferMode == jumpBufferModes.Single) performJump = false;
            }
            timeFell = Time.time;
        }
        else {
            SetGravityDirection(gravity, testingTransform.transform.up, false);

            landed = false;
        }

        withinCoyoteTime = (Time.time - timeFell) <= coyoteTime;


    }

    //Plays the footstep sounds
    void FootstepSounds() { 
        if (!grounded || movementInput.magnitude == 0) return;

        stepTime += Time.deltaTime;

        if (stepTime >= walkStepTime * (walkSpeed / rb.velocity.magnitude)) {
            stepTime = 0;

            PlaySounds(footstepAudioClips);
            Debug.Log("Should play sound");
        }
    }

    //Plays a random sound effect from a list of audio clips
    private void PlaySounds(List<AudioSettings> clips) {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + (transform.up * .15f), -testingTransform.transform.up, out hit, 2)) {

            foreach (AudioSettings soundClip in clips) {
                if (hit.transform.tag == soundClip.tag) {
                    aSource.PlayOneShot(soundClip.sounds[Random.Range(0, soundClip.sounds.Count)]);
                    return;
                }
            }
            if (enableDefaultSounds && clips.Count > 0) aSource.PlayOneShot(clips[0].sounds[Random.Range(0, clips[0].sounds.Count)]);
        }
    }

    private void SetHeight() {
        stepCollider.height = playerHeight - maxStepHeight ;
        stepCollider.center = new Vector3(0, (playerHeight - maxStepHeight) / 2 + maxStepHeight, 0);

    }

    private void GetMaxUnCrouchDistance() {
        RaycastHit unCrouchHit;
        Debug.DrawRay(floorPos + transform.up * 1, transform.up * 1, Color.red);

        if (Physics.Raycast(floorPos + (transform.up * crouchingHeight), transform.up, out unCrouchHit, walkingHeight, groundLayer)) {
            maxUncrouchDistance = Mathf.Lerp(maxUncrouchDistance, unCrouchHit.distance, stepSmoothingSpeed * Time.deltaTime);
            playerHeight = Mathf.Clamp(playerHeight, crouchingHeight, maxUncrouchDistance - dynamicCrouchOffset + crouchingHeight);
            canUncrouch = false;
        }
        else {
            maxUncrouchDistance = walkingHeight;
            canUncrouch = true;
        }
    }

    private void StepCheck() {
        if (Physics.BoxCast(transform.position + (transform.up * playerHeight), new Vector3(groundCheckDistance, groundCheckDistance, groundCheckDistance), -transform.up, out stepRay, Quaternion.identity, (playerHeight) + maxStepHeight, groundLayer)) {
            if (!jumping) {
                float newStepOffset = (playerHeight - groundCheckDistance * 2) / 2 - stepRay.distance;
                //floorPos = transform.position - transform.up * (stepRay.distance + groundCheckDistance);
                floorPos = transform.position + (transform.up * playerHeight) - transform.up * (stepRay.distance + groundCheckDistance);

                //transform.position = transform.position + (transform.up * newStepOffset);
                //transform.position = Vector3.Lerp(transform.position, transform.position + (transform.up * newStepOffset), smoothStep ? stepSmoothingSpeed * Time.deltaTime : 1);
                //cameraStartingPos = Vector3.Lerp(cameraStartingPos, new Vector3(0, (playerHeight - cameraOffset) / 2, 0), stepSmoothingSpeed * Time.deltaTime);

                transform.position = Vector3.Lerp(transform.position, floorPos, smoothStep ? stepSmoothingSpeed * Time.deltaTime : 1);

                //transform.position = floorPos;
                cameraStartingPos = Vector3.Lerp(cameraStartingPos, new Vector3(0, playerHeight - cameraOffset, 0), stepSmoothingSpeed * Time.deltaTime);

                string stepLayer = LayerMask.LayerToName(stepRay.transform.gameObject.layer);

                transform.SetParent(stepLayer == "Moving platform" || stepLayer == "Dynamic gravity + Moving platform" ? stepRay.transform : null, true);
                testingTransform.transform.SetParent(stepLayer == "Moving platform" || stepLayer == "Dynamic gravity + Moving platform" ? stepRay.transform : null, true);

            }
            grounded = true;
        }
        else {
            grounded = false;
            floorPos = transform.position - transform.up * (playerHeight / 2);
            transform.SetParent(null);
        }
    }

    private void GetInput() {
        //Gets sprint and crouch input
        sprinting = pi.actions.FindAction("Sprint").IsPressed();
        crouching = pi.actions.FindAction("Crouch").IsPressed();

        //Sets players movment speed 
        if (sprinting && crouching) moveSpeed = sprintCrouchSpeed;
        else if (sprinting) moveSpeed = sprintSpeed;
        else if (crouching) moveSpeed = walkCrouchSpeed;
        else moveSpeed = walkSpeed;

        //Set player height
        //playerHeight = !crouching && canUncrouch? walkingHeight : crouchingHeight;
        playerHeight = crouching ? crouchingHeight : walkingHeight;

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

    //Called every frame
    private void Update() {


        //Gets the players input
        GetInput();

        //Handels all forms of rotation for the player
        RotatePlayer();

        //Handels crouching 
        GetMaxUnCrouchDistance();
        SetHeight();

        //Checks both if the player is grounded and if there is a wall in front of them 

        //Smoothly rotate the player to the target rotation
        //transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, gravityChangeSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, testingTransform.transform.rotation, gravityChangeSpeed * Time.deltaTime);

        GroundChecks();


        //moveSpeed = grounded ? 0 : moveSpeed; // temp code, change for more robust 

        //Sets player drag depending on weather they are grounded or not
        if (grounded && ((Time.time - timeSinceFall) > .2f)) {
            rb.drag = playerDrag;
            isFalling = false;
            numberOfJumps = 0;

        }
        else {
            rb.drag = 0;
        }

        localVelocty = transform.InverseTransformDirection(rb.velocity);

        //Makes sure the player does not exceed a certain speed
        if (grounded) moveSpeed *= movementInput.magnitude;
        LimitPlayerSpeed();

        //Rotates the player camera

        //Sets values for the animation graph as well as rotates player to desired direction
        AnimateCharcter();

        //Handles playing all the footstep sounds
        FootstepSounds();

        //If enabled, all the debug options will be visable or testing purposes
        DebugMode();


    }

    //passes through variables to the animation controller 
    void AnimateCharcter() {
        //Animate third person 
        if (animator != null) {
            animator.SetFloat("inputMagnitude", movementInput.magnitude, .1f, Time.deltaTime);
            if (!grounded) playerObject.GetComponent<Animator>().SetFloat("verticalVelocity", localVelocty.y, .1f, Time.deltaTime);
            animator.SetBool("falling", !grounded);
            animator.SetFloat("jumpCharge", (Time.time - jumpHoldTime) / 2, .1f, Time.deltaTime);
            animator.SetBool("sprinting", sprinting);


            if (cameraStyle == cameraStyles.Standard) {
                animator.SetFloat("vInput", movementInput.magnitude, .1f, Time.deltaTime);

            }
            else {
                animator.SetFloat("vInput", movementInput.y, .1f, Time.deltaTime);
                animator.SetFloat("hInput", movementInput.x, .1f, Time.deltaTime);
            }
        }

    }

    //Called every fixed framerate frame
    private void FixedUpdate() {

        MovePlayer();

        if (jumpMode == jumpModes.Hold && pi.actions.FindAction("Jump").IsPressed()) ExecuteJump(jumpHeight * (Time.time - jumpHoldTime));
    }

    //Draws debug rays in the scene as well as the debug text in the corner 
    private void DebugMode() {
        //Debug text 
        GameObject.Find("Debug1").GetComponent<TMP_Text>().text = "Parent delta rotation :  " + maxUncrouchDistance;
        GameObject.Find("Debug2").GetComponent<TMP_Text>().text = "Vertical speed : " + localVelocty.y.ToString("F2");
        GameObject.Find("Debug3").GetComponent<TMP_Text>().text = "Grounded : " + grounded;
        GameObject.Find("Debug4").GetComponent<TMP_Text>().text = "Speed " + rb.velocity.magnitude.ToString("F2");

        //Debug rays
        Debug.DrawRay(transform.position, Physics.gravity.normalized * 10, Color.yellow, Time.deltaTime);
        Debug.DrawRay(transform.position, slopeAngle * 10, Color.green, Time.deltaTime);
        Debug.DrawRay(transform.TransformPoint(groundAngleCheckOrigin), Vector3.ProjectOnPlane(moveDirection, testingTransform.transform.up), Color.red);
        Debug.DrawLine(transform.TransformPoint(groundAngleCheckOrigin), transform.TransformPoint(groundAngleCheckOrigin) + cachedMoveDirection * groundAngleCheckDistance, Color.black);

    }

    private void OnDrawGizmos() {
        Gizmos.DrawCube(floorPos, new Vector3(.5f, 0,.5f));
    }

    private void OnValidate() {
        SetHeight();
    }
}

