using Fusion;
using UnityEngine;

public struct DodgeItInputData : INetworkInput
{
    public Vector2 Move;
    public NetworkBool Jump;
    public NetworkBool DropObstacle;
}