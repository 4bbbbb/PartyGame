using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorBlockManager : MonoBehaviour
{
    [Header("<< Round >>")]
    [SerializeField] private float moveTime = 3f;
    [SerializeField] private float roundEndDelay = 0f;

    [Header("<< Monitor >>")]
    [SerializeField] private MonitorUI monitorUI;

    [Header("<< Blocks >>")]
    [SerializeField] private ColorBlock[] blocks;

    [Header("<< Countdown >>")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownTime = 1f;

    private List<BlockCondition> currentConditions = new List<BlockCondition>();

    private int roundIndex = 0;

    private bool isGameRunning = false;


    private void Update()
    {
        StartGame();
    }

    public void StartGame()
    {
        if (isGameRunning)
            return;

        StartCoroutine(GameRoutine());
    }

    #region < GameRoutine >

    private IEnumerator GameRoutine()
    {
        isGameRunning = true;
        roundIndex = 0;


        countdownText.gameObject.SetActive(true);

        countdownText.text = "3";
        yield return new WaitForSeconds(countdownTime);

        countdownText.text = "2";
        yield return new WaitForSeconds(countdownTime);

        countdownText.text = "1";
        yield return new WaitForSeconds(countdownTime);

        countdownText.text = "START";
        yield return new WaitForSeconds(1.5f);

        countdownText.gameObject.SetActive(false);       


        while (true)
        {          
            int conditionCount = GetConditionCount();

            currentConditions = GetRandomConditions(conditionCount);

            monitorUI.ShowConditions(currentConditions);           

            yield return new WaitForSeconds(moveTime);
           

            CheckBlocks();


            yield return new WaitForSeconds(1.7f);
            

            if (roundEndDelay > 0f)
                yield return new WaitForSeconds(roundEndDelay);

            roundIndex++;
        }
    }
    #endregion


    #region < Condition >   

    private int GetConditionCount()
    {        
        if (roundIndex == 0)
            return 1;

       
        if (roundIndex == 1)
            return 1;

       
        if (roundIndex == 2)
            return 2;

       
        if (roundIndex == 3)
            return 2;

        
        if (roundIndex == 4)
            return 3;
       
        return Random.Range(1, 4);
    }    

    private List<BlockCondition> GetRandomConditions(int conditionCount)
    {
        List<BlockCondition> conditions =
            new List<BlockCondition>();

        List<int> groups = new List<int>()
        {
            0,
            1,
            2,
            3
        };


        for (int i = 0; i < groups.Count; i++)
        {
            int randomIndex = Random.Range(i, groups.Count);

            int temp = groups[i];

            groups[i] = groups[randomIndex];

            groups[randomIndex] = temp;
        }


        // 필요한 개수만큼 조건 생성
        for (int i = 0; i < conditionCount; i++)
        {
            int groupIndex = groups[i];

            ConditionType type =
                Random.value < 0.5f
                ? ConditionType.Color
                : ConditionType.Letter;


            conditions.Add(
                new BlockCondition(
                    type,
                    groupIndex
                )
            );
        }

        return conditions;
    }

    private void CheckBlocks()
    {
        foreach (ColorBlock block in blocks)
        {
            foreach (BlockCondition condition in currentConditions)
            {
                if (IsMatch(block, condition))
                {
                    block.Fall();
                    break;
                }
            }
        }
    }
    
    private bool IsMatch(
        ColorBlock block,
        BlockCondition condition)
    {
        switch (condition.type)
        {
            case ConditionType.Color:

                return IsColorMatch(
                    block.BlockColor,
                    condition.groupIndex
                );


            case ConditionType.Letter:

                return IsLetterMatch(
                    block.BlockLetter,
                    condition.groupIndex
                );
        }

        return false;
    }


    private bool IsColorMatch(BlockColor color, int groupIndex)
    {
        return groupIndex switch
        {
            0 => color == BlockColor.Red,
            1 => color == BlockColor.Green,
            2 => color == BlockColor.Blue,
            3 => color == BlockColor.Yellow,

            _ => false
        };
    }


    private bool IsLetterMatch(BlockLetter letter, int groupIndex)
    {
        return groupIndex switch
        {
            0 => letter == BlockLetter.A,
            1 => letter == BlockLetter.B,
            2 => letter == BlockLetter.D,
            3 => letter == BlockLetter.C,

            _ => false
        };
    }

    #endregion
}