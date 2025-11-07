using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [HideInInspector] public bool isDead = false;
    [HideInInspector] public bool isBlocking = false;
    //true means the sprite has flipped 180 degrees to face opponent.
    [HideInInspector] public bool isFlipped = false;    
    [HideInInspector] public bool isKnockdown = false;
    [HideInInspector] public bool isComboCooldown = false;

    //true means the player has dealt damage from its last move. This boolean is used by the Ml Agent.
    [HideInInspector] public bool isDamageApplied = false;
    //true means the player has dealt combo damage from its last move. This boolean is used by the Ml Agent.
    [HideInInspector] public bool isComboDamageApplied = false;
    //true means the player has recevied damage from the opponent. This boolean is used by the Ml Agent.
    [HideInInspector] public bool isDamageRecevied = false;
    //true means the player has recevied combo damage from the opponent. This boolean is used by the Ml Agent.
    [HideInInspector] public bool isComboDamageRecevied = false;
    //true means the player will face the Ml Agent.
    [System.NonSerialized] public bool isMLAgentPlaying = false;

    //Used by the game manager to tell the player when the game finished.
    [HideInInspector] public bool isGameFinished = false;
    [HideInInspector] public bool isJumping;

    //The player's current velocity.
    [HideInInspector] public float jumpVelocity;

    [HideInInspector] public int currentHealth;
    //used to count how many punches are thrown.
    [HideInInspector] public int noOfPunches = 0;
    //used to count how many kicks are made.
    [HideInInspector] public int noOfKicks = 0;

    [Header("General")]
    //false will disable player controls
    public bool isPlayerController;

    public float playerSpeed;
    public float jumpSpeed;
    //how much time the player has to perform a combo
    public float maxComboDelay;
    //the radius of the collision sphere that detects the oppoenents hit box
    public float attackRange;
    //how long before another combo can be used
    public float comboCooldownTimer;

    public int maxHealth;
    //punch and kick damage
    public int attackDamage;

    //the area where the fist of the character is when the punch animation is played 
    public Transform punchPoint;
    //the area where the foot of the character is when the kick animation is played 
    public Transform kickPoint;
    //used to detect the opponent when drawing the collision sphere
    public LayerMask enemyLayers;
    public HealthBar healthbar;
    public HealthBar comboCooldown;
    public BoxCollider2D boxCollider;
    public Transform ground;
    public string Opponent;

    //did the player use combo attack 1
    private bool isCombo1Att = false;
    //did the player use combo attack 2
    private bool isCombo2Att = false;
    private bool isRoundFinished = false;

    private float move;
    //the last time a punch or kick was thrown
    private float lastAttackedTime = 0;

    private Vector3 startPos;
    private Transform target;
    private AnimationActions animActions;
    private PlayerManager playerManager;
    private Rigidbody2D rb;
    private CSVWriter csvWriter;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animActions = GetComponent<AnimationActions>();
        playerManager = GetComponent<PlayerManager>();
        csvWriter = GetComponent<CSVWriter>();

        target = GameObject.FindWithTag(Opponent).transform;

        currentHealth = maxHealth;
        healthbar.SetMaxValue(maxHealth);
        comboCooldown.SetMaxValue(comboCooldownTimer);

        startPos = transform.position;

        csvWriter.Round++;

        isMLAgentPlaying = ButtonPress.isMLAgent;
    }

    // Update is called once per frame
    void Update()
    {        
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("MenuScene");
        }

        //Ignore collision with other player.
        Physics2D.IgnoreCollision(boxCollider, GameObject.FindWithTag(Opponent).GetComponent<BoxCollider2D>());

        //If the player glitches through the wall and falls off, then reset its position
        if(ground.transform.position.y > transform.position.y)
        {
            transform.position = startPos;
        }

        //always face oppenent
        if (Mathf.Sign(target.position.x - transform.position.x) <= -1.0f)
        {
            transform.eulerAngles = new Vector3(0, 180, 0);
            isFlipped = true;
        }
        else if (Mathf.Sign(target.position.x - transform.position.x) >= 1.0f)
        {
            transform.eulerAngles = new Vector3(0, 0, 0);
            isFlipped = false;
        }

        if (!isGameFinished)
        {
            //only move while not KO'd or using a combo
            if (currentHealth > 0 && !isCombo1Att && !isCombo2Att)
            {
                //Cooldown timer when the player uses a combo
                if (isComboCooldown)
                {
                    Cooldown();

                    if (comboCooldownTimer <= 0.0f)
                    {
                        isComboCooldown = false;

                        comboCooldownTimer = 3.0f;

                        comboCooldown.SetCurrentValue(comboCooldownTimer);
                    }
                }
                if (isPlayerController)
                {
                    //performs basic attacks, combos, blocks, movement and jumping
                    PlayerActions();
                }                
            }
        }

        //Resets if the input for the combo is not done quick enough
        if (Time.time - lastAttackedTime > maxComboDelay)
        {
            noOfPunches = 0;
            noOfKicks = 0;
        }

        //KO'd
        if (currentHealth <= 0 && !isDead && !isRoundFinished)
        {
            animActions.KO();
            isDead = true;
            if (csvWriter.Round == 1)
            {
                csvWriter.WriteHeadings();
            }
            isRoundFinished = true;
            csvWriter.Losses++;
            StartCoroutine(KOCoroutine());
        }

        //opponent is KO'd
        if(!playerManager.canPlayerTakeComboDamage && !isRoundFinished)
        {
            isRoundFinished = true;
            if (csvWriter.Round == 1)
            {
                csvWriter.WriteHeadings();
            }
            csvWriter.Wins++;
            StartCoroutine(KOCoroutine());
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //collided with the ground therefore not jumping
        if (collision.gameObject.CompareTag("Ground"))
        {
            isJumping = false;
            animActions.Jump(isJumping);
        }
    }

    //Player Controlled Actions
    void PlayerActions()
    {
        move = Input.GetAxis("Horizontal");

        //The player should only be able to move if it is not blocking or knocked down
        if (!isBlocking && !isKnockdown)
        {
            rb.velocity = new Vector2(move * playerSpeed, rb.velocity.y);
            animActions.Move(move);
        }

        //jumping
        if (Input.GetButton("Jump") && !isJumping && !isKnockdown && !isBlocking)
        {
            rb.AddForce(new Vector2(rb.velocity.x, jumpSpeed));
            isJumping = true;
            animActions.Jump(isJumping);
            csvWriter.Jumps++;
        }

        //Punch
        if (Input.GetKeyDown(KeyCode.G))
        {
            Punch(false);
        }
        //LowKick
        if (Input.GetKeyDown(KeyCode.B))
        {
            Kick(false);
        }
        //Block
        if (Input.GetKeyDown(KeyCode.F) && !isBlocking && !isJumping && !isKnockdown)
        {
            isBlocking = true;
            animActions.Block(isBlocking);
        }
        if (Input.GetKeyUp(KeyCode.F) && isBlocking)
        {
            isBlocking = false;
            animActions.Block(isBlocking);
            csvWriter.Blocks++;
        }
    }

    //This function has logic to play the punch and the combo 1 attack animations while applying the appropriate damage
    public void Punch(bool isAICombo)
    {
        if (!isJumping && !isKnockdown)
        {
            lastAttackedTime = Time.time;
            noOfPunches++;

            //detect enemies in range of an attack
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(punchPoint.position, attackRange, enemyLayers);

            if (noOfPunches == 1 || noOfPunches == 2) //basic punch
            {
                if (!isAICombo)
                {
                    animActions.Punch();
                    csvWriter.Punches++;

                    //Damage opponent
                    foreach (Collider2D enemy in hitEnemies)
                    {
                        //check the game manager if the other player can take damage right now
                        if (playerManager.canPlayerTakeBasicDamage)
                        {
                            enemy.SendMessage("TakeDamage", attackDamage);

                            if (isMLAgentPlaying)
                            {
                                //tell the ml agent script that is has applied basic damage.
                                isDamageApplied = true;
                            }
                        }
                    }
                }
            }
            if (noOfPunches == 3 || isAICombo) //combo attack
            {
                //to use this combo attack the up arrow will need to be held down before the third punch.
                //Unless the AIs is using the combo attack, therefore it can be called directly
                if (Input.GetKey(KeyCode.UpArrow) || isAICombo)
                {
                    if (!isComboCooldown)
                    {
                        animActions.ComboAttack1();
                        csvWriter.Combo1++;

                        isCombo1Att = true;
                        isComboCooldown = true;

                        //Damage opponent
                        foreach (Collider2D enemy in hitEnemies)
                        {
                            //check the game manager if the other player can take damage right now
                            if (playerManager.canPlayerTakeComboDamage)
                            {
                                enemy.SendMessage("TakeDamage", attackDamage * 2);

                                if (isMLAgentPlaying)
                                {
                                    //tell the ml agent script that it has applied combo damage.
                                    isComboDamageApplied = true;
                                }
                            }
                        }

                        //add delay for animation to finish
                        StartCoroutine(Combo1AttCoroutine());
                    }
                }
            }

            //If a combo is not performed when the punch count is 3 then reset punch count so the character can continue to throw punches.
            if (noOfPunches >= 4)
            {
                noOfPunches = 0;
            }
        }
    }

    //This function has logic to play the kick and the combo 2 attack animations while applying the appropriate damage
    public void Kick(bool isAICombo)
    {
        if (!isJumping && !isKnockdown)
        {
            lastAttackedTime = Time.time;
            noOfKicks++;

            if (noOfKicks == 1 || noOfKicks == 2)//basic kick
            {
                if (!isAICombo)
                {
                    animActions.Kick();
                    csvWriter.Kicks++;

                    //detect enemies in range of an attack
                    Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(kickPoint.position, attackRange, enemyLayers);

                    //Damage opponent
                    foreach (Collider2D enemy in hitEnemies)
                    {
                        //check the game manager if the other player can take damage right now
                        if (playerManager.canPlayerTakeBasicDamage)
                        {
                            enemy.SendMessage("TakeDamage", attackDamage);

                            if (isMLAgentPlaying)
                            {
                                //tell the ml agent script that it has applied basic damage.
                                isDamageApplied = true;
                            }
                        }
                    }
                }
            }
            if (noOfKicks == 3 || isAICombo)//combo attack
            {
                //to use this combo attack the up arrow will need to be held down before the third kick.
                //Unless the AIs is using the combo attack, therefore it can be called directly
                if (Input.GetKey(KeyCode.UpArrow) || isAICombo)
                {
                    if (!isComboCooldown)
                    {
                        animActions.ComboAttack2();
                        csvWriter.Combo2++;

                        isCombo2Att = true;
                        isComboCooldown = true;

                        //detect enemies in range of attack
                        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(punchPoint.position, attackRange, enemyLayers);

                        //Damage opponent
                        foreach (Collider2D enemy in hitEnemies)
                        {
                            //check the game manager if the other player can take damage right now
                            if (playerManager.canPlayerTakeComboDamage)
                            {
                                enemy.SendMessage("TakeDamage", attackDamage * 2);

                                if (isMLAgentPlaying)
                                {
                                    //tell the ml agent script that it has applied combo damage.
                                    isComboDamageApplied = true;
                                }
                            }
                        }

                        // add delay for animation to finish                    
                        StartCoroutine(Combo2AttCoroutine());
                    }
                }
            }

            //If a combo is not performed when the kick count is 3 then reset kick count so the character can continue to kick.
            if (noOfKicks >= 4)
            {
                noOfKicks = 0;
            }
        }
    }

    //A Coroutine that plays out the combo 1 attack animation
    IEnumerator Combo1AttCoroutine()
    {
        yield return new WaitForSeconds(0.6f);

        isCombo1Att = false;
    }

    //A Coroutine that plays out the combo 2 attack animation
    IEnumerator Combo2AttCoroutine()
    {
        yield return new WaitForSeconds(0.9f);

        isCombo2Att = false;
    }

    //A Coroutine that plays out the knockdown animation
    IEnumerator KnockdownCoroutine()
    {
        yield return new WaitForSeconds(1.3f);

        isKnockdown = false;
    }

    //Reset player's position and health
    IEnumerator KOCoroutine()
    {
        yield return new WaitForSeconds(1.0f);
        isDead = false;
        isRoundFinished = false;
        
        csvWriter.Health = currentHealth;

        //Write end of round values to file.
        csvWriter.WriteCSV();
        csvWriter.Round++;

        //reset variables for the next round.
        csvWriter.ResetVariables();

        //Reset position and animation
        transform.position = startPos;
        animActions.Idle();

        //Reset health and cooldown values
        currentHealth = maxHealth;
        healthbar.SetMaxValue(maxHealth);
        comboCooldown.SetMaxValue(comboCooldownTimer);

        //after the data from the final round is written then exit to the menu screen.
        if(isGameFinished)
        {
            StartCoroutine(ExitToMenuCoroutine());
        }
    }

    //Loads menu scene.
    IEnumerator ExitToMenuCoroutine()
    {
        yield return new WaitForSeconds(1.0f);

        SceneManager.LoadScene("MenuScene");
    }

    //updates health
    public void TakeDamage(int damage)
    {
        //if opponent uses a combo, it breaks the players block
        //Also, the opponent will no longer be able to damage the player if it is knocked down
        if (damage == (attackDamage * 2) && !isKnockdown)//combo is used therefore get knockdown
        {
            currentHealth -= damage;

            healthbar.SetCurrentValue(currentHealth);

            //play knockdown animation
            animActions.Knockdown();

            isKnockdown = true;

            if (isMLAgentPlaying)
            {
                //tell the ml agent script that it has recevied combo damage.
                isDamageRecevied = true;
            }

            csvWriter.Knockdown++;

            //add delay for animation to finish
            StartCoroutine(KnockdownCoroutine());
        }
        else if (!isKnockdown)//no combo was used therefore it was a basic attack
        {
            currentHealth -= damage;

            healthbar.SetCurrentValue(currentHealth);

            if (isMLAgentPlaying)
            {
                //tell the ml agent script that it has recevied basic damage.
                isComboDamageRecevied = true;
            }

            //play hurt animation
            animActions.Hit();
            csvWriter.Hit++;
        }
    }

    //decrements combo cooldown timer
    private void Cooldown()
    {
        comboCooldownTimer -= 1.0f * Time.deltaTime;

        comboCooldown.SetCurrentValue(comboCooldownTimer);
    }

    //visually show the radius of the punch and kick position in the editor
    private void OnDrawGizmosSelected()
    {
        if (punchPoint == null)
        {
            return;
        }
        if (kickPoint == null)
        {
            return;
        }
        Gizmos.DrawWireSphere(punchPoint.position, attackRange);
        Gizmos.DrawWireSphere(kickPoint.position, attackRange);
    }
}
