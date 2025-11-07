using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationActions : MonoBehaviour
{
    public string idleAnimation = "";
    public string jumpAnimation = "";
    public string punchAnimation = "";
    public string kickAnimation = "";
    public string blockAnimation = "";
    public string comboAnimation1 = "";
    public string comboAnimation2 = "";
    public string hitAnimation = "";
    public string knockdownAnimation = "";
    public string koAnimation = "";
    
    private Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    //Plays Idle  
    public void Idle()
    {
        anim.SetTrigger(idleAnimation);
    }

    //plays the move animation
    public void Move(float speed)
    {
        //set movement to be absolute as it can be negative which will not play the running animations
        anim.SetFloat("Speed", Mathf.Abs(speed));
    }

    //plays the jump animation
    public void Jump(bool isJumping)
    {
        if (isJumping)
        {            
            anim.SetBool(jumpAnimation, isJumping);           
        }
        else
        {
            anim.SetBool(jumpAnimation, isJumping);
        }
    }

    //plays the block animation 
    public void Block(bool isBlocking)
    {
        if (isBlocking)
        {
            anim.SetBool(blockAnimation, isBlocking);
        }
        else
        {
            anim.SetBool(blockAnimation, isBlocking);
        }        
    }

    //playd the punch animation
    public void Punch()
    {       
        anim.SetTrigger(punchAnimation);
    }

    //plays the kick
    public void Kick()
    {        
        anim.SetTrigger(kickAnimation);        
    }

    //plays comboAttack1 animation
    public void ComboAttack1()
    {
        anim.SetTrigger(comboAnimation1);
    }

    //plays comboAttack2 aniamtion
    public void ComboAttack2()
    {
        anim.SetTrigger(comboAnimation2);
    }

    //plays hit animation
    public void Hit()
    {
        anim.SetTrigger(hitAnimation);
    }

    //plays knockdown animation
    public void Knockdown()
    {
        anim.SetTrigger(knockdownAnimation);
    }

    //plays KO animation
    public void KO()
    {
        anim.SetTrigger(koAnimation);
    }
}
