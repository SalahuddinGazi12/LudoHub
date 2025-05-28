using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
//using Photon.Realtime;
public class CoinMove : MonoBehaviour
{
    public static CoinMove instance;
    public GameObject coinPrefab;      // Prefab of the coin to be instantiated
    //public Transform targetSprite;     //The target position for coins
    public float duration = 1f;      //Duration of the movement in seconds
    public int numberOfCoins = 5;      // Number of coins to instantiate and move
    public float offset = 10f;         // Offset to avoid overlapping coins

    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {

        //coinAnimator = GetComponent<Animator>();
        //PlayVSAnimation();
        // coinAnimator.SetInteger();
    }
    public void MoveCoins(Transform startPos)
    {
        GameObject targetImage = GameObject.Find("VS Image");

        for (int i = 0; i < numberOfCoins; i++)
        {
            // Instantiate each coin at a slight offset from the start position
            Vector3 spawnPosition = startPos.position + new Vector3(Random.Range(-0.5f, 0.5f), i * offset, 0); // Random x offset for initial spread
            GameObject coin = Instantiate(coinPrefab, spawnPosition, Quaternion.identity);

            // Set parent, scale, and canvas layer
            coin.transform.SetParent(GameObject.Find("Canvas").transform);
            coin.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            // Staggered delay for each coin
            float delay = i * 0.1f;

            // Animate the coin's movement to the target position
            coin.transform.DOMove(targetImage.transform.position, duration)
                .SetDelay(delay) // Add delay to stagger each coin's movement
                .SetEase(Ease.OutBack) // Easing out for a smooth, bouncy finish
                .OnStart(() =>
                {
                    // Floating effect before moving to the target
                    coin.transform.DOLocalMoveY(spawnPosition.y + 0.3f, duration / 2).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);

                    // Rotation and scaling effects at the start
                    coin.transform.DORotate(new Vector3(0, 0, 360), duration, RotateMode.LocalAxisAdd).SetLoops(-1, LoopType.Incremental); // Continuous rotation
                    coin.transform.DOScale(1.5f, duration / 2).SetEase(Ease.OutQuad); // Scale up initially
                })
                .OnComplete(() =>
                {
                    Debug.Log("Coin reached target!");
                    coin.transform.DOKill(); // Stop rotation and scale animations
                    coin.transform.DOScale(1f, 0.2f).SetEase(Ease.InQuad); // Scale back down quickly
                    Destroy(coin); // Optionally destroy the coin after reaching the target
                });
        }



    }
}
