using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using TMPro;
using System;
public class FusionConnectionHandler : MonoBehaviour
{
    private NetworkRunner _runner;
    [Header("UI References")]
    public TMP_InputField roomCodeInput;
    public TMP_InputField playerNameInput;

    private NetworkSceneManagerDefault _sceneManager;
    public GameObject connectionUiRoot;
    public GameObject leaveRoomButton;

    private bool _callbacksRegistered;

    void Awake()
    {
        SetConnectedUi(false);
    }

    private void CacheLocalPlayerName()
    {
        string uiName = string.Empty;
        if (playerNameInput != null)
        {
            uiName = playerNameInput.text;
        }
        else if (roomCodeInput != null)
        {
            uiName = roomCodeInput.text;
        }
       LocalPlayerProfile.SetName(uiName);
    }

    private void SetConnectedUi(bool isConnected)
    {
        if (connectionUiRoot != null)
        {
            connectionUiRoot.SetActive(!isConnected);
        }
        if (leaveRoomButton != null)
        {
            leaveRoomButton.SetActive(isConnected);
        }
    }

    private void EnsureRunner()
    {
        if (_runner != null && _sceneManager != null)
        {
            return;
        }
        var runnerGo = new GameObject("FusionRunner");
        DontDestroyOnLoad(runnerGo);
        _runner = runnerGo.AddComponent<NetworkRunner>();
        _sceneManager = runnerGo.AddComponent<NetworkSceneManagerDefault>();
        _callbacksRegistered = false;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void JoinAsHost()
    {
        CacheLocalPlayerName();
        string roomName = BuildRoomName();
        StartGame(GameMode.Host, roomName);
    }
    public void JoinAsClient()
    {
        CacheLocalPlayerName();
        string roomName = BuildRoomName();
        StartGame(GameMode.Client, roomName);
    }
    public void JoinAsAuto()
    {
        CacheLocalPlayerName();
        string roomName = BuildRoomName();
        StartGame(GameMode.AutoHostOrClient, roomName);
    }

    public async void StartGame(GameMode mode, string roomName)
    {
        EnsureRunner();
        RegisterRunnerCallbacks();
        _runner.ProvideInput = true;
        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().
        buildIndex), LoadSceneMode.Additive);
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            Scene = sceneInfo,
            SceneManager =

        gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        if (result.Ok)
        {
            Debug.Log($"Joined as {mode} in room: {roomName}");
            SetConnectedUi(true);
        }
        else
        {
            Debug.LogError($"Error: {result.ShutdownReason} |{ result.ErrorMessage}");
            SetConnectedUi(false);
        }
    }

    private void RegisterRunnerCallbacks()
    {
        if (_runner == null || _callbacksRegistered)
        {
            return;
        }
        var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var behaviour in allBehaviours)
        {
            if (behaviour is INetworkRunnerCallbacks callbacks)
            {
                _runner.AddCallbacks(callbacks);
            }
        }
        _callbacksRegistered = true;
    }

    public async void LeaveRoom()
    {
        if (_runner == null)
        {
            SetConnectedUi(false);
            return;
        }
        await _runner.Shutdown();
        if (_runner != null && _runner.gameObject != null)
        {
            Destroy(_runner.gameObject);
        }
        _runner = null;
        _sceneManager = null;
        _callbacksRegistered = false;
        SetConnectedUi(false);
    }

    private string BuildRoomName()
    {
        if (playerNameInput == null)
        {
            return "AutoRoom";
        }
        string roomName = roomCodeInput != null ? roomCodeInput.text : "AutoRoom";
        if (string.IsNullOrWhiteSpace(roomName))
        {
            roomName = "AutoRoom";
        }
        return roomName.Trim();
    }
}
