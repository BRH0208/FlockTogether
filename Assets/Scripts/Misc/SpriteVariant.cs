using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteVariant : MonoBehaviour
{
	public Texture2D spritesheet;
	private SpriteRenderer sprite;
	private Sprite[] variants;
	
	private int _id;
	
    public int id{
		get{return _id;}
		set{
			_id = value;
			sprite.sprite = variants[id];
		}
	}
	// Start is called before the first frame update
    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
		variants = Resources.LoadAll<Sprite>(spritesheet.name);
		id = Random.Range(0,variants.Length);
    }
}