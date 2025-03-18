using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PublicPrizeItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI prizeTxt;
    [SerializeField] private Button selfBtn;


    public void SetPublicPrizeItem(string prizeText, Action<string> onClickAction)
    {
        prizeTxt.text = prizeText;
        
        selfBtn.onClick.RemoveAllListeners();
        selfBtn.onClick.AddListener(() =>
        {
            onClickAction?.Invoke(prizeTxt.text);
        });
    }
}
