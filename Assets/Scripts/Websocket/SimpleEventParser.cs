using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;

/// <summary>
/// Helper class to safely parse Socket.IO events using string operations
/// rather than direct deserialization which can be error-prone with dynamic data.
/// </summary>
public static class SimpleEventParser
{
    /// <summary>
    /// Process a system event safely
    /// </summary>
    public static void ProcessSystemEvent(SocketIOResponse response)
    {
        try
        {
            string rawData = response.ToString();
            Debug.Log($"[SimpleEventParser] Processing system event: {rawData}");
            
            // Extract room code if present
            if (rawData.Contains("Joined new room:"))
            {
                int roomCodeStart = rawData.IndexOf("Joined new room:") + 15;
                if (roomCodeStart > 15)
                {
                    string roomCode = rawData.Substring(roomCodeStart).Trim().Trim('"', ']', '[');
                    Debug.Log($"[SimpleEventParser] Joined room with code: {roomCode}");
                    // Here you can add custom handling if needed
                }
            }
            
            // Extract player ID if present
            if (rawData.Contains("Your ID set to"))
            {
                int idStart = rawData.IndexOf("Your ID set to") + 13;
                if (idStart > 13)
                {
                    string playerId = rawData.Substring(idStart).Trim().Trim('"', ']', '[');
                    Debug.Log($"[SimpleEventParser] Player ID set to: {playerId}");
                    // Here you can add custom handling if needed
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SimpleEventParser] Error processing system event: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Process a timer event safely
    /// </summary>
    public static void ProcessTimerEvent(SocketIOResponse response)
    {
        try
        {
            string rawData = response.ToString();
            Debug.Log($"[SimpleEventParser] Processing timer event: {rawData}");
            
            // Extract timer value
            if (rawData.Contains("\"timer\":"))
            {
                int timerStart = rawData.IndexOf("\"timer\":") + 8;
                if (timerStart > 8)
                {
                    string timerValue = "";
                    int i = 0;
                    while (timerStart + i < rawData.Length && 
                          (char.IsDigit(rawData[timerStart + i]) || rawData[timerStart + i] == '.'))
                    {
                        timerValue += rawData[timerStart + i];
                        i++;
                    }
                    
                    if (!string.IsNullOrEmpty(timerValue))
                    {
                        Debug.Log($"[SimpleEventParser] Timer value: {timerValue}");
                        // Add logic for timer updates here
                    }
                }
            }
            
            // Check if room is open or closed
            bool isOpen = !rawData.Contains("\"open\":false") && !rawData.Contains("\"open\": false");
            Debug.Log($"[SimpleEventParser] Room is open: {isOpen}");
            
            if (!isOpen)
            {
                Debug.Log("[SimpleEventParser] Room is now closed");
                // Add logic for handling room closure here
            }
            
            // Extract player count if available
            if (rawData.Contains("\"player_online\":"))
            {
                int playerStart = rawData.IndexOf("\"player_online\":") + 16;
                if (playerStart > 16)
                {
                    // Skip to the actual value
                    while (playerStart < rawData.Length && 
                          (rawData[playerStart] == ':' || rawData[playerStart] == ' '))
                    {
                        playerStart++;
                    }
                    
                    // Extract the count
                    string playerCount = "";
                    int i = 0;
                    while (playerStart + i < rawData.Length && 
                          char.IsDigit(rawData[playerStart + i]))
                    {
                        playerCount += rawData[playerStart + i];
                        i++;
                    }
                    
                    if (!string.IsNullOrEmpty(playerCount))
                    {
                        Debug.Log($"[SimpleEventParser] Player count: {playerCount}");
                        // Handle player count here
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SimpleEventParser] Error processing timer event: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Process a join event safely
    /// </summary>
    public static void ProcessJoinEvent(SocketIOResponse response)
    {
        try
        {
            string rawData = response.ToString();
            Debug.Log($"[SimpleEventParser] Processing join event: {rawData}");
            
            // Extract game code
            if (rawData.Contains("\"game_code\""))
            {
                int codeStart = rawData.IndexOf("\"game_code\"") + 12;
                if (codeStart > 12)
                {
                    // Skip to the actual value (after the colon and quotes)
                    while (codeStart < rawData.Length && 
                          (rawData[codeStart] == '\"' || rawData[codeStart] == ':' || 
                           rawData[codeStart] == ' '))
                    {
                        codeStart++;
                    }
                    
                    // Extract the code
                    string gameCode = "";
                    int i = 0;
                    while (codeStart + i < rawData.Length && 
                          rawData[codeStart + i] != '\"' && rawData[codeStart + i] != ',')
                    {
                        gameCode += rawData[codeStart + i];
                        i++;
                    }
                    
                    if (!string.IsNullOrEmpty(gameCode))
                    {
                        Debug.Log($"[SimpleEventParser] Game code: {gameCode}");
                        // Handle game code here
                    }
                }
            }
            
            // Extract match fee
            if (rawData.Contains("\"match_fee\""))
            {
                int feeStart = rawData.IndexOf("\"match_fee\"") + 12;
                if (feeStart > 12)
                {
                    // Skip to the actual value
                    while (feeStart < rawData.Length && 
                          (rawData[feeStart] == '\"' || rawData[feeStart] == ':' || 
                           rawData[feeStart] == ' '))
                    {
                        feeStart++;
                    }
                    
                    // Extract the fee
                    string matchFee = "";
                    int i = 0;
                    while (feeStart + i < rawData.Length && 
                          rawData[feeStart + i] != '\"' && rawData[feeStart + i] != ',')
                    {
                        matchFee += rawData[feeStart + i];
                        i++;
                    }
                    
                    if (!string.IsNullOrEmpty(matchFee))
                    {
                        Debug.Log($"[SimpleEventParser] Match fee: {matchFee}");
                        // Handle match fee here
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SimpleEventParser] Error processing join event: {ex.Message}");
        }
    }
}
