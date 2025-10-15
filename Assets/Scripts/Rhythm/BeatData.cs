using UnityEngine;

public enum Direction { Up, Down, Left, Right }

[System.Serializable]
public class BeatData
{
    public Direction requiredDirection;
    public float timeToHit;
    public float travelDuration;
}