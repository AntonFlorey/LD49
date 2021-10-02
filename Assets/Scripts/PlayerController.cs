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
    [SerializeField] private float jumpTime = 0.5f;
    [SerializeField] private Sprite[] mySprites;
    private SpriteRenderer myRenderer;
    public TileManager.Level myLevel;

	private void Start()
	{
        myRenderer = this.GetComponent<SpriteRenderer>();
        AdjustDepth();
	}

	// Update is called once per frame
	void Update()
    {
        if (canMove)
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
			return;

        if (myLevel.CanShiftTiles(pos, dir))
            PushTiles(dir);
    }

    private void PushTiles(TileManager.TilePos pushDir)
	{
        canPush = false;
        // Play animation
        // Play sound
        myLevel.ShiftTiles(pos, pushDir);
        canPush = true; // remove this later on
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
		    if ((down && nextDown) || (down && this.myLevel.Get(this.pos + dirs[nextOppositePos]) == null) || (nextDown && this.myLevel.Get(this.pos + dirs[nextPos]) == null))
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
        Vector2 startPos = this.transform.position;
        Vector2 targetPos = GetPosInWorldSpace(newTile);
        float currentStepDelta = 0.0f;
        canMove = false;

        // Play some animation
        // Insert a sound or smth
        
        while(currentStepDelta < 1)
		{
            Vector2 curr = startPos + currentStepDelta * (targetPos - startPos);
            transform.position = new Vector3(curr.x, curr.y, transform.position.z);
            currentStepDelta += Time.deltaTime / jumpTime;
            yield return null;
		}
        pos = newTile;
        canMove = true; // Put in animation
        AdjustDepth();
        
        // check win condition
        if (this.myLevel.Get(pos).HasFlag)
        {
	        this.myLevel.Manager.currentLevelId++;
	        this.myLevel.Manager.RestartCurrentLevel();
        }
	}

    private void AdjustDepth()
	{
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -2.0f + pos.Y - pos.X);
	}

    private Vector2 GetPosInWorldSpace(TileManager.TilePos tile)
	{
        return new Vector2(0.5f * tile.X + 0.5f * tile.Y, -0.25f * tile.X + 0.25f * tile.Y);
	}

    private bool TileClear(TileManager.TilePos checkPos)
	{
        return myLevel.Get(checkPos) != null && !myLevel.Get(checkPos).HasRock;
	}
}
