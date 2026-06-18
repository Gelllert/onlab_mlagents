public class InputSystemManager 
{
    public InputSystem_Actions IO { get; }    
    private static  InputSystemManager _instance;
    public static InputSystemManager Instance => _instance ??= new InputSystemManager();
    
    private InputSystemManager()
    {
        IO = new InputSystem_Actions();
        IO.Enable();
    }
}
