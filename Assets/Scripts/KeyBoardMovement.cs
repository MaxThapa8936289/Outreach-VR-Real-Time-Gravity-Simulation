#region KeyBoardMovement.cs - READ ME
// KeyBoardMovement.cs
// Primary Functionality:
//      - To move the player based on input from the keyboard
//
// Assignment Object: "Player"
// 
// Notes: 
//      Axes run from -1 to +1 and are pressure sensitive
#endregion

using UnityEngine;
using System.Collections;

public class KeyBoardMovement : MonoBehaviour
{
    private GameObject _Camera;
    public static float moveSpeed = 10f;    // base speed of the player
    private float speed;                    // speed of the player during runtime
    bool LeftShift;                         // hold to increase speed

    Vector3 forward;
    Vector3 left;
    Vector3 up;

    void Start()
    {
        // Fetch the needed scripts from the "MainCamera" object
        _Camera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    void Update()
    {
        forward = _Camera.transform.TransformDirection(Vector3.forward);
        left = _Camera.transform.TransformDirection(Vector3.left);
        up = _Camera.transform.TransformDirection(Vector3.up);

        LeftShift = Input.GetKey(KeyCode.LeftShift);

        speed = (LeftShift) ? moveSpeed * 1.7f : moveSpeed;

        if (Input.GetKey(KeyCode.W))
            transform.Translate(forward * speed * Time.deltaTime, Space.World);

        if (Input.GetKey(KeyCode.S))
            transform.Translate(-forward * speed * Time.deltaTime, Space.World);

        if (Input.GetKey(KeyCode.A))
            transform.Translate(left * speed * Time.deltaTime, Space.World);

        if (Input.GetKey(KeyCode.D))
            transform.Translate(-left * speed * Time.deltaTime, Space.World);

        if (Input.GetKey(KeyCode.Space))
            transform.Translate(up * 0.5f * speed * Time.deltaTime, Space.World);

        if (Input.GetKey(KeyCode.Z))
            transform.Translate(-up * 0.5f * speed * Time.deltaTime, Space.World);
    }
}