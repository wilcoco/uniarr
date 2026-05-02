using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerDefense_RotateAround : MonoBehaviour
{
   
    //Assign a GameObject in the Inspector to rotate around
    [SerializeField] GameObject target;
    [SerializeField] float speed = 20;

    void Update()
    {
        // Spin the object around the target at 20 degrees/second.
        transform.RotateAround(target.transform.position, Vector3.up, speed * Time.deltaTime);
    }
}
