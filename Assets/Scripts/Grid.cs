using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

//IDs of cell shapes: 0 -> straight, 1 -> 90 degree turn, 2 -> T shaped, 3 -> endpoint, 4 -> straight water source, 5 -> T shaped water source

public class Grid : MonoBehaviour
{
    [SerializeField] private Sprite white90DegreeTurn;
    [SerializeField] private Sprite blue90DegreeTurn;
    [SerializeField] private Sprite whiteEndpoint;
    [SerializeField] private Sprite blueEndpoint;
    [SerializeField] private Sprite whiteStraight;
    [SerializeField] private Sprite blueStraight;
    [SerializeField] private Sprite waterSourceStraight;
    [SerializeField] private Sprite waterSourceT;
    [SerializeField] private Sprite whiteT;
    [SerializeField] private Sprite blueT;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private SpriteRenderer[] cells1D;

    private readonly SpriteRenderer[,] cells2D = new SpriteRenderer[4, 4];

    //positions correspond to the grid
    private readonly int[,] shapeIdsOfCells = new int[4, 4];

    private string[] levelDefinitions;

    private readonly HashSet<Sprite> blueSprites = new();

    //pipeSpriteLookup guide: [id of shape sprite, 0 and 1 for white and blue respectively]
    private Sprite[,] pipeSpriteLookup;

    /*
    flowDirectionsLookup guide:
    [id of shape sprite][index of the direction you want to check, 0-3 for the directions UP, RIGHT, DOWN, LEFT in this order].
    The value you get is true if water can flow to the given direction from the given shape sprite, it's false otherwise.
    Note that it interprets all shapes with a rotation of 0 degrees.
    */
    bool[][] flowDirectionsLookup;

    //the row and column index of the water source inside of cells
    private int waterSourceRowIdx = -1;
    private int waterSourceColIdx = -1;

    public bool currentLevelComplete = false;
    private float elapsedTime = 0f;

    public void LoadLevel(int levelIdx)
    {
        string[] levelDefinitionSplit = levelDefinitions[levelIdx].Split(",");

        for (int i = 0; i < levelDefinitionSplit.Length; i++)
        {
            String[] idAndRotation = levelDefinitionSplit[i].Split(":");
            int id = int.Parse(idAndRotation[0]);
            float rotation = float.Parse(idAndRotation[1]);
            int rowIdx = i / 4;
            int colIdx = i % 4;

            cells2D[rowIdx, colIdx].sprite = pipeSpriteLookup[id, 0];
            cells2D[rowIdx, colIdx].transform.rotation = Quaternion.Euler(0f, 0f, rotation);

            shapeIdsOfCells[rowIdx, colIdx] = id;

            if (id == 4 || id == 5)
            {
                waterSourceRowIdx = rowIdx;
                waterSourceColIdx = colIdx;
            }
        }

        RecalculateWaterFlow();

        currentLevelComplete = false;
        elapsedTime = 0f;
    }

    void Start()
    {   
        for (int i = 0; i < cells1D.Length; i++)
        {
            SpriteRenderer cell = cells1D[i];
            int rowIdx = i / 4;
            int colIdx = i % 4;

            cells2D[rowIdx, colIdx] = cell;
            cell.sortingLayerName = "Default";
            cell.sortingOrder = 1;
        }
    }

    void Awake()
    {
        levelDefinitions = Resources.Load<TextAsset>("levelDefinitions").text.Split("\n");

        pipeSpriteLookup = new Sprite[,]
        {
            { whiteStraight, blueStraight },
            { white90DegreeTurn, blue90DegreeTurn },
            { whiteT, blueT },
            { whiteEndpoint, blueEndpoint},
            { waterSourceStraight, waterSourceStraight }, //always blue
            { waterSourceT, waterSourceT }, //always blue
        };

        for (int i = 0; i < pipeSpriteLookup.GetLength(0); i++)
            blueSprites.Add(pipeSpriteLookup[i, 1]);

        flowDirectionsLookup = new bool[][]
        {
            new bool[] { true, false, true, false },
            new bool[] { true, true, false, false },
            new bool[] { true, true, false, true },
            new bool[] { true, false, false, false },
            new bool[] { true, false, true, false },
            new bool[] { true, true, false, true }
        };
    }

    void Update()
    {
        if (!currentLevelComplete)
        {
            elapsedTime += Time.deltaTime;
            timerText.text = GetFormattedTime(elapsedTime);
        }
    }

    private string GetFormattedTime(float time)
    {
        int minutes = (int)(time / 60f);
        int seconds = (int)(time % 60f);
        int fractions = (int)((time * 100f) % 100f); //1/100 seconds

        return $"{minutes:00}:{seconds:00}.{fractions:00}";
    }

    public void OnCellClicked(int rowIdx, int colIdx)
    {
        cells2D[rowIdx, colIdx].transform.rotation = Quaternion.Euler(0, 0, (cells2D[rowIdx, colIdx].transform.eulerAngles.z + 90) % 360);
        RecalculateWaterFlow();
    }

    private void RecalculateWaterFlow()
    {
        //make everything white except water source sprites because those are always blue
        for (int r = 0; r < cells2D.GetLength(0); r++)
            for (int c = 0; c < cells2D.GetLength(1); c++)
                cells2D[r, c].sprite = pipeSpriteLookup[shapeIdsOfCells[r, c], 0];

        RecalculateWaterFlowHelper(waterSourceRowIdx, waterSourceColIdx); //begin recursion from the water source

        bool everyCellIsBlue = true;

        for (int r = 0; r < cells2D.GetLength(0); r++)
        {
            for (int c = 0; c < cells2D.GetLength(1); c++)
            {
                if (!blueSprites.Contains(cells2D[r, c].sprite))
                {
                    everyCellIsBlue = false;
                    break;
                }
            }
        }

        if (everyCellIsBlue)
            currentLevelComplete = true;
    }

    private void RecalculateWaterFlowHelper(int rowIdx, int colIdx)
    {
        cells2D[rowIdx, colIdx].sprite = pipeSpriteLookup[shapeIdsOfCells[rowIdx, colIdx], 1]; //color the current cell blue

        int[][] neighborIndices = new int[][]
        {
            new int[] { rowIdx - 1, colIdx }, //UP
            new int[] { rowIdx, colIdx + 1 }, //RIGHT
            new int[] { rowIdx + 1, colIdx }, //DOWN
            new int[] { rowIdx, colIdx - 1 } //LEFT
        };

        // Traverse to neighboring cells if in bounds, unvisited (white), and both cells form a valid pipe connection (they point at each other).
        for (int i = 0; i < neighborIndices.Length; i++)
        {
            int neighborRowIdx = neighborIndices[i][0];
            int neighborColIdx = neighborIndices[i][1];

            if (
                    neighborRowIdx >= 0 && neighborRowIdx <= 3 && neighborColIdx >= 0 && neighborColIdx <= 3 &&
                    !blueSprites.Contains(cells2D[neighborRowIdx, neighborColIdx].sprite) &&
                    CheckFlowFromCellToDirection(rowIdx, colIdx, i) && CheckFlowFromCellToDirection(neighborRowIdx, neighborColIdx, (i + 2) % 4)
                )
                RecalculateWaterFlowHelper(neighborRowIdx, neighborColIdx);
        }
    }

    //direction: numbers 0-3 for the directions UP, RIGHT, DOWN, LEFT in this order
    private bool CheckFlowFromCellToDirection(int rowIdx, int colIdx, int direction)
    {
        int rotationStepsCounterClockwise = ((int)cells2D[rowIdx, colIdx].transform.eulerAngles.z / 90) % 4; //rotation in 90-degree steps
        int rotationStepsClockwise = (4 - rotationStepsCounterClockwise) % 4; //convert to clockwise steps to match the UP, RIGHT, DOWN, LEFT order
        return flowDirectionsLookup[shapeIdsOfCells[rowIdx, colIdx]][(direction - rotationStepsClockwise + 4) % 4];
    }
}