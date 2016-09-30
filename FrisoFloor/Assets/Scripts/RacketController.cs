using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class RacketController : MonoBehaviour {

    //GameManager gameManager;

	// Use this for initialization
	void Start () {
        //gameManager = FindObjectOfType<GameManager>();

        Destroy(gameObject, 0.5f);
	}
	
	// Update is called once per frame
	void Update () {

        //var pos = Input.mousePosition;
        //pos.z = 3;
        //pos = Camera.main.ScreenToWorldPoint(pos);
        //transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 10);

        
        RaycastHit hit;

        Vector3 dir = transform.position - Camera.main.transform.position;
        if (Physics.Raycast(transform.position, dir, out hit, 1000, LayerMask.GetMask("Fish")))
        {
            Fish fish = hit.collider.GetComponent<Fish>();
            fish.Hit();
            //gameManager.Score();
            //gameManager.LostFish();
            GameManager.Instance.Score();
            GameManager.Instance.LostFish();
        }
	}
}
