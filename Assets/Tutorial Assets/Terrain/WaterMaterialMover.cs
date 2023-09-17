using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterMaterialMover : MonoBehaviour
{
	public Vector2 scrollSpeed = new Vector2(0.5f, 0);

	protected Renderer rend;

	protected virtual void Start()
	{
		rend = GetComponent<Renderer>();
	}

	protected virtual void Update()
	{
		rend.material.mainTextureOffset = this.scrollSpeed * Time.time;
	}
}
