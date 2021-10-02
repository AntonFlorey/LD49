using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Some attributes
    public TileManager.TilePos pos;
    public bool canMove = true;
    [SerializeField] private float jumpTime = 0.5f;
    [SerializeField] private Sprite[] mySprites;
    private SpriteRenderer myRenderer;
    public TileManager.Level myLevel;

	private void Start()
	{
        myRenderer = this.GetComponent<SpriteRenderer>();
	}

	// Update is called once per frame
	void Update()
    {
        if (canMove)
		{
            this.Move();
		}
    }

    private void Move()
	{
        Dictionary<KeyCode, TileManager.TilePos> test = new Dictionary<KeyCode, TileManager.TilePos>();
        test.Add(KeyCode.D, new TileManager.TilePos(1, 0));
        test.Add(KeyCode.A, new TileManager.TilePos(-1, 0));
        test.Add(KeyCode.W, new TileManager.TilePos(0, 1));
        test.Add(KeyCode.S, new TileManager.TilePos(0, -1));

        foreach (var entry in test){
            if (Input.GetKey(entry.Key))
            {
                // Toggle sprite flipX
                myRenderer.flipX = entry.Value.X == -1 || entry.Value.Y == -1;

                TileManager.TilePos newPos = pos + entry.Value;
                if (TileClear(newPos))
                {
                    StartCoroutine(MoveToTile(newPos));
                }
                return;
            }
        }
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
        canMove = true;
        AdjustDepth();
        
	}

    private void AdjustDepth()
	{
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -2.0f + pos.Y - pos.X);
	}

    private Vector2 GetPosInWorldSpace(TileManager.TilePos tile)
	{
        return new Vector2(0.5f * tile.X + 0.5f * tile.Y, -0.25f * tile.X + 0.25f * tile.Y);
	}

    // TODO UPDATE THIS
    private bool TileClear(TileManager.TilePos checkPos)
	{
        return myLevel.Get(checkPos) != null;
	}
}
