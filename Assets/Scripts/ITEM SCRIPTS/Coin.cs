using System;
using UnityEngine;

public class Coin : MonoBehaviour, IItem
{
    public static event Action<int> OnCoinCollect;
    public int worth = 5;

    public void Collect()
    {
        // Safe invocation
        OnCoinCollect?.Invoke(worth);
        Destroy(gameObject);
    }
}
