using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public float acceleration = 10;
    public float jumpHeight = 1;
    public float lookSpeed;
    public float gravity = -9.81f;
    public Vector2 maxLookAngle = new Vector2(-85, 85);
    public float playerDrag = 1;

    public Vector3 groundCheckOrigin;
    public float groundCheckDistance = .1f;
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

    // Update is called once per frame
    

    bool IsGrounded() {
        return Physics.CheckSphere(transform.TransformPoint(groundCheckOrigin), groundCheckDistance, groundMask);


    }

    void OnJump() {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
    }

    

    //Moves rhe player along the desired plane
    void MovePlayer() {
        rb.AddForce(moveDirection.normalized * walkSpeed * acceleration, ForceMode.Force);
    }



    public void LimitPlayerSpeed() {
        //Limit movement speed
        Vector3 flatVal;// = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        flatVal = transform.InverseTransformDirection(rb.velocity);

        Vector3 horizontalSpeed = Vector2.ClampMagnitude(new Vector2(flatVal.x, flatVal.y), walkSpeed);
        flatVal.x = horizontalSpeed.x;
        flatVal.x = horizontalSpeed.y;

        //flatVal.x = Mathf.Clamp(flatVal.x, -walkSpeed, walkSpeed);
        //flatVal.z = Mathf.Clamp(flatVal.z, -walkSpeed, walkSpeed);

        rb.velocity = transform.TransformDirection(flatVal);

        //flatVal = Vector3.zero;

        //if (rb.velocity.magnitude >= walkSpeed) {
        //    //rb.velocity = Vector3.ClampMagnitude(rb.velocity, walkSpeed);
        //    Vector3 limitedVal = flatVal.normalized * walkSpeed;
        //    rb.velocity = new Vector3(limitedVal.x, rb.velocity.y, limitedVal.z);
        //}
        GameObject.Find("Debug1").GetComponent<TMP_Text>().text = "Normalized val :  " + flatVal;


        //if (flatVal.magnitude >= walkSpeed) {
        //    Vector3 limitedVal = flatVal.normalized * walkSpeed;
        //
        //    rb.velocity = new Vector3(limitedVal.x, rb.velocity.y, limitedVal.z);
        //}

        //Vector3 xzVel = Vector3.Normalize( new Vector3(rb.velocity.x, 0, rb.velocity.z));
        //Vector3 yVel = Vector3.Cross(new Vector3(0, rb.velocity.y, 0).normalized, Vector3.up);
        //
        //xzVel = Vector3.ClampMagnitude(xzVel, walkSpeed);
        //yVel = Vector3.ClampMagnitude(yVel, 50);

        //rb.velocity = xzVel;

        //rb.velocity = Vector3.ClampMagnitude(rb.velocity, walkSpeed) - transform.up;
        //rb.velocity = Vector3.ClampMagnitude(new Vector3(rb.velocity.x, rb.velocity.y, rb.velocity.z), walkSpeed) - transform.up;


    }

    //Rotates the players camera
    void RotatePlayer() {
        transform.Rotate(Vector3.up, mousePosition.x);
        Camera.main.transform.Rotate(Vector3.right, -mousePosition.y);
    }

    public void SetGravityDirection(Vector3 newGravityDirection, float newGrvaityStrenght, Vector3 upVector) {
        gravityDirection = newGravityDirection;
        Physics.gravity = upVector * newGrvaityStrenght;
        transform.eulerAngles = gravityDirection;
    }

    private void Update() {
        //Gets axis inputs from the player
        movement = pi.actions.FindAction("Move").ReadValue<Vector2>();
        moveDirection = transform.forward * movement.y + transform.right * movement.x;
        mousePosition = pi.actions.FindAction("Look").ReadValue<Vector2>() * lookSpeed;
        mousePosition.y = Mathf.Clamp(mousePosition.y, maxLookAngle.x, maxLookAngle.y);

        //Rotates the player camera
        RotatePlayer();

        //Temp player teleport upwards code
        if (Input.GetKeyDown(KeyCode.F)) {
            gameObject.transform.position += new Vector3(0, 110, 0);
        }



        //sets player drag depending on weather they are grounded or not
        rb.drag = IsGrounded() ? playerDrag * (1 - movement.magnitude) : 0;

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
        GameObject.Find("Debug4").GetComponent<TMP_Text>().text = "Speed " + rb.velocity.magnitude;

    }

    private void FixedUpdate() {

        MovePlayer();
        LimitPlayerSpeed();

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
