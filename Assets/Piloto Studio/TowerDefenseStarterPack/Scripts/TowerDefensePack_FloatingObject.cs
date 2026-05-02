using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerDefensePack_FloatingObject : MonoBehaviour
{
    [SerializeField] float bobPosSpeed;
    [SerializeField] float bobPosAmount;

    [SerializeField] float bobRotSpeed;
    [SerializeField] float bobRotAmount;


    // Update is called once per frame
    void Update()
    {
        float addToPos = (Mathf.Sin(Time.time * bobPosSpeed) * bobPosAmount);
        transform.position += Vector3.up * addToPos * Time.deltaTime;

        float xRot = (Mathf.Sin(Time.time * bobRotSpeed) * bobRotAmount);

        float zRot = (Mathf.Sin((Time.time - 1.0f) * bobRotSpeed) * bobRotAmount);

        transform.eulerAngles = new Vector3(xRot, transform.eulerAngles.y, zRot);
    }
}
