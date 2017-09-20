#region Click.cs - READ ME
// Click.cs
// Primary Functionality:
//      - To take input from the mouse and keyboard and inject it into NBodySim.cs
//
//      - To allow toggling of user controller viewing (which is independant of the occulus headgear movement)
//      - To increment and decrement the colour scale from the keyboard
//      - To increment and decrement the slideshow from the keyboard
//
// Assignment Object: "Player"
// 
// Notes: 
//      "user controller viewing" refers to moving the mouse or the xbox analogue stick to change the view direction. If toggled off, then ONLY the occulus headgear can change the view direction.
//      This is solely for inputs effecting the slideshow of plots, and a few toggleable things. 
//      This script does not manage player movement
#endregion

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Click : MonoBehaviour {

    // scripts to communicate with
    private NBodySim NBodySim;
    private SimpleSmoothMouseAndControllerLook MouseAndControllerLook;
    private SimpleXboxControllerInput ControllerInput;
    private CycleInitialConditions CycleICs;

    // bools for inputs
    private bool EscapeDown;        // Quit Application

    private bool BackSpaceDown;     // disable user controller viewing
    private bool EnterKeyDown;      // enable user controller viewing

    private bool F1KeyDown;         // enable/disable all inputs except movement from the controller

    private bool RightMouseDown;    // Pause/Unpause Simulation
    private bool PKeyDown;          // Pause/Unpause Simulation (alternative)

    private bool UpArrow;           // decrease the precision of denisity colour scaling
    private bool DownArrow;         // increase the precision of denisity colour scaling

    private bool LeftArrowDown;     // load up the previous file
    private bool RightArrowDown;    // load up the next file

    void Start()
    {
        // Fetch the needed scripts from the "Player" object
        GameObject Player = GameObject.FindGameObjectWithTag("Player");
        MouseAndControllerLook = Player.GetComponent<SimpleSmoothMouseAndControllerLook>();
        ControllerInput = Player.GetComponent<SimpleXboxControllerInput>();
        CycleICs = Player.GetComponent<CycleInitialConditions>();

        MouseAndControllerLook.enabled = false;          // disable MouseAndControllerLook by default to avoid disorientation of the person with the headset on

        // Fetch the needed script from the "Main Camera" object
        GameObject Camera = GameObject.FindGameObjectWithTag("MainCamera");
        NBodySim = Camera.GetComponent<NBodySim>();
    }
    // Update() is called once per frame
	void Update ()
    {
        // Input.GetKeyDown() is true if the key gets pressed within that frame
        EscapeDown = Input.GetKeyDown(KeyCode.Escape);

        BackSpaceDown = Input.GetKeyDown(KeyCode.Backspace);
        EnterKeyDown = Input.GetKeyDown(KeyCode.Return);

        F1KeyDown = Input.GetKeyDown(KeyCode.F1);

        RightMouseDown = Input.GetKeyDown(KeyCode.Mouse1);
        PKeyDown = Input.GetKey(KeyCode.P);

        UpArrow = Input.GetKey(KeyCode.UpArrow);
        DownArrow = Input.GetKey(KeyCode.DownArrow);

        LeftArrowDown = Input.GetKeyDown(KeyCode.LeftArrow);
        RightArrowDown = Input.GetKeyDown(KeyCode.RightArrow);

        // Quit Application
        if (EscapeDown) { Application.Quit(); }

        // Enable or Disable user controller viewing
        if (BackSpaceDown) { MouseAndControllerLook.enabled = false; }
        if (EnterKeyDown) { MouseAndControllerLook.enabled = true; }

        // Enable/disable all inputs except movement from the controller
        if (F1KeyDown) { ControllerInput.movementOnly = !ControllerInput.movementOnly; }

        // Pause or unpause the simulation
        if (RightMouseDown || PKeyDown) { NBodySim.TogglePause(); }

        // Change Speed Scale (i.e. highlight precision)
        if (UpArrow) { NBodySim.IncreaseSpeedScale(); }
        if (DownArrow) { NBodySim.DecreaseSpeedScale(); }

        // Cycle Data files
        if (LeftArrowDown) { CycleICs.PreviousInitialContdition(); }
        if (RightArrowDown) { CycleICs.NextInitialContdition(); }
    }
}
