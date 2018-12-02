﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Transmission {

    [Header("Shifting")]
    public int targetShiftUp = 2000;
    public int targetShiftDown = 800;
    
    public float shiftingTime = 0.3f;
    
    [Header("Gears")]
    [Range(0.0f, 20f)] public float[] forwardGears;
    [Range(0.0f, 20f)] public float[] reverseGears;

    private int currentGear = 1;
    public int CurrentGear
    {
        get
        {
            return currentGear;
        }
    }

    public float getCurrentGearRatio()
    {
        if (0 < currentGear) return forwardGears[currentGear - 1];
        else if (currentGear < 0) return reverseGears[Mathf.Abs(currentGear) - 1];
        return 0f;
    }

    public void ShiftUp()
    {
        currentGear += 1;

        if (forwardGears.Length < currentGear) currentGear = forwardGears.Length;
    }
    public void ShiftDown()
    {
        currentGear -= 1;

        if (reverseGears.Length < Mathf.Abs(currentGear)) currentGear = -reverseGears.Length;
    }

}
