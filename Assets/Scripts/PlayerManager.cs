using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script keeps track of the opposing player game state.
//Each player will have this script so they can check if they are able to deal damage to their opponent.

public class PlayerManager : MonoBehaviour
{
    public PlayerController Opponent;

    [HideInInspector]
    public bool canPlayerTakeBasicDamage = true;
    [HideInInspector]
    public bool canPlayerTakeComboDamage = true;
    [HideInInspector]
    public float currentHealth = 300f;

    // Update is called once per frame
    void Update()
    {
        if (Opponent)
        {
            if (!Opponent.isDead && !Opponent.isBlocking)
            {
                canPlayerTakeBasicDamage = true;
            }
            else
            {
                canPlayerTakeBasicDamage = false;
            }

            if (!Opponent.isDead)
            {
                canPlayerTakeComboDamage = true;
            }
            else
            {
                canPlayerTakeComboDamage = false;
            }
        }
    }
}
