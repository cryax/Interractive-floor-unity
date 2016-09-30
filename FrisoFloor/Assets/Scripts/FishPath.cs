using UnityEngine;
using System.Collections;

public class FishPath : MonoBehaviour {

    public WayPoint[] waypoints;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        for (int i = 1; i < waypoints.Length; i++)
        {
            if (i > 0)
            {
                Gizmos.DrawLine(waypoints[i - 1].transform.position, waypoints[i].transform.position);
            }
        }
    }
}
