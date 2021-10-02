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
    }

    private void Move()
	{
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        direction = new Vector2(x, y);
        direction.Normalize();
        this.transform.position += speed * new Vector3(direction.x, 0.5f * direction.y, 0) * Time.deltaTime;
    }

    private void AdjustDepth()
	{
        Vector2 posInTileSpace = getPosInTileSpace(this.transform.position);
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -2.0f + posInTileSpace.y - posInTileSpace.x);
	}

    private void ToggleSprite()
	{
        myRenderer.flipX = direction.x < 0;
	}

    private Vector2 getPosInTileSpace(Vector2 worldPos)
	{
        return new Vector2(worldPos.x - 2.0f * worldPos.y, worldPos.x + 2.0f * worldPos.y);
	}
}
