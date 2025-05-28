//using Photon.Pun;
using System.Collections;
using UnityEngine;

public class DiceAnimationController : MonoBehaviour
{
    [SerializeField] private GameObject diceAnimationGobj;
    [SerializeField] private SpriteRenderer numberSprite;
    [SerializeField] private Sprite[] numberedSprites;
   // private PhotonView photonView;

    private void Awake()
    {
       // photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        HideDice();
    }


    public void AnimateAndShowGotNumber(int generatedNum, float animationTime)
    {
        if(DataManager.Instance.GameType == GameType.Multiplayer)
        {
           // photonView.RPC(nameof(AnimateAndShow), RpcTarget.AllBuffered, generatedNum, animationTime);
            return;
        }

        AnimateAndShow(generatedNum, animationTime);
    }

   // [PunRPC]
    private void AnimateAndShow(int generatedNum, float animationTime)
    {
        animateAndShowCoroutine ??= StartCoroutine(AnimateAndShowCoroutine(generatedNum, animationTime));
    }

    private Coroutine animateAndShowCoroutine;
    private IEnumerator AnimateAndShowCoroutine(int generatedNum, float animationTime)
    {
        HideDice();
        diceAnimationGobj.SetActive(true);
        yield return new WaitForSeconds(animationTime);
        numberSprite.sprite = numberedSprites[generatedNum];
        diceAnimationGobj.SetActive(false);
        numberSprite.gameObject.SetActive(true);

        animateAndShowCoroutine = null;
    }

    public void HideDice()
    {
        //if (DataManager.Instance.GameType == GameType.Multiplayer)
        //{
        //    photonView.RPC(nameof(HideDiceRPC), RpcTarget.All);
        //    return;
        //}

        HideDiceRPC();

    }

   // [PunRPC]
    private void HideDiceRPC()
    {
        numberSprite.gameObject.SetActive(false);
        diceAnimationGobj.SetActive(false);
    }
}
