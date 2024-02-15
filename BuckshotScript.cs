using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using System.Xml.Serialization;
using TMPro;
using System;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using static UnityEditor.Progress;
using Unity.VisualScripting;

public class BuckshotScript : Agent
{

    #region instance variables
    int healthMax;
    int dealerHealth;
    int playerHealth;
    bool isPlayerTurn = true;
    int blankNum;
    int liveNum;
    int itemNum;

    bool turnInProgress = false;
    int p_cigNum = 0;
    int d_cigNum = 0;
    bool p_cuffed = false;
    bool d_cuffed = false;
    bool p_lastCuffed = false;
    bool d_lastCuffed = false;
    int thinksNextShot = -1;
    int sawDmg = 0; //will change to 1 when sawed off, adding 1 to total damage

    int nextShot; //0 blank, 1 live

    [SerializeField] float secBetweenTurns = 0.01f;

    int[] playerItemArray = new int[8];
    int[] dealerItemArray = new int[8];
    TMP_Text[] p_item_textArray = new TMP_Text[8];
    TMP_Text[] d_item_textArray = new TMP_Text[8];

    //text box serialized fields (This is super shitty, plz dont judge me :(
    [SerializeField] TMP_Text D_Health;
    [SerializeField] TMP_Text P_Health;

    [SerializeField] TMP_Text D_item1; //dealer items
    [SerializeField] TMP_Text D_item2;
    [SerializeField] TMP_Text D_item3;
    [SerializeField] TMP_Text D_item4;
    [SerializeField] TMP_Text D_item5;
    [SerializeField] TMP_Text D_item6;
    [SerializeField] TMP_Text D_item7;
    [SerializeField] TMP_Text D_item8;

    [SerializeField] TMP_Text P_item1; //player items
    [SerializeField] TMP_Text P_item2;
    [SerializeField] TMP_Text P_item3;
    [SerializeField] TMP_Text P_item4;
    [SerializeField] TMP_Text P_item5;
    [SerializeField] TMP_Text P_item6;
    [SerializeField] TMP_Text P_item7;
    [SerializeField] TMP_Text P_item8;

    [SerializeField] TMP_Text rounds;
    [SerializeField] TMP_Text turns;
    [SerializeField] TMP_Text generation;
    [SerializeField] TMP_Text lastAction;
    int genNum = 0;
    [SerializeField] TMP_Text p_wins;
    int playerWins = 0;
    [SerializeField] TMP_Text d_wins;
    int dealerWins = 0;

    #endregion

    #region initializing Functions
    private void Start()
    {
        //add item texts to arraylist
        p_item_textArray[0] = P_item1;
        p_item_textArray[1] = P_item2;
        p_item_textArray[2] = P_item3;
        p_item_textArray[3] = P_item4;
        p_item_textArray[4] = P_item5;
        p_item_textArray[5] = P_item6;
        p_item_textArray[6] = P_item7;
        p_item_textArray[7] = P_item8;

        d_item_textArray[0] = D_item1;
        d_item_textArray[1] = D_item2;
        d_item_textArray[2] = D_item3;
        d_item_textArray[3] = D_item4;
        d_item_textArray[4] = D_item5;
        d_item_textArray[5] = D_item6;
        d_item_textArray[6] = D_item7;
        d_item_textArray[7] = D_item8;

        //roundStart(); //will be covered in onEpisodeBegin() i think?
    }

    private void roundStart() 
    {
        //reset items, could be optimized with give items, but idc rn
        for (int i = 0; i < 8; i++)
        {
            playerItemArray[i] = 0;
            dealerItemArray[i] = 0;
        }

        //will implement give items later :p
        //item num is between 1 and 4, item is completely random, also 2 cigs max
        itemNum = UnityEngine.Random.Range(1, 5);
        for (int i = 0; i < itemNum; i++)
        {
            int newPItem = UnityEngine.Random.Range(1, 6);
            while(newPItem == 1 && p_cigNum > 1)
            {
                newPItem = UnityEngine.Random.Range(1, 6);
            }
            if(newPItem == 1)
            {
                p_cigNum++;
            }
            playerItemArray[i] = newPItem;


            int newDItem = UnityEngine.Random.Range(1, 6);

            while (newDItem == 1 && d_cigNum > 1)
            {
                newDItem = UnityEngine.Random.Range(1, 6);
            }
            if (newDItem == 1)
            {
                d_cigNum++;
            }

            dealerItemArray[i] = newDItem;

        }

        //random num from 2 to 4 (both inclusive im pretty sure) for health
        healthMax = UnityEngine.Random.Range(2, 5);
        playerHealth = healthMax;
        dealerHealth = healthMax;

        //random num from 2 to 8, half floored are live, rest are blank
        int totalShots = UnityEngine.Random.Range(2, 9);
        liveNum = (int)Math.Floor(totalShots / 2f);
        blankNum = totalShots - liveNum;

        sendToCanvas();

    }


    void newSubRound()
    {
        sawDmg = 0;
        StartCoroutine(newSubRoundWait());
        turnInProgress = false;
        isPlayerTurn = true;
        //add new items
        itemNum = UnityEngine.Random.Range(1, 5);
        int pItemAdd = itemNum;
        int dItemAdd = itemNum;

        for (int i = 0; i < 8 && (pItemAdd > 0 && dItemAdd > 0); i++)
        {
            if (playerItemArray[i] == 0 && pItemAdd > 0)
            {
                int newPItem = UnityEngine.Random.Range(1, 6);
                while (newPItem == 1 && p_cigNum > 1)
                {
                    newPItem = UnityEngine.Random.Range(1, 6);
                }
                if (newPItem == 1)
                {
                    p_cigNum++;
                }
                playerItemArray[i] = newPItem;
                pItemAdd--;
            }



            if (dealerItemArray[i] == 0 && dItemAdd > 0)
            {
                int newDItem = UnityEngine.Random.Range(1, 6);

                while (newDItem == 1 && d_cigNum > 1)
                {
                    newDItem = UnityEngine.Random.Range(1, 6);
                }
                if (newDItem == 1)
                {
                    d_cigNum++;
                }

                dealerItemArray[i] = newDItem;
                dItemAdd--;
            }
        }

        //random num from 2 to 8, half floored are live, rest are blank
        int totalShots = UnityEngine.Random.Range(2, 9);
        liveNum = (int)Math.Floor(totalShots / 2f);
        blankNum = totalShots - liveNum;

        sendToCanvas();
    }


    public override void OnEpisodeBegin()
    {
        roundStart();
        genNum++;
        generation.text = "Generation: " + genNum;
    }

    #endregion

    #region ToolSet Functions

    IEnumerator wait()
    {
        Debug.Log("waited at time" + Time.time);
        checkEndofShots();
        checkWinCondition();
        sawDmg = 0;
        sendToCanvas();
        yield return new WaitForSeconds(secBetweenTurns);
        lastAction.text = "";
        turnInProgress = false;
    }

    IEnumerator newSubRoundWait()
    {
        yield return new WaitForSeconds(secBetweenTurns);
        lastAction.text = "";
    }

    void checkWinCondition()
    {
        if (playerHealth < 1)
        {
            //player looses
            SetReward(-1);
            dealerWins++;
            d_wins.text = "Wins: " + dealerWins;
            EndEpisode();
        }
        else if (dealerHealth < 1)
        {
            //player wins
            SetReward(1);
            playerWins++;
            p_wins.text = "Wins: " + playerWins;
            EndEpisode();
        }
    }
    bool checkEndofShots()
    {
        if ((liveNum) + blankNum < 1)
        {
            newSubRound();
            return true;
        }
        return false;
    }

    private void sendToCanvas()
    {
        for(int i = 0; i < 8; i++)
        {
            p_item_textArray[i].text = "Item: " + itemIntToString(playerItemArray[i]);
            d_item_textArray[i].text = "Item: " + itemIntToString(dealerItemArray[i]);
        }
        P_Health.text = "Health: " + playerHealth +"/"+ healthMax;
        D_Health.text = "Health: " + dealerHealth +"/"+ healthMax;

        rounds.text = liveNum + " live\n" + blankNum + " blank";
        if(isPlayerTurn)
        {
            turns.text = "Player";
        }
        else
        {
            turns.text = "Dealer";
        }
    }

    private string itemIntToString(int itemId)
    {
     /*
     * item id chart
     * --------------
     * 0 --> no item
     * 1 --> cig
     * 2 --> mag glass
     * 3 --> handcuff
     * 4 --> beer
     * 5 --> saw
     * 
     */
        switch (itemId)
        {
            case 0:
                return "none";
            case 1:
                return "cig";
            case 2:
                return "mag glass";
            case 3:
                return "handcuff";
            case 4:
                return "beer";
            case 5:
                return "saw";
        }

        return "no item found";
    }

    int dealerUseItem(int itemId) //return 1 if item was used correctly, -1 if not
    {
        switch (itemId)
        {
            case 0:
                return -1;
            case 1:
                if(dealerHealth == healthMax)
                {
                    return -1;
                }
                dealerHealth++;
                if (dealerHealth > healthMax)
                {
                    dealerHealth = healthMax;
                }
                d_cigNum--;
                return 1;
            case 2:
                if (thinksNextShot != -1)
                {
                    return -1;
                }
                thinksNextShot = nextShot;
                return 1;
            case 3:
                if (p_cuffed || p_lastCuffed)
                {
                    return -1;
                }
                p_cuffed = true;
                return 1;
            case 4:
                if (nextShot == 1)
                {
                    liveNum--;
                }
                else
                {
                    blankNum--;
                }
                nextShot = UnityEngine.Random.Range(0, liveNum + blankNum); //realized this is like the fifth time this code chuck has shown up
                if (nextShot < liveNum)                                     //i should make it a method... ill do it later
                {
                    nextShot = 1;
                }
                else
                {
                    nextShot = 0;
                }
                return 1;
            case 5:
                if(sawDmg > 0)
                {
                    return -1;
                }
                sawDmg = 1;
                return 1;
        }

        return -1;
    }

    int playerUseItem(int itemId)
    {                    //i seperated into two functions instead of one function with a check
                         //because i wanted to avoid an extra if statement, both for readability and (probably insignificant) optimization
                         //update: this was way worse for readability, you live you learn ig
        switch (itemId)
        {
            case 0:
                return -1;
            case 1:
                playerHealth++;
                if(playerHealth > healthMax)
                {
                    playerHealth = healthMax;
                }
                p_cigNum--;
                return 1;
            case 2:
                if(thinksNextShot != -1)
                {
                    return -1;
                }
                thinksNextShot = nextShot;
                return 1;
            case 3:
                if (d_cuffed || d_lastCuffed)
                {
                    return -1;
                }
                d_cuffed = true;
                return 1;
            case 4:
                if(nextShot == 1)
                {
                    liveNum--;
                }
                else
                {
                    blankNum--;
                }
                nextShot = UnityEngine.Random.Range(0, liveNum + blankNum); //realized this is like the fifth time this code chuck has shown up
                if (nextShot < liveNum)                                     //i should make it a method... ill do it later
                {
                    nextShot = 1;
                }
                else
                {
                    nextShot = 0;
                }
                return 1;
            case 5:
                if(sawDmg > 0)
                {
                    return -1;
                }
                sawDmg = 1;
                return 1;
        }

        return -1;
    }
    #endregion

    #region Dealer and Player AIs

    private void Update() //lord have mercy on whoever needs to read this update function
    {

        if (!isPlayerTurn && !turnInProgress) //dealer turn block
        {
            if(d_cuffed)
            {
                isPlayerTurn = true;
                d_cuffed = false;
                d_lastCuffed = true;
                return;
            }
            d_lastCuffed = false;
            turnInProgress = true;
            bool willSetPlayerTurn = true;

            nextShot = UnityEngine.Random.Range(0, (liveNum + blankNum));

            if(nextShot < liveNum)
            {
                nextShot = 1;
                liveNum--;
            }
            else
            {
                nextShot = 0;
                blankNum--;
            }

            for (int i = 0; i < 8; i++)
            {
                //attempt to use item
                if (dealerUseItem(dealerItemArray[i]) == 1)//successfully used item
                {
                    lastAction.text += "\nDealer Used " + itemIntToString(dealerItemArray[i]);
                    dealerItemArray[i] = 0;
                    if (checkEndofShots())
                    {
                        return;
                    }
                }
            }

            //dealer shoots

            //last bullet check, dealer will make best play
            if ((liveNum + blankNum) == 1 && nextShot == 1)
            {
                if(nextShot == 1)
                {
                    lastAction.text += "\nDealer Successfully Shot Player for " + (1 + sawDmg) + " damage;";
                    playerHealth -= (1 + sawDmg);

                    thinksNextShot = -1; //must set this back to -1 at the end of every turn, so be careful if you use returns here or smth

                    sendToCanvas();
                    isPlayerTurn = willSetPlayerTurn;
                    StartCoroutine(wait());
                    return;
                }
                else
                {
                    lastAction.text += "\nDealer last bullet blank";
                    thinksNextShot = -1; //must set this back to -1 at the end of every turn, so be careful if you use returns here or smth

                    isPlayerTurn = willSetPlayerTurn;
                    StartCoroutine(wait());
                    return;
                }
            }
            

            int dealerShot;
            if(thinksNextShot != -1)
            {
                dealerShot = thinksNextShot;
            }
            else
            {
                dealerShot = UnityEngine.Random.Range(0, 2); //0 shoot self, 1 shoot player
            }
            
            if(dealerShot == 0)
            {
                if(nextShot == 1)
                {
                    dealerHealth -= 1 + sawDmg;
                    lastAction.text += "\nDealer Shot himself for " + (1 + sawDmg) + " damage;";
                }
                else
                {
                    lastAction.text += "\nDealer shot himself with a blank";
                    willSetPlayerTurn = false; //dealer will go again
                }
            }
            else
            {
                if (nextShot == 1)
                {
                    lastAction.text += "\nDealer Successfully Shot Player for " + (1 + sawDmg) + " damage;";
                    playerHealth -= 1 + sawDmg;
                }
                else
                //else nothing
                {
                    lastAction.text += "\nDealer Shot Player with blank";
                }
            }

            thinksNextShot = -1; //must set this back to -1 at the end of every turn, so be careful if you use returns here or smth

            isPlayerTurn = willSetPlayerTurn;
            StartCoroutine(wait());
        }
        else 
        if(isPlayerTurn && !turnInProgress)//starting player turn stuff
        {
            if (p_cuffed)
            {
                isPlayerTurn = false;
                p_cuffed = false;
                p_lastCuffed = true;
                return;
            }
            p_lastCuffed = false;
            turnInProgress = true;

            nextShot = UnityEngine.Random.Range(0, (liveNum + blankNum));
            if (nextShot < liveNum)
            {
                nextShot = 1;
            }
            else
            {
                nextShot = 0;
            }
            RequestDecision();
        }
    }

    //player AI is located in CollectObservations and OnActionRecieved.
    //calling sequence is update --> collectObservations --> OnActionRecieved
    //OnActionRecieved will recursively call --> collectObservations --> OnActionRecieved if an item is used
    //will finish once AI does not use an item, or has no more items to use

    /*Player AI Sequence (I hope)
     * Update --> collectObservations --> OnActionRecieved --> finished
     *                      ^                  |
     *                      |__________________|                              
     * 
     */

    //left off, set up way for player to use items
    //to next: dealer use items, finish implementing items, then done?
    public override void OnActionReceived(ActionBuffers actions)
    {

        for(int i = 0; i < 8; i++)
        {
            int act = actions.DiscreteActions[i];
            if(act == 1)//attempt to use item
            {
                if(playerUseItem(playerItemArray[i]) == 1)//successfully used item
                {
                    lastAction.text += "\nPlayer used " + itemIntToString(playerItemArray[i]);
                    playerItemArray[i] = 0;
                    if (checkEndofShots())
                    {
                        return;
                    }
                    RequestDecision();
                    return;//??? maybe? like I dont want the reset of the function to run, so I should return, right?
                }
            }
        }

        int playerShot = actions.DiscreteActions[8]; //0 shoot self, 1 shoot dealer

        if (playerShot == 0)
        {
            if (nextShot == 1)
            {
                lastAction.text += "\nPlayer Shot themselves for " + (1 + sawDmg) + " damage";
                playerHealth -= (1 + sawDmg);
                liveNum--;
            }
            else
            {
                lastAction.text += "\nPlayer Shot themselves with a blank";
                isPlayerTurn = true; //player will go again
                blankNum--;
            }
        }
        else
        {
            if (nextShot == 1)
            {
                lastAction.text += "\nPlayer Shot dealer for " + (1 + sawDmg) + " damage;";
                dealerHealth -= 1 + sawDmg;
                liveNum--;
            }
            else
            {
                lastAction.text += "\nPlayer Shot dealer with a blank";
                blankNum--;
            }
        }

        isPlayerTurn = false;
        thinksNextShot = -1; //so technically should always be -1 when it gets here, but just in case
        StartCoroutine(wait());
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //first 16 abservations are items
        for (int i = 0; i < 8;  i++)
        {
            sensor.AddObservation(playerItemArray[i]);
            sensor.AddObservation(dealerItemArray[i]);
        }

        //next 2 are number of live and blanks
        sensor.AddObservation(liveNum); 
        sensor.AddObservation(blankNum);

        //then next 2 player and dealer health
        sensor.AddObservation(playerHealth);
        sensor.AddObservation(dealerHealth);

        //I literally dont know if this works, i can only pray
        sensor.AddObservation(thinksNextShot);
        Debug.Log(thinksNextShot);
        thinksNextShot = -1; //reset to avoid infinite loop hopefully

        //i think above worked, not sure
        //next two is saw and if dealer is cuffed
        sensor.AddObservation(sawDmg);
        sensor.AddObservation(d_cuffed);

        //23 observations
    }
    #endregion
}
