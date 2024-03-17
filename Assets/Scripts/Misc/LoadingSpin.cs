using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingSpin : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
		float z = transform.localRotation.eulerAngles.z;
        transform.localRotation = Quaternion.Euler(0,0, z-45f * Time.deltaTime);
    }
}
