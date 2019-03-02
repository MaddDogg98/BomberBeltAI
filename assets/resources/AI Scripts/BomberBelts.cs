using UnityEngine;
using System.Collections;
using System;
using System.Threading;

public class BomberBelts : MonoBehaviour {

	//define variables
    public CharacterScript mainScript;
    public float[] bombSpeeds;
    public float[] buttonCooldowns;
    public float playerSpeed;
    public int[] beltDirections;
    public float[] buttonLocations;
    private float[] bombDistances;
    private float playerLocation;
    private float enemyLocation;
    private float[] buttonVals;

	//initialize variables
    private int playerLives = 8, enemyLives = 8;
    private const float PADDING = 0.1f;
    private const int NUM_BUTTONS = 8;
    private const int MOVE = 1, PUSH = 2, THINK = 3;
    private int action = PUSH;
    private int nextStep = 0;
    private int lastStep = -1;
    private int liveBombs = 0;
    private int oldBombs = 0;
    private float stressedBot = 1f;

    private Thread t;

    //method start
    void Start () {
        mainScript = GetComponent<CharacterScript>();

        if (mainScript == null)
        {
            print("No CharacterScript found on " + gameObject.name);
            this.enabled = false;
        }

        buttonLocations = mainScript.getButtonLocations();

        playerSpeed = mainScript.getPlayerSpeed();

        buttonVals = new float[NUM_BUTTONS];

        playerLocation = mainScript.getCharacterLocation();
        enemyLocation = mainScript.getOpponentLocation();
        
        t = new Thread(new ThreadStart(findPath));
        t.Start();

    }


	//update method
	void Update()
	{
        buttonCooldowns = mainScript.getButtonCooldowns();
        beltDirections = mainScript.getBeltDirections();

        bombSpeeds = mainScript.getBombSpeeds();
        playerSpeed = mainScript.getPlayerSpeed();
        bombDistances = mainScript.getBombDistances();
        playerLocation = mainScript.getCharacterLocation();
        enemyLocation = mainScript.getOpponentLocation();

        if (t.IsAlive == false)
        {
            t = new Thread(new ThreadStart(findPath));
            t.Start();
        }

        // Keep track of player and enemy lives
        liveBombs = bombsInOurDirection();
        if (liveBombs != oldBombs)
        {
            foreach (float f in bombDistances)
            {
                if (f <= 0.1f)
                {
                    playerLives--;
                    stressedBot *= 2f;
                }

                if (f >= 18.05f)
                {
                    enemyLives--;
                    stressedBot *= 0.9f;
                }

            }

            if (liveBombs >= playerLives)
            {
                stressedBot *= 10f;
            }
            else
            {
                stressedBot /= 10f;
            }

            oldBombs = liveBombs;
        }
        
        switch (action)
		{
            case MOVE:
                MoveToButton(buttonLocations[nextStep]);
                break;
                
            case PUSH:
                if (buttonCooldowns[nextStep] <= 0)
                {
                    mainScript.push();
                    lastStep = nextStep;
                    action = THINK;
                }

                break;
                
            case THINK:
                //findPath();
                nextStep = findHighestIndex(buttonVals);
                if (nextStep != lastStep)
                {
                    action = MOVE;
                }
                break;
        }       
    }

    void findPath()
    {
        // Part 1: Start with distance from player... closer = higher
        float[] distanceVals = getDistanceValues();
        for (int i = 0; i < NUM_BUTTONS; i++)
        {
            buttonVals[i] = distanceVals[i];
        }

        // Part 2: Lower value if going away from you (no point pushing it again)
        for (int i = 0; i < NUM_BUTTONS; i++)
        {
            if (beltDirections[i] == 1 || canPush(i) == false)
            {
                buttonVals[i] -= 100f;
            }
        }

        // Part 3: Higher value if bomb is coming towards you and can be saved
        for (int i = 0; i < NUM_BUTTONS; i++)
        {
            if (beltDirections[i] == -1 && canPush(i) == true)
            {
                buttonVals[i] += (10f * (20f - bombDistances[i]) * stressedBot);
            }
        }

        // Part 4: Higher threat if bomb is faster
        for (int i = 0; i < NUM_BUTTONS; i++)
        {
            if (beltDirections[i] == -1 && canPush(i) == true)
            {
                buttonVals[i] += bombSpeeds[i] * 5f;
            }
        }        
    }

    bool canPush(int i)
    {
		// buffer time for player actually hitting the button before it hits
        float BUFFER = 2.0f;
		
        if ((bombDistances[i] / bombSpeeds[i]) > (buttonDistanceFromPlayer(i) / playerSpeed) + BUFFER)
        {
            return true;
        }
        else
            return false;
        

    }
    
    float buttonDistanceFromPlayer(int i)
    {
        return Mathf.Abs(buttonLocations[i] - playerLocation);
    }

    // Part 1
    float[] getDistanceValues()
    {
        float[] a = new float[NUM_BUTTONS];

        float[] d = new float[NUM_BUTTONS];
        for(int i = 0; i < NUM_BUTTONS; i++)
        {
            d[i] = buttonDistanceFromPlayer(i);
        }

        float counter = 50f;
        for(int i = 0; i < NUM_BUTTONS; i++)
        {
            int index = findSmallest(d);
            a[index] = counter;
            counter -= 5f;
            d[index] = 100f;
        }
		return a;
    }

    int findSmallest(float[] array)
    {
        int minIndex = 0;
        float min = array[0];
        for(int i = 0; i < NUM_BUTTONS; i++)
        {
            if(array[i] < min)
            {
                min = array[i];
                minIndex = i;
            }
        }

        return minIndex;
    }

    void MoveToButton(float location) {

        if(playerLocation < (location + PADDING) && playerLocation > (location - PADDING))
        {
            action = PUSH;
        }
        else if(playerLocation < location  ){
          mainScript.moveUp();
        }
        else if(playerLocation > location){
          mainScript.moveDown();
        }
        
    }

    int bombsInOurDirection()
    {
        int counter = 0;
        foreach (int i in beltDirections)
        {
            if (i == -1) counter++;
        }

        return counter;
    }

    int findHighestIndex(float[] array)
    {
        float max = array[0];
        int maxIndex = 0;
        for(int i = 0; i < NUM_BUTTONS; i++)
        {
            if(max < array[i])
            {
                max = array[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }
}