// ORIGINAL SOURCES > http://ncase.me/sight-and-light/
// Converted to Unity > http://unitycoder.com/blog/2014/10/12/2d-visibility-shadow-for-unity-indie/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Visibility2D
{
	
	public class ShadowCasting2D : MonoBehaviour 
	{



		List<Segment2D> segments = new List<Segment2D>(); // wall line segments
		List<float> uniqueAngles = new List<float>(); // wall angles
		List<Vector3> uniquePoints = new List<Vector3>(); // wall points
		List<Intersection> intersects = new List<Intersection>(); // returned line intersects
		
		Mesh lightMesh;
		MeshFilter meshFilter;
		
		GameObject go;
		
		
		// DEBUGGING
//		System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
		//public GUIText stats;
	
		
		// INIT
		void Awake() 
		{
			lightMesh = new Mesh();
			meshFilter = GetComponent<MeshFilter>();
		
			CollectVertices ();
		} // Awake
	
	


		// Main loop
		void Update () 
		{
			
			// DEBUGGING
			//stopwatch.Start();
			
			// Get mouse position
			Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y,10));
			
			// Move "player" to mouse position
			transform.position = mousePos;
	
			// Get all angles
			uniqueAngles.Clear();
			for(var j=0;j<uniquePoints.Count;j++)
			{
				float angle = Mathf.Atan2(uniquePoints[j].y-mousePos.y,uniquePoints[j].x-mousePos.x);
				uniqueAngles.Add(angle-0.00001f);
				uniqueAngles.Add(angle);
				uniqueAngles.Add(angle+0.00001f);
			}
			
			// Rays in all directions
			intersects.Clear();
			for(var j=0;j<uniqueAngles.Count;j++)
			{
				float angle = uniqueAngles[j];
				
				// Calculate dx & dy from angle
				float dx = Mathf.Cos(angle);
				float dy = Mathf.Sin(angle);
				
				// Ray from center of screen to mouse
				Ray2D ray = new Ray2D(new Vector2(mousePos.x,mousePos.y), new Vector2(mousePos.x+dx,mousePos.y+dy));
				
				// Find CLOSEST intersection
				Intersection closestIntersect = new Intersection();
				bool founded = false;
	
				for(int i=0;i<segments.Count;i++)
				{
					Intersection intersect = getIntersection(ray,segments[i]);
	
					if(intersect.v==null) continue;
	
	//				if(!closestIntersect.v==null || intersect.angle<closestIntersect.angle)
					if(!founded || intersect.angle<closestIntersect.angle)
					{
						founded = true;
						closestIntersect=intersect;
					}
				} // for segments
	
				// Intersect angle
				if(closestIntersect==null) continue;
				closestIntersect.angle = angle;
	
				// Add to list of intersects
				intersects.Add(closestIntersect);
				
			} // for uniqueAngles
			
			
			// Sort intersects by angle
			intersects.Sort((x, y) =>{ return Comparer<float?>.Default.Compare(x.angle, y.angle); });
					
	
			// Mesh generation
			List<Vector3> verts = new List<Vector3>();
			List<int> tris = new List<int>();
			verts.Clear();
			tris.Clear();
			// TODO: UV's
	
			// Place first vertex at mouse position ("dummy triangulation")
			verts.Add(transform.InverseTransformPoint(transform.position));
			
			for(var i=0;i<intersects.Count;i++)
			{
				if (intersects[i].v!=null)
				{
					verts.Add(transform.InverseTransformPoint((Vector3)intersects[i].v));
					verts.Add(transform.InverseTransformPoint((Vector3)intersects[(i+1) % intersects.Count].v));
					
					//GLDebug.DrawLine((Vector3)intersects[i].v,(Vector3)intersects[(i+1) % intersects.Count].v,Color.red,0,false);
				}
			} // for intersects
	
			
			// Build triangle list
			for(var i=0;i<verts.Count+1;i++)
			{
				tris.Add((i+1) % verts.Count);
				tris.Add((i) % verts.Count);
				tris.Add(0);
			}
	
			// Create mesh
			lightMesh.Clear();
			lightMesh.vertices = verts.ToArray();
			lightMesh.triangles = tris.ToArray();
			lightMesh.RecalculateNormals(); // FIXME: no need if no lights..or just assign fixed value..
	
			meshFilter.mesh = lightMesh;
	
			// Debug lines from mouse to intersection
			/*
			for(var i=0;i<intersects.Count;i++)
			{
				if (intersects[i].v!=null)
				{
					GLDebug.DrawLine(new Vector3(mousePos.x,mousePos.y,0),(Vector3)intersects[i].v,Color.red,0,false);
				}
			}
			*/
	
			// DEBUG TIMER
			//stopwatch.Stop();
			//Debug.Log("Stopwatch: " + stopwatch.Elapsed);
			// Debug.Log("Stopwatch: " + stopwatch.ElapsedMilliseconds);
			//stats.text = ""+stopwatch.ElapsedMilliseconds+"ms";
			//stopwatch.Reset();		
			
			
		} // Update
	
	
		
		
		void CollectVertices ()
		{
			// Collect all gameobjects, with tag Wall
			GameObject[] gos = GameObject.FindGameObjectsWithTag("Wall");
			
			// Get all vertices from those gameobjects
			// WARNING: Should only use 2D objects, like unity Quads for now..
			foreach (GameObject go in gos)
			{
				Mesh goMesh = go.GetComponent<MeshFilter>().mesh;
				int[] tris = goMesh.triangles;
				
				List<int> uniqueTris = new List<int>();
				uniqueTris.Clear();
				
				// Collect unique tri's
				for (int i = 0; i < tris.Length; i++) 
				{
				
					if (!uniqueTris.Contains(tris[i])) 
					{
						uniqueTris.Add(tris[i]);
					}
				} // for tris
			
			
				// Sort by pseudoangle
				List<pseudoObj> all = new List<pseudoObj>();
				for (int n=0;n<uniqueTris.Count;n++)
				{
					float x= goMesh.vertices[uniqueTris[n]].x;
					float y= goMesh.vertices[uniqueTris[n]].y;
					float a = copysign(1-x/(Mathf.Abs (x)+Mathf.Abs(y)),y);
					pseudoObj o = new pseudoObj();
					o.pAngle = a;
					o.point = goMesh.vertices[uniqueTris[n]];
					all.Add(o);
				}
				
				// Actual sorting
				all.Sort(delegate(pseudoObj c1, pseudoObj c2) { return c1.pAngle.CompareTo(c2.pAngle); });
				
				// Get unique vertices to list
				List<Vector3> uniqueVerts = new List<Vector3>();
				uniqueTris.Clear();
				for (int n=0;n<all.Count;n++)
				{
					uniqueVerts.Add(all[n].point);
				}
			
				// Add world borders 
				int tempRange = 990;
				Camera cam = Camera.main;
				Vector3 b1 = cam.ScreenToWorldPoint(new Vector3(-tempRange, -tempRange, Camera.main.nearClipPlane + 0.1f+tempRange)); // bottom left
				Vector3 b2 = cam.ScreenToWorldPoint(new Vector3(-tempRange, cam.pixelHeight+tempRange, cam.nearClipPlane + 0.1f+tempRange)); // top left
				Vector3 b3 = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth+tempRange, cam.pixelHeight, cam.nearClipPlane + 0.1f+tempRange)); // top right
				Vector3 b4 = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth+tempRange, -tempRange, cam.nearClipPlane + 0.1f+tempRange)); // bottom right
			
				// Get world borders as vertices
				Segment2D seg1 = new Segment2D();
				seg1.a = new Vector2(b1.x,b1.y);
				seg1.b = new Vector2(b2.x,b2.y);
				segments.Add(seg1);
				
				seg1.a = new Vector2(b2.x,b2.y);
				seg1.b = new Vector2(b3.x,b3.y);
				segments.Add(seg1);
				
				seg1.a = new Vector2(b3.x,b3.y);
				seg1.b = new Vector2(b4.x,b4.y);
				segments.Add(seg1);
				
				seg1.a = new Vector2(b4.x,b4.y);
				seg1.b = new Vector2(b1.x,b1.y);
				segments.Add(seg1);
			
				// Get segments from unique vertices
				for (int n=0;n<uniqueVerts.Count;n++)
				{
					// Segment start
					Vector3 wPos1 = go.transform.TransformPoint(uniqueVerts[n]);
					
					// Segment end
					Vector3 wPos2 = go.transform.TransformPoint(uniqueVerts[(n+1) % uniqueVerts.Count]);
					
					// TODO: duplicate of unique verts?
					uniquePoints.Add(wPos1);
					
					Segment2D seg = new Segment2D();
					seg.a = new Vector2(wPos1.x,wPos1.y);
					seg.b = new Vector2(wPos2.x, wPos2.y);
					segments.Add(seg);
					
					//GLDebug.DrawLine(wPos1, wPos2,Color.white,10);
				}
			} // foreach gameobject
		} // CollectVertices
		
		
		
		
		// Find intersection of RAY & SEGMENT
		Intersection getIntersection(Ray2D ray, Segment2D segment)
		{
			Intersection o = new Intersection();
	
			// RAY in parametric: Point + Delta*T1
			float r_px = ray.a.x;
			float r_py = ray.a.y;
			float r_dx = ray.b.x-ray.a.x;
			float r_dy = ray.b.y-ray.a.y;
	
			// SEGMENT in parametric: Point + Delta*T2
			float s_px = segment.a.x;
			float s_py = segment.a.y;
			float s_dx = segment.b.x-segment.a.x;
			float s_dy = segment.b.y-segment.a.y;
			
			// Are they parallel? If so, no intersect
			var r_mag = Mathf.Sqrt(r_dx*r_dx+r_dy*r_dy);
			var s_mag = Mathf.Sqrt(s_dx*s_dx+s_dy*s_dy);
	
			if(r_dx/r_mag==s_dx/s_mag && r_dy/r_mag==s_dy/s_mag) // Unit vectors are the same
			{
				return o; 
			}
			
			// SOLVE FOR T1 & T2
			// r_px+r_dx*T1 = s_px+s_dx*T2 && r_py+r_dy*T1 = s_py+s_dy*T2
			// ==> T1 = (s_px+s_dx*T2-r_px)/r_dx = (s_py+s_dy*T2-r_py)/r_dy
			// ==> s_px*r_dy + s_dx*T2*r_dy - r_px*r_dy = s_py*r_dx + s_dy*T2*r_dx - r_py*r_dx
			// ==> T2 = (r_dx*(s_py-r_py) + r_dy*(r_px-s_px))/(s_dx*r_dy - s_dy*r_dx)
			var T2 = (r_dx*(s_py-r_py) + r_dy*(r_px-s_px))/(s_dx*r_dy - s_dy*r_dx);
			var T1 = (s_px+s_dx*T2-r_px)/r_dx;
			
			// Must be within parametic whatevers for RAY/SEGMENT
			if(T1<0) return o;
			if(T2<0 || T2>1) return o;
	
			o.v = new Vector3(r_px+r_dx*T1, r_py+r_dy*T1, 0);
			o.angle = T1;
	
			// Return the POINT OF INTERSECTION
			return o;
	
		} // getIntersection
	


		// *** Helper functions ***


		// http://stackoverflow.com/questions/16542042/fastest-way-to-sort-vectors-by-angle-without-actually-computing-that-angle
		float pseudoAngle(float dx,float dy)
		{
			float ax = Mathf.Abs(dx);
			float ay = Mathf.Abs(dy);
			float p = dy/(ax+ay);
			if (dx < 0) p = 2 - p;
			//# elif dy < 0: p = 4 + p
			return p;
		}
		
		// http://stackoverflow.com/a/1905142
		// TODO: not likely needed to use this..
		float copysign(float a,float b)
		{
			return (a*Mathf.Sign(b));
		}
	
	} // Class
} // Namespace
