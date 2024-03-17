using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchPos : MonoBehaviour
{
	public GameObject target;
    // Called after updates so that it always matches the position on the frame
    void LateUpdate()
    {
        this.transform.position = target.transform.position;
    }
}
