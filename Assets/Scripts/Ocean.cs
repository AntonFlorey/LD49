using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ocean : MonoBehaviour
{
    // Public Parameters
    [Range(0, 10)] public float waveAmplitude = 1f;
    [Range(0, 10)] public float waveSpeed = 1f;
    [Range(0, 10)] public float waveFrequency = 1f;
    public Vector2 waveDirection;
    [Range(0, 10)] public float noiseIntensity = 0.1f;
    [Range(0, 10)] public float noiseFrequency = 0.1f;
    public Vector2 windDirection;
    [Range(0, 10)] public float windSpeed = 1f;
    private List<Wave> activeWaves = new List<Wave>();
    public AnimationCurve wobbleCurve;

    // Private variables
    private float waveTimePassed = 0f;


    // Start is called before the first frame update
    void Start()
    {
        waveTimePassed = 0;
    }

    // Update is called once per frame
    void Update()
    {
        waveTimePassed += Time.deltaTime * waveSpeed;
    }

    public float GetOceanHeight(Vector2 position)
    {
        var normalizedDir = waveDirection.normalized;
        float wavePart = waveAmplitude * Mathf.Sin(waveFrequency * (Vector2.Dot(position, normalizedDir) / Vector2.Dot(normalizedDir, normalizedDir) - waveTimePassed));
        float noisePart = Mathf.PerlinNoise(noiseFrequency * (position.x + 2f * Time.time), noiseFrequency * (position.y + Time.time));
        float res = wavePart + noiseIntensity * noisePart;
        foreach (Wave wave in activeWaves)
		{
            res += wave.MeasureEffect(position);
		}
        return res;
    }

    public void MakeWave(Vector2 position, float duration, float intensity, float radius)
	{
        Wave newWave = new Wave(position, duration, intensity, Time.time, wobbleCurve, radius);
        activeWaves.Add(newWave);
        StartCoroutine(DeleteWaveAfterWait(newWave, duration));
    }

    IEnumerator DeleteWaveAfterWait(Wave wave, float duration)
	{
        yield return new WaitForSeconds(duration);
        activeWaves.Remove(wave);
    }


    public class Wave
	{
        private AnimationCurve curve;
        private float intensity;
        private float duration;
        private float startTime;
        private float radius; 
        private Vector2 position;

        public Wave(Vector2 position, float duration, float intensity, float startTime, AnimationCurve curve, float radius)
		{
            this.position = position;
            this.duration = duration;
            this.intensity = intensity;
            this.startTime = startTime;
            this.curve = curve;
            this.radius = radius;
		}

        public float MeasureEffect(Vector2 atPosition)
		{
            float relTime = (Time.time - startTime) / duration;
            if (Vector2.Distance(atPosition, this.position) < radius)
			{
                return intensity * curve.Evaluate(relTime);
            }
            return 0.0f;
		}
	}

}

