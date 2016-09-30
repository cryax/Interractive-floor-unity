using System;
using System.IO;
using UnityEngine;

public class FrisoFloorContext : MonoBehaviour
{

    private static FrisoFloorContext instance;
    public float ScaleFactor = 1;

    public static FrisoFloorContext Current
    {
        get { return instance ?? (instance = new FrisoFloorContext()); }
    }


    private FrisoFloorContext()
    {
        ReadTextFile(FrisoFloorConf.CONFIG_FILENAME);

    }


    public void Reload()
    {

    }
    public static void ReadTextFile(string path)
    {
        /*   if (!File.Exists(path))
           {
               return;
           }*/
        
        try
        {
            string[] configData = File.ReadAllLines(path);
            FillData(configData);
        }
        catch (Exception)
        {
        }


    }

    public static void FillData(string[] configData)
    {

        foreach (string t in configData)
        {
            if (string.IsNullOrEmpty(t))
            {
                continue;
            }
            Debug.Log("File exist");
            if (t.Contains(FrisoFloorConf.KEY_TIME_COUNTER_MAIN))
                int.TryParse(t.Split(Convert.ToChar("|"))[1], out  FrisoFloorConf.TIME_COUNTER_MAIN);

            if (t.Contains(FrisoFloorConf.KEY_FISH_COUNTER_MAIN))
                int.TryParse(t.Split(Convert.ToChar("|"))[1], out FrisoFloorConf.FISH_COUNTER_MAIN);

            if (t.Contains(FrisoFloorConf.KEY_TIME_OVER))
                int.TryParse(t.Split(Convert.ToChar("|"))[1], out FrisoFloorConf.TIME_OVER);

            if (t.Contains(FrisoFloorConf.KEY_TIME_RESET))
                int.TryParse(t.Split(Convert.ToChar("|"))[1], out FrisoFloorConf.TIME_RESET);

            if (t.Contains(FrisoFloorConf.KEY_KINECT_Z))
                int.TryParse(t.Split(Convert.ToChar("|"))[1], out FrisoFloorConf.KINECT_Z);
            Debug.Log("Main Counter" + FrisoFloorConf.TIME_COUNTER_MAIN);
        }

    }

    /*  public static void SaveConfig(string pathName = "Config.txt")
    {
        string content = isLocationHcm + "|" + KinhDoConfig.IsLocationHcm + "\n" +
                            imageNumber + "|" + KinhDoConfig.ImageNumber.ToString() + "\n" +
                            minDistance + "|" + KinhDoConfig.MinDistance.ToString() + "\n" +
                            maxDistance + "|" + KinhDoConfig.MaxDistance.ToString() + "\n" +
                            rangeDetectSkeleton + "|" + KinhDoConfig.RangeDetectSkeleton.ToString() + "\n" +
                            isDebug + "|" + KinhDoConfig.IsDebug.ToString() + "\n" +
                            is16X9 + "|" + KinhDoConfig.Is16X9.ToString() + "\n"

            ;
        SaveData(pathName, content);
    }

    public static void SaveData(string pathName, string content)
    {

        File.WriteAllText(pathName, content);

    }
*/
}

