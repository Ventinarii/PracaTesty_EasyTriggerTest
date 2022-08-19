using UnityEngine;

public class GeneralObject
{
    public float x;
    public float y;
    public bool isOK = true;

    protected Main main;
	protected Game game;
	protected Gfx gfx;
    protected Snd snd;
    protected Sprite[] sprites;
    protected int direction;

    protected void SetGeneralVars(Main inMain, int inX, int inY){
 
		main         = inMain;
		game         = main.game;
		gfx          = main.gfx;
        snd          = main.snd;
		x            = inX;
		y            = inY;

    }

	public virtual bool FrameEvent(){ return false; }
    public virtual void Kill() {}

	
}
