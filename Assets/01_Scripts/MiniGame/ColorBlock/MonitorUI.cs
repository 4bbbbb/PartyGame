using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonitorUI : MonoBehaviour
{
    [Header("<< Case Objects >>")]
    [SerializeField] private GameObject case1;
    [SerializeField] private GameObject case2;
    [SerializeField] private GameObject case3;

    [Header("<< Case 1 >>")]
    [SerializeField] private Image case1Slot;

    [Header("<< Case 2 >>")]
    [SerializeField] private Image[] case2Slots = new Image[2];

    [Header("<< Case 3 >>")]
    [SerializeField] private Image[] case3Slots = new Image[3];

    [Header("<< Color Sprites >>")]
    [SerializeField] private Sprite redSprite;
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite blueSprite;
    [SerializeField] private Sprite yellowSprite;

    [Header("<< Letter Sprites >>")]
    [SerializeField] private Sprite aSprite;
    [SerializeField] private Sprite bSprite;
    [SerializeField] private Sprite cSprite;
    [SerializeField] private Sprite dSprite;


    // ========================================
    // Conditions 표시
    // ========================================

    public void ShowConditions(List<BlockCondition> conditions)
    {
        // 먼저 모든 Case 끄기
        HideAllCases();


        if (conditions == null || conditions.Count == 0)
            return;


        // 조건 개수에 따라 Case 선택
        switch (conditions.Count)
        {
            case 1:

                case1.SetActive(true);

                case1Slot.sprite = GetConditionSprite(conditions[0]);

                break;


            case 2:

                case2.SetActive(true);

                case2Slots[0].sprite =
                    GetConditionSprite(conditions[0]);

                case2Slots[1].sprite =
                    GetConditionSprite(conditions[1]);

                break;


            case 3:

                case3.SetActive(true);

                case3Slots[0].sprite =
                    GetConditionSprite(conditions[0]);

                case3Slots[1].sprite =
                    GetConditionSprite(conditions[1]);

                case3Slots[2].sprite =
                    GetConditionSprite(conditions[2]);

                break;
        }
    }


    // ========================================
    // Condition → Sprite
    // ========================================

    private Sprite GetConditionSprite(BlockCondition condition)
    {
        if (condition.type == ConditionType.Color)
        {
            switch (condition.groupIndex)
            {
                case 0:
                    return redSprite;

                case 1:
                    return greenSprite;

                case 2:
                    return blueSprite;

                case 3:
                    return yellowSprite;
            }
        }
        else if (condition.type == ConditionType.Letter)
        {
            switch (condition.groupIndex)
            {
                case 0:
                    return aSprite;

                case 1:
                    return bSprite;

                case 2:
                    return dSprite;

                case 3:
                    return cSprite;
            }
        }

        return null;
    }


    // ========================================
    // Case 전부 끄기
    // ========================================

    private void HideAllCases()
    {
        case1.SetActive(false);
        case2.SetActive(false);
        case3.SetActive(false);
    }
}