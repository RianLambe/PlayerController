using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(transform.position, transform.up, Color.blue);
    }
}
