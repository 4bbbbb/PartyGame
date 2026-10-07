using Fusion;
using UnityEngine;

public enum ColorBlockInputButton
{
    Jump
}

public struct ColorBlockInputData : INetworkInput
{
    public Vector2 Move;
    public NetworkButtons Buttons;
}