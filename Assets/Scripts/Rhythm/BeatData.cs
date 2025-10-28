using UnityEngine;

public enum Direction { Up, Down, Left, Right, Ultimate }

[System.Serializable]
public class BeatData
{
    public Direction requiredDirection;
    public float timeToHit;
    public float travelDuration;

    [Header("Ultimate Settings")]
    public float ultimateMissSoundDamage = 30f;
}