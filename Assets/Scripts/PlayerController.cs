using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Some attributes
    public TileManager.TilePos pos;
    public bool canMove = true;
    public bool canPush = true;
    public bool jumpedOff = false;
    public bool pushedOff = false;

    [SerializeField] private float jumpTime = 0.5f;
    [SerializeField] private Sprite[] mySprites;
    private SpriteRenderer myRenderer;
    private Animator myAnimator;
    public TileManager.Level myLevel;
    public Transform childRendererTransform;
    private Ocean myOcean;

	private void Start()
	{
        myRenderer = this.GetComponentInChildren<SpriteRenderer>();
        myAnimator = this.GetComponent<Animator>();
        myOcean = GameObject.Find("Ocean").GetComponent<Ocean>();
        AdjustDepth();
	}

	// Update is called once per frame
	void Update()
	{
		if (this.myLevel.Manager.fadingOutLevel != null)
			return;
        if (canMove)
		{
            this.Move();
		}
        if (canPush)
		{
            this.PushTiles();
		}
        // I dont know what im doing
        if (canMove)
		{
            // Adjust height according to odean movement
            float x = transform.localPosition.x;
            float z = transform.localPosition.z;
            transform.localPosition = new Vector3(x, myOcean.GetOceanHeight(new Vector2(pos.X, pos.Y)), z);
        }
    }

    private void PushTiles()
	{
		var dir = this.GetInputDir(new[] { KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.LeftArrow });
		if (dir.X == 0 && dir.Y == 0)
			return;

        if (myLevel.CanShiftTiles(pos, dir))
            StartCoroutine(PushTiles(dir));
    }

    IEnumerator PushTiles(TileManager.TilePos pushDir)
	{
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
	    var dir = this.GetInputDir(new[] { KeyCode.W, KeyCode.D, KeyCode.S, KeyCode.A });
	    if (dir.X == 0 && dir.Y == 0)
		    return;

	    // Toggle sprite flipX
	    myRenderer.flipX = dir.X == -1 || dir.Y == -1;

	    var newPos = this.pos + dir;
	    if (TileClear(newPos))
		    StartCoroutine(MoveToTile(newPos));
    }

    IEnumerator MoveToTile(TileManager.TilePos newTile)
	{
        Vector3 startPos = this.transform.localPosition;
        Vector3 targetPos = newTile.ToTransformPosition();
        targetPos.z -= 2;
        float currentStepDelta = 0.0f;
        canMove = false;
        canPush = false;
        jumpedOff = false;

        // Play some animation
        myAnimator.speed = 1.0f / jumpTime;
        myAnimator.Play("JumpDown", -1,  0.0f);
        
		// Insert a sound or smth

		while (!jumpedOff)
		{
            yield return null;
		}
        

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

            
        // check win condition
        if (this.myLevel.Get(pos).HasFlag)
        {
	        this.myLevel.Manager.ProgressToNextLevel();
        }
	}

    private void AdjustDepth()
	{
        this.transform.localPosition = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y, -2.0f + pos.Y - pos.X);
	}

    private bool TileClear(TileManager.TilePos checkPos)
	{
        return myLevel.Get(checkPos) != null && !myLevel.Get(checkPos).HasRock;
	}
}
