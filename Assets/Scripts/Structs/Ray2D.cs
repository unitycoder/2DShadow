using UnityEngine;
using System.Collections;

// Ray from A to B
public struct Ray2D
{
	public Vector2 a {get; set;}
	public Vector2 b {get; set;}

	public Ray2D(Vector2 _a, Vector2 _b)
	{
		this.a = _a;
		this.b = _b;
	}

}
