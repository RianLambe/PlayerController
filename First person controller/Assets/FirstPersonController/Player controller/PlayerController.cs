using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.UIElements;
using UnityEngine;

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

    public Vector3 groundCheckOrigin;
    public float groundCheckDistance = .1f;
    public LayerMask groundMask;

    //Misc
    Vector3 rotation;

    private void Start() {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    private void FixedUpdate() {
        Vector3 newVelocity = new Vector2(0f,0f);

        //Adds to the new velocity of the player
        newVelocity += transform.forward * Input.GetAxis("Vertical") * acceleration;
        newVelocity += transform.right * Input.GetAxis("Horizontal") * acceleration;
        //if (!IsGrounded()) newVelocity += transform.up * gravity;
        Debug.Log(IsGrounded());

        //Limit movement speed
        if (rb.velocity.magnitude >= walkSpeed) {
            rb.velocity = rb.velocity.normalized * walkSpeed;
        }

        //Jump
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded()) {
            newVelocity.y = jumpHeight;
        }





        //Rotates the player camera
        rotation.y += Input.GetAxis("Mouse X") * lookSpeed;
        rotation.x += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotation.x = Mathf.Clamp(rotation.x, maxLookAngle.x, maxLookAngle.y);
        //transform.eulerAngles = new Vector2(0, rotation.y) * lookSpeed;
        transform.eulerAngles += new Vector3(0, Input.GetAxis("Mouse X"), 0);
        Camera.main.transform.localRotation = Quaternion.Euler(rotation.x , 0, 0);

        rb.velocity = newVelocity;
        GameObject.Find("Debug1").GetComponent<TMP_Text>().text = "Raw velocity" + newVelocity.ToString();
        GameObject.Find("Debug2").GetComponent<TMP_Text>().text = "Magnitude" + newVelocity.magnitude;
    }

    bool IsGrounded() {
        return Physics.CheckSphere(transform.localPosition + groundCheckOrigin, groundCheckDistance, groundMask);
    }

}
