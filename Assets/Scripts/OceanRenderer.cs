using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OceanRenderer : MonoBehaviour
{
	// Width and height of the texture in pixels.
    private int pixWidth;
    private int pixHeight;

    // The number of cycles of the basic noise pattern that are repeated
    // over the width and height of the texture.
    public float scale;
	public float intensity;
	public float timeDelta = 0.1f;
	public int pixelScale; // How many pixels per pixel of noise?
    private Texture2D noiseTex;
    private Color[] pix;
    private RawImage image;
	private Canvas renderedCanvas;
	private Camera gameCamera;
	private bool inPlay = false;

    void Start()
    {
		renderedCanvas = transform.parent.GetComponent<Canvas>();
		gameCamera = renderedCanvas.worldCamera;
        image = GetComponent<RawImage>();
		inPlay = true; // prevent errors in editor
		updateSize();
    }
	private void OnRectTransformDimensionsChange()
    { 
		if(inPlay){
			updateSize();
		}
	}
	
	private void updateSize() {
		// Set up the texture and a Color array to hold pixels during processing.
        pixWidth = Screen.width / pixelScale;
		pixHeight = Screen.height / pixelScale;
		noiseTex = new Texture2D(pixWidth, pixHeight);
        pix = new Color[noiseTex.width * noiseTex.height];
        image.texture = noiseTex;
	}
    void CalcNoise()
    {
		float time = Time.time;
        // For each pixel in the texture...
        float y = 0.0F;

        while (y < noiseTex.height)
        {
            float x = 0.0F;
            while (x < noiseTex.width)
            {
				Vector3 noisePos = gameCamera.ScreenToWorldPoint(new Vector3(x*pixelScale,y*pixelScale,0));
                float xCoord = noisePos.x * scale;
                float yCoord = noisePos.y * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
				//sample = Mathf.PerlinNoise(-time/timeDelta, sample); // Lazy 3d noise;
                
				pix[(int)y * noiseTex.width + (int)x] = new Color(1-sample*intensity, 1-sample*intensity, 1-sample*intensity);
                x++;
            }
            y++;
        }

        // Copy the pixel data to the texture and load it into the GPU.
        noiseTex.SetPixels(pix);
        noiseTex.Apply();
    }

    void Update()
    {
		CalcNoise();
    }
}
