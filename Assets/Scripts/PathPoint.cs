using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathPoint : MonoBehaviour
{
    PathPoint[] pathPointToMoveOn_;
    public PathObjectsParent pathObjParent;
    public List<PlayerPiece> playerPiecesList = new List<PlayerPiece>();
    public byte pairId;
    
    private void Start()
    {
        pathObjParent = GetComponentInParent<PathObjectsParent>();
    }


    public bool AddPlayerPiece(PlayerPiece playerPiece_)
    {
        if(this.name== "CentreHomePoint")
        {
            Completed(playerPiece_);
        }
        if (this.name!= "PathPoint"  && this.name != "PathPoint (13)"  && this.name != "PathPoint (26)"  && this.name != "PathPoint (39)" &&  this.name != "CentreHomePoint")
        {
            if (playerPiecesList.Count == 1)
            {
                string prevPlayerPieceName = playerPiecesList[0].name;
                string currentPlayerPieceName = playerPiece_.name;
                currentPlayerPieceName = currentPlayerPieceName.Substring(0, currentPlayerPieceName.Length - 4);
                if (!prevPlayerPieceName.Contains(currentPlayerPieceName))
                {
                    playerPiecesList[0].isReady = false;

                    StartCoroutine(revertOnStart(playerPiecesList[0]));


                    playerPiecesList[0].numberOfStepsAlreadyMoved = 0;
                    RemovePlayerPiece(playerPiecesList[0]);
                    playerPiecesList.Add(playerPiece_);

                    return false;
                }
            }
        }
        addPlayer(playerPiece_);
        return true;
    }

    IEnumerator revertOnStart(PlayerPiece playerPiece_)
    {
        //if (playerPiece_.name.Contains("Red"))
        if (playerPiece_.SelfDiceColor == DiceColor.Red)
        {
            GameManager.Instance.redOutPlayers -= 1;
            pathPointToMoveOn_ = pathObjParent.redPathPoints;
        }
        // else if (playerPiece_.name.Contains("Blue"))
        else if (playerPiece_.SelfDiceColor == DiceColor.Blue)
        {
            GameManager.Instance.blueOutPlayers -= 1;
            pathPointToMoveOn_ = pathObjParent.bluePathPoints;
        }
        // else if (playerPiece_.name.Contains("Yellow"))
        else if (playerPiece_.SelfDiceColor == DiceColor.Yellow)
        {
            GameManager.Instance.yellowOutPlayers -= 1;
            pathPointToMoveOn_ = pathObjParent.yellowPathPoints;
        }
        // else if (playerPiece_.name.Contains("Green"))
        else if (playerPiece_.SelfDiceColor == DiceColor.Green)
        {
            GameManager.Instance.greenOutPlayers -= 1;
            pathPointToMoveOn_ = pathObjParent.greenPathPoints;
        }

        for (int i = playerPiece_.numberOfStepsAlreadyMoved; i >= 0; i--)
        {
            playerPiece_.transform.position = pathPointToMoveOn_[i].transform.position;
            yield return new WaitForSeconds(0.05f);
        }

        Debug.LogError($"{playerPiece_.name}");
        // playerPiece_.transform.position = pathObjParent.BasePoints[BasePointPosition(playerPiece_.name)].transform.position;
        playerPiece_.transform.position = pathObjParent.BasePoints[BasePointPosition(playerPiece_.selfId)].transform.position;
    
    }

    private int BasePointPosition(string name)
    {
        for (int i = 0; i < pathObjParent.BasePoints.Length; i++)
        {
            if (pathObjParent.BasePoints[i].name == name)
            {
                return i;
            }
        }
        
        return -1;
    }
    
    private int BasePointPosition(byte selfId)
    {
        for (int i = 0; i < pathObjParent.BasePoints.Length; i++)
        {
            if (pathObjParent.BasePoints[i].pairId == selfId)
            {
                return i;
            }
        }
        
        return -1;
    }
    

    void addPlayer(PlayerPiece playerPiece_)
    {
        playerPiecesList.Add(playerPiece_);
        RescaleAndRepositionAllPlayerPieces();
    }
    public void RemovePlayerPiece(PlayerPiece PlayerPiece_)
    {
        if (playerPiecesList.Contains(PlayerPiece_))
        {
            playerPiecesList.Remove(PlayerPiece_);
            RescaleAndRepositionAllPlayerPieces();
        }
    }


    public void Completed(PlayerPiece playerPiece_)
    {
        switch (playerPiece_.SelfDiceColor)
        {
            case DiceColor.Red:
                GameManager.Instance.redCompletePlayer += 1;
                GameManager.Instance.redOutPlayers -= 1;
                if (GameManager.Instance.redCompletePlayer == 4)
                {
                    ShowCelebration();
                }
                break;

            case DiceColor.Blue:
                GameManager.Instance.blueCompletePlayer += 1;
                GameManager.Instance.blueOutPlayers -= 1;
                if (GameManager.Instance.blueCompletePlayer == 4)
                {
                    ShowCelebration();
                }
                break;

            case DiceColor.Yellow:
                GameManager.Instance.yellowCompletePlayer += 1;
                GameManager.Instance.yellowOutPlayers -= 1;
                if (GameManager.Instance.yellowCompletePlayer == 4)
                {
                    ShowCelebration();
                }
                break;

            case DiceColor.Green:
                GameManager.Instance.greenCompletePlayer += 1;
                GameManager.Instance.greenOutPlayers -= 1;
                if (GameManager.Instance.greenCompletePlayer == 4)
                {
                    ShowCelebration();
                }
                break;

            default:
                Debug.LogError("Unhandled DiceColor: " + playerPiece_.SelfDiceColor);
                break;
        }
    }
    void ShowCelebration()
    {
        GameManager.Instance.CheckForWinner();
    }
    public void RescaleAndRepositionAllPlayerPieces()
    {
        int plsCount = playerPiecesList.Count;
        bool isOdd = plsCount % 2 != 0;
        int spriteLayers = 0;

        int extent = plsCount / 2;
        int counter = 0;

        // Scale reduction factor (0.9 means 90% of the original size)
        float scaleReductionFactor = 0.75f;

        // Determine the currently active player based on active dice
        //string activeColor = GetActivePlayerDiceColor();
        DiceColor activeColor = GetActivePlayerDiceColor();
        // Adjust for odd count
        if (isOdd)
        {
            for (int i = -extent; i <= extent; i++)
            {
                // Apply the scale reduction factor
                float newScale = pathObjParent.scales[plsCount - 1] * scaleReductionFactor;
                playerPiecesList[counter].transform.localScale = new Vector3(newScale, newScale, 1f);
                playerPiecesList[counter].transform.position = new Vector3(transform.position.x + (i * pathObjParent.positionsDifference[plsCount - 1]), transform.position.y, 0f);
                counter++;
            }
        }
        // Adjust for even count
        else
        {
            for (int i = -extent; i < extent; i++)
            {
                // Apply the scale reduction factor
                float newScale = pathObjParent.scales[plsCount - 1] * scaleReductionFactor;
                playerPiecesList[counter].transform.localScale = new Vector3(newScale, newScale, 1f);
                playerPiecesList[counter].transform.position = new Vector3(transform.position.x + (i * pathObjParent.positionsDifference[plsCount - 1]), transform.position.y, 0f);
                counter++;
            }
        }

        // Set sprite layers to avoid overlap
        for (int i = 0; i < playerPiecesList.Count; i++)
        {
            SpriteRenderer spriteRenderer = playerPiecesList[i].GetComponentInChildren<SpriteRenderer>();

            // Check if the player's color matches the active color
            //if (playerPiecesList[i].name.Contains(activeColor))
            if (playerPiecesList[i].SelfDiceColor == activeColor && playerPiecesList[i].isReady)
            {
                // Animate scale increase
                spriteRenderer.transform.DOScale(new Vector3(3f, 3f, 3f), 0.3f); // Scale up over 0.3 seconds
            }
            else
            {
                // Animate scale reset
                spriteRenderer.transform.DOScale(new Vector3(2.5f, 2.5f, 2.5f), 0.3f); // Scale back over 0.3 seconds
            }
            spriteLayers++;
        }
    }

    // Helper method to determine the active player's color based on the active dice
    private DiceColor GetActivePlayerDiceColor()
    {
        // Check which dice is currently active and return corresponding player color
        if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[0])
        {
            //return "Blue";
            return DiceColor.Blue;
        }
        else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[1])
        {
            // return "Red";
            return DiceColor.Red;
        }
        else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[2])
        {
            // return "Green";
            return DiceColor.Green;
        }
        else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[3])
        {
            // return "Yellow";
            return DiceColor.Yellow;
        }

        // Default fallback, though this case shouldn't normally happen
        // return string.Empty;
        return DiceColor.Unknown;
    }
}


