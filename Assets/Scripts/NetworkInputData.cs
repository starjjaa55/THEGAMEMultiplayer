using Fusion;
using UnityEngine;
public struct NetworkInputData : INetworkInput
{
    public Vector2 direction;
    public NetworkButtons buttons;
    public const int BUTTON_ATTACK = 0;
}