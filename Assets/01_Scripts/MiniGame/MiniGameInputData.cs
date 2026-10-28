using Fusion;
using UnityEngine;

public struct MiniGameInputData : INetworkInput
{
    public Vector2 Move;
    public NetworkBool Jump;
    public NetworkBool DropObstacle;
}