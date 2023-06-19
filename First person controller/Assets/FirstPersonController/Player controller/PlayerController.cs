using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
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

    public float walkSpeed = 5;
    public float sprintSpeed = 8;
    float moveSpeed = 5;
    public float acceleration = 10;
    public float playerDrag = 1;
    public float jumpHeight = 1;

    public Vector3 groundCheckOrigin;
    public float groundCheckDistance = .1f;
    public Vector3 groundAngleCheckOrigin;
    public LayerMask groundMask;
    [Range(0,1)]public float gravityChangeSpeed = .1f;
    public Vector2 maxGravityChange;

    private float timeFell;
    [HideInInspector] public float timeSinceFall = 0;
    [HideInInspector] public bool isFalling;
    public Vector3 gravityDirection;
    public Transform attractor;
    
    //Input variables
    PlayerInput pi;
    Vector2 movement;
    Vector3 moveDirection;
    Vector2 mousePosition;

    //misc game variables
    float upAngle = 0;
    Quaternion targetRotation = Quaternion.identity;
    CinemachineVirtualCamera cam;
    Vector3 currentDirection;

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
        rb.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
    }

    //Moves rhe player along the desired plane
    void MovePlayer() {
        rb.AddForce(moveDirection.normalized * moveSpeed * acceleration, ForceMode.Force);
    }

    //Limit movement speed
    public void LimitPlayerSpeed() {
        Vector3 convertedVelocity = transform.InverseTransformDirection(rb.velocity);
        Vector3 horizontalVelocity = new Vector3(convertedVelocity.x, 0, convertedVelocity.z);

        if (horizontalVelocity.magnitude > moveSpeed) {
            Vector3 limitedVal = horizontalVelocity.normalized * moveSpeed;
            rb.velocity = transform.TransformDirection(limitedVal.x, convertedVelocity.y, limitedVal.z);
        }
    }


    //Sets the direction of the gravity
    public void SetGravityDirection(float newGrvaityStrenght, Vector3 upVector) {
        //gravityDirection = newGravityDirection;
        //Physics.gravity = upVector * newGrvaityStrenght;
        //transform.eulerAngles = gravityDirection;

        //transform.rotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;
        //Physics.gravity = upVector * -9.81f;
        Debug.Log("Gravity direction changed");
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
        //RaycastHit hit;
        //if (Physics.Raycast(transform.localPosition + groundAngleCheckOrigin, transform.forward, out hit, .65f)) {
        //    SetGravityDirection(Quaternion.LookRotation(hit.normal).eulerAngles, -9.81f, hit.normal);
        //    Debug.Log(hit.normal);
        //    Debug.DrawLine(transform.localPosition + groundAngleCheckOrigin, hit.point);
        //}

        //Sets players movment speed 
        moveSpeed = pi.actions.FindAction("Sprint").IsPressed() ? sprintSpeed : walkSpeed;

        //Gets axis inputs from the player
        mousePosition = pi.actions.FindAction("Look").ReadValue<Vector2>() * lookSpeed;
        mousePosition.y = Mathf.Clamp(mousePosition.y, maxLookAngle.x, maxLookAngle.y);
        movement = pi.actions.FindAction("Move").ReadValue<Vector2>();
        moveDirection = transform.forward * movement.y + transform.right * movement.x;

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
        GameObject.Find("Debug3").GetComponent<TMP_Text>().text = "Grounded : " + IsGrounded();
        GameObject.Find("Debug4").GetComponent<TMP_Text>().text = "Speed " + rb.velocity.magnitude.ToString("F2");

;

        currentDirection = moveDirection - transform.up;

        RaycastHit hit;
        if (attractor != null) {
            SetGravityDirection(9.81f, (transform.position - attractor.transform.position).normalized);
        }
        else if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), currentDirection, out  hit, 2f)) {
            if(Vector3.Angle(hit.normal, transform.up) >= maxGravityChange.x && Vector3.Angle(hit.normal, transform.up) <= maxGravityChange.y) {
                //transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                //targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                //Physics.gravity = hit.normal * -9.81f;
                currentDirection = hit.normal.normalized;
                Debug.Log(hit.transform.name);
                SetGravityDirection(9.81f, hit.normal);
                test.transform.position = hit.point;

            }   
        }
        else if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), -transform.up, out hit, .65f)) {
            if (Vector3.Angle(hit.normal, transform.up) >= maxGravityChange.x && Vector3.Angle(hit.normal, transform.up) <= maxGravityChange.y) {
                //transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                //targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                //Physics.gravity = hit.normal * -9.81f;
                SetGravityDirection(9.81f, hit.normal);

            }
        }
        
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, gravityChangeSpeed);
    }

    private void FixedUpdate() {
        MovePlayer();
    }

    private void OnValidate() {

    }
}
