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
        return wavePart + noiseIntensity * noisePart;
    }
}

