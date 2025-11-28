using UnityEngine;
using UnityEngine.InputSystem;

public class ResetAllBindingsToDefault : MonoBehaviour
{
    public InputActionAsset actions;

    public void ResetAllBindingsThroughUI()
    {
        foreach (var actionMap in actions.actionMaps)
        {
            actionMap.RemoveAllBindingOverrides();

            // save
            var rebinds = actions.SaveBindingOverridesAsJson();
            FBPP.SetString("rebinds", rebinds);
            FBPP.Save();
        }
    }
}