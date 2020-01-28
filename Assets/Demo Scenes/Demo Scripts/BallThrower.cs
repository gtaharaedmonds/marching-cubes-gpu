/*
    Author: Gus Tahara-Edmonds
    Date: Summer 2019
    Purpose: Simple script to throw a RigidBody ball the direction the camera is looking.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallThrower : MonoBehaviour {
    public GameObject prefab;
    public float throwStrength;

    void Start() {
        
    }

    void Update() {
        if(Input.GetMouseButtonDown(0)) {
            GameObject obj = GameObject.Instantiate(prefab, transform.position, Quaternion.identity);
            obj.GetComponent<Rigidbody>().AddForce(transform.forward * throwStrength, ForceMode.VelocityChange);
        }
    }
}
