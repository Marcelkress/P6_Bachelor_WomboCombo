using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveSystemSettings : MonoBehaviour
{
    public SaveSlot currentSaveSlot;
    public int testInt;
    void Start()
    {
        SaveSystem.currentSaveSlot = currentSaveSlot;
        SaveSystem.SaveData(testInt, "Test");
    }
}

public enum SaveSlot
{
    Test = 0,
    Slot1 = 1,
    Slot2 = 2,
    Slot3 = 3,
    Slot4 = 4
}