using UnityEngine;
using System.Collections;

// Line segment from A to B
public struct Segment2D
{
	public Vector2 a {get; set;}
	public Vector2 b {get; set;}

	public Segment2D(Vector2 _a, Vector2 _b)
	{
		this.a = _a;
		this.b = _b;
	}

}
