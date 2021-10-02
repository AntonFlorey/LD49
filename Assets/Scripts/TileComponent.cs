using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;

public class TileComponent : MonoBehaviour
{
    public TileManager.Level Level;
    public TileManager.TilePos TilePos = new TileManager.TilePos(0, 0);
    public TileManager.TilePos PrevPos = new TileManager.TilePos(0, 0);

    // Update is called once per frame

    public void DoMoveTo(TileManager.TilePos toPos)
    {
        this.PrevPos = this.TilePos;
        this.TilePos = toPos;
    }

    void Update()
    {
        var fromPos = this.PrevPos.ToTransformPosition();
        var toPos = this.TilePos.ToTransformPosition();
        this.transform.localPosition = fromPos + this.Level.CurrentStepDelta * (toPos - fromPos);
    }

    public void UpdateStep()
    {
        this.PrevPos = this.TilePos;
    }
}
