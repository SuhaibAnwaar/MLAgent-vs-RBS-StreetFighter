using System.Collections;
using UnityEngine;

/* 
 * 0.2 seconds for punch and kick animation
 * 0.5 seconds for combo 1 animation
 * 0.8 seconds for combo 2 animation
 * 1.1 seconds for jump animation 
 */

public class StaticAI : MonoBehaviour
{    
    public BoxCollider2D wallColliderLeft;
    public BoxCollider2D wallColliderRight;

    //true means the opponent is within striking range.
    private bool isInRange = false;
    //true means AI is blocking.
    private bool isBlocking = false;
    //true means AI is jumping.
    private bool isJumping = false;
    //true means the AI is moving forward.
    private bool isMovingForward = true;
    //true means move AI to the center of the platform.
    private bool isMoveCenter = false;
    //true means AI is still playing.
    private bool isPlaying = true;
    //true means currently performing an attack
    private bool isAttacking = false;
    //true means the AI is allowed to move backwards.
    private bool canMoveBack = true;
    //true means the AI is currently moving backwards.
    private bool isMovingBackward = false;

    //the time the AI will need to wait for its attack animation to finish
    private float attackTime = 1.0f;
    //How long the AI moves back for.
    private float movingBackTimer = 1f;

    //all types of actions the AI can perform
    private string[] actions = new string[6] { "Punch", "Kick", "Block", "Combo1", "Combo2", "Jump" };

    private PlayerController playerController;
    private AnimationActions animActions;
    private PlayerManager playerManager;
    private Rigidbody2D rb;
    private CSVWriter csvWriter;

    // Start is called before the first frame update
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        animActions = GetComponent<AnimationActions>();
        playerManager = GetComponent<PlayerManager>();
        rb = GetComponent<Rigidbody2D>();
        csvWriter = GetComponent<CSVWriter>();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController)
        {
            if (!playerController.isMLAgentPlaying)
            {
                //If either player loses, the AI should stop fighting.
                if (playerManager)
                {
                    //If the opponent cannot take any more combo damage then we can assume its health is zero, so reset booleans.
                    if (!playerManager.canPlayerTakeComboDamage && isPlaying)
                    {
                        isInRange = false;
                        isPlaying = false;
                        isAttacking = false;
                        isBlocking = false;
                        canMoveBack = true;
                        StopAllCoroutines();
                    }
                    else if (playerManager.canPlayerTakeComboDamage)
                    {
                        isPlaying = true;
                    }
                }
                if (playerController)
                {
                    //When the AI loses, reset the booleans.
                    if (playerController.isDead && isPlaying)
                    {
                        isInRange = false;
                        isPlaying = false;
                        isAttacking = false;
                        isBlocking = false;
                        canMoveBack = true;
                        StopAllCoroutines();
                    }
                    else if (!playerController.isDead)
                    {
                        isPlaying = true;
                    }
                }
                if (playerController && playerManager)
                {
                    if (isPlaying && !playerController.isGameFinished)
                    {
                        //reset blocking action so the AI does not get stuck using it after a knockdown.
                        if (playerController.isKnockdown)
                        {
                            isBlocking = false;
                            playerController.isBlocking = isBlocking;

                            animActions.Block(false);
                            StopCoroutine(BlockCoroutine());

                            isAttacking = false;
                            attackTime = 0.0f;
                        }

                        if (isInRange && !isAttacking && !isBlocking && !isJumping && !playerController.isKnockdown && !isMovingBackward)
                        {
                            isAttacking = true;

                            //perfrom an action
                            StartCoroutine(AttackCoroutine());
                        }

                        Vector3 playerT = playerController.transform.position;
                        Vector3 oppenentT = playerManager.Opponent.transform.position;
                        Vector3 center = new Vector3(0f, playerT.y, playerT.z);

                        //Check distance from both wall colliders.
                        CheckWallCollider(playerT);

                        //If the AI's back is near a wall collider then move it back to the center.
                        if (isMoveCenter)
                        {
                            playerController.transform.position = Vector3.MoveTowards(playerT, center, Time.deltaTime);
                            animActions.Move(1.0f);
                        }

                        //When the AI reaches the center, reset the booleans.
                        if (playerT == center)
                        {
                            isMovingForward = true;
                            isMoveCenter = false;
                        }

                        //Move AI back
                        MovingBack();

                        //Check distance from opponent.
                        float distancePlayer = Vector2.Distance(playerT, oppenentT);

                        if (isMovingForward)
                        {
                            //Move towards player if out of attacking range.
                            if (distancePlayer >= 1.0f && !isBlocking && !playerController.isDead)
                            {
                                playerController.transform.position = Vector3.MoveTowards(playerT, oppenentT, Time.deltaTime);
                                animActions.Move(1.0f);
                            }
                            else
                            {
                                animActions.Move(0f);
                            }
                        }
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (playerController)
        {            
            if (collision.CompareTag(playerController.Opponent))
            {
                isInRange = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (playerController)
        {
            if (collision.CompareTag(playerController.Opponent))
            {
                isInRange = false;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (playerController)
        {
            if (!playerController.isMLAgentPlaying)
            {
                //collided with the ground therefore not jumping
                if (collision.gameObject.CompareTag("Ground"))
                {
                    if (animActions)
                    {
                        isMovingForward = true;
                        isJumping = false;
                        animActions.Jump(isJumping);
                    }
                }
            }
        }
    }

    //jump and move back
    private void Retreat()
    {        
        animActions.Jump(isJumping);

        isMovingForward = false;

        //Jump
        rb.AddForce(new Vector2(rb.velocity.x, playerController.jumpSpeed));

        //move back depending on the way the character is facing
        if (playerController.isFlipped)
        {
            isInRange = false;
            rb.velocity = new Vector2(1.0f * playerController.playerSpeed, rb.velocity.y);
        }
        else if (!playerController.isFlipped)
        {
            isInRange = false;
            rb.velocity = new Vector2(-1.0f * playerController.playerSpeed, rb.velocity.y);
        }
    }

    //depending on the parameter value an action will be performed
    private void AIAction(string actionType)
    {
        switch(actionType)
        {
            case "Punch":
                playerController.Punch(false);
                break;
            case "Kick":
                playerController.Kick(false);
                break;
            case "Block":
                if (!isMoveCenter)//only block when the AI is not moving to the center
                {
                    isBlocking = true;
                    animActions.Block(isBlocking);
                    playerController.isBlocking = isBlocking;
                    csvWriter.Blocks++;
                    StartCoroutine(BlockCoroutine());
                }
                break;
            case "Combo1":
                playerController.Punch(true);
                break;
            case "Combo2":
                playerController.Kick(true);
                break;
            case "Jump":             
                isJumping = true;
                playerController.isJumping = true;
                csvWriter.Jumps++;
                Retreat();//if jumping, use Retreat function
                break;
            default:
                Debug.Log("Invalid action");
                break;
        }
        isAttacking = false;
    }

    //Checks if the AI's back is close to any of the two wall colliders 
    private void CheckWallCollider(Vector3 opponent)
    {
        //get position of both wall colliders
        Vector3 leftColliderT = wallColliderLeft.transform.position;
        Vector3 rightColliderT = wallColliderRight.transform.position;

        //The distance from the left wall collider
        float distanceLeftCollider = Vector2.Distance(opponent, leftColliderT);

        //The distance from the right wall collider
        float distanceRightCollider = Vector2.Distance(opponent, rightColliderT);

        //check whether the AI is close to the right or left wall collider
        if (distanceLeftCollider <= 3.0f && !playerController.isFlipped && !isBlocking)
        {
            isMovingForward = false;
            isMoveCenter = true;
        }
        else if (distanceRightCollider <= 3.0f && playerController.isFlipped && !isBlocking)
        {
            isMovingForward = false;
            isMoveCenter = true;
        }
    }

    //Moves the AI back.
    private void MovingBack()
    {
        if (canMoveBack)
        {
            if (!isMovingBackward)
            {
                if (!isBlocking && !isJumping && !playerController.isKnockdown && playerController.isComboCooldown  && !isMoveCenter)
                {
                    //if the random number equals 1 then move AI back
                    int moveDirection = Random.Range(0, 2);
                    if (moveDirection == 1)
                    {
                        isMovingBackward = true;
                        isMovingForward = false;
                    }
                    else
                    {
                        canMoveBack = false;

                        //Start Coroutine
                        StartCoroutine(CanMoveBackCoroutine());
                    }
                }
            }
            else
            {
                //Decrement timer
                movingBackTimer -= Time.deltaTime;

                //move back depending on the way the character is facing
                if (playerController.isFlipped)
                {
                    isInRange = false;
                    rb.velocity = new Vector2(1.0f, rb.velocity.y);
                    animActions.Move(1.0f);
                }
                else if (!playerController.isFlipped)
                {
                    isInRange = false;
                    rb.velocity = new Vector2(-1.0f, rb.velocity.y);
                    animActions.Move(1.0f);
                }

            }
        }
        //When the timer reaches zero, the AI is able to walk forward again
        if (movingBackTimer <= 0.0f)
        {
            isMovingBackward = false;
            canMoveBack = false;
            isMovingForward = true;
            movingBackTimer = 1f;

            StartCoroutine(CanMoveBackCoroutine());
        }
    }

    //every few seconds a random number will be generated which will decide which action the AI will take
    IEnumerator AttackCoroutine()
    {
        yield return new WaitForSeconds(attackTime);

        //generate random number
        int rand = Random.Range(0, 6);

        string randActions = actions[rand];

        //set the wait time relative to how long each animation lasts.
        switch (randActions)
        {
            case "Punch":
                attackTime = 0.2f;
                break;
            case "Kick":
                attackTime = 0.2f;
                break;
            case "Block":
                attackTime = 1.0f;
                break;
            case "Comb1":
                if (playerController.isComboCooldown)//if this combo is currently on cooldown then don't play this animation and move to the next action.
                {
                    attackTime = 0.0f;
                }
                else
                {
                    attackTime = 0.5f;
                }
                break;
            case "Comb2":
                if (playerController.isComboCooldown)//if this combo is currently on cooldown then don't play this animation and move to the next action.
                {
                    attackTime = 0.0f;
                }
                else
                {
                    attackTime = 0.8f;
                }
                break;
            case "Jump":
                attackTime = 1.1f;
                break;
            default:
                break;
        }

        //do not perform any other actions until conditions are met
        if (!isBlocking && !isJumping && !playerController.isKnockdown)
        {
            //perform action
            AIAction(randActions);
        }
        else
        {
            isAttacking = false;
        }
    }

    //if the block action is chosen the AI will block for 1 second before moving back to idle state
    IEnumerator BlockCoroutine()
    {
        yield return new WaitForSeconds(1.0f);

        isBlocking = false;
        animActions.Block(isBlocking);
        playerController.isBlocking = isBlocking;
    }

    //After the AI has moved back, wait 2 seconds before it can use this action again.
    IEnumerator CanMoveBackCoroutine()
    {
        yield return new WaitForSeconds(2f);

        canMoveBack = true;
    }
}
