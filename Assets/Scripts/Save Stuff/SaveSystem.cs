using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class SaveSystem
{
    public static int saveIteration;
    
    public static SaveSlot currentSaveSlot;

    /// <summary>
    /// Save data in the persistent data path. Data is saved as json text.
    /// Specify T as the type of data to be stored.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="name"></param>
    public static void SaveData<T>(T data, string name)
    {
        SaveData<T> classToSave = new();

        classToSave.data = data;
        classToSave.name = name;

        string jsonString = JsonUtility.ToJson(classToSave);

        string filePath = GetFilePath(name);

        File.WriteAllText(filePath, jsonString);

        Debug.Log("Data saved to: " + filePath);
    }

    /// <summary>
    /// Load data from the name it was saved as. Specify T as the type expected.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T LoadData<T>(string name)
    {
        string filePath = GetFilePath(name); // Include the name in the path

        if (!File.Exists(filePath))
        {
            Debug.LogWarning("No file found at path: " + filePath);
            return default;
        }

        string jsonString = File.ReadAllText(filePath);

        SaveData<T> loadedData = JsonUtility.FromJson<SaveData<T>>(jsonString);

        Debug.Log("Data successfully loaded from: " + filePath);

        return loadedData.data;
    }

    /// <summary>
    /// Returns the path to a file found by its name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    static string GetFilePath(string name)
    {
        // Construct the directory path based on the save slot
        string slotString = currentSaveSlot.ToString();
        string directoryPath = Path.Combine(Application.persistentDataPath, slotString);
        
        // Ensure the directory exists
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            Debug.Log("Directory created: " + directoryPath);
        }

        // Return the full path including the file name
        return Path.Combine(directoryPath, name + ".json");
    }
}

public class SaveData<T>
{
    public string name;
    public T data;
}

