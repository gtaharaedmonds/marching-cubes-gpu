/*
    Author: Gus Tahara-Edmonds
    Date: Summer 2019
    Purpose: This script generates smoothed noise to be turned into a mesh with Marching Cubes. It sums up
    noise at various frequencies/amplitudes for a more interesting look.
*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityCPU : MonoBehaviour {
    public float globalScale = 1;
    [Header("Noise Octaves")]
    public NoiseOctave[] noiseOctaves;

    public float Evaluate(Vector3 ws) {
        float density = 0;

        foreach (NoiseOctave octave in noiseOctaves) {
            density += Sample3DNoise((ws + octave.offset) * octave.frequency * globalScale) * octave.amplitude;
        }

        return density;
    }

    [System.Serializable]
    public struct NoiseOctave {
        public float frequency;
        public float amplitude;
        public Vector3 offset;

        public NoiseOctave(float frequency, float amplitude, Vector3 offset) {
            this.frequency = frequency;
            this.amplitude = amplitude;
            this.offset = offset;
        }
    }

    public float Sample3DNoise(Vector3 p) {
        float ab = Mathf.PerlinNoise(p.x, p.y);
        float bc = Mathf.PerlinNoise(p.y, p.z);
        float ac = Mathf.PerlinNoise(p.x, p.z);

        float ba = Mathf.PerlinNoise(p.y, p.x);
        float cb = Mathf.PerlinNoise(p.z, p.y);
        float ca = Mathf.PerlinNoise(p.z, p.x);

        float abc = ab + bc + ac + ba + cb + ca;
        abc /= 6f;

        return abc;
    }
}
