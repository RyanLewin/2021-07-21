using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using PropertyListenerTool;

[CreateAssetMenu(fileName = "PlayerSaveData", menuName = "SaveData/SaveManager", order = 0)]
public class PlayerSaveData : ScriptableObject
{
    //For when/if I add inventory stuff --- "Key - Item Name","Enabled;Cost;OtherData"
    public static PlayerSaveData Instance;
    public int points;
    public string playerName;
    public string playerUsername;
    [HideInInspector] public string playerPassword;
    public SaveData saveData { get; private set; }
    private static string saveLocation;

#if UNITY_EDITOR
    [MenuItem("Save Data/Delete Save Data")]
    static private void DeleteSaveData()
    {
        string location = $"{Application.persistentDataPath}/gamesave.save";
        File.Delete($"{saveLocation}");
        PlayerSaveData.Instance.points = 1000;
        PlayerSaveData.Instance.playerName = "";
    }
#endif

    private void OnEnable()
    {
        Instance = this;
        saveLocation = $"{Application.persistentDataPath}/gamesave.save";
        saveData = LoadSaveData();
    }

    private SaveData LoadSaveData()
    {
        if (File.Exists(saveLocation))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(saveLocation, FileMode.Open);
            SaveData save = (SaveData)bf.Deserialize(file);
            file.Close();
            return save;
        }
        return CreateSaveGameData();
    }

    private SaveData CreateSaveGameData()
    {
        saveData = new SaveData();
        playerName = "";
        points = 1000;
        SaveGameData();

        return saveData;
    }

    public void SaveGameData()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create($"{saveLocation}");
        bf.Serialize(file, saveData);
        file.Close();
    }

    public void SetPoints(int newPoints)
    {
        points = newPoints;
        saveData.points = points;
        SaveGameData();
    }

    public void PointsChanged(int change)
    {
        points = saveData.points + change;
        saveData.points = points;
        SaveGameData();
    }

    public void SetName(string newName)
    {
        playerName = newName;
        saveData.playerName = playerName;
        SaveGameData();
    }

    public void SetUsername(string username)
    {
        playerUsername = username;
        saveData.playerUsername = username;
        SaveGameData();
    }

    public void SetPassword(string password)
    {
        playerPassword = password;
        saveData.playerPassword = password;
        SaveGameData();
    }
}
