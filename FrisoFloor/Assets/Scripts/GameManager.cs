using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : SingletonMono<GameManager>
{
    public Text TimeText;
    public Text ScoreText;
    public GameObject imgTimeOver;
    public float fixTime;
    public int maxFishInScene;
    public bool isUDP;
    public GameObject foot;
    public Text ScoreFinish;
    public GameObject CanvasNormal;
    public GameObject CanvasFinish;

    public GameObject blueFish;
    public GameObject[] fishes;
    public FishPath[] paths;
    public GameObject[] endFishes;
    public FishPath[] endPaths;

    float playTime;
    int fishCount;
    bool isPlaying = true;
    
    int score;
    string GetScore 
    {
        get 
        {
            //return NumberToWords(score);
            return score.ToString();
        }
    }

    UDPReceive udpReceive;

    //float countsdown;
    float timeOver = 2f;
    float resetTime = 5f;
    float kinect_z = 2f;

	// Use this for initialization
	void Start () {
        if (udpReceive == null)
            udpReceive = UDPReceive.Instance;

        //Read Config
        FrisoFloorContext.ReadTextFile(FrisoFloorConf.CONFIG_FILENAME);
        fixTime = FrisoFloorConf.TIME_COUNTER_MAIN;
        maxFishInScene = FrisoFloorConf.FISH_COUNTER_MAIN;
        timeOver = FrisoFloorConf.TIME_OVER;
        resetTime = FrisoFloorConf.TIME_RESET;
        kinect_z = FrisoFloorConf.KINECT_Z;

        playTime = fixTime;
        
        StartCoroutine(GenerateFish());
        StartCoroutine(GenerateBlueFish());
	}
	
	// Update is called once per frame
	void Update () {
        if (!isPlaying)
        {
            //if (countsdown >= resetTime + timeOver)
            //{
            //    CanvasNormal.SetActive(true);
            //    CanvasFinish.SetActive(false);
            //    countsdown = 0f;
            //    SceneManager.LoadScene("Start");
            //}
            //else
            //{
            //    countsdown += Time.deltaTime;
            //}
            return;
        }
        
        SpawnObject();

        if (Input.GetKey(KeyCode.Alpha1))
        {
            SceneManager.LoadScene("Start");
        }

        if (playTime > 0)
        {
            playTime -= Time.deltaTime;
        }
        else
        {
            playTime = fixTime;
            isPlaying = false;            
            //CanvasNormal.SetActive(false);
            //CanvasFinish.SetActive(true);
            //ScoreFinish.text = GetScore;
            //StartCoroutine(GenerateFishEnd());
            Camera.main.GetComponent<AudioSource>().Stop();
            //GetComponent<AudioSource>().Play();
            imgTimeOver.SetActive(true);
            Invoke("TimeOver", timeOver);
        }
        TimeText.text = Mathf.CeilToInt(playTime).ToString();
	}

    void TimeOver()
    {
        imgTimeOver.SetActive(false);
        CanvasNormal.SetActive(false);
        CanvasFinish.SetActive(true);
        ScoreFinish.text = GetScore;
        StartCoroutine(GenerateFishEnd());
        GetComponent<AudioSource>().Play();
        Invoke("Restart", resetTime);
    }

    void Restart()
    {
        CanvasNormal.SetActive(true);
        CanvasFinish.SetActive(false);        
        SceneManager.LoadScene("Start");
    }

    IEnumerator GenerateFish()
    {
        while (isPlaying)
        {
            if (fishCount < maxFishInScene)
            {
                FishPath path = paths[Random.Range(0, paths.Length)];
                GameObject fish = Instantiate(fishes[Random.Range(0, fishes.Length)]);
                fish.GetComponent<Fish>().path = path;
                fishCount++;
            }
            yield return null;
        }
    }

    IEnumerator GenerateBlueFish()
    {
        while (isPlaying)
        {
            FishPath path = paths[Random.Range(0, paths.Length)];
            GameObject fish = Instantiate(blueFish);
            fish.GetComponent<Fish>().path = path;
            fishCount++;
            yield return new WaitForSeconds(5f);
        }
    }

    IEnumerator GenerateFishEnd()
    {
        for (int i = 0; i < endFishes.Length; i++)
        {
            GameObject fish = Instantiate(endFishes[i]);
            fish.GetComponent<FishEnd>().path = endPaths[Random.Range(0, endPaths.Length)];
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void LostFish()
    {
        fishCount--;
    }

    public void Score()
    {
        score++;
        ScoreText.text = GetScore;
    }

    private void SpawnObject()
    {
        if (udpReceive.z > 0 && isUDP)
        {
            GameObject go = Instantiate(foot);
            var pos = new Vector3(udpReceive.x, udpReceive.y, kinect_z);            
            pos = Camera.main.ScreenToWorldPoint(pos);
            pos.z = 11;
            go.transform.position = pos;

            udpReceive.z = 0;
        }
        else if (!isUDP)
        {
            GameObject go = Instantiate(foot);
            go.transform.localScale = Vector3.one;

            var pos = Input.mousePosition;
            pos.z = 3;
            pos = Camera.main.ScreenToWorldPoint(pos);
            go.transform.position = pos;
        }
    }

    public string NumberToWords(int number)
    {
        if (number == 0)
            return "Không";

        //if (number < 0)
        //    return "âm " + NumberToWords(Mathf.Abs(number));

        string words = "";

        //if ((number / 1000000) > 0)
        //{
        //    words += NumberToWords(number / 1000000) + " triệu ";
        //    number %= 1000000;
        //}

        //if ((number / 1000) > 0)
        //{
        //    words += NumberToWords(number / 1000) + " ngàn ";
        //    number %= 1000;
        //}

        if ((number / 100) > 0)
        {
            words += NumberToWords(number / 100) + " trăm ";
            number %= 100;
        }

        if (number > 0)
        {
            if (words != "")
                words += "và ";

            var unitsMap = new[] { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín", "mười", "mười một", "mười hai", "mười ba", "mười bốn", "mười lăm", "mười sáu", "mười bảy", "mười tám", "mười chín" };
            var tensMap = new[] { "không", "mười", "hai mươi", "ba mươi", "bốn mươi", "năm mươi", "sáu mươi", "bảy mươi", "tám mươi", "chín mươi" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) == 1)
                    words += " " + "mốt";
                else if ((number % 10) > 0)
                    words += " " + unitsMap[number % 10];
            }
        }

        return words;
    }
}
