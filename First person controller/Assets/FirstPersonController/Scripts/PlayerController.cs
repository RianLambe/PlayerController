using Cinemachine;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.Rendering.DebugUI;
using Cursor = UnityEngine.Cursor;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
    //Camera settings
    public float lookSpeed = .2f;
    public Vector2 cameraAngleLimits = new Vector2(-80, 80);
    public Vector3 cameraOffset;
    public AnimationCurve cameraFOVCurve;
    public float cameraFOVAdjustSpeed = .1f;
    public bool rotateWithMovingPlatforms;
    public enum cameraStyles {Standard, Locked}
    public cameraStyles cameraStyle;
    public float TPRotationSpeed = 500;
    public Transform cameraTarget;

    //Moving variables
    public float walkSpeed = 5;
    public float sprintSpeed = 8;
    float moveSpeed = 5;
    public float acceleration = 10;
    public float playerDrag = 1;
    public AnimationCurve verticalInputMap;
    public float maxStepHeight = 0.5f;
    public float minStepDepth = 0.4f;
    public float stepSmoothing = 9;
    public float maxStepAngle = 85;

    //Jumping variables
    public float jumpHeight = 1;
    public int maxJumps = 1;
    public bool dynamicJumpForce;           // Add dyanamic jumps //
    public AnimationCurve dynaamicJumpForceCurve;

    //Ground check variables
    public Vector3 groundCheckOrigin;
    public float groundCheckDistance = .1f;
    public Vector3 groundAngleCheckOrigin;
    public float groundAngleCheckDistance = .7f;

    //Gravity variables
    private float timeFell;
    [HideInInspector] public float timeSinceFall = 0;
    [HideInInspector] public bool isFalling;
    public Vector3 gravityDirection;
    public Transform attractor;
    public LayerMask groundLayer;
    public LayerMask dynamicGravityLayer;
    [Range(0,10)]public float gravityChangeSpeed = .1f;
    public Vector2 maxGravityChange;

    //Input variables
    PlayerInput pi;
    Vector2 movementInput;
    Vector3 moveDirection;
    Vector2 mousePosition;

    //Object assignment
    Rigidbody rb;
    public Transform playerObject;
    public CinemachineVirtualCamera playerCamera;

    //misc game variables
    float upAngle = 0;
    float sideAngle = 0;
    [SerializeField]Quaternion targetRotation = Quaternion.identity;
    Vector3 currentDirection;
    int numberOfJumps;
    bool sprinting;
    [SerializeField]bool grounded;
    Vector3 horizontalVelocity;

    Vector3 localVelocty;

    Vector3 cachedMoveDirection;
    Vector3 gravityCache;
    Vector2 inputCache;

    RaycastHit gcHit;
    Vector3 slopeAngle;

    public bool overideGravity;

    //public Transform cam;

    //Called on script load
    private void Awake() {
        //Get references to GaemObjects 
        playerCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        rb = GetComponent<Rigidbody>();
        pi = GetComponent<PlayerInput>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    //Checks if the player is touching the ground -- Soon to be depreciated --
    bool IsGrounded() {
        
        Debug.Log("Ground check done ere");
        
        grounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOrigin), groundCheckDistance, groundLayer + dynamicGravityLayer);
        
        RaycastHit hit;
        if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), -transform.up, out hit, .1f)) {
            transform.SetParent(hit.transform.CompareTag("Moving platform") ? hit.transform : null);
        }
        else transform.SetParent(null);
        
        playerObject.GetComponent<Animator>().SetBool("falling", !grounded);

        //RaycastHit test;
        //if (Physics.SphereCast(transform.TransformPoint(groundCheckOrigin), groundCheckDistance, -transform.up, out test, groundLayer + dynamicGravityLayer)) {
        //    transform.position = test.point;
        //    Debug.Log("hit ground");
        //}

        return Physics.CheckSphere(transform.TransformPoint(groundCheckOrigin), groundCheckDistance, groundLayer);
    }

    //Makes the player jump
    void OnJump() {
        if (numberOfJumps < maxJumps) {
            localVelocty = transform.InverseTransformDirection(rb.velocity);
            localVelocty = new Vector3(localVelocty.x, 0, localVelocty.z);

            rb.velocity = transform.TransformDirection(localVelocty);
            rb.AddForce(transform.up * jumpHeight, ForceMode.Force);
            numberOfJumps ++;
            playerObject.GetComponent<Animator>().SetTrigger("jump");
        }
    }

    //Moves rhe player along the desired plane
    void MovePlayer() {
        rb.AddForce(slopeAngle * moveSpeed * acceleration, ForceMode.Force);
    }

    //Limit movement speed
    public void LimitPlayerSpeed() {
        Matrix4x4 matrix4x4 = Matrix4x4.identity;
        matrix4x4.SetTRS(Vector3.zero, Quaternion.LookRotation(-slopeAngle, gcHit.normal), Vector3.one);

        Vector3 convertedVelocity = matrix4x4.inverse.MultiplyVector(rb.velocity);
        horizontalVelocity = new Vector3(convertedVelocity.x, 0, convertedVelocity.z);
        Vector3 limitedVal = horizontalVelocity.normalized * moveSpeed;

        //Control speed
        if (horizontalVelocity.magnitude > moveSpeed) {
            //rb.velocity = test.transform.TransformDirection(limitedVal.x, convertedVelocity.y, limitedVal.z);
            rb.velocity = matrix4x4.MultiplyVector(new Vector3(limitedVal.x, convertedVelocity.y, limitedVal.z));
        }
    }

    //Sets the direction of the gravity
    public void SetGravityDirection(float newGrvaityStrenght, Vector3 upVector, bool adjustRotation) {
        if (adjustRotation) targetRotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;
        Physics.gravity = upVector.normalized * -9.81f;
        Debug.Log("Gravity function");
    }

    //Rotates the players camera
    void RotatePlayer() {
        Quaternion angle = Quaternion.identity;

        if (cachedMoveDirection != Vector3.zero) {
            switch (cameraStyle) {
                case cameraStyles.Standard:
                    angle = Quaternion.LookRotation(cachedMoveDirection.normalized, transform.up);
                    break;

                case cameraStyles.Locked:
                    angle = Quaternion.LookRotation(Vector3.ProjectOnPlane(playerCamera.transform.forward, transform.up), transform.up);
                    break;

                //case cameraStyles.Focused:
                //    angle = Quaternion.LookRotation(cachedMoveDirection.normalized, transform.up);
                //    playerCamera.LookAt = cameraTarget;
                //    break;
            }
        }
        playerCamera.LookAt = cameraTarget;

        //Rotate player
        if (movementInput != Vector2.zero) {
            playerObject.rotation = Quaternion.RotateTowards(playerObject.rotation, angle, TPRotationSpeed * Time.deltaTime);
            //targetRotation = transform.rotation; //Used to store current look direction for smooth gravity changes
        }

        //Camera rotation
        upAngle = Mathf.Clamp(mousePosition.y + upAngle, cameraAngleLimits.x, cameraAngleLimits.y);
        sideAngle = mousePosition.x + sideAngle;
        playerCamera.transform.localRotation = Quaternion.Euler(-upAngle, sideAngle, 0);

        //Set camera FOV
        playerCamera.m_Lens.FieldOfView = Mathf.Lerp(playerCamera.m_Lens.FieldOfView, cameraFOVCurve.Evaluate(rb.velocity.magnitude), cameraFOVAdjustSpeed);
    }

    private void OnDrawGizmos() {
        Gizmos.DrawSphere(transform.TransformPoint(groundCheckOrigin), groundCheckDistance);

    }

    //Checks to see if the player is grounded as well as the angle of the ground
    void GroundChecks() {


        grounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOrigin), groundCheckDistance, groundLayer + dynamicGravityLayer);

        //Overide gravity
        if (overideGravity) {
            Debug.Log("Using overided gravity");
        }
        //Attractor
        else if (attractor != null) {
            SetGravityDirection(9.81f, (transform.position - attractor.transform.position).normalized, true);
        }
        //Horizontal checks
        else if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), moveDirection, out gcHit, groundAngleCheckDistance, dynamicGravityLayer + groundLayer)) {
        
            float hitAngle = Vector3.Angle(gcHit.normal, transform.up);
        
        
            //Steps ---- Add check here for min angle to be considered step ---
            RaycastHit stepHit;
            if (Vector3.Angle(gcHit.normal, transform.up) > maxStepAngle) {
                if (!Physics.Raycast(transform.TransformPoint(new Vector3(groundAngleCheckOrigin.x, groundAngleCheckOrigin.x + maxStepHeight, groundAngleCheckOrigin.z)), moveDirection, out stepHit, groundAngleCheckDistance + .2f)) {
            
                    Debug.Log("Step");
            
                    //rb.position += transform.TransformVector(new Vector3(0, stepSmoothing * Time.deltaTime, 0));
                    rb.position += transform.up * stepSmoothing * Time.deltaTime;
                    grounded = true;
            
                }
                else {
                    grounded = true;
                    Debug.Log("Something happed here");
            
                    rb.velocity = transform.TransformVector(new Vector3(rb.velocity.x, 0, rb.velocity.y));
                }
            }
            else {
                grounded = true;
            
                slopeAngle = Vector3.ProjectOnPlane(moveDirection, gcHit.normal).normalized;
                SetGravityDirection(9.81f, gcHit.normal, false);
            }


            //Vector3.Angle(gcHit.normal, transform.up) >= maxGravityChange.x && Vector3.Angle(gcHit.normal, transform.up) <= maxGravityChange.y
            if (gcHit.transform.gameObject.layer == LayerMask.NameToLayer("Dynamic gravity")) {
        
                Debug.DrawLine(transform.TransformPoint(groundAngleCheckOrigin), transform.TransformPoint(groundAngleCheckOrigin) + cachedMoveDirection * groundAngleCheckDistance, Color.cyan, 1f);
                SetGravityDirection(9.81f, gcHit.normal, true);
                Debug.Log("Gravity direction changed");
            }
        
        }       
        
        //Vertical checks
        else if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), -transform.up, out gcHit, 10f)) {

            //targetRotation = Quaternion.FromToRotation(transform.up, gcHit.normal) * transform.rotation;
            //SetGravityDirection(9.81f, gcHit.normal, true);


            if (Vector3.Angle(gcHit.normal, transform.up) >= maxGravityChange.x && Vector3.Angle(gcHit.normal, transform.up) <= maxGravityChange.y && gcHit.transform.gameObject.layer == LayerMask.NameToLayer("Dynamic gravity")) {
                slopeAngle = moveDirection;

                SetGravityDirection(9.81f, gcHit.normal, true);
                targetRotation = Quaternion.FromToRotation(transform.up, gcHit.normal) * transform.rotation;
                Debug.Log("Gravity changed");

            }
            else {
                slopeAngle = Vector3.ProjectOnPlane(moveDirection, gcHit.normal).normalized;
                SetGravityDirection(9.81f, gcHit.normal, false);         
                Debug.Log("Test test");
            }
        }

        playerObject.GetComponent<Animator>().SetBool("falling", !grounded);

        if (rotateWithMovingPlatforms) targetRotation = transform.rotation; //Used to store current look direction for smooth gravity changes


    }

    //Called every frame
    private void Update() {


        //Sets players movment speed 
        sprinting = pi.actions.FindAction("Sprint").IsPressed();
        moveSpeed = sprinting ? sprintSpeed : walkSpeed;
        playerObject.GetComponent<Animator>().SetBool("sprinting", sprinting);

        //Gets axis inputs from the player
        mousePosition = pi.actions.FindAction("Look").ReadValue<Vector2>() * lookSpeed; //Mouse inputs
        mousePosition.y = Mathf.Clamp(mousePosition.y, cameraAngleLimits.x, cameraAngleLimits.y);

        movementInput = pi.actions.FindAction("Move").ReadValue<Vector2>(); //Movement inputs, Taking into acount camera direction
        moveDirection = playerCamera.transform.forward * movementInput.y + playerCamera.transform.right * movementInput.x;
        moveDirection = Vector3.ProjectOnPlane(moveDirection, transform.up).normalized;

        if (movementInput != Vector2.zero) inputCache = movementInput; //The saved direction from the last time the player moved
        cachedMoveDirection = playerCamera.transform.forward * inputCache.y + playerCamera.transform.right * inputCache.x;
        cachedMoveDirection = Vector3.ProjectOnPlane(cachedMoveDirection, transform.up).normalized;
        slopeAngle = moveDirection;

        //animator.SetFloat("inputMagnitude", movementInput.magnitude); 

        //Temp player teleport upwards code
        if (Input.GetKeyDown(KeyCode.F)) {
            gameObject.transform.position += new Vector3(0, 110, 0);
        }

        GroundChecks();
        //Limits the players speed 
        //LimitPlayerSpeed();

        //IsGrounded();
        LimitPlayerSpeed();

        //Sets player drag depending on weather they are grounded or not
        rb.drag = grounded ? playerDrag : 0;

        //Gets the time since player has fallen off of a ledge
        if (grounded) {
            timeSinceFall = 0;
            isFalling = false;
            numberOfJumps = 0;
        }
        else {
            if (!isFalling) {
                timeFell = Time.time;
                isFalling = true;
                numberOfJumps ++;
            }
            timeSinceFall = Time.time - timeFell;
        }

        //Rotates the player camera
        RotatePlayer();

        localVelocty = transform.InverseTransformDirection(rb.velocity);

        //Debug text 
        GameObject.Find("Debug1").GetComponent<TMP_Text>().text = "Movement " + moveDirection;
        GameObject.Find("Debug2").GetComponent<TMP_Text>().text = "Vertical speed : " + localVelocty.y.ToString("F2");
        GameObject.Find("Debug3").GetComponent<TMP_Text>().text = "Grounded : " + grounded;
        GameObject.Find("Debug4").GetComponent<TMP_Text>().text = "Speed " + rb.velocity.magnitude.ToString("F2");
        Debug.DrawRay(transform.position, Physics.gravity.normalized * 10, Color.yellow, Time.deltaTime);
        Debug.DrawRay(transform.position, slopeAngle * 10, Color.green, Time.deltaTime);

        
        Debug.DrawLine(transform.TransformPoint(groundAngleCheckOrigin), transform.TransformPoint(groundAngleCheckOrigin) + cachedMoveDirection * groundAngleCheckDistance, Color.red);

        //Rotates player to target direection
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, gravityChangeSpeed * Time.deltaTime);

        //Animate third person 
        playerObject.GetComponent<Animator>().SetFloat("inputMagnitude", movementInput.magnitude, .1f, Time.deltaTime);

        playerObject.GetComponent<Animator>().SetFloat("verticalVelocity", localVelocty.y, .1f, Time.deltaTime);

        if (cameraStyle == cameraStyles.Standard) {
            playerObject.GetComponent<Animator>().SetFloat("vInput", movementInput.magnitude, .1f, Time.deltaTime);

        }
        else {
            playerObject.GetComponent<Animator>().SetFloat("vInput", movementInput.y, .1f, Time.deltaTime);
            playerObject.GetComponent<Animator>().SetFloat("hInput", movementInput.x, .1f, Time.deltaTime);
        }

    }

    //Called every fixed framerate frame
    private void FixedUpdate() {
        //GroundChecks();

        //IsGrounded();

        MovePlayer();
    }

    
}
