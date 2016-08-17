﻿using InControl;
using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

	public float speed = 6000f;
    public float rotateSpeed = 10f;

	public InputDevice Device { get; set; }

	Renderer cachedRenderer;

	private Rigidbody rigidbody;

	public bool isWolf { get; set; }

	private Vector3 hitDirection;
	private float attackRange = 0.5f;

	private int hittableMask = 1 << 8;

	// Use this for initialization
	void Start () {
		MakeWolf ();
		rigidbody = GetComponent<Rigidbody> ();
	}

	void Update() {
		if (isWolf) {
			if (Device.AnyButton.IsPressed) {
				Attack ();
			}
		}
	}

	public void MakeWolf() {
		isWolf = true;
		tag = "Wolf";
	}

	void Attack() {
		Vector3 attackPos = transform.position;
		hitDirection = Vector3.zero;
		attackPos += hitDirection * (attackRange/1.5f);
		Collider[] hitColliders = Physics.OverlapSphere(attackPos, attackRange, hittableMask);
		for(int i = 0; i < hitColliders.Length; i++){
			if (hitColliders [i].tag != "Wolf") {
				hitColliders[i].SendMessage("TakeDamage");
			}

		}
	}

	void TakeDamage() {
		print ("You killed a sheep!");
		//Destroy ( this.gameObject );
	}

	// Update is called once per frame
	void FixedUpdate () {
        var direction = new Vector2(Device.LeftStickX, Device.LeftStickY);
        RotateTowardsDirection(direction);
		rigidbody.velocity = new Vector3(direction.x * speed , 0.0f, direction.y * speed);
	}

    public void RotateTowardsDirection(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                Time.deltaTime * rotateSpeed);
        }
    }
}
