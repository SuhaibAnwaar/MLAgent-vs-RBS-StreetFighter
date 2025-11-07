using UnityEngine;
using System.IO;

public class GameManager : MonoBehaviour
{
    public PlayerController Ryu;
    public PlayerController Ken;
    public Round round;

    public int maxRound;
    [HideInInspector]
    public int currentRound = 1;
    public string folderPath = "";

    //true means waiting to reset.
    private bool isRoundReset = false;

    public void Start()
    {
        //the folder where the fight data will be stored.
        Directory.CreateDirectory(Application.streamingAssetsPath + folderPath);
    }

    // Update is called once per frame
    void Update()
    {        
        //check if both players are alive
        if(Ryu && Ken)
        {
            //If one of them loses then increment round
            if (Ryu.isDead || Ken.isDead)
            {
                if (currentRound <= maxRound && !isRoundReset)
                {
                    currentRound += 1;
                    if(round)
                    {
                        round.UpdateRound(currentRound);
                    }
                    isRoundReset = true;
                }
                else if(currentRound > maxRound)
                {
                    if(round)
                    {
                        round.GameFinished();
                    }
                    Ryu.isGameFinished = true;
                    Ken.isGameFinished = true;
                }
            }
            else //When both players reset their position and health, the boolean is reseted
            {
                isRoundReset = false;
            }           
        }
    }
}
