/*
    Author: Gus Tahara-Edmonds
    Date: Summer 2019
    Purpose: Template script with functionality to setup the desired compute shader which generates the world data for the 
    marching cubes algorithim to polygonalize. Scripts that inherit from this one can add stuff to customize how the world is generated.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DensityBehaviour : MonoBehaviour {
    public ComputeShader compute;
    [HideInInspector]
    public int pointsPerAxis;

    public void Init(RenderTexture densityData, int pointsPerAxis, float scale) {
        compute.SetTexture(0, "densityData", densityData);
        this.pointsPerAxis = pointsPerAxis;
        compute.SetInt("pointsPerAxis", pointsPerAxis);
        compute.SetFloat("scale", scale);
        UpdateParams();
    }

    public abstract void UpdateParams();

    public void Generate(int threads, Vector3 worldPos) {
        float[] positionArray = { worldPos.x, worldPos.y, worldPos.z };
        compute.SetFloats("worldPos", positionArray);
        compute.Dispatch(0, threads, threads, threads);
    }
}
