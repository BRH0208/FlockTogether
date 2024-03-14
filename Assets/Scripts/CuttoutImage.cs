using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;


// Code sourced from the video "Make a Cutout Mask In Unity! (Inverted Mask)" by Code Monkey
public class CuttoutImage : Image
{
    public override Material materialForRendering {
		get {
			Material material = new Material(base.materialForRendering);
			material.SetInt("_StencilComp",(int) CompareFunction.NotEqual);
			return material;
		}
	}
}
