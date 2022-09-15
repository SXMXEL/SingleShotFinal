using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Pause : MonoBehaviour
{
    public static bool IsPaused = false;
    [SerializeField] private GameObject _pauseTab;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _quitButton;
    
    private bool _disconnecting = false;

    private void Awake()
    {
        _resumeButton.onClick.RemoveAllListeners();
        _resumeButton.onClick.AddListener(TogglePause);
        _quitButton.onClick.RemoveAllListeners();
        _quitButton.onClick.AddListener(Quit);
    }


    public void TogglePause()
    {
        if (_disconnecting)
        {
            return;
        }

        IsPaused = !IsPaused;
        
        _pauseTab.SetActive(IsPaused);
        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Confined;
        Cursor.visible = IsPaused;
    }

    private void Quit()
    {
        _disconnecting = true;
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(0);
    }

}
