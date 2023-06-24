using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
    //Components
    Rigidbody rb;

    //Player settings
    [Header("Player settings")]
    public float lookSpeed = .2f;
    public Vector2 maxLookAngle = new Vector2(-80, 80);
    public Vector3 cameraOffset;
    public AnimationCurve cameraFOVCurve;

    public float walkSpeed = 5;
    public float sprintSpeed = 8;
    float moveSpeed = 5;
    public float acceleration = 10;
    public float playerDrag = 1;

    public float jumpHeight = 1;
    public int maxJumps = 1;

    public Vector3 groundCheckOrigin;
    public float groundCheckDistance = .1f;
    public Vector3 groundAngleCheckOrigin;
    public float groundAngleCheckDistance = .7f;
    public LayerMask groundMask;
    [Range(0,10)]public float gravityChangeSpeed = .1f;
    public Vector2 maxGravityChange;
    private float angleTolerance = 0.001f;

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

    //misc game variables
    float upAngle = 0;
    [SerializeField]Quaternion targetRotation = Quaternion.identity;
    CinemachineVirtualCamera cam;
    Vector3 currentDirection;
    int numberOfJumps;

    Vector3 f;
    Vector3 gravityCache;
    Vector2 inputCache;

    public GameObject test;

    private void Awake() {
        cam = GetComponentInChildren<CinemachineVirtualCamera>();
    }

    //Called at begining of game
    private void Start() {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        pi = GetComponent<PlayerInput>();
    }



    //Checks if the player is touching the ground
    bool IsGrounded() {
        return Physics.CheckSphere(transform.TransformPoint(groundCheckOrigin), groundCheckDistance, groundMask);
    }

    //Makes the player jump
    void OnJump() {
        //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (numberOfJumps < maxJumps) {
            rb.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
            numberOfJumps ++;
            Debug.Log(numberOfJumps);
        }
    }

    //Moves rhe player along the desired plane
    void MovePlayer() {
        rb.AddForce(moveDirection.normalized * moveSpeed * acceleration, ForceMode.Force);

        cam.m_Lens.FieldOfView = cameraFOVCurve.Evaluate(rb.velocity.magnitude);
    }

    //Limit movement speed
    public void LimitPlayerSpeed() {
        Vector3 convertedVelocity = transform.InverseTransformDirection(rb.velocity);
        Vector3 horizontalVelocity = new Vector3(convertedVelocity.x, 0, convertedVelocity.z);


        Vector3 limitedVal = horizontalVelocity.normalized * moveSpeed;

        if (horizontalVelocity.magnitude > moveSpeed) {
            rb.velocity = transform.TransformDirection(limitedVal.x, convertedVelocity.y, limitedVal.z);
        }

        if (rb.velocity != Vector3.zero) 
        currentDirection = transform.TransformDirection(convertedVelocity.normalized.x, convertedVelocity.y, convertedVelocity.normalized.z).normalized;



    }


    //Sets the direction of the gravity
    public void SetGravityDirection(float newGrvaityStrenght, Vector3 upVector, bool concerveMomentum) {
        targetRotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;
        Physics.gravity = upVector * -9.81f;
    }

    //Rotates the players camera
    void RotatePlayer() {
        upAngle = Mathf.Clamp(mousePosition.y + upAngle, maxLookAngle.x, maxLookAngle.y);
        cam.transform.localRotation = Quaternion.Euler(-upAngle, 0, 0);
        transform.Rotate(Vector3.up, mousePosition.x);

        targetRotation = transform.rotation; //Used to store current look direction for smooth gravity changes
    }



    private void Update() {
        //Sets players movment speed 
        moveSpeed = pi.actions.FindAction("Sprint").IsPressed() ? sprintSpeed : walkSpeed;

        //Gets axis inputs from the player
        mousePosition = pi.actions.FindAction("Look").ReadValue<Vector2>() * lookSpeed;
        mousePosition.y = Mathf.Clamp(mousePosition.y, maxLookAngle.x, maxLookAngle.y);
        movementInput = pi.actions.FindAction("Move").ReadValue<Vector2>();
        moveDirection = transform.forward * movementInput.y + transform.right * movementInput.x;

        //Temp player teleport upwards code
        if (Input.GetKeyDown(KeyCode.F)) {
            gameObject.transform.position += new Vector3(0, 110, 0);
        }

        //Limits the players speed 
        LimitPlayerSpeed();

        //Sets player drag depending on weather they are grounded or not
        rb.drag = IsGrounded() ? playerDrag : 0;

        //Gets the time since player has fallen off of a ledge
        if (IsGrounded()) {
            timeSinceFall = 0;
            isFalling = false;
            numberOfJumps = 0;
        }
        else {
            if (!isFalling) {
                timeFell = Time.time;
                isFalling = true;
            }
            timeSinceFall = Time.time - timeFell;
        }

        //Rotates the player camera
        RotatePlayer();

        //Debug text 
        GameObject.Find("Debug1").GetComponent<TMP_Text>().text = "Movement " + moveDirection;
        GameObject.Find("Debug2").GetComponent<TMP_Text>().text = "Somehting " + (currentDirection + moveDirection.normalized);
        GameObject.Find("Debug3").GetComponent<TMP_Text>().text = "Grounded : " + IsGrounded();
        GameObject.Find("Debug4").GetComponent<TMP_Text>().text = "Speed " + rb.velocity.magnitude.ToString("F2");


        if (movementInput != Vector2.zero) {
            inputCache = movementInput;
        }

        //if (movementInput != Vector2.zero) {
        //    f = transform.forward * movementInput.y + transform.right * movementInput.x;
        //}

        f = transform.forward * inputCache.y + transform.right * inputCache.x;



        Debug.DrawLine(transform.TransformPoint(groundAngleCheckOrigin), transform.TransformPoint(groundAngleCheckOrigin) + f * groundAngleCheckDistance, Color.red);

        RaycastHit hit;
        if (attractor != null) {
            SetGravityDirection(9.81f, (transform.position - attractor.transform.position).normalized, true);
        }
        else if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), f, out hit, groundAngleCheckDistance)) {

            //Debug.Log(Vector3.Angle(hit.normal, transform.up)); debug show angle

            if (Vector3.Angle(hit.normal, transform.up) >= maxGravityChange.x + angleTolerance && Vector3.Angle(hit.normal, transform.up) <= maxGravityChange.y + angleTolerance) {
                Debug.DrawLine(transform.TransformPoint(groundAngleCheckOrigin), transform.TransformPoint(groundAngleCheckOrigin) + f * groundAngleCheckDistance, Color.cyan, 1f);

                SetGravityDirection(9.81f, hit.normal, true);
            }
        }
        else if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), -transform.up, out hit, groundAngleCheckDistance)) {
            if (Vector3.Angle(hit.normal, transform.up) >= maxGravityChange.x + angleTolerance && Vector3.Angle(hit.normal, transform.up) <= maxGravityChange.y + angleTolerance) {
                SetGravityDirection(9.81f, hit.normal, false);
            }
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, gravityChangeSpeed * Time.deltaTime);
    }

    void OnTest1() {
        gravityCache = transform.InverseTransformVector(rb.velocity);
    }

    void OnTest2() {
        rb.velocity = transform.TransformVector(gravityCache);
    }

    private void FixedUpdate() {
        MovePlayer();
    }

    private void OnValidate() {

    }
}
