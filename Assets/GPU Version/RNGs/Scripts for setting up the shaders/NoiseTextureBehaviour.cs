/*
    Author: Gus Tahara-Edmonds
    Date: Summer 2019
    Purpose: Wanted to make a script that reads 2D noise texture instead of calculating it with math. This could lead to 
    more unique looks and also be more performant. However this is very much unfinished. 
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseTextureBehaviour : DensityBehaviour
{
    public float scale;
    public Texture2D noiseTex;

    public override void UpdateParams() {
        compute.SetTexture(0, "noiseTex", noiseTex);
        compute.SetInt("size", noiseTex.width);
        compute.SetFloat("scale", (float)noiseTex.width / (pointsPerAxis - 1) * scale);
    }
}
