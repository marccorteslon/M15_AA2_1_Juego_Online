using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputSystem_Actions actions;
    public static InputManager instance;
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
            actions = new InputSystem_Actions();
            actions.Enable();
            DontDestroyOnLoad(this);
        }
        else
        {
            DestroyImmediate(this);
        }
    }
}
