using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class DataManager : MonoBehaviour
{
    public static void SavePlayerProfile(ProfileData p_profile)
    {
        try
        {
            string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            FileStream file = File.Create(path);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, p_profile);
            file.Close();
            Debug.Log("Saved successfully");
        }
        catch
        {
            Debug.LogError("Save failed!");
        }
    }

    public static ProfileData LoadPlayerProfile()
    {
        ProfileData ret = new ProfileData();
        try
        {
            string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path))
            {
                FileStream file = File.Open(path, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                ret = (ProfileData) bf.Deserialize(file);
                Debug.Log("Loaded successfully");
            }
        }
        catch
        {
            Debug.LogError("Load error!");
        }
        

        return ret;
    }
}