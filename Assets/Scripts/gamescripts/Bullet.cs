using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : GeneralObject
{
    GameObject gameObject;
    bool Firendly;
    float LifeTime;
    float Velocity;
    int Damage;

    public Bullet(Main inMain, int inX, int inY, int inDirection, bool inFirendly, float lifeTime = 2, float inVelocity = 200, int inDamage = 49)
    {
        main = inMain;
        SetGeneralVars(inMain, inX, inY);
        direction = inDirection;
        Firendly = inFirendly;
        LifeTime = lifeTime;
        Velocity = inVelocity;
        Damage = inDamage;

        sprites = gfx.GetLevelSprites("Bullet");

        gameObject = gfx.MakeGameObject("Bullet", sprites[0], x, -y);
        gameObject.transform.localScale = new Vector3(100, 100, 1);

        gfx.SetDirX(gameObject, direction);
        
        //exmaple colors. can also be changed to be passed as arg of function or even change over flight
        if(Firendly)
            gfx.SetSpriteTint(gameObject, Color.yellow);
        else
            gfx.SetSpriteTint(gameObject, Color.red);
    }

    public override bool FrameEvent()
    {
        //move bullet
        var newPos = gameObject.transform.localPosition;
        newPos += Vector3.right * direction * Velocity * Time.deltaTime;
        x = newPos.x;//i suggest switching to vectors Altogether 
        y = newPos.y;

        UpdatePos();

        Vector2 bulletPosition = gameObject.transform.localPosition;

        if (Firendly)
        {
            var hit = main.game.destructables.Exists(x => x.IsHit(bulletPosition, Damage));
            if (hit)
                LifeTime = -1;
        }
        else 
        {
            var hit = main.game.player.IsHit(bulletPosition, Damage);
            if (hit)
                LifeTime = -1;
        }

        LifeTime -= Time.deltaTime;
        if (LifeTime <= 0) {
            Kill();
        }
        return !(LifeTime <= 0);
    }

    void UpdatePos()
    {
        gfx.SetPos(gameObject, x, y);
    }

    public override void Kill()
    {
        GameObject.Destroy(gameObject);
    }
}
