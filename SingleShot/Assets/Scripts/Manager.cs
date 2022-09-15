using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerInfo
{
    public ProfileData Profile;
    public int Actor;
    public bool AwayTeam;
    public short Kills;
    public short Deaths;

    public PlayerInfo(ProfileData profile, int actor, short kills, short deaths, bool awayTeam)
    {
        Profile = profile;
        Actor = actor;
        Kills = kills;
        Deaths = deaths;
        AwayTeam = awayTeam;
    }
}

public enum GameState
{
    Waiting = 0,
    Starting = 1,
    Playing = 2,
    Ending = 3
}

public enum EventCodes : byte
{
    NewPlayer,
    UpdatePlayers,
    ChangeStat,
    NewMatch,
    RefreshTimer
}

public class Manager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    #region Variables

    [Header("Settings")] [SerializeField] private int _killCount = 3;
    [SerializeField] private int _matchLength = 180;
    [SerializeField] private bool Perpetual;

    [Header("UI")] [SerializeField] private HeadsUpDisplay _display;
    [SerializeField] private TextMeshProUGUI ui_kills;
    [SerializeField] private TextMeshProUGUI ui_deaths;
    [SerializeField] private TextMeshProUGUI ui_timer;
    [SerializeField] private Transform ui_leaderboard;
    [SerializeField] private Transform ui_endGame;

    [Header("Other references")] [SerializeField]
    private GameObject _mapCamera;

    [SerializeField] private string _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;

    private const int MAIN_MENU = 0;
    private int _myInd;
    private bool _playerAdded;
    private int _currentMatchTime;
    private Coroutine _timerCoroutine;
    private GameState _state = GameState.Waiting;
    private List<PlayerInfo> _playerInfos = new List<PlayerInfo>();

    #endregion


    #region Monobehavior Callbacks

    private void Start()
    {
        _mapCamera.SetActive(false);
        ValidateConnection();
        InitializeUI();
        InitializeTimer();
        NewPlayer_S(Launcher.MyProfile);

        if (PhotonNetwork.IsMasterClient)
        {
            _playerAdded = true;
            Spawn();
        }
    }

    private void Update()
    {
        if (_state == GameState.Ending)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (ui_leaderboard.gameObject.activeSelf)
            {
                ui_leaderboard.gameObject.SetActive(false);
            }
            else
            {
                OpenLeaderboard(ui_leaderboard);
            }
        }
    }

    private new void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private new void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    #endregion

    #region Photon

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200)
        {
            return;
        }

        EventCodes e = (EventCodes) photonEvent.Code;
        object[] o = (object[]) photonEvent.CustomData;

        switch (e)
        {
            case EventCodes.NewPlayer:
                NewPlayer_R(o);
                break;
            case EventCodes.UpdatePlayers:
                UpdatePlayers_R(o);
                break;
            case EventCodes.ChangeStat:
                ChangeStat_R(o);
                break;
            case EventCodes.NewMatch:
                NewMatch_R();
                break;
            case EventCodes.RefreshTimer:
                RefreshTimer_R(o);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(MAIN_MENU);
    }

    #endregion

    #region Methods

    private void InitializeUI()
    {
        //TODO remove this
        // ui_kills = GameObject.Find("HUD/Stats/Kills/Text").GetComponent<TextMeshProUGUI>();
        // ui_deaths = GameObject.Find("HUD/Stats/Deaths/Text").GetComponent<TextMeshProUGUI>();
        // ui_timer = GameObject.Find("HUD/Timer/Text").GetComponent<TextMeshProUGUI>();
        // ui_leaderboard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;
        // ui_endGame = GameObject.Find("Canvas").transform.Find("End Game").transform;
        RefreshMyStats();
    }

    private void RefreshMyStats()
    {
        if (_playerInfos.Count > _myInd)
        {
            ui_kills.text = $"{_playerInfos[_myInd].Kills} kills";
            ui_deaths.text = $"{_playerInfos[_myInd].Deaths} deaths";
        }
        else
        {
            ui_kills.text = "0 kills";
            ui_deaths.text = "0 deaths";
        }
    }

    public void Spawn()
    {
        Transform t_spawn = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

        GameObject newPlayer;

        if (PhotonNetwork.IsConnected)
        {
            newPlayer = PhotonNetwork.Instantiate(_playerPrefab, t_spawn.position, t_spawn.rotation);
        }
        else
        {
            Debug.Log("Working");
            newPlayer = PhotonNetwork.Instantiate(_playerPrefab, t_spawn.position, t_spawn.rotation);
        }

        newPlayer.GetComponent<Player>().Init(_display);
    }

    private void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected)
        {
            return;
        }

        SceneManager.LoadScene(MAIN_MENU);
    }

    private void StateCheck()
    {
        if (_state == GameState.Ending)
        {
            EndGame();
        }
    }

    private void ScoreCheck()
    {
        bool detectWin = false;

        //Check to see if any player has met the win condition

        foreach (var info in _playerInfos)
        {
            if (info.Kills > _killCount)
            {
                detectWin = true;
                break;
            }
        }

        //Did we find the winner

        if (detectWin)
        {
            //Are we master client? is the game still going?

            if (PhotonNetwork.IsMasterClient && _state != GameState.Ending)
            {
                UpdatePlayers_S((int) GameState.Ending, _playerInfos);
            }
        }
    }

    private void InitializeTimer()
    {
        _currentMatchTime = _matchLength;
        RefreshTimerUI();

        if (PhotonNetwork.IsMasterClient)
        {
            _timerCoroutine = StartCoroutine(Timer());
        }
    }

    private void RefreshTimerUI()
    {
        string minutes = (_currentMatchTime / 60).ToString("00");
        string seconds = (_currentMatchTime % 60).ToString("00");
        ui_timer.text = $"{minutes}:{seconds}";
    }

    private void EndGame()
    {
        _state = GameState.Ending;

        //Set timer to 0
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
        }

        _currentMatchTime = 0;
        RefreshTimerUI();

        //disable room
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();

            if (!Perpetual)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
        }

        //Activate map camera
        _mapCamera.SetActive(true);

        //Show end game ui
        ui_endGame.gameObject.SetActive(true);
        OpenLeaderboard(ui_endGame.Find("Leaderboard"));

        //Wait x seconds and return to main menu
        StartCoroutine(End(6f));
    }

    private void OpenLeaderboard(Transform p_lb)
    {
        if (GameSettings.GameMode == GameMode.FFA)
        {
            p_lb = p_lb.GetChild(0);
        }
        else if (GameSettings.GameMode == GameMode.TDM)
        {
            p_lb = p_lb.GetChild(1);
        }

        //clean up
        for (int i = 2; i < p_lb.childCount; i++)
        {
            Destroy(p_lb.GetChild(i).gameObject);
        }

        //Set scores
        int t_homeKills = 0;
        int t_awayKills = 0;

        foreach (var playerInfo in _playerInfos)
        {
            if (playerInfo.AwayTeam)
            {
                t_awayKills += playerInfo.Kills;
            }
            else if (!playerInfo.AwayTeam)
            {
                t_homeKills += playerInfo.Kills;
            }
        }

        //Set up
        p_lb.GetComponent<Leaderboard>().Init(
            GameSettings.GameMode == GameMode.TDM,
            Enum.GetName(typeof(GameMode), GameSettings.GameMode)?.ToUpper(),
            SceneManager.GetActiveScene().name,
            t_homeKills,
            t_awayKills);

        // TODO remove this
        // if (GameSettings.GameMode == GameMode.TDM)
        // {
        //     p_lb.Find("Header/Score/Home").GetComponent<TextMeshProUGUI>().text = t_homeKills.ToString();
        //     p_lb.Find("Header/Score/Away").GetComponent<TextMeshProUGUI>().text = t_awayKills.ToString();
        // }

        //Cache prefab
        GameObject playerCard = p_lb.GetChild(1).gameObject;
        playerCard.SetActive(false);

        //Sort
        List<PlayerInfo> sorted = SortPlayers(_playerInfos);

        //Display
        bool t_alternateColors = false;

        foreach (var a in sorted)
        {
            GameObject newCard = Instantiate(playerCard, p_lb);


            if (t_alternateColors)
            {
                newCard.GetComponent<Image>().color = new Color(0, 0, 180);
            }

            t_alternateColors = !t_alternateColors;
            newCard.GetComponent<PlayerCard>().Init(
                GameSettings.GameMode == GameMode.TDM,
                a.AwayTeam,
                a.Profile.Username,
                a.Profile.Level,
                a.Kills * 100,
                a.Kills,
                a.Deaths);

            //TODO remove
            // if (GameSettings.GameMode == GameMode.TDM)
            // {
            //     newCard.transform.Find("Home").gameObject.SetActive(!a.AwayTeam);
            //     newCard.transform.Find("Away").gameObject.SetActive(a.AwayTeam);
            // }
            // newCard.transform.Find("Username").GetComponent<TextMeshProUGUI>().text = a.Profile.Username;
            // newCard.transform.Find("Level").GetComponent<TextMeshProUGUI>().text = a.Profile.Level.ToString("00");
            // newCard.transform.Find("Score Value").GetComponent<TextMeshProUGUI>().text = (a.Kills * 100).ToString();
            // newCard.transform.Find("Kills Value").GetComponent<TextMeshProUGUI>().text = a.Kills.ToString();
            // newCard.transform.Find("Deaths Value").GetComponent<TextMeshProUGUI>().text = a.Deaths.ToString();

            newCard.SetActive(true);
        }

        p_lb.gameObject.SetActive(true);
        p_lb.parent.gameObject.SetActive(true);
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> p_info)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        if (GameSettings.GameMode == GameMode.FFA)
        {
            while (sorted.Count < p_info.Count)
            {
                //Set Defaults
                short highest = -1;
                PlayerInfo selection = p_info[0];

                // Grab next highest player
                foreach (var playerInfo in p_info)
                {
                    if (sorted.Contains(playerInfo))
                    {
                        continue;
                    }

                    if (playerInfo.Kills > highest)
                    {
                        selection = playerInfo;
                        highest = playerInfo.Kills;
                    }
                }

                sorted.Add(selection);
            }
        }
        else if (GameSettings.GameMode == GameMode.TDM)
        {
            List<PlayerInfo> homeSorted = new List<PlayerInfo>();
            List<PlayerInfo> awaySorted = new List<PlayerInfo>();

            int homeSize = 0;
            int awaySize = 0;

            foreach (var playerInfo in p_info)
            {
                if (playerInfo.AwayTeam)
                {
                    awaySize++;
                }
                else
                {
                    homeSize++;
                }
            }

            while (homeSorted.Count < homeSize)
            {
                //Set Defaults
                short highest = -1;
                PlayerInfo selection = p_info[0];

                // Grab next highest player
                foreach (var playerInfo in p_info)
                {
                    if (playerInfo.AwayTeam)
                    {
                        continue;
                    }

                    if (homeSorted.Contains(playerInfo))
                    {
                        continue;
                    }

                    if (playerInfo.Kills > highest)
                    {
                        selection = playerInfo;
                        highest = playerInfo.Kills;
                    }
                }

                homeSorted.Add(selection);
            }

            while (awaySorted.Count < awaySize)
            {
                //Set Defaults
                short highest = -1;
                PlayerInfo selection = p_info[0];

                // Grab next highest player
                foreach (var playerInfo in p_info)
                {
                    if (!playerInfo.AwayTeam)
                    {
                        continue;
                    }

                    if (awaySorted.Contains(playerInfo))
                    {
                        continue;
                    }

                    if (playerInfo.Kills > highest)
                    {
                        selection = playerInfo;
                        highest = playerInfo.Kills;
                    }
                }

                awaySorted.Add(selection);
            }

            sorted.AddRange(homeSorted);
            sorted.AddRange(awaySorted);
        }

        return sorted;
    }

    #endregion

    #region Events

    private bool CalculateTeam()
    {
        return PhotonNetwork.CurrentRoom.PlayerCount % 2 == 0;
    }

    private void NewPlayer_S(ProfileData profile)
    {
        object[] package = new object[7];

        package[0] = profile.Username;
        package[1] = profile.Level;
        package[2] = profile.XP;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short) 0;
        package[5] = (short) 0;
        package[6] = CalculateTeam();

        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.NewPlayer,
            package,
            new RaiseEventOptions {Receivers = ReceiverGroup.MasterClient},
            new SendOptions {Reliability = true});
    }

    private void NewPlayer_R(object[] data)
    {
        PlayerInfo p = new PlayerInfo(
            new ProfileData(
                (string) data[0],
                (int) data[1],
                (int) data[2]
            ),
            (int) data[3],
            (short) data[4],
            (short) data[5],
            (bool) data[6]
        );

        _playerInfos.Add(p);

        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            //if so resync our local player information
            player.GetComponent<Player>().TrySync();
        }

        UpdatePlayers_S((int) _state, _playerInfos);
    }


    private void UpdatePlayers_S(int state, List<PlayerInfo> info)
    {
        object[] package = new object[info.Count + 1];

        package[0] = state;
        for (int i = 0; i < info.Count; i++)
        {
            object[] piece = new object[7];

            piece[0] = info[i].Profile.Username;
            piece[1] = info[i].Profile.Level;
            piece[2] = info[i].Profile.XP;
            piece[3] = info[i].Actor;
            piece[4] = info[i].Kills;
            piece[5] = info[i].Deaths;
            piece[6] = info[i].AwayTeam;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.UpdatePlayers,
            package,
            new RaiseEventOptions {Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true});
    }

    private void UpdatePlayers_R(object[] data)
    {
        _state = (GameState) data[0];

        //Check if there is a new player
        if (_playerInfos.Count > data.Length - 1)
        {
            foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
            {
                //if so resync our local player information
                player.GetComponent<Player>().TrySync();
            }
        }

        _playerInfos = new List<PlayerInfo>();

        for (int i = 1; i < data.Length; i++)
        {
            object[] extract = (object[]) data[i];

            PlayerInfo p = new PlayerInfo(
                new ProfileData(
                    (string) extract[0],
                    (int) extract[1],
                    (int) extract[2]
                ),
                (int) extract[3],
                (short) extract[4],
                (short) extract[5],
                (bool) extract[6]
            );

            _playerInfos.Add(p);
            if (PhotonNetwork.LocalPlayer.ActorNumber == p.Actor)
            {
                _myInd = i - 1;

                //if we are waiting to be added to the game then spawn us in

                if (!_playerAdded)
                {
                    _playerAdded = true;
                    GameSettings.IsAwayTeam = p.AwayTeam;
                    Spawn();
                }
            }
        }

        StateCheck();
    }

    public void ChangeStat_S(int actor, byte stat, byte amt)
    {
        object[] package = {actor, stat, amt};

        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.ChangeStat,
            package,
            new RaiseEventOptions {Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true});
    }

    private void ChangeStat_R(object[] data)
    {
        int actor = (int) data[0];
        byte stat = (byte) data[1];
        byte amt = (byte) data[2];

        for (int i = 0; i < _playerInfos.Count; i++)
        {
            if (_playerInfos[i].Actor == actor)
            {
                switch (stat)
                {
                    case 0:
                        _playerInfos[i].Kills += amt;
                        Debug.Log($"Player {_playerInfos[i].Profile.Username} : kills = {_playerInfos[i].Kills}");
                        break;
                    case 1:
                        _playerInfos[i].Deaths += amt;
                        Debug.Log($"Player {_playerInfos[i].Profile.Username} : deaths = {_playerInfos[i].Deaths}");
                        break;
                }

                if (i == _myInd)
                {
                    RefreshMyStats();
                }

                if (ui_leaderboard.gameObject.activeSelf)
                {
                    OpenLeaderboard(ui_leaderboard);
                }

                break;
            }
        }

        ScoreCheck();
    }

    private void NewMatch_S()
    {
        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.NewMatch,
            null,
            new RaiseEventOptions {Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true}
        );
    }

    private void NewMatch_R()
    {
        _state = GameState.Waiting;
        _mapCamera.SetActive(false);
        ui_endGame.gameObject.SetActive(false);

        // reset scores
        foreach (var info in _playerInfos)
        {
            info.Kills = 0;
            info.Deaths = 0;
        }

        // reset
        RefreshMyStats();

        InitializeTimer();

        Spawn();
    }

    private void RefreshTimer_S()
    {
        object[] package = {_currentMatchTime};

        PhotonNetwork.RaiseEvent(
            (byte) EventCodes.RefreshTimer,
            package,
            new RaiseEventOptions {Receivers = ReceiverGroup.All},
            new SendOptions {Reliability = true});
    }

    private void RefreshTimer_R(object[] data)
    {
        _currentMatchTime = (int) data[0];
        RefreshTimerUI();
    }

    #endregion

    #region Couroutins

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(1f);

        _currentMatchTime -= 1;

        if (_currentMatchTime <= 0)
        {
            _timerCoroutine = null;
            UpdatePlayers_S((int) GameState.Ending, _playerInfos);
        }
        else
        {
            RefreshTimer_S();
            _timerCoroutine = StartCoroutine(Timer());
        }
    }

    private IEnumerator End(float p_wait)
    {
        yield return new WaitForSeconds(p_wait);

        if (Perpetual)
        {
            // new match
            if (PhotonNetwork.IsMasterClient)
            {
                NewMatch_S();
            }
        }
        else
        {
            //Disconnect
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
    }

    #endregion
}