using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[System.Serializable]
public class ProfileData
{
    public string Username;
    public int Level;
    public int XP;

    public ProfileData()
    {
        Username = "Default";
        Level = 1;
        XP = 0;
    }

    public ProfileData(string username, int level, int xp)
    {
        Username = username;
        Level = level;
        XP = xp;
    }
}

[System.Serializable]
public class MapData
{
    public string Name;
    public int Scene;
}

public class Launcher : MonoBehaviourPunCallbacks
{
    public static ProfileData MyProfile = new ProfileData();

    [Header("UI")]
    [SerializeField] private TMP_InputField _usernameField;
    [SerializeField] private TMP_InputField _roomNameField;
    [SerializeField] private Slider _maxPlayersSlider;
    [SerializeField] private TextMeshProUGUI _mapValue;
    [SerializeField] private TextMeshProUGUI _modeValue;
    [SerializeField] private TextMeshProUGUI _maxPlayersValue;
    [SerializeField] private TextMeshProUGUI _level;
    [SerializeField] private TextMeshProUGUI _xp;
    [SerializeField] private GameObject _tabMain;
    [SerializeField] private GameObject _tabRooms;
    [SerializeField] private GameObject _tabCreate;
    [SerializeField] private GameObject _buttonRoom;
    [SerializeField] private Button b_closeRoomsList;
    [SerializeField] private Button b_closeCreate;
    [SerializeField] private Button b_create;
    [SerializeField] private Button b_mapSelect;
    [SerializeField] private Button b_modeSelect;
    [SerializeField] private Transform _content;
    
    [Header("Maps")]
    [SerializeField] private MapData[] _maps;

    private int _currentMap;
    private List<RoomInfo> _roomList;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        MyProfile = DataManager.LoadPlayerProfile();
        _usernameField.text = MyProfile.Username;
        _level.text = "Level " + MyProfile.Level;
        _xp.text = "XP " + MyProfile.XP + " / 60";

        Connect();
    }

    private void Start()
    {
        TabOpenMain();
        b_closeRoomsList.onClick.RemoveAllListeners();
        b_closeRoomsList.onClick.AddListener(TabOpenMain);
        b_closeCreate.onClick.RemoveAllListeners();
        b_closeCreate.onClick.AddListener(TabOpenRooms);
        b_create.onClick.RemoveAllListeners();
        b_create.onClick.AddListener(Create);
        b_mapSelect.onClick.RemoveAllListeners();
        b_mapSelect.onClick.AddListener(ChangeMap);
        b_modeSelect.onClick.RemoveAllListeners();
        b_modeSelect.onClick.AddListener(ChangeMode);
        _maxPlayersSlider.onValueChanged.RemoveAllListeners();
        _maxPlayersSlider.onValueChanged.AddListener(ChangeMaxPlayersSlider);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected!");

        PhotonNetwork.JoinLobby();

        base.OnConnectedToMaster();
    }

    public override void OnJoinedRoom()
    {
        StartGame();

        base.OnJoinedRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Create();

        base.OnJoinRandomFailed(returnCode, message);
    }

    public void Connect()
    {
        Debug.Log("Trying to connect... ");
        PhotonNetwork.GameVersion = "0.0.0";
        PhotonNetwork.ConnectUsingSettings();
    }

    // private void Join()
    // {
    //     PhotonNetwork.JoinRandomRoom();
    // }

    private void Create()
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte) _maxPlayersSlider.value, CustomRoomPropertiesForLobby = new[] {"map", "mode"}
        };

        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable
        {
            {"map", _currentMap}, {"mode", (int) GameSettings.GameMode}
        };
        options.CustomRoomProperties = properties;
        PhotonNetwork.CreateRoom(_roomNameField.text, options);
    }

    private void ChangeMap()
    {
        _currentMap++;

        if (_currentMap >= _maps.Length)
        {
            _currentMap = 0;
        }

        _mapValue.text = "Map: " + _maps[_currentMap].Name;
    }

    private void ChangeMode()
    {
        int newMode = (int) GameSettings.GameMode + 1;

        if (newMode >= System.Enum.GetValues(typeof(GameMode)).Length)
        {
            newMode = 0;
        }

        GameSettings.GameMode = (GameMode) newMode;
        _modeValue.text = "Mode: " + System.Enum.GetName(typeof(GameMode), newMode);
    }

    private void ChangeMaxPlayersSlider(float p_value)
    {
        _maxPlayersValue.text = Mathf.RoundToInt(p_value).ToString();
    }

    private void TabCloseAll()
    {
        _tabMain.SetActive(false);
        _tabRooms.SetActive(false);
        _tabCreate.SetActive(false);
    }

    private void TabOpenMain()
    {
        TabCloseAll();
        _tabMain.SetActive(true);
    }

    public void TabOpenRooms()
    {
        TabCloseAll();
        _tabRooms.SetActive(true);
    }

    public void TabOpenCreate()
    {
        TabCloseAll();
        _tabCreate.SetActive(true);

        _roomNameField.text = "";

        _currentMap = 0;
        _mapValue.text = "Map: " + _maps[_currentMap].Name;

        GameSettings.GameMode = 0;
        _modeValue.text = "Mode: " + System.Enum.GetName(typeof(GameMode), (GameMode) 0);

        _maxPlayersSlider.value = _maxPlayersSlider.maxValue;
        _maxPlayersValue.text = Mathf.RoundToInt(_maxPlayersSlider.value).ToString();
    }

    private void ClearRoomsList()
    {
        // Transform content = _tabRooms.transform.Find("ScrollView/Viewport/Content");

        foreach (Transform a in _content)
        {
            Destroy(a.gameObject);
        }
    }

    private void VerifyUsername()
    {
        if (string.IsNullOrEmpty(_usernameField.text) || _usernameField.text == "Default" || _usernameField.text == "Username")
        {
            MyProfile.Username = "NEWBIE_" + Random.Range(100, 999);
        }
        else
        {
            MyProfile.Username = _usernameField.text;
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> p_list)
    {
        _roomList = p_list;
        ClearRoomsList();

        Debug.Log("Loaded rooms @ " + Time.time);
        // _content = _tabRooms.transform.Find("ScrollView/Viewport/Content");

        foreach (var roomInfo in _roomList)
        {
            GameObject newRoomButton = Instantiate(_buttonRoom, _content);
            
            // newRoomButton.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = roomInfo.Name;
            // newRoomButton.transform.Find("Players").GetComponent<TextMeshProUGUI>().text =
            //     roomInfo.PlayerCount + " / " + roomInfo.MaxPlayers;

            var mapName = roomInfo.CustomProperties.ContainsKey("map") ? 
                _maps[(int) roomInfo.CustomProperties["map"]].Name : "-----";
            var rB = newRoomButton.GetComponent<RoomButton>();
            rB.Init(mapName, roomInfo.Name, roomInfo.PlayerCount + " / " + roomInfo.MaxPlayers);

            newRoomButton.GetComponent<Button>().onClick.AddListener(() => 
                JoinRoom(rB));
        }

        base.OnRoomListUpdate(_roomList);
    }

    private void JoinRoom(RoomButton p_button)
    {
        Debug.Log("Joining room @ " + Time.time);

        // string t_roomName = p_button.transform.Find("Name").GetComponent<TextMeshProUGUI>().text;
        
        string t_roomName = p_button.RoomName;

        VerifyUsername();

        RoomInfo roomInfo = null;
        Transform buttonParent = p_button.transform.parent;

        for (int i = 0; i < buttonParent.childCount; i++)
        {
            if (buttonParent.GetChild(i).Equals(p_button.transform))
            {
                roomInfo = _roomList[i];
                break;
            }
        }

        if (roomInfo != null)
        {
            LoadGameSettings(roomInfo);
            PhotonNetwork.JoinRoom(t_roomName);
        }
    }

    private void LoadGameSettings(RoomInfo roomInfo)
    {
        GameSettings.GameMode = (GameMode) roomInfo.CustomProperties["mode"];
        Debug.Log("Current game mode: " + System.Enum.GetName(typeof(GameMode), GameSettings.GameMode));
    }

    private void StartGame()
    {
        VerifyUsername();

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            DataManager.SavePlayerProfile(MyProfile);
            PhotonNetwork.LoadLevel(_maps[_currentMap].Scene);
        }
    }
}