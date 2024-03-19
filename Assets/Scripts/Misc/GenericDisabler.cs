using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericDisabler : MonoBehaviour
{
	public void OnSit(){disable();}
	public void OnDesit(){enable();}
	
    public void disable () {
		Collider2D[] colliders = GetComponents<Collider2D>();
		SpriteRenderer[] sprites = GetComponents<SpriteRenderer>();
		foreach (Collider2D col in colliders) {col.enabled = false;}
		foreach (SpriteRenderer sprite in sprites) {sprite.enabled = false;}
	}
	
	public void enable () {
		Collider2D[] colliders = GetComponents<Collider2D>();
		SpriteRenderer[] sprites = GetComponents<SpriteRenderer>();
		foreach (Collider2D col in colliders) {col.enabled = true;}
		foreach (SpriteRenderer sprite in sprites) {sprite.enabled = true;}
	}
}
