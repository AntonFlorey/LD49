using UnityEngine;

public class TileComponent : MonoBehaviour
{
    public TileManager.Level Level;
    public TileManager.TilePos TilePos = new TileManager.TilePos(0, 0);
    private TileManager.TilePos prevPos = new TileManager.TilePos(0, 0);
    [SerializeField] public Transform blockTransform;
    public Ocean myOcean;

    private bool changingType = false;
    private TileManager.TileType changingToType = null;
    
    public void Init(TileManager.Level level, Sprite sprite, TileManager.TilePos pos)
    {
        this.Level = level;
        this.TilePos = pos;
        this.GetComponentInChildren<SpriteRenderer>().sprite = sprite;
        this.prevPos = pos;
        myOcean = GameObject.Find("Ocean").GetComponent<Ocean>();
    }

    public void DoMoveTo(TileManager.TilePos toPos)
    {
        this.prevPos = this.TilePos;
        this.TilePos = toPos;
    }

    public void DoChangeTo(TileManager.TileType newType)
    {
        this.changingType = true;
        this.changingToType = newType;
    }

    void Update()
    {
        var fromPos = this.prevPos.ToTransformPosition();
        var toPos = this.TilePos.ToTransformPosition();
        this.transform.localPosition = fromPos + this.Level.CurrentStepDelta * (toPos - fromPos);
        float x = transform.localPosition.x;
        float y = transform.localPosition.y;
        blockTransform.localPosition = new Vector3(0.0f, myOcean.GetOceanHeight(TileManager.TilePos.TransformToTileCoords(x, y)), blockTransform.localPosition.z);
    }

    public void UpdateStep()
    {
        this.prevPos = this.TilePos;
        if (this.changingType)
        {
            if (this.changingToType == null)
                Destroy(this.gameObject);
            else
                this.Init(this.Level, this.Level.Manager.tileSprites[this.changingToType], this.TilePos);

            this.changingType = false;
            this.changingToType = null;
        }
    }
}
