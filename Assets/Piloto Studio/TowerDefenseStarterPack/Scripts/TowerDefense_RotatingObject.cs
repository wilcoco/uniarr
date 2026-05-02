using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerDefense_RotatingObject : MonoBehaviour
{
    [SerializeField] Vector3 rotateDirection = Vector3.zero;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(rotateDirection * Time.deltaTime);
    }
}
