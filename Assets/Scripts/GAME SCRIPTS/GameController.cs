using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    int progressAmount;
    public Slider progressSlider;

    public GameObject player;
    public GameObject LoadCanvas;
    public List<GameObject> levels;
    private int currentLevelIndex = 0;


    // Start is called before the first frame update
    void Start()
    {
        progressAmount = 0;
        progressSlider.value = 0;
        Coin.OnCoinCollect += IncreaseProgressAmount;
        HoldToProgress.OnHoldComplete += LoadNextLevel;
        LoadCanvas.SetActive(false);
        
    }
    void IncreaseProgressAmount(int amount)
    {
        progressAmount += amount;
        progressSlider.value = progressAmount;
        if(progressAmount >= 100)
        {
            //Level completion
            LoadCanvas.SetActive(true);
            Debug.Log("Level Clear!");
            
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    void LoadNextLevel()
    {
        int nextLevelIndex = (currentLevelIndex == levels.Count - 1) ? 0 : currentLevelIndex+1;
        LoadCanvas.SetActive(false);
        levels[currentLevelIndex].gameObject.SetActive(false);
        levels[nextLevelIndex].gameObject.SetActive(true);
        player.transform.position = new Vector3(0,0,0);
        currentLevelIndex = nextLevelIndex;
        progressAmount = 0;
        progressSlider.value = 0;
    }
}
