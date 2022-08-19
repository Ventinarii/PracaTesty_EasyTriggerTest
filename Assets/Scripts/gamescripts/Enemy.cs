using Assets.Scripts.gamescripts;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class Enemy : GeneralObject, Destructable
{
    GameObject gameObject;

    // Animation Vars
    Animator Animator;
    public enum StateEnum
    {
        Idle = 0,
        DeathBack = 1,
        DeathFront = 2,
        Run = 3,
        Crouch = 4,
        CrouchBack = 5,
        Fire = 6,
        Meele = 7
    }
    public StateEnum State { 
        get 
        { 
            return m_State;  
        } 
        set 
        { 
            m_State = value;
            Animator.SetInteger("StateEnum", ((int)value));
        } }
    private StateEnum m_State;

    private float Timer = 0;
    // AI vars
    public int minX { get; set; }
    public int maxX { get; set; }
    public List<StateEnum> DefaultBehavior = new List<StateEnum>();

    public float VisionRange = 400;
    public float FireRange = 300;

    public bool Combat { get; set; }
    private const float combatTimeStart = 10;
    private float combatTime = 0;
    private const float frenzyTimeStart = 10;
    private float frenzyTime = 0;

    private float frenzyBoost = 2;//MUST be greater than 1. 

    //how long AI spends in State
    public float TimeIdle = 10;
    public float TimeCrouch = 5;
    public float TimeCrouchBack = .6f;
    public float TimeFire = .4f;
    public float TimeMeele = 1f;

    // Game vars
    public const float pawnHeightHalf = 20;//aproximate hitbox
    public const float pawnWidthHalf = 5;
    private int Hp = 100;

    public Enemy(Main inMain, int inX, int inY) {

        main = inMain;
        SetGeneralVars(inMain, inX, inY);

        sprites = gfx.GetLevelSprites("Enemies/Enemy3_2");

        gameObject = gfx.MakeGameObject("Enemy", sprites[22], x, y);

        Animator = gameObject.AddComponent<Animator>();
        Animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Enemies/EnemyAnimationController");

        //used to detemin boudaries - there is no point to make BT for this simple demo
        minX = 480;
        maxX = 600;
        //default behavior - seroius implementation would use dedicated instucion set (enum) and state machine.
        DefaultBehavior.Add(StateEnum.Run);
        if (Random.value >= 0.5)
            DefaultBehavior.Add(StateEnum.Idle);
        else {
            DefaultBehavior.Add(StateEnum.Crouch);
            DefaultBehavior.Add(StateEnum.CrouchBack);
        }
        DefaultBehavior.Add(StateEnum.Run);
        DefaultBehavior.Add(StateEnum.Idle);
        Combat = false;

        SetDirection(-1);
    }

    // enemy logic here
    /* IF we are DEAD => do nothing
     * IF NOT in combat => patrol
     * IF player on the same height AND in front of us AND in range => go IN COMBAT for 10(?)seconds
     * IF we are HIT => GO IN FRENZY for 10(?)seconds AND go IN COMBAT for 20(?)seconds
     * WHILE we are in FRENZY => THIS enemy time scale *2  (this is called from hit
     * 
     * IF in COMBAT AND see (on the same level, in front of us, in range) player:
     * => IF in range of fire => FIRE (or meele - it goes on auto)
     * -- ELSE => run to player
     * 
     * IF in COMBAT AND NOT see player:
     * => run all the way in direction player was seen last time
     * => look back and forth for player (do NOT crouch)
     * 
     * IF effect timer reaches 0 we wear off effects. COMBAT -> PATROL; FRENZY -> NOT FRENZY. wearing off either does NOT affect the other.
     * 
     * should be enough for prototype.
     */
    public override bool FrameEvent() {
        //called for animations
        Timer -= Time.deltaTime;

        if (0 < frenzyTime)
            Timer -= Time.deltaTime * (frenzyBoost - 1);

        var relativePlayerLocation = (Vector2)(main.game.player.gameObject.transform.localPosition - gameObject.transform.localPosition);

        //IF we are DEAD => do nothing
        if (0 < Hp) 
        {
            //IF NOT in combat => patrol
            if (!Combat)
            {
                switch (State)
                {
                    case StateEnum.Run:
                        {
                            Timer = 1;
                            if ((direction == -1 && x < minX) || (direction == 1 && x > maxX))
                            {
                                Timer = 0;
                            }
                            else
                            {
                                if (0 < frenzyTime)
                                    x += .4f * direction * frenzyBoost;
                                else
                                    x += .4f * direction;
                            }
                        }
                        break;
                    default: break;
                }
                if (Timer <= 0)
                    NextBehavior();

            }

            var seePlayer = -pawnHeightHalf <= relativePlayerLocation.y && relativePlayerLocation.y <= pawnHeightHalf &&//height
                            0 <= relativePlayerLocation.x * direction &&//direction
                            Mathf.Abs(relativePlayerLocation.x) <= VisionRange &&//range
                            0 < main.game.player.GetHp();// only see player that ARE alive

            //IF player on the same height AND in front of us AND in range => go IN COMBAT for 10(?)seconds
            if (seePlayer) {
                Combat = true;
                combatTime = combatTimeStart;
            }
            //=> IF in range of fire => FIRE(or meele - it goes on auto)
            if (seePlayer && Mathf.Abs(relativePlayerLocation.x) <= FireRange)
            {
                if (State == StateEnum.Run)
                    Timer = 0;

                if (Timer <= 0) { 
                    var startX = gameObject.transform.localPosition.x + (pawnWidthHalf * direction);
                    var startY = gameObject.transform.localPosition.y + pawnHeightHalf;
                    var hit = main.game.player.IsHit(new Vector2(startX, startY), 60, 20);
                    if (hit)
                    {
                        State = StateEnum.Meele;
                        Timer = TimeMeele;
                    }
                    else
                    {
                        State = StateEnum.Fire;
                        Timer = TimeFire;
                        var bullet = new Bullet(main, ((int)startX), ((int)startY), direction, false);
                        main.game.AddLevelObject(bullet);

                        snd.PlayAudioClip("Gun");
                    }
                }
            }
            //=> run all the way in direction player was seen last time
            //=> look back and forth for player(do NOT crouch)
            else if (Combat) 
            {
                lookForPlayer(relativePlayerLocation);
            }

            //IF effect timer reaches 0 we wear off effects. COMBAT -> PATROL; FRENZY -> NOT FRENZY. wearing off either does NOT affect the other.
            if (0 < combatTime)
            {
                combatTime -= Time.deltaTime;
                if (combatTime <= 0)
                    Combat = false;
            }

            if (0 < frenzyTime) {
                frenzyTime -= Time.deltaTime;
                if (frenzyTime <= 0)
                    Frenzy(false);
            }
        }

        UpdatePos();

        HitAnimClock();

        return isOK;
    }

    //scaffold - for now it:
    // - changes animator speed 
    // - activates combat
    void Frenzy(bool StartStop) {
        if (StartStop) {
            frenzyTime = frenzyTimeStart;

            Animator.speed = frenzyBoost;

            Combat = true;
            combatTime = combatTimeStart;
        }
        else {
            Animator.speed = 1;
        }
    }

    //=> run all the way in direction player was seen last time
    //=> look back and forth for player(do NOT crouch)
    void lookForPlayer(Vector2 relativePlayerLocation) {
        //turn in direction where player is (track for X sec afeter lossing target and start running torward it
        if ((combatTimeStart - combatTime) <= .5f)
        {
            var relativeDirection = relativePlayerLocation.x * direction;
            if (relativeDirection < 0) {
                direction *= -1;
                SetDirection(direction);
            }
            State = StateEnum.Run;
        }

        switch (State)
        {
            case StateEnum.Run:
                {
                    Timer = 1;
                    if ((direction == -1 && x < minX) || (direction == 1 && x > maxX))
                        Timer = 0;
                    else
                    {
                        if (0 < frenzyTime)
                            x += .4f * direction * frenzyBoost;
                        else
                            x += .4f * direction;
                    }

                    if (Timer <= 0) {
                        State = StateEnum.Idle;
                        Timer = 1;
                    }
                }
                break;
            case StateEnum.Idle: 
                {
                    if (Timer <= 0)
                    {
                        direction *= -1; 
                        SetDirection(direction);
                        Timer = 1 * (.5f + Random.value); ;
                    }
                }
                break;
            default: break;
        }
    }

    void NextBehavior() {
        State = DefaultBehavior[0];
        DefaultBehavior.Add(State);
        DefaultBehavior.RemoveAt(0);

        //update vars if needed
        switch (State)
        {
            case StateEnum.Idle: 
                {
                    Timer = TimeIdle * (.5f+Random.value);
                }
                break;
            case StateEnum.Crouch:
                {
                    Timer = TimeCrouch * (.5f + Random.value);
                }
                break;
            case StateEnum.CrouchBack:
                {
                    Timer = TimeCrouchBack * (.5f + Random.value);
                }
                break;
            case StateEnum.Run:
                {
                    direction *= -1;
                    SetDirection(direction);
                }
                break;
            default: break;
        }
    }

    void UpdatePos() {
        gfx.SetPos(gameObject, x, y);
    }

    void SetDirection(int inDirection) {
        direction = inDirection;
        gfx.SetDirX(gameObject, direction);
    }
    
    /// <summary>
    /// scaffold function
    /// if state NOT set to death it automatically sets it to death
    /// </summary>
    override public void Kill()
    {
        if (State != StateEnum.DeathFront && State != StateEnum.DeathBack)
        {
            State = StateEnum.DeathBack;
            //HERE <==
        }
    }

    //Destructable

    public int GetHp()
    {
        return Hp;
    }
    /// <summary>
    /// this function automatically checks if pawn is dead. death CAN -=NOT=- be reversed
    /// if pawn dies it calls Kill
    /// </summary>
    public void SetHp(int value)
    {
        Hp = value;

        if (Hp <= 0)
            Kill();
    }

    /// <summary>
    /// check if bullet hit this object and apply damage / death animation if needed.
    /// </summary>
    /// <param name="location">location of bullet in LEVEL RELATIVE coords.</param>
    /// <param name="Damage">damage of bullet</param>
    /// /// <param name="punchSize">optional - if we punch how far do we reach?</param>
    /// <returns>true if bullet hit THIS Destructable</returns>
    public bool IsHit(Vector2 location, int Damage, float punchSize = 0)
    {
        var relativeLocation = location - ((Vector2)gameObject.transform.localPosition + Vector2.up * pawnHeightHalf);
        var relativeDirection = relativeLocation.x * direction;

        if (relativeLocation.x < 0)
            relativeLocation.x *= -1;
        if (relativeLocation.y < 0)
            relativeLocation.y *= -1;

        relativeLocation.x -= punchSize;

        var hit = (relativeLocation.x < pawnWidthHalf) && (relativeLocation.y < pawnHeightHalf);

        if (!hit)
            return false;

        Frenzy(true);
        HitAnimStart();

        var newHp = GetHp() - Damage;

        if (newHp <= 0) { 
            if (relativeDirection < 0)
                State = StateEnum.DeathFront;
            else
                State = StateEnum.DeathBack;

            main.game.destructables.Remove(this);
        }
        SetHp(newHp);

        return true;
    }


    //hit animation

    /* this code is responsibe for visual feedback that object got hit and took damage
     * it would be best to expand it with sound
     * ALSO it should run a a seperate timer and not from frame event since it technically should be considered animation.
     * but for prototype it is enough
     */

    /* DOES NOT WORK. for some reason unity has prpblem with it and wothout use of material (and even ten it is a problem
     * the only THEORETICAL solution is to use SHADER. //abandoned no time for implementation
     */
    private float HitClock;

    private void HitAnimClock()
    {
        if (0 < HitClock)
        {
            HitClock -= Time.deltaTime;
            if (0 < HitClock) {
                //gameObject.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
    }

    private void HitAnimStart()
    {
        HitClock = 1;
        //gameObject.GetComponent<SpriteRenderer>().color = Color.red;
    }
}