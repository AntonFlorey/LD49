using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Some attributes
    [SerializeField] private float speed = 1;
    public bool canMove = true;
    private Vector2 direction;
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
            this.ToggleSprite();
            this.AdjustDepth();
		}
        Debug.Log(Input.GetAxis("Horizontal").ToString());
    }

    private void Move()
	{
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        direction = new Vector2(x, y);
        direction = CollisionHandle(direction);
        this.transform.position += speed * new Vector3(direction.x, direction.y, 0) * Time.deltaTime;
    }

    private void AdjustDepth()
	{
        Vector2 posInTileSpace = GetPosInTileSpace(this.transform.position);
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -2.0f + posInTileSpace.y - posInTileSpace.x);
	}

    private void ToggleSprite()
	{
        myRenderer.flipX = direction.x < 0;
	}

    private Vector2 GetPosInTileSpace(Vector2 worldPos)
	{
        return new Vector2(worldPos.x - 2.0f * worldPos.y, worldPos.x + 2.0f * worldPos.y);
	}

    private Vector2 GetPosInWorldSpace(Vector2 tilePos)
	{
        return new Vector2(0.5f * tilePos.x + 0.5f * tilePos.y, -0.25f * tilePos.x + 0.25f * tilePos.y);
	}

    private Vector2 CollisionHandle(Vector2 worldDir)
	{
        Vector2 tilePos = GetPosInTileSpace(transform.position);
        Vector2 tileDir = GetPosInTileSpace(worldDir);
        tileDir.Normalize();
        if(tileDir.x != 0.0f)
		{
            float horizontal = tileDir.x >= 0 ? 0.5f : -0.5f;
            TileManager.TilePos checkTile = new TileManager.TilePos(Mathf.RoundToInt(tilePos.x + horizontal), Mathf.RoundToInt(tilePos.y));

            // TODO CHANGE TO OBSTACLE CHECK
            if (myLevel.Get(checkTile) == null)
			{
                tileDir.x = 0;
			}
            
		}
        if (tileDir.y != 0.0f)
        {
            float vertical = tileDir.y >= 0 ? 0.5f : -0.5f;
            TileManager.TilePos checkTile = new TileManager.TilePos(Mathf.RoundToInt(tilePos.x), Mathf.RoundToInt(tilePos.y + vertical));

            // TODO CHANGE TO OBSTACLE CHECK
            if (myLevel.Get(checkTile) == null)
            {
                tileDir.y = 0;
            }

        }
        return GetPosInWorldSpace(tileDir);
	}
}
