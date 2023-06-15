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
    public float walkSpeed = 5;
    public float sprintSpeed = 8;
    public float moveSpeed = 5;
    public float acceleration = 10;
    public float jumpHeight = 1;
    public float lookSpeed;
    public float gravity = -9.81f;
    public Vector2 maxLookAngle = new Vector2(-85, 85);
    public float playerDrag = 1;

    public Vector3 groundCheckOrigin;
    public float groundCheckDistance = .1f;
    public Vector3 groundAngleCheckOrigin;
    public LayerMask groundMask;
    private float timeFell;
    [HideInInspector] public float timeSinceFall = 0;
    [HideInInspector] public bool isFalling;
    public Vector3 gravityDirection;
    
    //Input variables
    PlayerInput pi;
    Vector2 movement;
    Vector3 moveDirection;
    Vector2 mousePosition;

    public GameObject temp;

    private void Start() {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        pi = GetComponent<PlayerInput>();
    }


    Vector3 newJumpVelocity = new Vector2(0f, 0f);

    //Rotates the players camera
    public float upAngle = 0;
    void RotatePlayer() {
        
        //Camera.main.transform.Rotate(Vector3.right, -mousePosition.y);

        upAngle = Mathf.Clamp(mousePosition.y + upAngle, -80,80);
        Camera.main.transform.localRotation = Quaternion.Euler(-upAngle, 0, 0);
        transform.Rotate(Vector3.up, mousePosition.x);
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
    public void SetGravityDirection(Vector3 newGravityDirection, float newGrvaityStrenght, Vector3 upVector) {
        //gravityDirection = newGravityDirection;
        //Physics.gravity = upVector * newGrvaityStrenght;
        //transform.eulerAngles = gravityDirection;

        transform.rotation = Quaternion.FromToRotation(transform.up, upVector) * transform.rotation;
        Physics.gravity = upVector * -9.81f;
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

        //Debug text 
        GameObject.Find("Debug3").GetComponent<TMP_Text>().text = "Grounded : " + IsGrounded();
        GameObject.Find("Debug4").GetComponent<TMP_Text>().text = "Speed " + rb.velocity.magnitude.ToString("F2");

        RaycastHit hit;

        if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), transform.forward, out  hit, .65f)) {
            transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            Physics.gravity = hit.normal * -9.81f;

        }
        if (Physics.Raycast(transform.TransformPoint(groundAngleCheckOrigin), -transform.up, out hit, .65f)) {
            transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            Physics.gravity = hit.normal * -9.81f;
        }
        //Rotates the player camera
        RotatePlayer();
    }

    private void FixedUpdate() {

        MovePlayer();

        //LimitPlayerSpeed();

        //Vector3 newVelocity = new Vector2(0f, 0f);
        //
        //
        //
        //newVelocity += transform.forward * movement.y * acceleration;
        //newVelocity += transform.right * movement.x * acceleration;
        //if (!IsGrounded()) newJumpVelocity += transform.up * gravity * timeSinceFall;
        //
        ///////Limit movement speed
        /////if (rb.velocity.magnitude >= walkSpeed) {
        /////    rb.velocity = rb.velocity.normalized * walkSpeed;
        /////}
        /////
        /////if (Input.GetKey(KeyCode.Space)) {
        /////    newJumpVelocity = transform.up * 0;
        /////    newJumpVelocity += transform.up * jumpHeight;
        /////    Debug.Log("Jumped");
        /////}
        /////
        ///////Adds final new velocity to the players rigidbody
        /////rb.velocity = newVelocity + newJumpVelocity;

        //Debuging displays
        /////GameObject.Find("Debug1").GetComponent<TMP_Text>().text = "Raw velocity " + newVelocity.ToString();
        /////GameObject.Find("Debug2").GetComponent<TMP_Text>().text = "Magnitude " + newVelocity.magnitude;
    }
}
