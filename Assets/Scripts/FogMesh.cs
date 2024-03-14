using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogMesh : MonoBehaviour
{
	public List<FogCutterCone> cones; 
	private Mesh mesh;
	// Add a cone to be tracked by this sprite
	public void addCone(FogCutterCone cone){
		cones.Add(cone);
	}
	
	// Untrack a fog cone
	public void removeCone(FogCutterCone cone){
		cones.Remove(cone);
	}
	
	public class FogCutterCone {
		// These two variables can be changed without consequence
		private Vector2 center;
		private float range; // The range of the cone;
		
		// The following variables are "fixed" in that they cannot be changed once a fog cutter is created
		private float fov; // Measured in degrees. 
		// if fov == 180 then the view is circular. 
		private float radPerVec; // Vectors per degree of the camera cone. 
		private float angle; // The angle of the center of the view cone, with 0=+y,90=+x,-90=-x. 
		
		// Internal variables
		Vector2[] _rays;
		public static Vector2 angleVec(float radians) {
			return new Vector2(Mathf.Sin(radians),Mathf.Cos(radians));
		}

		// Constructor
		// Creates a view camera starting at some origin and extending in some direction, extending in both directions with an FOV. 
		// Deg per vec indicates how many degrees are represented by a single vector. 
		public FogCutterCone (Vector2 origin, Vector2 dir, float fov = 180f, float radPerVec = 5f) {
			// Check for invalid objects
			if (dir == Vector2.zero){
				Debug.LogError("Invalid fog cutter. Cones cannot have no direction!");
			} else if (fov > 180f){
				Debug.LogError("Invalid fog cutter. Fov must be <= 180");
			} else if (radPerVec <= 0){
				Debug.LogError("Invalid fog cutter. Degrees per vector must be positive");
			}
			
			// Assign variables
			this.center = origin;
			this.radPerVec = radPerVec;
			this.fov = fov;
			this.angle = Mathf.Deg2Rad * Vector2.SignedAngle(Vector2.up,dir);
			this.range = dir.magnitude;
			if(this.rayCount() < 3){
				Debug.LogError("Invalid fog cutter. Must have atleast 3 rays");
			}
		}
		
		public int rayCount(){
			return 2*(int) (Mathf.Floor(fov/radPerVec)) + 1;
		}
		// Get all of the vectors(without obstacle detection) that represent this object
		// These are not scaled nor centered as those should be able to change without requiring new rays. 
		public Vector2[] getRays(){
			if(_rays == null){
				_rays = _getRays();
			}
			return _rays;
		}
		private Vector2[] _getRays(){
			int count = rayCount();
			int hcount = count/2;
			Vector2[] rayArray = new Vector2[count];
			rayArray[0] = angleVec(angle);
			for (int i = 1; i <= hcount; i++){
				rayArray[i] = angleVec(angle+radPerVec*i);
				rayArray[i+hcount] = angleVec(angle-radPerVec*i);
			}
			return rayArray;
		}
		
		public Vector2[] getScaledRays(){
			Vector2[] rays = getRays();
			for(int i = 0; i < rays.Length; i++){
				rays[i] = rays[i] * range + center; 
			}
			return rays;
		}
		
	}
	
	private int totalRayCount(){
		int sum = 0;
		foreach (FogCutterCone cone in cones){
			sum += cone.rayCount();
		}
		return sum;
	}
	public void Start() {
		cones = new List<FogCutterCone>();
		mesh  = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
	}
	public void LateUpdate()
    {
		// Don't do anything if we don't have view cones
		if(cones.Count <= 0){
			return;
		}
		
		int totalRays = totalRayCount();
		Vector3[] vertices = new Vector3[totalRays + 2]; // Each ray has 1 vertex, one vertex is origin.
		Vector2[] uv = new Vector2[vertices.Length]; // We have as many uv as verticies
		int[] triangles = new int[(totalRays - 1) * 3]; // A number of trianges equal to total rays - 1(fenceposting problem) 
		
		// We then EACH FRAME calculate the triangles for each ray.
		foreach (FogCutterCone cone in cones) {
			
		}
		vertices[0] = Vector3.zero;
		vertices[1] = new Vector3(50,0);
		vertices[2] = new Vector3(0,-50);
		
		triangles[0] = 0;
		triangles[1] = 1;
		triangles[2] = 2;
		
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		
    }
}
