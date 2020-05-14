
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script requires you to have setup your animator with 3 parameters, "InputMagnitude", "InputX", "InputZ"
//With a blend tree to control the inputmagnitude and allow blending between animations.
[RequireComponent(typeof(CharacterController))]
public class WaypointInput : MonoBehaviour
{

	public float Velocity = 2.0f;
	[Space]

	public Vector3 desiredMoveDirection;
	public bool blockRotationPlayer;
	public float desiredRotationSpeed = 0.1f;
	private Animator anim;
	public float Speed;
	public float allowPlayerRotation = 0.1f;
	private Camera cam;
	private CharacterController controller;
	public bool isGrounded;
	public GameObject destinationMarker;

	[Header("Animation Smoothing")]
	[Range(0, 1f)]
	public float HorizontalAnimSmoothTime = 0.2f;
	[Range(0, 1f)]
	public float VerticalAnimTime = 0.2f;
	[Range(0, 1f)]
	public float StartAnimTime = 0.3f;
	[Range(0, 1f)]
	public float StopAnimTime = 0.15f;

	private Vector3 dest;
	private float verticalVel;
	private Vector3 moveVector;
	private Vector3 dir;
	private GameObject marker;

	// Use this for initialization
	void Start()
	{
		anim = this.GetComponent<Animator>();
		cam = Camera.main;
		controller = this.GetComponent<CharacterController>();
	    dest = destinationMarker.transform.position;
	}

	// Update is called once per frame
	void Update()
	{
		InputMagnitude();
		/*
		//If you don't need the character grounded then get rid of this part.
		isGrounded = controller.isGrounded;
		if (isGrounded) {
			verticalVel -= 0;
		} else {
			verticalVel -= 2;
		}
		moveVector = new Vector3 (0, verticalVel, 0);
		controller.Move (moveVector);
        */
		//Updater
	}

	void PlayerMoveAndRotation()
	{

		var camera = Camera.main;
		var forward = cam.transform.forward;
		var right = cam.transform.right;

		forward.y = 0f;
		right.y = 0f;

		forward.Normalize();
		right.Normalize();


		// desiredMoveDirection = forward * dir.normalized.z + right * dir.normalized.x;
		desiredMoveDirection = new Vector3(dir.normalized.x, 0.0f, dir.normalized.z);

		if (blockRotationPlayer == false)
		{
			if (desiredMoveDirection.sqrMagnitude != 0)
				transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), desiredRotationSpeed);
			controller.Move(desiredMoveDirection * Time.deltaTime * Velocity);
		}
	}


	void InputMagnitude()
	{
		//Calculate Input Vectors
		//if (Input.GetMouseButtonDown(0))
		//{
		//	RaycastHit hit;
		//	Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		//	if (Physics.Raycast(ray, out hit))
		//	{
		//		Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
		//		if (hit.transform.tag == "Ground")
		//		{
		//			dest = hit.point;
		//			Debug.Log(dest);
		//			Destroy(marker);
		//			marker = Instantiate(destinationMarker, dest, Quaternion.identity);
		//		}
		//	}
		//}

		dir = dest - transform.position;
		dir.y = 0;
		float distance = dir.sqrMagnitude;
		if (distance < 0.1f)
		{
			distance = 0.0f;
			dir = new Vector3();
		}
		Speed = Mathf.Min(distance, 1.0f);

		//Physically move player
		if (Speed > allowPlayerRotation)
		{
			anim.SetFloat("Blend", Speed, StartAnimTime, Time.deltaTime);
		}
		else if (Speed < allowPlayerRotation)
		{
			anim.SetFloat("Blend", Speed, StopAnimTime, Time.deltaTime);
		}
		PlayerMoveAndRotation();
	}
}
