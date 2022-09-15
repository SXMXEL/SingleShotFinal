using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Launcher))]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Launcher _launcher;
    [SerializeField] private Button b_join;
    [SerializeField] private Button b_create;
    [SerializeField] private Button b_quit;

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        Pause.IsPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Init()
    {
        b_join.onClick.RemoveAllListeners();
        b_join.onClick.AddListener(_launcher.TabOpenRooms);
        b_create.onClick.RemoveAllListeners();
        b_create.onClick.AddListener(_launcher.TabOpenCreate);
        b_quit.onClick.RemoveAllListeners();
        b_quit.onClick.AddListener(QuitGame);
    }
    
    private void QuitGame()
    {
        Application.Quit();
    }
}
