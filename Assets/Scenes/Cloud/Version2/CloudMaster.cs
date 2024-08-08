﻿using UnityEngine;

[ExecuteInEditMode]
public class CloudMaster : MonoBehaviour
{
    const string headerDecoration = " --- ";
    [Header (headerDecoration + "Main" + headerDecoration)]
    public Material material;
    public Transform container;
    public Vector3 cloudTestParams;

    [Header ("March settings" + headerDecoration)]
    public int numStepsLight = 8;
    public float rayOffsetStrength;
    public Texture2D blueNoise;

    [Header (headerDecoration + "Base Shape" + headerDecoration)]
    public float cloudScale = 1;
    public float densityMultiplier = 1;
    public float densityOffset;
    public Vector3 shapeOffset;
    public Vector2 heightOffset;
    public Vector4 shapeNoiseWeights;

    [Header (headerDecoration + "Detail" + headerDecoration)]
    public float detailNoiseScale = 10;
    public float detailNoiseWeight = .1f;
    public Vector3 detailNoiseWeights;
    public Vector3 detailOffset;
    

    [Header (headerDecoration + "Lighting" + headerDecoration)]
    public float lightAbsorptionThroughCloud = 1;
    public float lightAbsorptionTowardSun = 1;
    [Range (0, 1)]
    public float darknessThreshold = .2f;
    [Range (0, 1)]
    public float forwardScattering = .83f;
    [Range (0, 1)]
    public float backScattering = .3f;
    [Range (0, 1)]
    public float baseBrightness = .8f;
    [Range (0, 1)]
    public float phaseFactor = .15f;

    [Header (headerDecoration + "Animation" + headerDecoration)]
    public float timeScale = 1;
    public float baseSpeed = 1;
    public float detailSpeed = 2;

    [Header (headerDecoration + "Sky" + headerDecoration)]
    public Color colA;
    public Color colB;
    
    [Header("cloud texture")]
    public Texture2D weatherMap;
    public Texture3D cloud3D;
    public Texture3D cloudNoiseDetail;
 
    void SetDebugParams()
    {

    }

    void Awake ()
    {
        // var weatherMapGen = FindObjectOfType<WeatherMap> ();
        // if (Application.isPlaying)
        //     weatherMapGen.UpdateMap();
    }

    void Update()
    {
        // Validate inputs
        if (!material) return;

        numStepsLight = Mathf.Max (1, numStepsLight);

 

        material.SetTexture ("NoiseTex",cloud3D);
        material.SetTexture ("DetailNoiseTex", cloudNoiseDetail);
        material.SetTexture ("BlueNoise", blueNoise);

        material.SetTexture ("WeatherMap", weatherMap);

        Vector3 size = container.localScale;
        int width = Mathf.CeilToInt (size.x);
        int height = Mathf.CeilToInt (size.y);
        int depth = Mathf.CeilToInt (size.z);

        material.SetFloat ("scale", cloudScale);
        material.SetFloat ("densityMultiplier", densityMultiplier);
        material.SetFloat ("densityOffset", densityOffset);
        material.SetFloat ("lightAbsorptionThroughCloud", lightAbsorptionThroughCloud);
        material.SetFloat ("lightAbsorptionTowardSun", lightAbsorptionTowardSun);
        material.SetFloat ("darknessThreshold", darknessThreshold);
        material.SetVector ("params", cloudTestParams);
        material.SetFloat ("rayOffsetStrength", rayOffsetStrength);

        material.SetFloat ("detailNoiseScale", detailNoiseScale);
        material.SetFloat ("detailNoiseWeight", detailNoiseWeight);
        material.SetVector ("shapeOffset", shapeOffset);
        material.SetVector ("detailOffset", detailOffset);
        material.SetVector ("detailWeights", detailNoiseWeights);
        material.SetVector ("shapeNoiseWeights", shapeNoiseWeights);
        material.SetVector ("phaseParams", new Vector4 (forwardScattering, backScattering, baseBrightness, phaseFactor));

        material.SetVector ("boundsMin", container.position - container.localScale / 2);
        material.SetVector ("boundsMax", container.position + container.localScale / 2);

        material.SetInt ("numStepsLight", numStepsLight);

        material.SetVector ("mapSize", new Vector4 (width, height, depth, 0));

        material.SetFloat ("timeScale", Application.isPlaying ? timeScale : 0);
        material.SetFloat ("baseSpeed", baseSpeed);
        material.SetFloat ("detailSpeed", detailSpeed);

        // SetDebugParams ();

        material.SetColor ("colA", colA);
        material.SetColor ("colB", colB);
    }
}