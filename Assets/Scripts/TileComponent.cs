using UnityEngine;

public class TileComponent : MonoBehaviour
{
    public TileManager.Level Level;
    public TileManager.TilePos TilePos = new TileManager.TilePos(0, 0);
    private TileManager.TilePos prevPos = new TileManager.TilePos(0, 0);

    
    public void Init(TileManager.Level level, TileManager.TilePos pos)
    {
        this.Level = level;
        this.TilePos = pos;
        this.prevPos = pos;
    }

    public void DoMoveTo(TileManager.TilePos toPos)
    {
        this.prevPos = this.TilePos;
        this.TilePos = toPos;
    }

    void Update()
    {
        var fromPos = this.prevPos.ToTransformPosition();
        var toPos = this.TilePos.ToTransformPosition();
        this.transform.localPosition = fromPos + this.Level.CurrentStepDelta * (toPos - fromPos);
    }

    public void UpdateStep()
    {
        this.prevPos = this.TilePos;
    }
}
