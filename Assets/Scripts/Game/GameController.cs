using UnityEngine;

public class GameController : MonoBehaviour
{
    public void SetupProject()
    {
#if UNITY_EDITOR
        // Opens the menu / death screen / world border / Library settings
        UnityEditor.EditorApplication.ExecuteMenuItem("Game/Settings and Library");
#endif
        Debug.Log("🚀 Game settings opened. Menu, death screen, world border and Library are set up automatically when you press Play.");
    }
}