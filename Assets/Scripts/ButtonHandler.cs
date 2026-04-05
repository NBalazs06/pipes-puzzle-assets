using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Grid grid;

    void Awake()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i;
            levelButtons[i].onClick.AddListener(() => {
                for (int j = 0; j < levelButtons.Length; j++)
                {
                    levelButtons[j].GetComponent<Image>().color = j == idx ? Color.yellow : Color.white;
                }

                grid.LoadLevel(idx);
            });
        }
    }

    private void Start()
    {
        levelButtons[0].onClick.Invoke();
    }
}
