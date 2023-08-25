using TMPro;
using VerySimpleTwitchChat;
using UnityEngine;
using System;
using System.Collections;

public class CounterTwitchGame : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameTMP;
    [SerializeField] private TextMeshProUGUI currentScoreTMP;
    [SerializeField] private TextMeshProUGUI maxScoreTMP;

    [SerializeField] private TextMeshProUGUI usernameTMPVersus;
    [SerializeField] private TextMeshProUGUI currentScoreTMPVersus;
    [SerializeField] private TextMeshProUGUI maxScoreTMPVersus;

    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI tropysTMP;

    private int currentScore;

    private string lastUsername = string.Empty;

    private int currentMaxScore;
    private readonly string maxScoreKey = "maxScore";

    private string currentMaxScoreUsername = "RothioTome";
    private readonly string currentMaxScoreUsernameKey = "currentMaxScoreUsername";
    private readonly string maxScoreUsernameKey = "maxScoreUsername";
    private readonly string TrophysKey = "tropyKey";
    private int tropys;

    private string lastUserIdVIPGranted;
    private readonly string lastUserIdVIPGrantedKey = "lastVIPGranted";

    private string nextPotentialVIP;

    [SerializeField] private GameObject startingCanvas;


    // Código para udate a MULTIPLAYER
    [SerializeField] private string currentDuelPlayer = "";

    [SerializeField] private NumSt multiplayerState = NumSt.NODUEL;
    private int dueltime = 5;     //time in minutes

    private int currentMPMaxScore = 0;
    private int currentMPScore = 0;
    private string currentMPMaxScoreUsername = "";
    private string lastMPusername="";
    [SerializeField] private Animator animator;
    private int totalTimeInSeconds = 0;

    private void Start()
    {
        Application.targetFrameRate = 30;           //set fps 30
        TwitchChat.onTwitchMessageReceived += OnTwitchMessageReceived;
        TwitchChat.onChannelJoined += OnChannelJoined;

        currentMaxScore = PlayerPrefs.GetInt(maxScoreKey);
        currentMaxScoreUsername = PlayerPrefs.GetString(maxScoreUsernameKey, currentMaxScoreUsername);
        lastUserIdVIPGranted = PlayerPrefs.GetString(lastUserIdVIPGrantedKey, string.Empty);
        tropys = PlayerPrefs.GetInt(TrophysKey,0);

        UpdateMaxScoreUI();
        UpdateCurrentScoreUI(lastUsername, currentScore.ToString());
        ResetGame();
        UpdateTrophyUI();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            TwitchChat.JoinChannel("sunavarro");
            Invoke("StartDuel",2f);
        }
    }
    private void OnDestroy()
    {
        TwitchChat.onTwitchMessageReceived -= OnTwitchMessageReceived;
        TwitchChat.onChannelJoined -= OnChannelJoined;
    }

    private void OnTwitchMessageReceived(Chatter chatter)
    {
        Debug.Log("----->Canal: " + chatter.channel + ", "+chatter.tags.displayName+": "+chatter.message);
        if (chatter.IsCommand())
        {
            string commandTw = chatter.message.Substring(1, chatter.message.Length-1);
            string[] argsTw = commandTw.Split(" ");
            string displayNameSafe = chatter.IsDisplayNameFontSafe() ? chatter.tags.displayName.ToLower() : chatter.login.ToLower();
            if (argsTw.Length > 0)
            {
                switch (argsTw[0])
                {
                    case "numduel":
                        if (argsTw.Length > 1)      //Main streamer send the duel
                        {
                            argsTw[1] = CheckValidUser(argsTw[1]);
                            currentDuelPlayer = argsTw[1];
                            TwitchChat.SendChatMessage("!numduel ,use '!numaccept' if you want a duel ", argsTw[1]);
                            TwitchChat.JoinChannel(argsTw[1]);
                            multiplayerState = NumSt.WAITDUEL;
                            Invoke("CancelDuelWait", 120);
                            timerTMP.gameObject.SetActive(true);
                            timerTMP.text = "Esperando oponente...";
                        }
                        else                        //if the command doesnt have arguments
                        {                           //Main streamer receive the duel
                            if (multiplayerState==NumSt.DUEL)
                            {
                                TwitchChat.SendChatMessage("Sorry, "+ displayNameSafe + " i'm in a duel right now. :( ");
                            }
                            else
                            {
                                if (chatter.channel.ToLower()== displayNameSafe)      //If Main streamer send the message
                                {
                                    TwitchChat.SendChatMessage("You forgot to write the channel to duel.");
                                }
                                else
                                {                                      //la otra persona te manda un duelo
                                    Invoke("CancelDuelWait", 120);  //empieza el duel
                                    timerTMP.gameObject.SetActive(true);
                                    timerTMP.text = "Esperando oponente...";
                                    multiplayerState = NumSt.WAITDUEL;
                                }
                            }
                        }
                        break;
                    case "numaccept":
                        if (multiplayerState == NumSt.DUEL)
                        {
                            TwitchChat.SendChatMessage("Sorry, i'm a duel rightnow :(", displayNameSafe);
                        }
                        else
                        {
                            if (chatter.channel.ToLower() == TwitchOAuth.Instance.GetChannelName())
                            {
                                if (argsTw.Length > 1)
                                {
                                    if (multiplayerState == NumSt.WAITDUEL)
                                        CancelInvoke("CancelDuelWait");
                                    argsTw[1] = CheckValidUser(argsTw[1]);
                                    TwitchChat.SendChatMessage("!numaccept ", argsTw[1]);
                                    TwitchChat.JoinChannel(argsTw[1]);
                                    currentDuelPlayer = argsTw[1];
                                    CancelInvoke("StopDuel");
                                    Invoke("StartDuel", 2f);
                                }
                                else
                                {
                                    TwitchChat.SendChatMessage("Forgive the channel to accept");
                                }
                            }
                            if (displayNameSafe.ToLower() == currentDuelPlayer.ToLower())
                            {
                                if (multiplayerState == NumSt.WAITDUEL)
                                    CancelInvoke("CancelDuelWait");
                                Invoke("StartDuel", 2f);
                            }
                        }
                        break;
                    case "help":
                        TwitchChat.SendChatMessage("Use !numduel [channel] for fight, !numaccept [channel] for accept the duel");
                        break;
                }
            }
        }
        else
        {
            if (multiplayerState==NumSt.DUEL)       //Si está en duelo
            { 
                if(chatter.channel.ToLower()== currentDuelPlayer)       
                {
                    Debug.Log("entra en el canal 2");
                    if (!int.TryParse(chatter.message, out int response)) return;
                    string displayName = chatter.IsDisplayNameFontSafe() ? chatter.tags.displayName : chatter.login;
                    if(lastMPusername.Equals(displayName)) return;
                    if (response == currentMPScore + 1) HandleCorrectResponseMP(displayName, chatter);
                    else HandleIncorrectResponseMP(displayName, chatter);
                    Debug.Log("Sale del canal 2");
                }
                else
                {
                    if (chatter.channel.ToLower() == TwitchOAuth.Instance.GetChannelName())
                    {
                        if (!int.TryParse(chatter.message, out int response1)) return;
                        string displayName = chatter.IsDisplayNameFontSafe() ? chatter.tags.displayName : chatter.login;
                        if (lastUsername.Equals(displayName)) return;
                        if (response1 == currentScore + 1) HandleCorrectResponse(displayName, chatter);
                        else HandleIncorrectResponse(displayName, chatter);
                    }
                }
            }
            else
            {
                if (chatter.channel.ToLower() == TwitchOAuth.Instance.GetChannelName())
                {
                    if (!int.TryParse(chatter.message, out int response)) return;
                    string displayName = chatter.IsDisplayNameFontSafe() ? chatter.tags.displayName : chatter.login;
                    if (lastUsername.Equals(displayName)) return;
                    if (response == currentScore + 1) HandleCorrectResponse(displayName, chatter);
                    else HandleIncorrectResponse(displayName, chatter);
                }
            }   
        }
    }

    private void StartDuel()
    {
        multiplayerState = NumSt.DUEL;
        totalTimeInSeconds = 60 * dueltime;
        timerTMP.gameObject.SetActive(true);
        StartCoroutine("TimerCoroutine");

        SavePVPData(currentMaxScoreUsername, currentMaxScore, usernameTMP.text, currentScore);
        SetMaxScore(TwitchOAuth.Instance.GetChannelName(), 0);
        SetMaxScoreVersus(currentDuelPlayer, 0);
        ResetGame();
        ResetGameVersus();
        animator.SetTrigger("Start");
    }
    private void StopDuel()
    {
        multiplayerState = NumSt.NODUEL;
        TwitchChat.LeaveChannel(currentDuelPlayer);
        if (currentMaxScore > currentMPMaxScore)
        {
            animator.SetTrigger("WinL");
            tropys++;
            PlayerPrefs.SetInt(TrophysKey, tropys);
            UpdateTrophyUI();
        }
        else
        {
            animator.SetTrigger("WinV");
        }
        LoadPVPData();
    }
    private string CheckValidUser(string usernameChannel)
    {
        if (String.IsNullOrEmpty(usernameChannel))
        {
            return "-1";
        }
        else
        {
            if (usernameChannel.Substring(0, 1) == "@")
            {
                usernameChannel = usernameChannel.Substring(1, usernameChannel.Length - 1);
                usernameChannel = usernameChannel.ToLower();
            }
            return usernameChannel;
        }
    } 
    private void CancelDuelWait()
    {
        currentDuelPlayer = "";
        TwitchChat.LeaveChannel(currentDuelPlayer);
        multiplayerState = NumSt.NODUEL;
        Debug.Log("Se cancelo la espera del duelo");
        timerTMP.gameObject.SetActive(false);
    }
    private void HandleCorrectResponseMP (string displayName, Chatter chatter)
    {
        currentMPScore++;
        UpdateCurrentScoreUIVersus(displayName, currentMPScore.ToString());
        lastMPusername = displayName;
        if (currentMPScore > currentMPMaxScore)
        {
            SetMaxScoreVersus(displayName, currentMPScore);
        }
    }
    private void HandleIncorrectResponseMP(string displayName, Chatter chatter)
    {
        if (currentMPScore != 0)
        {
            DisplayShameMessageVersus(displayName);
            ResetGameVersus();
            UpdateMaxScoreUIVersus();
            ResetGameVersus();
        }
    }
    private void HandleCorrectResponse(string displayName, Chatter chatter)
    {
        currentScore++;
        UpdateCurrentScoreUI(displayName, currentScore.ToString());
        lastUsername = displayName;
        if (currentScore > currentMaxScore)
        {
            SetMaxScore(displayName, currentScore);
            HandleVIPStatusUpdate(chatter);
        }
    }
    private void HandleIncorrectResponse(string displayName, Chatter chatter)
    {
        if (currentScore != 0)
        {
            DisplayShameMessage(displayName);
            if (TwitchOAuth.Instance.IsVipEnabled())
            {
                if (lastUserIdVIPGranted.Equals(chatter.tags.userId))
                {
                    RemoveLastVIP();
                }
                HandleNextPotentialVIP();
            }
            if (multiplayerState == NumSt.DUEL)
            {
                HandleTimeoutMP(chatter);
            }
            else
            {
                HandleTimeout(chatter);
            }

            UpdateMaxScoreUI();
            ResetGame();
        }
    }
    private void HandleNextPotentialVIP()
    {
        if (!string.IsNullOrEmpty(nextPotentialVIP))
        {
            if (nextPotentialVIP == "-1")
            {
                RemoveLastVIP();
            }
            else
            {
                if (!string.IsNullOrEmpty(lastUserIdVIPGranted)) 
                {
                    RemoveLastVIP();
                }

                GrantVIPToNextPotentialVIP();
            }

            nextPotentialVIP = string.Empty;
        }
    }
    private void HandleTimeout(Chatter chatter)
    {
        if (TwitchOAuth.Instance.IsModImmunityEnabled())
        {
            if (!chatter.HasBadge("moderator"))
            {
                TwitchOAuth.Instance.Timeout(chatter.tags.userId, currentScore);
            }
        }
        else
        {
            TwitchOAuth.Instance.Timeout(chatter.tags.userId, currentScore);
        }
    }
    private void HandleTimeoutMP(Chatter chatter)
    {
        if (!chatter.HasBadge("moderator"))
        {
            TwitchOAuth.Instance.TimeoutMP(chatter.tags.userId, currentScore);
        }
    }
    private void HandleVIPStatusUpdate(Chatter chatter)
    {
        if (TwitchOAuth.Instance.IsVipEnabled())
        {
            if (!chatter.tags.HasBadge("vip"))
            {
                nextPotentialVIP = chatter.tags.userId;
            }
            else if (chatter.tags.userId == lastUserIdVIPGranted)
            {
                nextPotentialVIP = "";
            }
            else
            {
                nextPotentialVIP = "-1";
            }
        }
    }
    private void RemoveLastVIP()
    {
        TwitchOAuth.Instance.SetVIP(lastUserIdVIPGranted, false);
        lastUserIdVIPGranted = "";
        PlayerPrefs.SetString(lastUserIdVIPGrantedKey, lastUserIdVIPGranted);
    }
    private void GrantVIPToNextPotentialVIP()
    {
        TwitchOAuth.Instance.SetVIP(nextPotentialVIP, true);
        lastUserIdVIPGranted = nextPotentialVIP;
        PlayerPrefs.SetString(lastUserIdVIPGrantedKey, lastUserIdVIPGranted);
    }
    private void DisplayShameMessage(string displayName)
    {
        usernameTMP.SetText($"<color=#00EAC0>Shame on </color>{displayName}<color=#00EAC0>!</color>");
    }
    private void DisplayShameMessageVersus(string displayName)
    {
        usernameTMPVersus.SetText($"<color=#00EAC0>Shame on </color>{displayName}<color=#00EAC0>!</color>");
    }
    private void OnChannelJoined()
    {
        startingCanvas.SetActive(false);
    }
    public void ResetHighScore()
    {
        SetMaxScore("RothioTome", 0);
        RemoveLastVIP();
        ResetGame();
    }
    private void SetMaxScore(string username, int score)
    {
        currentMaxScore = score;
        currentMaxScoreUsername = username;
        PlayerPrefs.SetString(maxScoreUsernameKey, username);
        PlayerPrefs.SetInt(maxScoreKey, score);
        UpdateMaxScoreUI();
    }
    private void SetMaxScoreVersus(string username, int score)
    {
        currentMPMaxScore = score;
        currentMPMaxScoreUsername = username;
        UpdateMaxScoreUIVersus();
    }
    private void UpdateMaxScoreUI()
    {
        string hs="";
        if (multiplayerState == NumSt.DUEL)
        {
            hs = "HS "+TwitchOAuth.Instance.GetChannelName();
        }
        else
        {
            hs = "HIGH SCORE";
        }
        string scoreText = $"{hs}: {currentMaxScore}\nby <color=#00EAC0>";
        if (TwitchOAuth.Instance.IsVipEnabled() &&
            (!string.IsNullOrEmpty(nextPotentialVIP) || !string.IsNullOrEmpty(lastUserIdVIPGranted)))
        {
            scoreText += $"<sprite=0>{currentMaxScoreUsername}</color>";
        }
        else
        {
            scoreText += currentMaxScoreUsername;
        }

        maxScoreTMP.SetText(scoreText);
    }
    private void UpdateMaxScoreUIVersus()
    {
        string scoreText = $"HS {currentDuelPlayer}: {currentMPMaxScore}\nby <color=#3D3C3C>";
        scoreText += currentMPMaxScoreUsername;
        maxScoreTMPVersus.SetText(scoreText);
    }
    private void UpdateCurrentScoreUI(string username, string score)
    {
        usernameTMP.SetText(username);
        currentScoreTMP.SetText(score);
    }
    private void UpdateCurrentScoreUIVersus(string username, string score)
    {
        usernameTMPVersus.SetText(username);
        currentScoreTMPVersus.SetText(score);
    }
    private void ResetGame()
    {
        lastUsername = "";
        currentScore = 0;
        currentScoreTMP.SetText(currentScore.ToString());
    }
    private void ResetGameVersus()
    {
        currentMPScore = 0;
        lastMPusername = "";
        currentScoreTMPVersus.SetText(currentScore.ToString());
    }

    private void SavePVPData(string username, int score, string currentUsername,int currentScore)
    {
        PlayerPrefs.SetString(maxScoreUsernameKey, username);
        PlayerPrefs.SetString(currentMaxScoreUsernameKey, currentUsername);
        PlayerPrefs.SetInt(maxScoreKey, score);
        PlayerPrefs.SetInt(maxScoreKey, currentScore);
    }
    private void LoadPVPData()
    {
        currentMaxScore = PlayerPrefs.GetInt(maxScoreKey);
        currentMaxScoreUsername = PlayerPrefs.GetString(maxScoreUsernameKey, currentMaxScoreUsername);
        lastUserIdVIPGranted = PlayerPrefs.GetString(lastUserIdVIPGrantedKey, string.Empty);
        currentMaxScoreUsername = PlayerPrefs.GetString(currentMaxScoreUsernameKey, string.Empty);
        UpdateMaxScoreUI();
        UpdateCurrentScoreUI(lastUsername, currentScore.ToString());
    }
    IEnumerator TimerCoroutine()
    {
        timerTMP.text = FormatTime(totalTimeInSeconds);
        while (totalTimeInSeconds > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            totalTimeInSeconds--;
            string formattedTime = FormatTime(totalTimeInSeconds);
            timerTMP.text = formattedTime;
        }
        timerTMP.gameObject.SetActive(false);
        StopDuel();
    }

    string FormatTime(int totalSeconds)
    {
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        return string.Format("{0:D2}:{1:D2}", minutes, seconds);
    }
    private void UpdateTrophyUI()
    {
        tropysTMP.text = tropys.ToString();
    }
    private enum NumSt
    {
        NODUEL,
        WAITDUEL,
        DUEL
    };
}