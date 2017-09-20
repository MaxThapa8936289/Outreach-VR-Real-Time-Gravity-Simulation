#region NBodySim.cs - READ ME
// NBodySim.cs
// Primary Functionality:
//      - To plot the initial condition of particles to a 3D space for a fly through experience
//      - To call IntegrateBodies.compute each fixed framerate frame to calculate forces and update positions and velocities
//      
//      - To take data from a file using csvFileReader.cs or generate data using hard coded configurations
//      - To read this data into a four vector which contains 3-position and mass [x,y,x,m] and another four vector which contains 3-velocities and an empty parameter [vx,vy,vz,u]
//      - To pass this data to IntegrateBodies.compute every frame to calculate forces, update positions and update velocities of all particles.
//      - To pass the position and velocity vectors to the particle shader which plots and colours the particles
//
//      - To continuously update the screen with the relative positions of the particles compared to the camera ( giving a fly through experience )
//      - To continuously update the screen with the other information that may be changed in runtime by Click.cs or SimpleXboxControllerInput.cs 
//
// Assignment Object: "Main Camera"
// 
// Notes: 
//      This is the 'Main Program'. It's essentially a hub for incoming and outgoing information to other scripts. It manages only one intial condition at a time so CycleInitialConditions.cs is used to change files.
//      The [u] parameter is not used in this program. It was experiented with but significant performance costs were found.
//      This is the script I worked on the most, and have written almost all of it. For that reason it is amateur and may be inefficient, fragile or poorly formatted, so I apologise in advance for that. 
//      I have tried to comment as much as possible so that my intentions are clear.
#endregion

using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

// Must be attached to a camera atm because DrawProcedural is used to render the points
[RequireComponent(typeof(Camera))]
public class NBodySim : MonoBehaviour {

    // const variables //
    // for compute buffers
    const int READ = 0;
    const int WRITE = 1;

    // Public variables // (note: all initialisers are overriden by values in the Inspector tab in Unity)
    // Initial Condition (IC) options (drop down menu)
    private enum IC { FILE, CUBE, TWO_CUBES, VERTICAL_SQUARE_SPIRAL, HORIZONTAL_SPINNING_SQUARE_SPIRAL };
    [HideInInspector] public int m_initialCondition;

    // Resources //
    public Material m_particleMat;                      // particle material ( which holds the particle shader )                                                   
    public ComputeShader m_integrateBodies;             // Compute Shader ( where the physics calculations are processed )
    public FileInfo m_particleDataFile;                 // Csv file for initial condition from file

    [HideInInspector] public int m_numBodies;           // Number of particles for non-file ICs ( must be multiple of 256. Overridden in inspector )
    [HideInInspector] public float m_spacingScale;      // Spacing between particles plotted for non-file ICs, in units of m_softeningSquared
    [HideInInspector] public float m_velocityScaling;   // Essentially scales the velocity input values while keeping the simulation experience the same. Used for mitigiating floating point errors. //
    private float m_accelerationScaling;                // Square of m_velocityScaling. Multiplies acceleration steps.
    private float m_timeScaling;                        // Inverse of m_velocityScaling. Multiplies the time so that the simulation runs at the same speed and along the same paths.
    [HideInInspector] public float m_zOffset;           // Shift the distribution along the z-axis. The camera looks toward the origin from z = -90.

    [HideInInspector] public bool m_paused = false;     // Pause option - When true, positions and velocities are updated with no change each frame
    private int pause_multiplier = 1;                   // multiplier equal to either 1 or 0. For translation of the bool above into mathematical use.

    [HideInInspector] public float m_softeningSquared;  // Softening term found in the denominator of the gravitaitonal force equation. Essentially set a minimum range below which the gravitational force will plateau. Prevents division by zero.

    [HideInInspector] public float m_maxColorSpeed;     // The colour scale will fade across speeds 0 to m_maxColorSpeed. Particles with greater speeds have the colour at the top of the scale.
    [HideInInspector] public float m_defaultMass;       // Mass attributed to all particles for non-file ICs
    
    ComputeBuffer[] m_positions, m_velocities;

    // DISCALIMER BY MAX THAPA: Can't say I really understand compute shaders, so this is left from the code I extracted from the GPU GEMS article https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch31.html. 
    //                          p and q refer to the p and q in said article. Some more clues can be found in the compute shader. 
    //                          The numbers I have chosen were the only ones that avoided drastic physical errors. 
    //                          Presumably other numbers could be used if the compute shader was correctly built for the graphics card on this machine.
    // Note: q is the number of threads per body, and p*q should be 
    // less than or equal to 256.
    // p must match the value of NUM_THREADS in the IntegrateBodies shader
    const int p = 256;        // Sets the width of the tile used in the simulation.
    const int q = 1;          // Sets the height of the tile used in the simulation.


    // Initiliasing the program
    void OnEnable() 
	{
        // Set pause status immmediately
        if (m_paused) { pause_multiplier = 0; }

        // Calculate these variables
        m_timeScaling = 1 / (m_velocityScaling);
        m_accelerationScaling = m_velocityScaling * m_velocityScaling;

        // assign array of compute buffers
        m_positions = new ComputeBuffer[2];
		m_velocities = new ComputeBuffer[2];

        if (m_initialCondition != (int)IC.FILE)
        {
            // Make sure m_numBodies is divisible by 256. If not, add particles until it is.
            if (m_numBodies % 256 != 0)
            {
                while (m_numBodies % 256 != 0)
                    m_numBodies++;

                Debug.Log("NBodySim::Start - numBodies must be a multiple of 256. Changing numBodies to " + m_numBodies);
            }

            // Set compute buffer sizes now that m_numBodies is defined
            m_positions[READ] = new ComputeBuffer(m_numBodies, sizeof(float) * 4);
            m_positions[WRITE] = new ComputeBuffer(m_numBodies, sizeof(float) * 4);

            m_velocities[READ] = new ComputeBuffer(m_numBodies, sizeof(float) * 4);
            m_velocities[WRITE] = new ComputeBuffer(m_numBodies, sizeof(float) * 4);

            // Call the appropriate plotting function for the current configuration setting.
            if (m_initialCondition == (int)IC.CUBE)
            {
                ConfigCube();
            }
            if (m_initialCondition == (int)IC.TWO_CUBES)
            {
                Config2Cubes();
            }
            if (m_initialCondition == (int)IC.VERTICAL_SQUARE_SPIRAL)
            {
                ConfigVerticalSquareSpiral();
            }
            if (m_initialCondition == (int)IC.HORIZONTAL_SPINNING_SQUARE_SPIRAL)
            {
                ConfigHorizontalSpinningSquareSpiral();
            }
        }
        else // if m_initialCondition == (int)IC.FILE
        {
            // Skip setting compute buffers for now, need to ascertain the value of m_numBodies from the file first.
            // Call ConfigFile() which will process the file into the compute buffers
            ConfigFile();
        }

        // set pause multiplier
        pause_multiplier = (m_paused) ? 0 : 1;

        // Set variable in IntegrateBodies.compute
        m_integrateBodies.SetFloat("_VelocityScaling", m_accelerationScaling);
        m_integrateBodies.SetFloat("_DeltaTime", Time.deltaTime * m_timeScaling * pause_multiplier);
        m_integrateBodies.SetFloat("_SofteningSquared", m_softeningSquared);
        m_integrateBodies.SetInt("_NumBodies", m_numBodies);
        m_integrateBodies.SetVector("_ThreadDim", new Vector4(p, q, 1, 0));
        m_integrateBodies.SetVector("_GroupDim", new Vector4(m_numBodies / p, 1, 1, 0));
        m_integrateBodies.SetBuffer(0, "_ReadPos", m_positions[READ]);
        m_integrateBodies.SetBuffer(0, "_WritePos", m_positions[WRITE]);
        m_integrateBodies.SetBuffer(0, "_ReadVel", m_velocities[READ]);
        m_integrateBodies.SetBuffer(0, "_WriteVel", m_velocities[WRITE]);
    }

    #region INITIAL CONDITION FUNCTION DEFENITIONS

    // Here we will process the file data and store it into four vectors. 
    // Reading the .csv files with the basic reader.
    void ConfigFile()
    {
        // Parse the file to a list of floating point arrays of the form [x,y,z,vx,vy,vz,m]
        List<float[]> ParticleData = csvFileReader.parseCSV(m_particleDataFile.Name);

        // Assign the number of particles in the file to m_numBodies
        m_numBodies = ParticleData.Count;


        // Check wether m_numbBodies is compatible with the compute shader. 
        // If not, we cull extra particles
        if (m_numBodies % 256 != 0)
        {
            while (m_numBodies % 256 != 0)
            {
                m_numBodies--;
            }
            Debug.Log("NBodySim::Start - numBodies must be a multiple of 256. Changing numBodies to " + m_numBodies);
        }

        // Now we have determined m_numBodies and made sure it is valid, we can create the compute buffers.
        m_positions[READ] = new ComputeBuffer(m_numBodies, sizeof(float) * 4);
        m_positions[WRITE] = new ComputeBuffer(m_numBodies, sizeof(float) * 4);

        m_velocities[READ] = new ComputeBuffer(m_numBodies, sizeof(float) * 4);
        m_velocities[WRITE] = new ComputeBuffer(m_numBodies, sizeof(float) * 4);

        // Create the 4 vector arrays to store the particle data 
        Vector4[] positions = new Vector4[m_numBodies];
        Vector4[] velocities = new Vector4[m_numBodies];

        // Assign the particle data to each 4 vector
        for (int i = 0; i < m_numBodies; i++)
        {
            positions[i] = new Vector4(ParticleData[i][0], ParticleData[i][1], ParticleData[i][2], ParticleData[i][6]*3);
            velocities[i] = new Vector4(ParticleData[i][3] * m_velocityScaling, ParticleData[i][4] * m_velocityScaling, ParticleData[i][5] * m_velocityScaling, 0.0f);
        }

        // set the data of the compute buffers to the 4 vector arrays. After this we can perform calculations and rendering.
        m_positions[READ].SetData(positions);
        m_positions[WRITE].SetData(positions);

        m_velocities[READ].SetData(velocities);
        m_velocities[WRITE].SetData(velocities);
    }

    // Intitial Conditon: Two Cubes with a bit of spin to generate a decaying binary system
    void Config2Cubes()
    {   
        int half_m_numBodies = m_numBodies / 2;                                     // Split the particles into two
        double d_Cbrt_half_m_numBodies = Mathf.Pow(half_m_numBodies, 1f / 3f);      // Cuberoot half to find the size of each cube
        int i_Cbrt_half_m_numBodies = (int)d_Cbrt_half_m_numBodies;                 // round down
        float spacing = m_softeningSquared * m_spacingScale;                        // spacing between particles (multiplied by two later)
        float sideLength = (i_Cbrt_half_m_numBodies) * spacing;                     // Length of each cube in unity units
        float separation = 5.0f*sideLength;                                         // separation between the cubes' centres
        Vector3 zAxis = new Vector3(0.0f, 0.0f, 1.0f);                              // Axis to spin around

        // Create the 4 vector arrays to store the particle data 
        Vector4[] positions = new Vector4[m_numBodies];
        Vector4[] velocities = new Vector4[m_numBodies];

        // Debugging info
        Debug.Log("N = " + m_numBodies);
        Debug.Log("Double Cbrt of N/2 = " + d_Cbrt_half_m_numBodies);
        Debug.Log("Int Cbrt of N/2 = " + i_Cbrt_half_m_numBodies);
        Debug.Log("Vector size = " + positions.Length);

        // l = 1 for the first cube, l = 2 for the second cube
        for (int l = 1; l <= 2; l++)
        {
            // we will build each cube in the exact same way, plus an offset in the x axis whihc is opposite for each cube
            float x_offset = separation / 2;
            if( l == 2 ) { x_offset = -separation / 2; }
            // building a cube
            // i iterates along the width
            // j iterates aling the hight
            // k iterates along the depthd
            for (int i = 1; i <= i_Cbrt_half_m_numBodies; i++)
            {
                for (int j = 1; j <= i_Cbrt_half_m_numBodies; j++)
                {
                    for (int k = 1; k <= i_Cbrt_half_m_numBodies; k++)
                    {
                        // every particle must have an unique index - this is the position of that particle's data in the 4 Vector arrays
                        int index = ((i - 1) * (int)Mathf.Pow(i_Cbrt_half_m_numBodies, 2) + (j - 1) * i_Cbrt_half_m_numBodies + (k - 1));
                        if (l == 2) { index += half_m_numBodies; }  // For the second cube, shift the index to the second half of available numbers

                        // Set coordinates for each particle. Calculation chosen so that (at this point) the cube's centre is the origin.
                        float xpos = ((2 * i) - (i_Cbrt_half_m_numBodies)) * spacing;
                        float ypos = ((2 * j) - (i_Cbrt_half_m_numBodies)) * spacing;
                        float zpos = ((2 * k) - (i_Cbrt_half_m_numBodies)) * spacing;

                        // Create a 3 vector position for the particle, now applying the offset in the x direction
                        Vector3 pos = new Vector3(xpos + x_offset, ypos, zpos); 
                        // Use the 3 vector position to apply spin to the system (also applying global scaling)
                        Vector3 vel = Vector3.Cross(pos, zAxis) * (0.01f/spacing) * m_velocityScaling;

                        // Finally apply the particle data to the 4 vectors, adding on the offset in the z direction
                        positions[index] = new Vector4(pos.x, pos.y, pos.z + m_zOffset, m_defaultMass);
                        velocities[index] = new Vector4(vel.x, vel.y, vel.z, 1.0f);
                    }
                }
            }

            // It is unlikely that the cubroot of half of the number of particles is a whole number.
            // Thus after cubing the rounded value, there are still many unassigned "spare" particles.
            // These must be assigned a postion or else they will all be put at the origin and create a gravity well.
            // (Which is interesting but not what we want here)
            // We instead randomly distribute the spare particles in each cube
            // We loop over the unused indicies in each half-set of the particles
            for (int i = (int)Mathf.Pow(i_Cbrt_half_m_numBodies, 3); i < half_m_numBodies; i++)
            {
                int index = i;                                 
                if (l == 2) { index += half_m_numBodies; }     // For the second cube, shift the index to the second half of available numbers
                // Randomly populate, including the x offset
                Vector3 pos = new Vector3(Random.Range(-sideLength, sideLength) + x_offset, Random.Range(-sideLength, sideLength), Random.Range(-sideLength, sideLength));

                // Apply spin as before
                Vector3 vel = Vector3.Cross(pos, zAxis) * (0.01f / spacing) * m_velocityScaling;

                // Apply the particle data to the 4 vectors, adding on the offset in the z direction
                positions[index] = new Vector4(pos.x, pos.y, pos.z + m_zOffset, m_defaultMass);
                velocities[index] = new Vector4(vel.x, vel.y, vel.z, 1.0f);
            }
        }

        // set the data of the compute buffers to the 4 vector arrays. After this we can perform calculations and rendering.
        m_positions[READ].SetData(positions);
        m_positions[WRITE].SetData(positions);

        m_velocities[READ].SetData(velocities);
        m_velocities[WRITE].SetData(velocities);
    }

    // Intitial Conditon: A single cube, with no intitial velocity
    // Generation is almost exactly as in Config2Cubes() so comments are ommitted
    void ConfigCube()
    {
        double d_Cbrt_m_numBodies = Mathf.Pow(m_numBodies, 1f/3f);
        int i_Cbrt_m_numBodies = (int)d_Cbrt_m_numBodies;
        float spacing = m_softeningSquared * m_spacingScale;
        float sideLength = (i_Cbrt_m_numBodies) * spacing;
        Vector3 zAxis = new Vector3(0.0f, 0.0f, 1.0f);

        Vector4[] positions = new Vector4[m_numBodies];
        Vector4[] velocities = new Vector4[m_numBodies];

        Debug.Log("N = " + m_numBodies);
        Debug.Log("Double Cbrt of N = " + d_Cbrt_m_numBodies);
        Debug.Log("Int Cbrt of N = " + i_Cbrt_m_numBodies);
        Debug.Log("Vector size = " + positions.Length);

        for (int i = 1; i <= i_Cbrt_m_numBodies; i++)
        {
            for (int j = 1; j <= i_Cbrt_m_numBodies; j++)
            {
                for (int k = 1; k <= i_Cbrt_m_numBodies; k++)
                {
                    int index = (i - 1) * (int)Mathf.Pow(i_Cbrt_m_numBodies,2) + (j - 1) * i_Cbrt_m_numBodies + (k - 1);
                    float xpos;
                    float ypos;
                    float zpos;

                    xpos =  ((2 * i) - (i_Cbrt_m_numBodies)) * spacing;
                    ypos =  ((2 * j) - (i_Cbrt_m_numBodies)) * spacing;
                    zpos =  ((2 * k) - (i_Cbrt_m_numBodies)) * spacing;

                    Vector3 pos = new Vector3(xpos, ypos, zpos);
                    Vector3 vel = new Vector3(0.0f, 0.0f, 0.0f);

                    positions[index] = new Vector4(xpos, ypos, zpos + m_zOffset, m_defaultMass);
                    velocities[index] = new Vector4(vel.x, vel.y, vel.z, 1.0f);
                }
            }
        }
        for (int i = (int)Mathf.Pow(i_Cbrt_m_numBodies,3); i < m_numBodies; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-sideLength, sideLength), Random.Range(-sideLength, sideLength), Random.Range(-sideLength, sideLength));
            Vector3 vel = new Vector3(0.0f, 0.0f, 0.0f);

            positions[i] = new Vector4(pos.x, pos.y, pos.z + m_zOffset, m_defaultMass);
            velocities[i] = new Vector4(vel.x, vel.y, vel.z, 1.0f);
        }

        m_positions[READ].SetData(positions);
        m_positions[WRITE].SetData(positions);

        m_velocities[READ].SetData(velocities);
        m_velocities[WRITE].SetData(velocities);
    }

    // Intitial Conditon: A square in the x-y plane, with no intitial velocity
    // It is generated as a spiral so will only be truly square if m_numBodies is a square number
    //      - The spirtal starts in the centre of the square, plots a particle then takes a step in one direction.
    //      - It takes the appropriate number of steps before turning 90 degrees
    //      - Repeating this process a square spiral is built up
    void ConfigVerticalSquareSpiral()
    {
        float spacing = m_softeningSquared * m_spacingScale * 2;    // spacing between particles (multiplied by 2 to match other ditributions
        
        // Create the 4 vector arrays to store the particle data 
        Vector4[] positions = new Vector4[m_numBodies];
        Vector4[] velocities = new Vector4[m_numBodies];


        int index = 0;          // index - the position of the particle's data in the 4 vector arrays
        int k = 1;              // the number of steps to take before turning
        int i = 0;              // current step
        int step = +1;         // positive/negative direction to step in
        int xpos_grid = 0;      // spiral x coordinate in arbitrary units. Multiplied by "spacing" later
        int ypos_grid = 0;      // spiral y coordinate in arbitrary units. Multiplied by "spacing" later
        // coordinate components
        float xpos;         
        float ypos;
        float zpos;

        // loop over all particles
        while (index < m_numBodies)
        {
            // set current steps (in this direction) to zero
            i = 0;
            while (i < k && index < m_numBodies)
            {
                // Set the particle's coordinates
                xpos = xpos_grid * spacing;
                ypos = ypos_grid * spacing;
                zpos = 0.0f;

                // Apply the particle data to the 4 vectors, adding on the offset in the z direction
                Vector3 vel = new Vector3(0.0f, 0.0f, 0.0f);
                positions[index] = new Vector4(xpos, ypos, zpos + m_zOffset, m_defaultMass);
                velocities[index] = new Vector4(vel.x, vel.y, vel.z, 1.0f);

                // step along the x axis in the step direction
                xpos_grid += step;
                i++;                    // count the step
                index++;                // iterate the index
            }

            // Now we've gone as far as we're supposed to inthe x direction, switch to the y direction (i.e. "turn" 90 degrees)
            // Reset the current steps
            i = 0;
            while (i < k && index < m_numBodies)
            {
                // Set the particle's coordinates
                xpos = xpos_grid * spacing;
                ypos = ypos_grid * spacing;
                zpos = 0.0f;

                // Apply the particle data to the 4 vectors, adding on the offset in the z direction
                Vector3 vel = new Vector3(0.0f, 0.0f, 0.0f);
                positions[index] = new Vector4(xpos, ypos, zpos + m_zOffset, m_defaultMass);
                velocities[index] = new Vector4(vel.x, vel.y, vel.z, 1.0f);

                // step along the y axis in the step direction
                ypos_grid += step;
                i++;                    // count the step
                index++;                // iterate the index
            }

            // Now we've built 2 sides of the square. The next two sides will be one step longer each.
            // So we increment k
            // We will also be stepping in the opposite direction along each axis. 
            // So we make "step" negative
            k++;
            step = -step;
        }

        // Set the data of the compute buffers to the 4 vector arrays. After this we can perform calculations and rendering.
        m_positions[READ].SetData(positions);
        m_positions[WRITE].SetData(positions);

        m_velocities[READ].SetData(velocities);
        m_velocities[WRITE].SetData(velocities);
    }

    // Intitial Conditon: A square in the x-z plane, with a spin applied throughout the plane
    // Generation is almost exactly as in ConfigVerticalSquareSpiral() so comments are ommitted
    void ConfigHorizontalSpinningSquareSpiral()
    {
        float spacing = m_softeningSquared * m_spacingScale * 1;
        Vector3 yAxis = new Vector3(0.0f, 1.0f, 0.0f);
        Vector4[] positions = new Vector4[m_numBodies];
        Vector4[] velocities = new Vector4[m_numBodies];

        int index = 0;
        int k = 1;
        int i = 0;
        int shift = +1;
        int xpos_grid = 0;
        int zpos_grid = 0;
        float xpos;
        float ypos;
        float zpos;
        while (index < m_numBodies)
        {
            i = 0;
            while (i < k && index < m_numBodies)
            {
                xpos = xpos_grid * spacing;
                ypos = 0.0f;
                zpos = zpos_grid * spacing;

                Vector3 pos = new Vector3(xpos, ypos, zpos);
                Vector3 vel = Vector3.Cross(pos, yAxis) * 0.15f;
                positions[index] = new Vector4(xpos, ypos, zpos + m_zOffset, m_defaultMass);
                velocities[index] = new Vector4(vel.x, vel.y, vel.z, 1.0f);

                xpos_grid += shift;
                i++;
                index++;
            }
            i = 0;
            while (i < k && index < m_numBodies)
            {
                xpos = xpos_grid * spacing;
                ypos = 0.0f;
                zpos = zpos_grid * spacing;

                Vector3 pos = new Vector3(xpos, ypos, zpos);
                Vector3 vel = Vector3.Cross(pos, yAxis) * 0.15f;
                positions[index] = new Vector4(xpos, ypos, zpos + m_zOffset, m_defaultMass);
                velocities[index] = new Vector4(vel.x, vel.y, vel.z, 1.0f);

                zpos_grid += shift;
                i++;
                index++;
            }
            k++;
            shift = -shift;
        }

        m_positions[READ].SetData(positions);
        m_positions[WRITE].SetData(positions);

        m_velocities[READ].SetData(velocities);
        m_velocities[WRITE].SetData(velocities);
    }

    #endregion

    #region FUNCTION DEFENITIONS
    void Swap(ComputeBuffer[] buffer) 
	{
		ComputeBuffer tmp = buffer[READ];
		buffer[READ] = buffer[WRITE];
		buffer[WRITE] = tmp;
	}

    public void TogglePause()
    {
        m_paused = !m_paused;
    }

    public void IncreaseSpeedScale()
    {
        // maximum speed is 100 units.
        if (m_maxColorSpeed < 100)
        {
            m_maxColorSpeed *= 1.1f;
            if (m_maxColorSpeed > 100) { m_maxColorSpeed = 100f; }

            Debug.Log("Set m_maxColorSpeed to " + m_maxColorSpeed);
        }
    }

    public void DecreaseSpeedScale()
    {
        // minimum speed is 100 units.
        if (m_maxColorSpeed > 0.001f)
        {
            m_maxColorSpeed /= 1.1f;
            if (m_maxColorSpeed < 0.001f) { m_maxColorSpeed = 0.001f; }

            Debug.Log("Set m_maxColorSpeed to " + m_maxColorSpeed);
        }
    }

    #endregion

    // Update() is called every fixed framerate frame
    void Update() 
	{
        // set pause multiplier
        pause_multiplier = (m_paused) ? 0 : 1;

        // Update the variables in IntegrateBodies.compute
        m_integrateBodies.SetFloat("_DeltaTime", Time.deltaTime * m_timeScaling * pause_multiplier);
		m_integrateBodies.SetBuffer(0, "_ReadPos", m_positions[READ]);
		m_integrateBodies.SetBuffer(0, "_WritePos", m_positions[WRITE]);
		m_integrateBodies.SetBuffer(0, "_ReadVel", m_velocities[READ]);
		m_integrateBodies.SetBuffer(0, "_WriteVel", m_velocities[WRITE]);
		
        // Run IntegrateBodies.compute on the GPU
		m_integrateBodies.Dispatch(0, m_numBodies/p, 1, 1);

        // Swap round the compute buffers
		Swap(m_positions);
		Swap(m_velocities);
    }

    //  OnPostRender is called once per frame after all camera rendering is complete
    void OnPostRender () 
	{
        // Set variable in SpriteParticleShader.shader
        m_particleMat.SetPass(0);
		m_particleMat.SetBuffer("_Positions", m_positions[READ]);
        m_particleMat.SetBuffer("_Velocities", m_velocities[READ]);
        m_particleMat.SetFloat("_MaxSpeed", m_maxColorSpeed * m_accelerationScaling);

        // Plot the particles to the screen
        Graphics.DrawProcedural(MeshTopology.Points, m_numBodies);
	}

    // OnDisable release the compute buffers
    void OnDisable()
	{
		m_positions[READ].Release();
		m_positions[WRITE].Release();
		m_velocities[READ].Release();
		m_velocities[WRITE].Release();
	}
	
}
















