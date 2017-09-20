#region SimpleXboxControllerInput.cs - READ ME
// SimpleXboxControllerInput.cs
// Primary Functionality:
//      - To move the player based on input from the controller
//      - To update colour scales or cycles initial conditions based on input from the controller
//
// Assignment Object: "Player"
// 
// Notes: 
//      Axes run from -1 to +1 and are pressure sensitive
#endregion

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleXboxControllerInput : MonoBehaviour
{
    // scripts to communicate with
    private CycleInitialConditions CycleICs;
    private NBodySim NBodySim;
    private GameObject _Camera;

    public static float moveSpeed = 10f;    // base speed of the player, editable in the inspector
    private float speed;                    // speed of the player in runtime
    private float LeftStickX;               // strafing
    private float LeftStickY;               // forward/backward motion
    private float Triggers;                 // ascending(right trigger)/descending(left trigger)

    [HideInInspector]
    public bool movementOnly = false;       // toggles wether the controller can only move or has full input control;

    private bool aButton;                   // hold to increase speed
    private bool bButtonDown;               // previous particle data file
    private bool xButtonDown;               // next particle data file
    private bool yButtonDown;               // unused
    private bool LeftBumper;                // increase colour scaling precision
    private bool RightBumper;               // decrease colour scaling precision
    private bool StartButtonDown;           // Pause or unpause the simulation

    Vector3 forward;
    Vector3 left;
    Vector3 up;

    void Start()
    {
        // Fetch the needed scripts from the "Player" object
        GameObject Player = GameObject.FindGameObjectWithTag("Player");
        CycleICs = Player.GetComponent<CycleInitialConditions>();

        // Fetch the needed scripts from the "MainCamera" object
        _Camera = GameObject.FindGameObjectWithTag("MainCamera");
        NBodySim = _Camera.GetComponent<NBodySim>();
    }

    void Update()
    {
        LeftStickX = -Input.GetAxis("Horizontal"); // left is +1, right is -1
        LeftStickY = -Input.GetAxis("Vertical");   // up is +1, down is -1

        // GetKey = active while held; GetKeyDown = active during the frame in which the button is pressed.
        // See http://wiki.unity3d.com/index.php?title=Xbox360Controller for button mappings (diagram here http://wiki.unity3d.com/index.php/File:X360Controller2.png)
        aButton = Input.GetKey(KeyCode.Joystick1Button0);
        bButtonDown = Input.GetKeyDown(KeyCode.Joystick1Button1);
        xButtonDown = Input.GetKeyDown(KeyCode.Joystick1Button2);
        yButtonDown = Input.GetKeyDown(KeyCode.Joystick1Button3);
        LeftBumper = Input.GetKey(KeyCode.Joystick1Button4);
        RightBumper = Input.GetKey(KeyCode.Joystick1Button5);
        StartButtonDown = Input.GetKeyDown(KeyCode.JoystickButton7);

        // right trigger is represented by range -1 to 0, left trigger by range 0 to 1
        Triggers = -Input.GetAxis("Triggers");


        // Movement
        forward = _Camera.transform.TransformDirection(Vector3.forward);
        left = _Camera.transform.TransformDirection(Vector3.left);

        up = _Camera.transform.TransformDirection(Vector3.up);

        speed = (aButton) ? moveSpeed * 1.7f : moveSpeed;
        if (LeftStickY != 0.0f)
        {
            transform.Translate(LeftStickY * forward * speed * Time.deltaTime, Space.World);
        }
        if (LeftStickX != 0.0f)
        {
            transform.Translate(LeftStickX * left * speed * Time.deltaTime, Space.World);
        }
        if (Triggers != 0.0f)
        {
            transform.Translate(Triggers * up * speed * Time.deltaTime, Space.World);
        }

        if (!movementOnly)
        {
            // Pause or unpause the simulation
            if (StartButtonDown) { NBodySim.TogglePause(); }

            // Change Speed Scale (i.e. highlight precision)
            if (RightBumper) { NBodySim.IncreaseSpeedScale(); }
            if (LeftBumper) { NBodySim.DecreaseSpeedScale(); }

            // Cycle Initial conditions
            if (bButtonDown) { CycleICs.PreviousInitialContdition(); }
            if (xButtonDown) { CycleICs.NextInitialContdition(); }
        }

    }

}
