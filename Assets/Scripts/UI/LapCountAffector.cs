using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LapCountAffector : MonoBehaviour
{
    public TextMeshProUGUI LapText ;
    public int LapCount = 1;
    private void Awake()
    {
        LapText.text = LapCount.ToString();
        GameManager.Instance.lapsToWin = LapCount;
    }
    public void OnIncrease()
    {
        if(LapCount < 10)
            LapText.text = (++LapCount).ToString();
        GameManager.Instance.lapsToWin = LapCount;
    }

    public void OnDecrease()
    {
        if(LapCount > 1)
            LapText.text = (--LapCount).ToString();
        GameManager.Instance.lapsToWin = LapCount;
    }
}
