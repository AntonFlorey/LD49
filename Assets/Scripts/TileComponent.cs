using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileComponent : MonoBehaviour
{
    public TileManager.Level Level;
    public TileManager.TilePos TilePos = new TileManager.TilePos(0, 0);
    private TileManager.TilePos prevPos = new TileManager.TilePos(0, 0);
    [SerializeField] public Transform blockTransform;
    public Ocean myOcean;
    [SerializeField] private AnimationCurve pushedCurve;
    [SerializeField] private ParticleSystem waterparticles1;
    [SerializeField] private ParticleSystem waterparticles2;
    
    public void Init(TileManager.Level level, Sprite sprite, TileManager.TilePos pos)
    {
        this.Level = level;
        this.TilePos = pos;
        this.prevPos = pos;
        this.GetComponentInChildren<SpriteRenderer>().sprite = sprite;
        myOcean = GameObject.Find("Ocean").GetComponent<Ocean>();
        this.Update();
    }

    public void DoMoveTo(TileManager.TilePos toPos)
    {
        this.prevPos = this.TilePos;
        this.TilePos = toPos;
    }

    public void DoChangeTo(TileManager.TileType newType)
    {
        if (newType == null)
            Destroy(this.gameObject);
        else
            this.GetComponentInChildren<SpriteRenderer>().sprite = this.Level.Manager.tileSprites[newType];
    }

    void Update()
    {
        var fromPos = this.prevPos.ToTransformPosition();
        var toPos = this.TilePos.ToTransformPosition();
        this.transform.localPosition = fromPos + pushedCurve.Evaluate(this.Level.CurrentStepDelta) * (toPos - fromPos);
        float x = transform.localPosition.x;
        float y = transform.localPosition.y;
        blockTransform.localPosition = new Vector3(0.0f, myOcean.GetOceanHeight(TileManager.TilePos.TransformToTileCoords(x, y)), blockTransform.localPosition.z);
    }

    public void UpdateStep()
    {
        this.prevPos = this.TilePos;
    }


    public void ShootWaterParticles()
	{
        ParticleSystem.Particle[] particleArr = new ParticleSystem.Particle[10];
        waterparticles1.Emit(Random.Range(2, 8));
        int particles = waterparticles1.GetParticles(particleArr);
        for (int i = 0; i < particles; i++)
		{
            Vector3 vel = particleArr[i].velocity;
            vel.z = 0.0f;
            particleArr[i].velocity = vel;
		}
        waterparticles1.SetParticles(particleArr, particles);
        waterparticles2.Emit(Random.Range(2, 8));
        particles = waterparticles2.GetParticles(particleArr);
        for (int i = 0; i < particles; i++)
        {
            Vector3 vel = particleArr[i].velocity;
            vel.z = 0.0f;
            particleArr[i].velocity = vel;
        }
        waterparticles2.SetParticles(particleArr, particles);
    }
}
