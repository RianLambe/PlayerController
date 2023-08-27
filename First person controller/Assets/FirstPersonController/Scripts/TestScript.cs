using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(transform.position, transform.up * 10f, Color.cyan);

        GameObject.Find("Debug1").GetComponent<TMP_Text>().text = "Parent delta rotation :  " + transform.rotation.eulerAngles;
    }
}
