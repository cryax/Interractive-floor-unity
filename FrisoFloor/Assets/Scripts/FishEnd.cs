using UnityEngine;
using System.Collections;
using System;

public class FishEnd : MonoBehaviour {

    public FishPath path;
    public float turningSpeed;

    private int nextWpIndex;
    private int curWpIndex;
    private bool isDead = false;
    //private GameManager gameManager;

    // Use this for initialization
    void Start()
    {
        transform.position = path.waypoints[0].transform.position;
        nextWpIndex = 1;

        //gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;
        
        Vector3 desirePos = path.waypoints[nextWpIndex].transform.position;
        Vector3 desiredDir = desirePos - transform.position;
        //transform.position = Vector3.MoveTowards(transform.position, desirePos, path.waypoints[nextWpIndex - 1].speed * Time.deltaTime);
        transform.Translate(transform.forward * path.waypoints[curWpIndex].speed * Time.deltaTime, Space.World);
        Quaternion desiredRot = Quaternion.LookRotation(desiredDir, Vector3.up);        
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, turningSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, desirePos) < 0.1f)
        {
            if (nextWpIndex < path.waypoints.Length - 1)
            {
                curWpIndex = nextWpIndex;
                nextWpIndex++;
            }
            else
            {
                Destroy(gameObject);
                //gameManager.LostFish();
                GameManager.Instance.LostFish();
            }
        }
    }
}
