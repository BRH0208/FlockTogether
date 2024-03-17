using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Literally just a container for the system this instance uses.
public class preservable : MonoBehaviour
{
    public preservationSystem sys; // The system to preserve the object with
	// If the object attached to the preservable does not match the system, unexpected behavior may occur. 
}
