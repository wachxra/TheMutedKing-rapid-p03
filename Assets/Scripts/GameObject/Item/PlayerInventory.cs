/*using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private HashSet<string> keys = new HashSet<string>();

    public void AddKey(string keyID)
    {
        if (!keys.Contains(keyID))
            keys.Add(keyID);
    }

    public bool HasKey(string keyID)
    {
        return keys.Contains(keyID);
    }

    public void UseKey(string keyID)
    {
        if (keys.Contains(keyID))
            keys.Remove(keyID);
    }
}*/