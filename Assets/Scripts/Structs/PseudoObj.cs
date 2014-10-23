using UnityEngine;
using System.Collections;

// http://stackoverflow.com/questions/16542042/fastest-way-to-sort-vectors-by-angle-without-actually-computing-that-angle
// Used for sorting points by angle
public struct pseudoObj
{
	public float pAngle {get;set;}
	public Vector3 point {get;set;}
}

