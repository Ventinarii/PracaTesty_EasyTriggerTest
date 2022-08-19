using Assets.Scripts.gamescripts;
using System.Xml.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : Destructable
{
    Main main;
    Game game;
    Gfx  gfx;
    Snd  snd;

    Sprite[] sprites;
    public GameObject gameObject;

    Vector2 playerPosition;//local
    Vector2 VelocityInAir = new Vector2(0, 0);

    // Animation objects
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
    public StateEnum State
    {
        get
        {
            return m_State;
        }
        set
        {
            m_State = value;
            Animator.SetInteger("StateEnum", ((int)value));
        }
    }
    private StateEnum m_State;

    // animation times
    private float Timer = 0;
    public float TimeCrouch = 1f;
    public float TimeCrouchBack = .5f;
    public float TimeFire = .6f;
    public float TimeMeele = .8f;

    //controll
    private bool InAir = false;
    public const float MaxInAirJumpDelay = .5f; // allow jump in air this many seconds AFTER already in AIR.
    private float TimeInAir = 0; //allow for air jump SHORTLY after wandering off edge.
    private int direction = 1;

    // Game vars
    public const float pawnHeightHalf = 20;//aproximate hitbox
    public const float pawnWidthHalf = 5;

    public const float jumpStartVelocity = 170;//adjust to platorm height
    public const float jumpHorizontalVelocity = 60;

    private int Hp = 200;

    public Player (Main inMain) {

        main = inMain;
        game = main.game;
        gfx  = main.gfx;
        snd  = main.snd;

        sprites = gfx.GetLevelSprites("Players/Player1");

        playerPosition = new Vector2(370, 624);

        gameObject = gfx.MakeGameObject("Player", sprites[22], playerPosition.x, playerPosition.y, "Player");

        Animator = gameObject.AddComponent<Animator>();
        Animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Players/PlayerAnimationController");

        playerPosition = gameObject.transform.localPosition;
    }

    // Player logic here
    /* (O) we check following things in exact order: 
     * - ARE we in PROGRESS of state (animation)?
     * - ARE we in a state that require exit (duck) and WANT to leave it?
     * - do we fire (this includes meele)? if yes we CAN'T do ANYTHING else
     * - do we duck? if yes we CAN'T do ANYTHING else
     * - do we move?
     * - do we jump?
     * 
     * (1) combat:
     * CONSIDER: we fire with bulets
     * CONSIDER: bullets DO TAKE TIME TO FLY
     * CONSIDER: pawn AUTO switch to meele when in range.
     * 
     * (2) movement:
     * CONSIDER: there IS controll over pawn MID AIR.
     * CONSIDER: set jump to be high enough to reach platforms (no anims for wall jumps etc)
     * CONSIDER: we do NOT(?) permit air-jumps / double-jumps etc.
     * HOWEVER:  we DO ALLOW jump SHORTLY after walking OFF platform (if we ever implement lifts it will cause JITTER,
     *           UNLESS we use local point of reference - then it will create problems with projectiles -> USE STICKY SURFACES.
     * CONSIDER: pawn do NOT(?) accelerate.
     *  
     * (3) death:
     * CONSIDER: block input after death.
     * 
     * (4) platforms and collision:
     * CONSIDER: drop naive implementation of momement based on physics (velocity & impulse) in favor or handmade dynamics.
     * NOTE: since tjis is PROTOTYPE I added coliders to map (in asset) and use RAYTRACE for colision detection. I DO realize it's cost.
     * CONSIDER: all enviromental forces should come from force generators. //abandoned - too complex for demo.
     * 
     * (X) Notes and thoughts:
     * NOTE0: I initially didin't realize this game has setting to duck (i thought it is for someting else). 
     *        overall i had to study Huntdown to work out how it is supposed to work.
     *       
     * NOTE1: I still struggle to find use for duck since graphically it would make bullets hit head instead of torso? unless it is for meele? 
     *        but then it is too slow. or for astehics? for life regen? - since i do not know what to do with it I consider it astetics, for now.
     *        I would need to buy huntdown and play it to get feel of what is expected of me.
     */
    public void FrameEvent(int inMoveX, int inMoveY, bool inShoot) {
        //get updated local position
        playerPosition = gameObject.transform.localPosition;//if you do something to player it will be noted.

        var wallToLeft = false;
        var wallToRight = false;

        GOcollision(wallToLeft: out wallToLeft, wallToRight: out wallToRight);

        if (0 < Hp)
            GOinput(inMoveX: inMoveX, inMoveY: inMoveY, inShoot: inShoot, wallToLeft: wallToLeft, wallToRight: wallToRight);

        if(InAir)
            playerPosition += VelocityInAir * Time.deltaTime;

        gameObject.transform.localPosition = playerPosition;

        HitAnimClock();
    }

    public void GOcollision(out bool wallToLeft, out bool wallToRight) {
        //prep variables for raycast (find world coords for center of object
        var SumarryScale = gameObject.transform.parent.lossyScale;
        var start3 = gameObject.transform.position + Vector3.up * (pawnHeightHalf * SumarryScale.y);
        var start2 = new Vector2(start3.x, start3.y);

        //detect map boundaries
        //this sily solution is used as alternative to 4 hitboxes that would detect the same thing
        var RaycastDown = Physics2D.Raycast(start2, Vector2.down).distance;
        var RaycastUp = Physics2D.Raycast(start2, Vector2.up).distance;
        var RaycastLeft = Physics2D.Raycast(start2, Vector2.left).distance;
        var RaycastRight = Physics2D.Raycast(start2, Vector2.right).distance;

        //scale distance from COM to target to local scale
        RaycastDown /= SumarryScale.y;
        RaycastUp /= SumarryScale.y;
        RaycastLeft /= SumarryScale.x;
        RaycastRight /= SumarryScale.x;

        //handle collision with map & handle flying 
        if (RaycastDown != 0)
        {
            var distFromFeetToGround = RaycastDown - pawnHeightHalf;

            //do NOT sink in ground. (ground is sticky and allow you to sink only tiny bit)
            if (distFromFeetToGround < -1)
            {
                playerPosition.y -= distFromFeetToGround + 0.5f;
            }

            //check if we stand
            if (distFromFeetToGround <= 0)
            {
                InAir = false;
                VelocityInAir = Vector2.zero;
                TimeInAir = 0;
            }

            //handle flying & check if we fall <<==== ADD_HERE - add changes to how player behaves IN AIR HERE
            if (0 < distFromFeetToGround)
            {
                InAir = true;
                VelocityInAir += Physics.Gravity * Time.deltaTime;
                TimeInAir += Time.deltaTime;
            }
        }
        if (RaycastUp != 0)
        {
            var distFromHeadToCelling = RaycastUp - pawnHeightHalf;

            //do NOT dive into celling
            if (distFromHeadToCelling < 0 && 0 < VelocityInAir.y)
                VelocityInAir *= Vector2.right;

        }

        /* what happnes here:
         * are we TOO close to WALL?
         * -IF we do we STOP furhter movment
         * -ARE we OVERLAPING TOO much?
         * --move pawn BACK (without throwing it too far back)
         */
        wallToLeft = false;
        wallToRight = false;

        if (RaycastLeft != 0)
        {
            var distFromCOMToWall = RaycastLeft - pawnWidthHalf;//COM -> Center of mass

            if (distFromCOMToWall < 0)
            {
                distFromCOMToWall += 1;
                if (distFromCOMToWall < 0)
                    playerPosition.x -= distFromCOMToWall;

                if (VelocityInAir.x < 0)
                    VelocityInAir.x = 0;

                wallToLeft = true;
            }
        }
        if (RaycastRight != 0)
        {
            var distFromCOMToWall = RaycastRight - pawnWidthHalf;

            if (distFromCOMToWall < 0)
            {
                distFromCOMToWall += 1;
                if (distFromCOMToWall < 0)
                    playerPosition.x += distFromCOMToWall;

                if (0 < VelocityInAir.x)
                    VelocityInAir.x = 0;

                wallToRight = true;
            }
        }
    }

    public void GOinput(int inMoveX, int inMoveY, bool inShoot, bool wallToLeft, bool wallToRight) {
        if (0 <= Timer)
        {
            Timer -= Time.deltaTime;
        }
        else if (State == StateEnum.Crouch && inMoveY != 1)
        {
            State = StateEnum.CrouchBack;
            Timer += TimeCrouchBack;
        }
        else if (inShoot)
        {
            //normally you can use socet (assuming we have some kind of skeleton) coords instead of this.
            var startX = playerPosition.x + (pawnWidthHalf * direction);
            var startY = playerPosition.y + pawnHeightHalf;

            //before we fire bullet we try to punch the enemy
            var hit = main.game.destructables.Exists(x => x.IsHit(new Vector2(startX,startY), 60, 20));
            if (hit) {
                State = StateEnum.Meele;
                Timer += TimeMeele;
            }
            else 
            { 
                State = StateEnum.Fire;
                Timer += TimeFire;
                var bullet = new Bullet(main, ((int)startX), ((int)startY), direction, true);
                main.game.AddLevelObject(bullet);

                snd.PlayAudioClip("Gun");
            }
        }
        else if (inMoveY == 1)
        {
            State = StateEnum.Crouch;
            Timer += TimeCrouch;
        }
        else
        {
            //if we are in AIR we don't have controll <<==== ADD_HERE - add changes to how player behaves IN AIR HERE 
            if (inMoveX != 0)
            {
                if ((inMoveX < 0 && !wallToLeft) || (0 < inMoveX && !wallToRight))
                {
                    playerPosition.x += inMoveX;
                    State = StateEnum.Run;
                }
                else
                    State = StateEnum.Idle;

                if (direction != inMoveX)
                    SetDirection(inMoveX);
            }
            else
                State = StateEnum.Idle;

            //jump?
            if (inMoveY == -1 && TimeInAir <= MaxInAirJumpDelay)
            {
                InAir = true;
                VelocityInAir = Vector2.up * jumpStartVelocity;
                TimeInAir = MaxInAirJumpDelay + 1;
            }
        }
    }

    void SetDirection(int inDirection)
    {
        direction = inDirection;
        gfx.SetDirX(gameObject, direction);
    }

    //todo: add to <HERE>:
    //wait few seconds with screen going dark and some humoristic comment
    //roll credits / respawn / reload quicksave ... call UI wiget responsible for any of that 
    /// <summary>
    /// scaffold function
    /// if state NOT set to death it automatically sets it to death
    /// </summary>
    public void Kill()
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

        HitAnimStart();

        var newHp = GetHp() - Damage;

        if (newHp <= 0)
            if (relativeDirection < 0)
                State = StateEnum.DeathBack;
            else
                State = StateEnum.DeathFront;

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
            if (0 < HitClock)
            {
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