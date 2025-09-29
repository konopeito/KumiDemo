using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class HoldToProgress : MonoBehaviour
{
    public float holdDuration = 1.0f;
    public Image fillCircle;

    private float holdTimer = 0;
    private bool isHolding = false;

    public static event Action OnHoldComplete;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            fillCircle.fillAmount = holdTimer / holdDuration;
            if (holdTimer >= holdDuration)
            {
                //Next scene
                OnHoldComplete.Invoke();
                ResetHold();
            }
        }
    }
        public void OnHold(InputAction.CallbackContext context)
        {
            if(context.started)
            {
                isHolding = true;
            }
            else if (context.canceled)
        {
            ResetHold();
        }
        
        }
    public void ResetHold()
    {
        isHolding=false;
        holdTimer = 0;
        fillCircle.fillAmount=0;
    }
    }


