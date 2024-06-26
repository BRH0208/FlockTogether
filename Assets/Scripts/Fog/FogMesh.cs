using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogMesh : MonoBehaviour
{
	public List<FogCutterCone> cones; 
	private Mesh mesh;
	public ContactFilter2D fogBlocker;
	private static RaycastHit2D[] rayBuffer;
	private const int bufferSize = 1; // We only care about the first hit, so we have a cap of 1.
	
	// We swap objects layers, we keep them in list
	public List<HashSet<GameObject>> tempVisableObjects;
	private const int fixedUpdatesPerBuffer = 10;
	private int fixedUpdatesCounter = 0;
	private const int swapTimeBuffer = 3;
	// What happends if the entire buffer is flushed before the lateUpdate?
	// This only happens at really, really low framerates. We are allowed to flicker if the frame difference is so long
	// 0.02 seconds * fixedUpdatesPerBuffer * swapTimeBuffer is the max flicker rate
	// With 10 updates per buffer and 3 buffers the max flicker rate(assuming optimal bad framerate) is 1.67 per second 
	// approximatly "three per second" is bad for epilipsy, this is well below the danger rate.
	// https://www.epilepsy.com/stories/shedding-light-photosensitivity-one-epilepsys-most-complex-conditions-0
	public static FogMesh instance;
	
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
		public Vector2 center;
		public float range; // The range of the cone;
		
		// The following variables are "fixed" in that they cannot be changed once a fog cutter is created
		public float fov {get;} // Measured in degrees. 
		// if fov == 180 then the view is circular. 
		public float degPerVec{get;} // Vectors per degree of the camera cone. 
		public float angle{get;} // The angle of the center of the view cone, with 0=+y,90=+x,-90=-x. 
		
		// Internal variables
		Vector2[] _rays;
		public static Vector2 angleVec(float degrees) {
			float radians = Mathf.Deg2Rad * degrees;
			return new Vector2(Mathf.Sin(radians),Mathf.Cos(radians));
		}

		// Constructor
		// Creates a view camera starting at some origin and extending in some direction, extending in both directions with an FOV. 
		// Deg per vec indicates how many degrees are represented by a single vector. 
		public FogCutterCone (Vector2 origin, Vector2 dir, float fov = 180f, float degPerVec = 5f) {
			// Check for invalid objects
			if (dir == Vector2.zero){
				Debug.LogError("Invalid fog cutter. Cones cannot have no direction!");
			} else if (fov > 180f){
				Debug.LogError("Invalid fog cutter. Fov must be <= 180");
			} else if (degPerVec <= 0){
				Debug.LogError("Invalid fog cutter. Degrees per vector must be positive");
			}
			// Assign variables
			this.center = origin;
			this.degPerVec = degPerVec;
			this.fov = fov;
			this.angle = -Vector2.SignedAngle(Vector2.down,dir);
			this.range = dir.magnitude;
			if(this.rayCount() < 3){
				Debug.LogError("Invalid fog cutter. Must have atleast 3 rays");
			}
		}
		
		public int rayCount(){
			return 2*(int) (Mathf.Floor(fov/degPerVec)) + 1;
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
			for (int i = 0; i < count; i++){
				rayArray[i] = angleVec(angle+degPerVec*(i-hcount));
			}
			return rayArray;
		}
		
		public Vector2[] getCenteredRays(){
			Vector2[] rays = getRays();
			for(int i = 0; i < rays.Length; i++){
				rays[i] = rays[i] + center; 
			}
			return rays;
		}
		
	}
	
	// Sum the number of rays in every cone
	// used to allocate mesh arrays.
	private int totalRayCount(){
		int sum = 0;
		foreach (FogCutterCone cone in cones){
			sum += cone.rayCount();
		}
		return sum;
	}
	
	// Calculate the number of triangles we need to cover the last triangle in 180 degree(full circle) cones. 
	private int get180Adj(){
		int sum = 0;
		foreach (FogCutterCone cone in cones){
			if (cone.fov == 180f) {
				sum+=1;
			}
		}
		return sum;
	}	
	
	public void Start() {
		cones = new List<FogCutterCone>();
		mesh  = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		rayBuffer = new RaycastHit2D[bufferSize];
		tempVisableObjects = new List<HashSet<GameObject>>();
		for(int i = 0; i < swapTimeBuffer; i++){
			tempVisableObjects.Add(new HashSet<GameObject>());
		}
		instance = this;
		fixedUpdatesCounter = 0;
	}
	
	public void FixedUpdate(){
		fixedUpdatesCounter++;
		if(fixedUpdatesCounter < fixedUpdatesPerBuffer){return;}
		fixedUpdatesCounter = 0;
		int makeVisable = LayerMask.NameToLayer("OpaqueBlocker");	
		foreach (GameObject obj in tempVisableObjects[0]) {
			if(obj == null){
				continue; // This object was destroyed, we ignore it
			}
			bool noRef = true;
			for(int i = 1; i < swapTimeBuffer; i++){
				if(tempVisableObjects[i].Contains(obj)){
					noRef = false;
				}
			}
			if(noRef){
				obj.layer = makeVisable; // We turn off the temporary visability
			}
		}
		tempVisableObjects.Add(new HashSet<GameObject>()); // Push!
		tempVisableObjects.RemoveAt(0); // Pop!
		// TODO: Make this clear and cycle hashsets, make things easy on the garbage collector
		// Because currently this creates so much garbage for the cleaner to clean
	}
	
	public void LateUpdate()
    {
		
		if(cones == null){return;} // In rare cases, we can late update before we start.
		int makeVisable = LayerMask.NameToLayer("OpaqueBlocker");	
		// Don't do anything if we don't have view cones
		if(cones.Count <= 0){
			return;
		}
		int tempVisible = LayerMask.NameToLayer("tempVisible");
		int totalRays = totalRayCount();
		Vector3[] vertices = new Vector3[totalRays + cones.Count]; // Each ray has 1 vertex, each cone has a vertex for origin
		Vector2[] uv = new Vector2[vertices.Length]; // We have as many uv as verticies
		int[] triangles = new int[(totalRays - cones.Count + get180Adj()) * 3]; // A number of triangles.
		// For each ray, we have a triangle, except each cone loses 1 traingle to fenceposting, except for 360 degree triangles which gain it back.
		// Mutliplied by three as each triangles has three vertexes. 
		
		// We then EACH FRAME calculate the triangles for each ray.
		int vertexIndex = 0;
		int triangeIndex = 0;
		foreach (FogCutterCone cone in cones) {
			// Create the origin vertex
			vertices[vertexIndex] = cone.center;
			int originVertex = vertexIndex;
			vertexIndex++;
			
			// Add the rays to the mesh
			int rayNum = 0;
			foreach (Vector2 ray in cone.getRays()){
				Vector2 rayHit;
				// The most expensive line of code in this project.
				int hits = Physics2D.Raycast(cone.center,ray,fogBlocker,rayBuffer,cone.range); 
				if(hits == 0){ 
					rayHit = cone.center + ray * (cone.range + 0.015625f/10);// Tenth of a pixel buffer
				} else {
					rayHit = cone.center + ray * (rayBuffer[0].distance + 0.015625f/10); // We only take the position of the first thing we hit. 
					//Manage the hit. OpaqueBlockers become fully rendered
					GameObject visObj = rayBuffer[0].transform.gameObject;
					if(visObj.layer == makeVisable || visObj.layer == tempVisible){
						visObj.layer = tempVisible;
						tempVisableObjects[swapTimeBuffer-1].Add(visObj);
					}
				}
				vertices[vertexIndex] = rayHit;
				if(rayNum > 0){ // Fenceposting prevention
					triangles[triangeIndex] = originVertex; // Origin
					triangles[triangeIndex + 1] = vertexIndex-1; // Left
					triangles[triangeIndex + 2] = vertexIndex; // Right
					triangeIndex += 3; // Account for 3 more triangles.
				}
				vertexIndex++;
				rayNum++;
			}
			// Make a true circle if it is one.
			if(cone.fov == 180f){
				triangles[triangeIndex] = originVertex;
				triangles[triangeIndex + 1] = vertexIndex - 1; // The most recent vertex that actually exists
				triangles[triangeIndex + 2] = originVertex + 1; // The leftmost vertex after the origin. 
				triangeIndex += 3;
			}
			
		}
		if(triangeIndex != triangles.Length){
			Debug.LogError("Mismatched traingle count in Fogmesh");
		}
		if(vertexIndex != vertices.Length){
			Debug.LogError("Mismatched vertex count in Fogmesh");
		}
		
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		
    }
}
