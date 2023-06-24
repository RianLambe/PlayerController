using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityZone : MonoBehaviour
{
    [SerializeField] float grvaityStrenght = 9.81f;

    private void OnTriggerEnter(Collider other) {
        other.GetComponent<PlayerController>().SetGravityDirection(grvaityStrenght, transform.up, true);
    }
}
