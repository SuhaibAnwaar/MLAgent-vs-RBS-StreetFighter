using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class LearningAI : Agent
{
    public float playerSpeed = 2f;

    private Transform enemyPos;
    private AnimationActions animActions;
    private PlayerController playerController;
    private PlayerManager playerManager;
    private Rigidbody2D rb;
    private CSVWriter csvWriter;

    private bool isJumping = false;
    private bool isPunching = false;
    private bool isKicking = false;
    private bool isBlocking = false;
    private bool isPlaying = true;
    private bool isCollider = false;
    private bool isPerformingCombo1 = false;
    private bool isPerformingCombo2 = false;
    private bool isLeftCollider = false;
    private bool isRightCollider = false;

    private int punchCombo = 0;
    private int kickCombo = 0;

    //These arrays hold the number of times the AI uses an action consectively.
    private int numPunches;
    private int numKicks;
    private int numBlocks;
    private int numJumps;
    private int numCombo1;
    private int numCombo2;

    private float distToPlayer = 0f;

    public void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animActions = GetComponent<AnimationActions>();
        playerController = GetComponent<PlayerController>();
        playerManager = GetComponent<PlayerManager>();
        csvWriter = GetComponent<CSVWriter>();

        enemyPos = GameObject.FindWithTag(playerController.Opponent).transform;

        if(playerManager)
        {
            playerManager.canPlayerTakeComboDamage = true;
        }
    }

    private void Update()
    {
        if (playerController)
        {
            if (playerController.isMLAgentPlaying)
            {
                //Check if the AI has dealt or recevied damage.
                CheckDamageStatus();

                //AI lost the round
                if (playerController.isDead)
                {
                    SetReward(-1.0f);
                    isPlaying = false;
                    EndEpisode();
                }
                else if (!playerManager.canPlayerTakeComboDamage) //AI won the round
                {
                    SetReward(1.0f);
                    isPlaying = false;
                    EndEpisode();
                }

                //If both players are still fighting then continue playing or if one player has lost the battle then wait for the player controller to reset, for the next round.
                if (!playerController.isDead || !playerManager.canPlayerTakeComboDamage)
                {
                    isPlaying = true;
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (playerController)
        {
            if (playerController.isMLAgentPlaying)
            {
                //Collided with the ground therefore not jumping.
                if (collision.gameObject.CompareTag("Ground"))
                {
                    if (animActions)
                    {
                        isJumping = false;
                        animActions.Jump(isJumping);
                    }
                }
                if (collision.gameObject.CompareTag("Left Wall Collider"))
                {
                    isCollider = true;
                    isLeftCollider = true;
                }
                if (collision.gameObject.CompareTag("Right Wall Collider"))
                {
                    isCollider = true;
                    isRightCollider = true;
                }
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (playerController)
        {
            if (playerController.isMLAgentPlaying)
            {
                sensor.AddObservation(transform.position.x);
                sensor.AddObservation(transform.position.y);

                if (enemyPos)
                {
                    sensor.AddObservation(enemyPos.position.x);
                    sensor.AddObservation(enemyPos.position.y);

                    distToPlayer = Vector2.Distance(transform.position, enemyPos.position);
                    sensor.AddObservation(distToPlayer);
                }

                sensor.AddObservation(playerController.comboCooldownTimer);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (playerController)
        {
            if (playerController.isMLAgentPlaying)
            {
                if (isPlaying && !playerController.isGameFinished)
                {
                    //This will encourage the AI to fight within range.
                    if (distToPlayer > 1f)
                    {
                        SetReward(-0.2f);
                    }
                    else if (distToPlayer < 1f)
                    {
                        SetReward(0.1f);
                    }

                    //Moving
                    //The AI should only be able to move if it is not blocking or knocked down.
                    if (!playerController.isBlocking && !playerController.isKnockdown)
                    {
                        if (!isCollider)
                        {
                            rb.velocity = new Vector2(actions.ContinuousActions[0] * playerSpeed, rb.velocity.y);
                            animActions.Move(actions.ContinuousActions[0]);
                        }
                        else//Prevents the AI from being stuck to the wall in mid air when it tries to jump and move into the collider.
                        {
                            if (isLeftCollider)
                            {
                                if (actions.ContinuousActions[0] > 0)
                                {
                                    isLeftCollider = false;
                                    isCollider = false;
                                }
                            }
                            if (isRightCollider)
                            {
                                if (actions.ContinuousActions[0] > 0)
                                {
                                    isRightCollider = false;
                                    isCollider = false;
                                }
                            }
                        }
                    }

                    if (!isBlocking && !isJumping && !playerController.isKnockdown)
                    {
                        if (isPerformingCombo1)
                        {
                            if (punchCombo <= 3)
                            {
                                if (!isPunching)
                                {
                                    //Punch three times
                                    playerController.Punch(false);
                                    isPunching = true;
                                    punchCombo++;
                                    StartCoroutine(PunchCoroutine());

                                }
                            }
                            else
                            {
                                //after three punches perform combo
                                playerController.Punch(true);
                                punchCombo = 0;
                                isPerformingCombo1 = false;
                            }
                        }

                        if (isPerformingCombo2)
                        {
                            if (kickCombo <= 3)
                            {
                                if (!isKicking)
                                {
                                    //Kick three times
                                    playerController.Kick(false);
                                    isKicking = true;
                                    kickCombo++;
                                    StartCoroutine(KickCoroutine());
                                }
                            }
                            else
                            {
                                //after three kicks perform combo
                                playerController.Kick(true);
                                kickCombo = 0;
                                isPerformingCombo2 = false;
                            }
                        }

                        if (!isPerformingCombo1 || !isPerformingCombo2)
                        {
                            //Jumping
                            if (actions.ContinuousActions[1] > 0)
                            {
                                if (!isJumping && !playerController.isKnockdown && !isBlocking)
                                {
                                    //When the AI uses the jump action 5 times in a row, it starts to receive a negative reward. This will force it to choose another action.
                                    if (numJumps >= 5)
                                    {
                                        SetReward(-0.1f);
                                    }
                                    else
                                    {
                                        numJumps++;
                                    }

                                    rb.AddForce(new Vector2(rb.velocity.x, playerController.jumpSpeed));
                                    isJumping = true;
                                    playerController.isJumping = isJumping;
                                    animActions.Jump(isJumping);
                                    csvWriter.Jumps++;

                                    numPunches = 0;
                                    numKicks = 0;
                                    numCombo1 = 0;
                                    numCombo2 = 0;
                                }
                            }

                            //Punching
                            if (actions.ContinuousActions[2] > 0 && !isPunching && !isJumping)
                            {
                                if (numPunches >= 5)
                                {
                                    SetReward(-0.1f);
                                }
                                else
                                {
                                    numPunches++;
                                }

                                playerController.Punch(false);
                                isPunching = true;                                
                                StartCoroutine(PunchCoroutine());

                                numJumps = 0;
                                numKicks = 0;
                                numBlocks = 0;
                                numCombo1 = 0;
                                numCombo2 = 0;
                            }

                            //Kicking
                            if (actions.ContinuousActions[3] > 0 && !isKicking && !isJumping)
                            {
                                if (numKicks >= 5)
                                {
                                    SetReward(-0.1f);
                                }
                                else
                                {
                                    numKicks++;
                                }

                                playerController.Kick(false);
                                isKicking = true;
                                StartCoroutine(KickCoroutine());

                                numJumps = 0;
                                numPunches = 0;
                                numBlocks = 0;
                                numCombo1 = 0;
                                numCombo2 = 0;
                            }

                            //Blocking
                            if (actions.ContinuousActions[4] > 0 && !isBlocking && !isJumping)
                            {
                                if (numBlocks >= 5)
                                {
                                    SetReward(-0.1f);
                                }
                                else
                                {
                                    numBlocks++;
                                }

                                isBlocking = true;
                                animActions.Block(isBlocking);
                                playerController.isBlocking = isBlocking;
                                StartCoroutine(BlockingCoroutine());
                                csvWriter.Blocks++;

                                numKicks = 0;
                                numPunches = 0;
                                numCombo1 = 0;
                                numCombo2 = 0;
                            }

                            //Combo Attack 1
                            if (actions.ContinuousActions[5] > 0 && !isJumping && playerController.comboCooldownTimer > 0)
                            {
                                if (numCombo1 >= 5)
                                {
                                    SetReward(-0.1f);
                                }
                                else
                                {
                                    numCombo1++;
                                }

                                isPerformingCombo1 = true;

                                numPunches = 0;
                                numKicks = 0;
                                numJumps = 0;
                                numBlocks = 0;
                                numCombo2 = 0;
                            }

                            //Combo attack 2
                            if (actions.ContinuousActions[6] > 0 && !isJumping && playerController.comboCooldownTimer > 0)
                            {
                                if (numCombo2 >= 5)
                                {
                                    SetReward(-0.1f);
                                }
                                else
                                {
                                    numCombo2++;
                                }

                                isPerformingCombo2 = true;

                                numPunches = 0;
                                numKicks = 0;
                                numJumps = 0;
                                numBlocks = 0;
                                numCombo1 = 0;

                            }
                        }
                    }
                }
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");

        //Reset Jump action
        continuousActions[1] = 0;
        //Jumping
        if (Input.GetKey(KeyCode.Space))
        {
            continuousActions[1] = 1;
        }

        //Reset Punch action
        continuousActions[2] = 0;
        //Punch
        if (Input.GetKey(KeyCode.G) && !isPunching)
        {
            continuousActions[2] = 1;
        }

        //Reset Kick action
        continuousActions[3] = 0;
        //Kick
        if (Input.GetKey(KeyCode.B) && !isKicking)
        {
            continuousActions[3] = 1;
        }       

        //Reset Block action
        continuousActions[4] = 0;
        //Block
        if(Input.GetKey(KeyCode.F) && !isBlocking)
        {
            continuousActions[4] = 1;
        }

        //Reset combo 1 action
        continuousActions[5] = 0;
        //Combo attack 1
        if(Input.GetKey(KeyCode.UpArrow))
        {
            continuousActions[5] = 1;
        }

        //Reset combo 2 action
        continuousActions[6] = 0;
        //Combo attack 2
        if (Input.GetKey(KeyCode.DownArrow))
        {
            continuousActions[6] = 1;
        }
    }

    //Check if the AI has dealt or recevied damage.
    private void CheckDamageStatus()
    {
        //Check if the AI has dealt basic damage to the oppenent.
        if (playerController.isDamageApplied)
        {
            SetReward(0.1f);
            playerController.isDamageApplied = false;
        }
        if (playerController.isComboDamageApplied)//Check for combo damage.
        {
            SetReward(0.2f);
            playerController.isComboDamageApplied = false;
        }

        //Check if the AI has recevied basic damage from the oppenent.
        if (playerController.isDamageRecevied)
        {
            SetReward(-0.1f);
            playerController.isDamageRecevied = false;
        }
        if (playerController.isComboDamageRecevied)//Check for combo damage.
        {
            SetReward(-0.2f);
            playerController.isComboDamageRecevied = false;
        }
    }

    //If the block action is chosen the AI will block for 1 second before moving back to idle state.
    IEnumerator BlockingCoroutine()
    {
        yield return new WaitForSeconds(1.0f);

        isBlocking = false;
        animActions.Block(isBlocking);
        playerController.isBlocking = isBlocking;
    }

    IEnumerator PunchCoroutine()
    {
        yield return new WaitForSeconds(0.3f);

        isPunching = false;
    }

    IEnumerator KickCoroutine()
    {
        yield return new WaitForSeconds(0.3f);

        isKicking = false;
    }
}
