using UnityEngine;
using Unity.Services.Multiplayer;

[CreateAssetMenu(fileName = "SessionHolder", menuName = "Scriptable Objects/SessionHolder")]
public class SessionHolder : ScriptableObject
{
    // stores the session information
    public ISession ActiveSession;
}
