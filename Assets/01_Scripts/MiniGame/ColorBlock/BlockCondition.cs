public enum BlockColor
{
    Red,
    Green,
    Blue,
    Yellow
}

public enum BlockLetter
{
    A,
    B,
    C,
    D
}

public enum ConditionType
{
    Color,
    Letter
}

[System.Serializable]
public struct BlockCondition
{
    public ConditionType type;
    public int groupIndex;

    public BlockCondition(ConditionType type, int groupIndex)
    {
        this.type = type;
        this.groupIndex = groupIndex;
    }
}