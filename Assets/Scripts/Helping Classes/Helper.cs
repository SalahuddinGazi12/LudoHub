using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Helper
{
    public static string GetPascalCaseString(string input)
    {
        return string.IsNullOrEmpty(input) ? input : System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(input);
    }
    
    public static string GetReadableNumber(int number)
    {
        float input = number;

        return number switch
        {
            >= 0 and <= 999 => input.ToString("N0"),
            >= 1000 and < 1000000 => string.Concat(Math.Floor(input / 1000f * 10) / 10, "K"),  // Avoids rounding to "10.0K"
            >= 1000000 and < 1000000000 => string.Concat(Math.Floor(input / 1000000f * 10) / 10, "M"),
            _ => string.Concat(Math.Floor(input / 1000000000f * 10) / 10, "B")
        };
    }
    
    public static int CalculatePercentage(int wholeNumber, int percentage)
    {
        float number = (wholeNumber * percentage * 1f) / 100f;
        return  (int) number;
    }
}
