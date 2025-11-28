using Unity.Netcode;
using UnityEngine;

public struct BallNetworkState : INetworkSerializable
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 LinearVelocity;
    public Vector3 AngularVelocity;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
        serializer.SerializeValue(ref LinearVelocity);
        serializer.SerializeValue(ref AngularVelocity);
    }
}