using Cinemachine;
using Cinemachine.Utility;
using System;
using System.Numerics;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.Rendering.DebugUI;
using Cursor = UnityEngine.Cursor;
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

    //Moving variables
    public float walkSpeed = 5;
    public float sprintSpeed = 8;
    float moveSpeed = 5;
    public float acceleration = 10;
    public float playerDrag = 1;
    public AnimationCurve verticalInputMap;
    public enum cameraStyles {Standard, Locked, Focused}
    public cameraStyles cameraStyle;
    public float TPRotationSpeed = 500;
    public Transform cameraTarget;

    //Junmping variables
    public float jumpHeight = 1;
    public int maxJumps = 1;

    public bool dynamicJumpForce;           // Add dyanamic jumps //
    public AnimationCurve dynaamicJumpForceCurve;

    //Ground check variables
    public Vector3 groundCheckOrigin;
    public float groundCheckDistance = .1f;
    public Vector3 groundAngleCheckOrigin;
    public float groundAngleCheckDistance = .7f;
    public LayerMask groundMask;
    public float maxStepHeight;
    public float maxStepDepth;
    public float stepSmoothing;
    [Range(0,10)]public float gravityChangeSpeed = .1f;
    public Vector2 maxGravityChange;
    private float angleTolerance = 0.001f;

    //Gravity variables
    private float timeFell;
    [HideInInspector] public float timeSinceFall = 0;
    [HideInInspector] public bool isFalling;
    public Vector3 gravityDirection;
    public Transform attractor;
    
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
    RaycastHit hit;

    public Vector3 slopeAngle;
    bool onSlope;

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
        grounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOrigin), groundCheckDistance, groundMask);

        return Physics.CheckSphere(transform.TransformPoint(groundCheckOrigin), groundCheckDistance, groundMask);
    }

    //Makes the player jump
    void OnJump() {
        //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (numberOfJumps < maxJumps) {
            rb.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
            numberOfJumps ++;
            playerObject.GetComponent<Animator>().SetTrigger("jump");
        }
    }

    //Moves rhe player along the desired plane
    void MovePlayer() {
        //rb.AddForce(moveDirection * moveSpeed * acceleration, ForceMode.Force);
        rb.AddForce(slopeAngle * moveSpeed * acceleration, ForceMode.Force);


        playerCamera.m_Lens.FieldOfView = Mathf.Lerp(playerCamera.m_Lens.FieldOfView, cameraFOVCurve.Evaluate(rb.velocity.magnitude), cameraFOVAdjustSpeed);
    }

    //Limit movement speed
    public void LimitPlayerSpeed() {
        Vector3 convertedVelocity = transform.InverseTransformDirection(rb.velocity);
        horizontalVelocity = new Vector3(convertedVelocity.x, 0, convertedVelocity.z);
        Vector3 limitedVal = horizontalVelocity.normalized * moveSpeed;

        //if (rb.velocity.magnitude > moveSpeed) {
        //    rb.velocity = rb.velocity.normalized - rb.velocity * moveSpeed;
        //}

        if (horizontalVelocity.magnitude > moveSpeed) {
            rb.velocity = transform.TransformDirection(limitedVal.x, convertedVelocity.y, limitedVal.z);
            //rb.velocity = Vector3.ProjectOnPlane(new Vector3(limitedVal.x, convertedVelocity.y, limitedVal.z), hit.normal);
        }

        //if (rb.velocity != Vector3.zero) 
        //currentDirection = transform.TransformDirection(convertedVelocity.normalized.x, convertedVelocity.y, convertedVelocity.normalized.z).normalized;
    }

    //Sets the direction of the gravity
    public void SetGravityDirection(float newGrvaityStrenght, Vector3 upVector, bool concerveMomentum) {
        //targetRotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;
        Physics.gravity = upVector * -9.81f;
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

                case cameraStyles.Focused:
                    angle = Quaternion.LookRotation(cachedMoveDirection.normalized, transform.up);
                    playerCamera.LookAt = cameraTarget;
                    break;
            }
        }



        

        //Rotate player
        if (movementInput != Vector2.zero) {
            playerObject.rotation = Quaternion.RotateTowards(playerObject.rotation, angle, TPRotationSpeed * Time.deltaTime);
            targetRotation = transform.rotation; //Used to store current look direction for smooth gravity changes
        }

        //Camera rotation
        upAngle = Mathf.Clamp(mousePosition.y + upAngle, cameraAngleLimits.x, cameraAngleLimits.y);
        sideAngle = mousePosition.x + sideAngle;
        playerCamera.transform.localRotation = Quaternion.Euler(-upAngle, sideAngle, 0);
    }

    void gravity() { 
        rb.AddForce(transform.up * -9.81f, ForceMode.Force);
    }

    //Checks to see if the player is grounded as well as the angle of the ground
    void GroundChecks() {


        ;
        //
        if (attractor != null) {
            SetGravityDirection(9.81f, (transform.position - attractor.transform.position).normalized, true);
        }
        //
        else if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), moveDirection, out hit, groundAngleCheckDistance)) {
        
            if (!Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin + new Vector3(0, maxStepHeight,0)), moveDirection, out hit, groundAngleCheckDistance + .2f)) {
                grounded = true;
        
                rb.position += new Vector3(0, stepSmoothing * Time.deltaTime,0);
            }
            else {
                grounded = true;
        
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.y);
            }
        
            //Ground angle check
            if (Vector3.Angle(hit.normal, transform.up) >= maxGravityChange.x + angleTolerance && Vector3.Angle(hit.normal, transform.up) <= maxGravityChange.y + angleTolerance) {
                Debug.DrawLine(transform.TransformPoint(groundAngleCheckOrigin), transform.TransformPoint(groundAngleCheckOrigin) + cachedMoveDirection * groundAngleCheckDistance, Color.cyan, 1f);
        
                SetGravityDirection(9.81f, hit.normal, true);
            }
        }       
        //
        else if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), -transform.up, out hit, 5f)) {

            //if (!grounded) transform.position = hit.point;s

            slopeAngle = Vector3.ProjectOnPlane(moveDirection, hit.normal).normalized;
            Debug.Log(slopeAngle);
            SetGravityDirection(9.81f, hit.normal, false);


            //if (Vector3.Angle(hit.normal, transform.up) >= maxGravityChange.x + angleTolerance && Vector3.Angle(hit.normal, transform.up) <= maxGravityChange.y + angleTolerance) {
            //    SetGravityDirection(9.81f, hit.normal, false);
            //}
        }

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
        //moveDirection = Vector3.ProjectOnPlane(moveDirection, transform.up).normalized;

        if (movementInput != Vector2.zero) inputCache = movementInput; //The saved direction from the last time the player moved
        cachedMoveDirection = playerCamera.transform.forward * inputCache.y + playerCamera.transform.right * inputCache.x;
        cachedMoveDirection = Vector3.ProjectOnPlane(cachedMoveDirection, transform.up).normalized;

        //animator.SetFloat("inputMagnitude", movementInput.magnitude); 

        //Temp player teleport upwards code
        if (Input.GetKeyDown(KeyCode.F)) {
            gameObject.transform.position += new Vector3(0, 110, 0);
        }

        //Limits the players speed 

        IsGrounded();

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


        GroundChecks();

        //LimitPlayerSpeed();


        //Debug text 
        GameObject.Find("Debug1").GetComponent<TMP_Text>().text = "On slope : " + onSlope;
        GameObject.Find("Debug2").GetComponent<TMP_Text>().text = "Horizontal : " + horizontalVelocity;
        GameObject.Find("Debug3").GetComponent<TMP_Text>().text = "Grounded : " + grounded;
        GameObject.Find("Debug4").GetComponent<TMP_Text>().text = "Speed " + rb.velocity.magnitude.ToString("F2");
        Debug.DrawLine(transform.TransformPoint(groundAngleCheckOrigin), transform.TransformPoint(groundAngleCheckOrigin) + cachedMoveDirection * groundAngleCheckDistance, Color.red);
        Debug.DrawRay(transform.position, transform.up * 10, Color.blue, Time.deltaTime);
        Debug.DrawRay(transform.position, Physics.gravity.normalized * 10, Color.yellow, Time.deltaTime);
        Debug.DrawRay(transform.position, slopeAngle * 10, Color.green, Time.deltaTime);




        //Rotates player to target direection
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, gravityChangeSpeed * Time.deltaTime);

        //Animate third person 
        playerObject.GetComponent<Animator>().SetFloat("inputMagnitude", movementInput.magnitude, .1f, Time.deltaTime);
        playerObject.GetComponent<Animator>().SetFloat("vInput", movementInput.y, .1f, Time.deltaTime);
        playerObject.GetComponent<Animator>().SetFloat("hInput", movementInput.x, .1f, Time.deltaTime);
        playerObject.GetComponent<Animator>().SetFloat("verticalVelocity", localVelocty.y, .1f, Time.deltaTime);
        playerObject.GetComponent<Animator>().SetBool("falling", !grounded);


    }

    //Called every fixed framerate frame
    private void FixedUpdate() {
        MovePlayer();
        //gravity();
    }

    
}
