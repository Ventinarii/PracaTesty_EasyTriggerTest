using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.gamescripts;

public class Game : MonoBehaviour {

    Main main;
    int myRes;
    Gfx gfx;
    Snd snd;

    static string PLAY = "play";

    string gameStatus;

    int   camWidth;
    int   camHeight;
    float camX;
    float camY;

    public Player player;

    bool leftKey, rightKey, jumpKey, duckKey, shootKey;
    int  playerHorizontal, playerVertical;
    bool playerShoot;
    bool playerShootRelease = true;

    List<GeneralObject> gameObjects;
    public List<Destructable> destructables;

    int gameObjectLength;

    public void Init(Main inMain) {

        main  = inMain;
        gfx   = main.gfx;
        myRes = gfx.myRes;
        snd   = main.snd;

        camWidth  = gfx.screenWidth / myRes;
        camHeight = gfx.screenHeight / myRes;

        gameObjects = new List<GeneralObject>();
        destructables = new List<Destructable>();
        gameObjectLength = 0;

        player = new Player(main);

        var enemy = new Enemy(main, 530, 560);
        AddLevelObject(enemy);
        destructables.Add(enemy);

        enemy = new Enemy(main, 516, 624);
        AddLevelObject(enemy);
        destructables.Add(enemy);

        gameStatus  = PLAY;

        camX = 480 - camWidth/2;
        camY = 600 - camHeight/2;
        gfx.MoveLevel(camX, camY);
    }

    void Update() {
       
        if (gameStatus==PLAY) {

            GoKeys();

            GoPlayer();

            GoCam();

            GoObjects();

        } 

    }

    void GoPlayer() {
        player.FrameEvent(playerHorizontal, playerVertical, playerShoot);
    }

    private void GoKeys() {
       
        // ---------------------------------------------------------------
        // NORMAL KEYBOARD
		// ---------------------------------------------------------------

		if (Input.GetKeyDown(KeyCode.LeftArrow))  { leftKey   = true; }
        if (Input.GetKeyUp(KeyCode.LeftArrow))    { leftKey   = false; }
        if (Input.GetKeyDown(KeyCode.RightArrow)) { rightKey  = true; }
        if (Input.GetKeyUp(KeyCode.RightArrow))   { rightKey  = false; }
        if (Input.GetKeyDown(KeyCode.UpArrow))    { jumpKey   = true; }
        if (Input.GetKeyUp(KeyCode.UpArrow))      { jumpKey   = false; }
        if (Input.GetKeyDown(KeyCode.DownArrow))  { duckKey   = true; }
        if (Input.GetKeyUp(KeyCode.DownArrow))    { duckKey   = false; }
        if (Input.GetKeyDown(KeyCode.M))          { jumpKey   = true; }
        if (Input.GetKeyUp(KeyCode.M))            { jumpKey   = false; }
        if (Input.GetKeyDown(KeyCode.N))          { shootKey  = true; }
        if (Input.GetKeyUp(KeyCode.N))            { shootKey  = false; }

        playerHorizontal = 0;
        if (leftKey) { playerHorizontal-=1; }
        if (rightKey) { playerHorizontal+=1; }

        playerVertical = 0;
        if (jumpKey) { playerVertical-=1; }
        if (duckKey) { playerVertical+=1; }

        playerShoot = false;

        if (shootKey) {
            if (playerShootRelease) {
                playerShootRelease = false;
                playerShoot = true;
            }
            
        } else {
            if (!playerShootRelease) {
                playerShootRelease = true;
            }
        }
    }

    // camera
    public readonly Vector2 CamDragBorder = new Vector2(200, 100);
    public readonly float CamCatchupSpeed = 10f;
    void GoCam() {
        var playerReferencePoint = player.gameObject.transform.position - (Vector3.down * 100);
        var relativeCam = playerReferencePoint - main.cam.transform.position;

        if (relativeCam.x < -CamDragBorder.x)
            gfx.MoveLevelRelative(-CamCatchupSpeed * Time.deltaTime * (relativeCam.x + CamDragBorder.x), 0);
        if (CamDragBorder.x < relativeCam.x)
            gfx.MoveLevelRelative(CamCatchupSpeed * Time.deltaTime * (CamDragBorder.x - relativeCam.x), 0);

        if (relativeCam.y < -CamDragBorder.y)
            gfx.MoveLevelRelative(0, -CamCatchupSpeed * Time.deltaTime * (relativeCam.y + CamDragBorder.y));
        if (CamDragBorder.y < relativeCam.y)
            gfx.MoveLevelRelative(0, CamCatchupSpeed * Time.deltaTime * (CamDragBorder.y - relativeCam.y));
    }
    
    public void AddLevelObject(GeneralObject inObj) {

        gameObjects.Add(inObj);
        gameObjectLength++;

    }

    void GoObjects(bool inDoActive=true) {

        for (int i = 0; i<gameObjectLength; i++) {

            if (!gameObjects[i].FrameEvent()) {
                gameObjects.RemoveAt(i);
                i--;
                gameObjectLength--;
            }
        }

    }
}