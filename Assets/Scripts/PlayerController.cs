using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    // Some attributes
    public TileManager.TilePos pos;
    public bool canMove = true;
    public bool canPush = true;
    public bool jumpedOff = false;
    public bool pushedOff = false;
    public bool dirtyLock = true;

    public bool unmovable = false;
    [SerializeField] private float jumpTime = 0.5f;
    [SerializeField] private Sprite[] mySprites;
    private SpriteRenderer myRenderer;
    public Animator myAnimator;
    public TileManager.Level myLevel;
    public GameObject childRenderer;
    private Ocean myOcean;
    
    public AudioSource[] splashAudios;
    public AudioSource pushAudio;

	private void Start()
	{
        myRenderer = this.GetComponentInChildren<SpriteRenderer>();
        myAnimator = this.GetComponent<Animator>();
        myOcean = GameObject.Find("Ocean").GetComponent<Ocean>();
        AdjustDepth();
	}
	
	public void Init(TileManager.Level level, TileManager.TilePos playerPos)
	{
		this.myLevel = level;
		this.pos = playerPos;
		this.Start();
		this.Update();
        dirtyLock = true;
        unmovable = false;
    }

	void Update()
	{
        // Adjust height according to ocean movement
        childRenderer.transform.localPosition = new Vector3(0.0f, myOcean.GetOceanHeight(new Vector2(pos.X, pos.Y)), 0.0f);
        if (this.myLevel.Manager.fadingOutLevel != null || this.myLevel.Manager.levelStarting || this.myLevel.Manager.levelEnding || unmovable)
			return;
        if (canMove && !jumpedOff && dirtyLock)
		{
            this.Move();
		}
        if (canPush)
		{
            this.PushTiles();
		}
    }

    private void PushTiles()
	{
		var dir = this.GetInputDir(new[] { KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.LeftArrow });
		if (dir.X == 0 && dir.Y == 0)
		{
            return;
        }
        if (myLevel.CanShiftTiles(pos, dir))
		{
            StartCoroutine(PushTiles(dir));
        }
    }

    IEnumerator PushTiles(TileManager.TilePos pushDir)
	{
		this.pushAudio.Play();
        myRenderer.flipX = pushDir.X == -1 || pushDir.Y == -1;
        canPush = false;
        // Play Animation
		if (pushDir.IsUpDir())
		{
            myAnimator.Play("PushUp", -1, 0.0f);
		}
		else
		{
            myAnimator.Play("PushDown", -1, 0.0f);
        }
        // Play sound
        pushedOff = false;
		while (!pushedOff)
		{
            yield return null;
		}
        myLevel.ShiftTiles(pos, pushDir);
	}

    private TileManager.TilePos GetInputDir(KeyCode[] keys)
    {
	    // WD -> up
	    // DS -> right
	    // ...
	    // e.g. var keys = new[] { KeyCode.W, KeyCode.D, KeyCode.S, KeyCode.A };
	    var dirs = new[] { new TileManager.TilePos(0, 1), new TileManager.TilePos(1, 0), new TileManager.TilePos(0, -1), new TileManager.TilePos(-1, 0) };
	    
	    for (var pos = 0; pos < keys.Length; pos++)
	    {
		    var nextPos = (pos + 1) % keys.Length;
		    var oppositePos = (pos + 2) % keys.Length;
		    var nextOppositePos = (pos + 3) % keys.Length;
		    var down = Input.GetKey(keys[pos]);
		    var nextDown = Input.GetKey(keys[nextPos]);
		    var othersDown = Input.GetKey(keys[oppositePos]) || Input.GetKey(keys[nextOppositePos]);
		    if ((down && nextDown) || (down && this.myLevel.Get(this.pos + dirs[nextOppositePos]) == null && !othersDown) || (nextDown && this.myLevel.Get(this.pos + dirs[nextPos]) == null && !othersDown))
		    {
			    return dirs[pos];
            }
        }

	    return new TileManager.TilePos(0, 0);
    }

    private void Move()
    {
        dirtyLock = false;
	    var dir = this.GetInputDir(new[] { KeyCode.W, KeyCode.D, KeyCode.S, KeyCode.A });
        if (dir.X == 0 && dir.Y == 0)
		{
            dirtyLock = true;
            return;
        }

        // Toggle sprite flipX
        myRenderer.flipX = dir.X == -1 || dir.Y == -1;

	    var newPos = this.pos + dir;
	    if (TileClear(newPos))
		{
            StartCoroutine(MoveToTile(newPos));
            return;
		}
        dirtyLock = true;
    }

    IEnumerator MoveToTile(TileManager.TilePos newTile)
	{
        canMove = false;
        canPush = false;
        jumpedOff = false;
        Vector3 startPos = this.transform.localPosition;
        Vector3 targetPos = newTile.ToTransformPosition();
        targetPos.z -= 0.95f;
        float currentStepDelta = 0.0f;

        // Play some animation
        myAnimator.speed = 1.0f / jumpTime;
        myAnimator.Play("JumpDown", -1,  0.0f);
        
		// Insert a sound or smth

		while (!jumpedOff)
		{
            yield return null;
		}

        myOcean.MakeWave(new Vector2(pos.X, pos.Y), 0.8f, 0.1f, 0.5f);

        while (currentStepDelta < 1.0f)
		{
            transform.localPosition = startPos + currentStepDelta * (targetPos - startPos);
            currentStepDelta += Time.deltaTime / jumpTime;
            yield return null;
		}
        // Set the target position (safety)
        transform.localPosition = targetPos;

        // Player arrived at the new tile
        pos = newTile;

		while (myAnimator.GetCurrentAnimatorStateInfo(-1).IsName("JumpDown"))
		{
            yield return null;
		}
        myAnimator.speed = 1.0f;
        jumpedOff = false;
        // visual water fun
        myOcean.MakeWave(new Vector2(newTile.X, newTile.Y), 1.0f, 0.2f, 0.5f);
        myLevel.Get(newTile).Comp.ShootWaterParticles();
        var splashNum = (pos.X + pos.Y + 1000000) % this.splashAudios.Length;  // c# mod is shit.
        this.splashAudios[splashNum].Play();
        
        // check win condition
        if (this.myLevel.Get(pos).HasFlag)
        {
	        this.myLevel.Manager.ProgressToNextLevel();
        }
        dirtyLock = true;
	}

    private void AdjustDepth()
	{
        this.transform.localPosition = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y, -2.0f + pos.Y - pos.X);
	}

    private bool TileClear(TileManager.TilePos checkPos)
	{
        return myLevel.Get(checkPos) != null && !myLevel.Get(checkPos).HasRock;
	}

    public void ResetPosition()
    {
	    var newPos = this.pos.ToTransformPosition();
	    transform.localPosition = new Vector3(newPos.x, newPos.y, transform.localPosition.z);
    }
}
