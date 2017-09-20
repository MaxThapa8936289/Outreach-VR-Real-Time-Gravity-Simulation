#region CycleInitialConditions.cs - READ ME
// CycleInitialConditions.cs
// Primary Functionality:
//      - To take variables from the inspector and inject them into the simulation script "NBodySim.cs"
//      - To retreive and hold a list of data files found in /StreamingAssets/
//      - To call the script "NBodySim.cs"
//      - To cycle through the files and hard coded initial conditions
//
// Assignment Object: "Player"
// 
// Notes: 
//      FILES CYCLE IN THE ORDER THEY ARE STORED IN STREAMING ASSETS. Name files accordingly to sort.
//      This is the 'Primary Script', so to speak; It provides vital information to, and calls, NBodySim.cs which is the main program. 
//      It must finish initialising before NBodySim.cs begins to run and so NBodySim.cs should be disabled on game start.
#endregion

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class CycleInitialConditions : MonoBehaviour {

    // Scripts to communicate with
    private NBodySim NBodySim;

    // Array to hold Particle Data .csv files.
    private FileInfo[] ParticleData;
    // Private variables
    private int sizeOfICEnum = 5;                   // Must be the number fo elements in the enum IC
    private int initialConditionIndex;              // integer cast of the enum for runtime updates and passing to NBodySim.
    private int currentFileIndex = 0;               // integer index for the array of Particle Data files.
    private bool filesPresent = true;               // bool for the case of no files in /StreamingAssets/
    private const int DEFALUT_SIZE = 49152;
    //private List<Plot> Plots;                     // List of 'Plot' class objects for fast slide re-loading


    // Public variables // (note: all initialisers are overriden by values in the Inspector tab in Unity)
    // Initial Condition (IC) options
    public enum IC { FILE, CUBE, TWO_CUBES, VERTICAL_SQUARE_SPIRAL, HORIZONTAL_SPINNING_SQUARE_SPIRAL };

    public IC m_initialCondition = IC.TWO_CUBES;    // For the Inspector drop down menu ONLY. Acts as default for startup. Will not update in runtime.
    public int m_numberOfParticles = DEFALUT_SIZE;  // Number of particles for non-file ICs ( must be multiple of 256. Overridden in inspector )
    public float m_spacingScale = 1000.0f;          // Spacing between particles plotted for non-file ICs, in units of m_softeningSquared
    public float m_velocityScaling = 1f;            // Essentially scales the velocity input values while keeping the simulation experience the same. Used for mitigiating floating point errors. //
    public float m_zOffset = -60.0f;                // Shift the distribution along the z-axis. The camera looks toward the origin from z = -90.
    public bool m_paused = false;                   // Pause option - When true, positions and velocities are updated with no change each frame
    public float m_softeningSquared = 0.0001f;      // Softening term found in the denominator of the gravitaitonal force equation. Essentially set a minimum range below which the gravitational force will plateau. Prevents division by zero.
    [Range(0.0f, 100.0f)]
    public float m_maxColorSpeed = 11.0f;           // Maximum speed of the colour scale
    public float m_defaultMass = 0.001f;            // Mass attributed to all particles for non-file ICs



    // Initialising the program
    void Start ()
    {
        GameObject Camera = GameObject.FindGameObjectWithTag("MainCamera");         // Fetch the Camera object which is a child of the "Player" Object. 
        NBodySim = Camera.GetComponent<NBodySim>();                                 // Assign the NBodyPlotter.cs script attached to the Camera object to variable name "NBodyPlotter"
        NBodySim.enabled = false;                                                   // NBodyPlotter.cs should be disabled anyway but just in case...

        // cast the enum to an int
        initialConditionIndex = (int)m_initialCondition;

        // Assign variables from inspector to NBodySim
        NBodySim.m_initialCondition = initialConditionIndex;
        NBodySim.m_numBodies = m_numberOfParticles;
        NBodySim.m_spacingScale = m_spacingScale;
        NBodySim.m_velocityScaling = m_velocityScaling;
        NBodySim.m_zOffset = m_zOffset;
        NBodySim.m_paused = m_paused;
        NBodySim.m_softeningSquared = m_softeningSquared;
        NBodySim.m_maxColorSpeed = m_maxColorSpeed;
        NBodySim.m_defaultMass = m_defaultMass;

        DirectoryInfo dir = new DirectoryInfo(Application.streamingAssetsPath);         // Obtain the Directory path of the StreamingAssets folder bundles with the game build
        ParticleData = dir.GetFiles("*.csv");                                           // Populate the FileInfo array with the files in the directory. 
        try { NBodySim.m_particleDataFile = ParticleData[currentFileIndex]; }           // Set the file to be loaded to the first one (currentFileIndex was initialised to 0)
        catch
        {
            NBodySim.m_particleDataFile = null;
            filesPresent = false;
            initialConditionIndex = (int)IC.TWO_CUBES;
        }

        NBodySim.enabled = true;
    }

    public void NextInitialContdition()
    {
        NBodySim.enabled = false;

        // If reading from the StreamingAssets directory AND there are files there
        if (initialConditionIndex == (int)IC.FILE && filesPresent)
        {
            // cycle through the files
            // if at end of files, move onto hard coded initial conditions
            if (currentFileIndex == ParticleData.Length - 1)
            {
                if (initialConditionIndex == sizeOfICEnum - 1) { initialConditionIndex = 0; }
                else { initialConditionIndex++; }
            }
            else { currentFileIndex++; }                                                    // if not, go to next file
            NBodySim.m_particleDataFile = ParticleData[currentFileIndex];                   // Use the index to retrieve and set the new file. 
        }
        // If not working through the directory
        else
        {
            // cycle through hard coded initial conditions
            if (initialConditionIndex == sizeOfICEnum - 1) { initialConditionIndex = 0; }
            else { initialConditionIndex++; }
            // if switching to files but theres no files, skip file load
            if (initialConditionIndex == (int)IC.FILE && !filesPresent) { NextInitialContdition(); }
        }

        NBodySim.m_initialCondition = initialConditionIndex;
        NBodySim.enabled = true;    // Launch the program!
    }

    public void PreviousInitialContdition()
    {
        NBodySim.enabled = false;

        // If reading from the StreamingAssets directory AND there are files there
        if (initialConditionIndex == (int)IC.FILE && filesPresent)
        {
            // cycle through the files
            // if at start of files, move onto hard coded initial conditions
            if (currentFileIndex == 0)
            {
                if (initialConditionIndex == 0) { initialConditionIndex = sizeOfICEnum - 1; }
                else { initialConditionIndex--; }
            }
            else { currentFileIndex--; }                                                    // if not, go to previous file
            NBodySim.m_particleDataFile = ParticleData[currentFileIndex];                   // Use the index to retrieve and set the new file. 
        }
        // If not working through the directory
        else
        {
            // cycle through hard coded initial conditions
            if (initialConditionIndex == 0) { initialConditionIndex = sizeOfICEnum - 1; }
            else { initialConditionIndex--; }
            // if switching to files but theres no files, skip file load
            if (initialConditionIndex == (int)IC.FILE && !filesPresent) { PreviousInitialContdition(); }
        }

        NBodySim.m_initialCondition = initialConditionIndex;
        NBodySim.enabled = true;    // Launch the program!
    }
}
