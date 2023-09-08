using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class MatrixTest : MonoBehaviour
{
    Matrix4x4 testMatrix;
    public Vector3 newRotation;

    private void Awake() {
        testMatrix.SetTRS(Vector3.zero, Quaternion.identity, Vector3.one);
    }

    // Update is called once per frame
    void Update()
    {
        //testMatrix.SetTRS(testMatrix.GetPosition(), Quaternion.Euler(newRotation), Vector3.one);
        //Debug.DrawRay(transform.position, testMatrix * Vector3.up * 10, Color.red);
        //Debug.DrawRay(transform.position, testMatrix * Vector3.forward * 10, Color.blue);
        //Debug.DrawRay(transform.position, -(testMatrix * Vector3.up * 10), Color.black);

        Debug.DrawRay(transform.position, transform.up * 10, Color.black);

    }
}
