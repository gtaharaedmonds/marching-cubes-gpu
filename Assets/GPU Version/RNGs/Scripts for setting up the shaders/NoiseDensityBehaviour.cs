/*
    Author: Gus Tahara-Edmonds
    Date: Summer 2019
    Purpose: Inherits from DensityBehaviour. This script holds settings for making terrain from layers (octaves) of 3D noise
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDensityBehaviour : DensityBehaviour {
    [Header("Noise")]
    public float globalScale = 1;
    public NoiseOctave[] octaves;

    ComputeBuffer octavesBuffer;

    public override void UpdateParams() {
        octavesBuffer = new ComputeBuffer(octaves.Length, 20);
        octavesBuffer.SetData(octaves);
        compute.SetFloat("globalScale", globalScale);
        compute.SetBuffer(0, "noiseOctaves", octavesBuffer);
        compute.SetInt("length", octaves.Length);
    }

    private void OnDestroy() {
        octavesBuffer.Release();
    }

    [System.Serializable]
    public struct NoiseOctave {
        public float frequency;         
        public float amplitude;
        public Vector3 offset;
    }
}
