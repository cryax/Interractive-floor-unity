using UnityEngine;
using System.Collections;
using System;

public class Fish : MonoBehaviour {

    public FishPath path;
    public float turningSpeed;
    public GameObject bubble;
    public GameObject bangBubble;
    //public Transform bone;

    private int nextWpIndex;
    //private int curWpIndex;
    private bool isDead = false;
    //private GameManager gameManager;

    // Use this for initialization
    void Start()
    {
        transform.position = path.waypoints[0].transform.position;
        nextWpIndex = 1;

        //gameManager = FindObjectOfType<GameManager>();

        //Set fish orientation
        if (path.waypoints[nextWpIndex].transform.position.x > transform.position.x)
        {
            //Vector3 dis = bone.position - transform.position;
            //bone.Translate(dis * 2);
            //bone.Rotate(0, 180, 0);
            transform.Rotate(0, 180, 0);
            Vector3 t = bubble.transform.localPosition;
            t.x = -t.x;
            bubble.transform.localPosition = t;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;
        
        Vector3 desirePos = path.waypoints[nextWpIndex].transform.position;
        //Vector3 desiredDir = desirePos - transform.position;
        transform.position = Vector3.MoveTowards(transform.position, desirePos, path.waypoints[nextWpIndex - 1].speed * Time.deltaTime);
        //transform.Translate(transform.forward * path.waypoints[curWpIndex].speed * Time.deltaTime, Space.World);
        //Quaternion desiredRot = Quaternion.LookRotation(desiredDir, Vector3.up);        
        //transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, turningSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, desirePos) < 0.1f)
        {
            if (nextWpIndex < path.waypoints.Length - 1)
            {
                //curWpIndex = nextWpIndex;
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

    public void Hit()
    {
        isDead = true;
        GetComponent<SphereCollider>().enabled = false;
        bangBubble.SetActive(true);
        bubble.SetActive(false);
        Destroy(gameObject, 0.7f);
    }
}
