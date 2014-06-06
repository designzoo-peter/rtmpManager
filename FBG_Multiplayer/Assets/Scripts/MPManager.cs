using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using GooglePlayGames;
using UnityEngine.SocialPlatforms;

public class MPManager : MonoBehaviour , GooglePlayGames.BasicApi.Multiplayer.RealTimeMultiplayerListener {

	#region Header Constants

	//Use the tag MPManager
	bool _roomDebugEnabled = true;

	const string _introductionConst = "PlayerIntroduction";
	const string _gameStartingConst = "GameStarting";
	const string _playerChangedState = "PlayerChangedState";

	#endregion

	#region Room Variables

	public enum RoomStates
	{
		None,
		LookingForGame,
		WaitingForPlayersConnected,
		WaitingForPlayersReady,
		GameInProgress,
		GameOver,
	}

	[HideInInspector]
	public RoomStates CurState;

	//will also include ourselves
	public List<MPPlayer> _playersInRoom;

	int _minOpponents, _maxOpponents;

	#endregion

	#region Events

	public MPPlayer OurPlayer;

	#endregion

	void Start() {

		PlayGamesPlatform.DebugLogEnabled = true;
		
		// Activate the Google Play Games platform
		PlayGamesPlatform.Activate();

		Social.localUser.Authenticate(LoginCallback);
	}

	#region Login Methods

	void LoginCallback(bool result) {

		if (result == true)
			LoginSucessful();
		else
			LoginFailed();
	}

	void LoginSucessful() {

		LogDebugMessage("Login sucessful");
	}

	void LoginFailed() {

		LogDebugMessage("Login Failed");
	}

	#endregion

	#region Room Methods

	public void LookForQuickGame(int minOpponents, int maxOpponents) {

		_minOpponents = minOpponents;
		_maxOpponents = maxOpponents;

		PlayGamesPlatform.Instance.RealTime.CreateQuickGame(minOpponents, maxOpponents, 0, this);

		CurState = RoomStates.LookingForGame;
	}

	/// <summary>
	/// Called when we have joined a game room
	/// </summary>
	void JoinedRoomSucessfuly() {

		LogDebugMessage("Joined room");

		_playersInRoom = new List<MPPlayer>();

		OurPlayer = new MPPlayer(PlayGamesPlatform.Instance.RealTime.GetSelf());
		OurPlayer.PlayerStateChangedEvent += OurUserStateChanged;
		_playersInRoom.Add(OurPlayer);

		CurState = RoomStates.WaitingForPlayersConnected;

		SendMessageToAll(_introductionConst);
	}

	/// <summary>
	/// Will inform other users of our state change
	/// </summary>
	/// <param name="thePlayer">The player.</param>
	void OurUserStateChanged(MPPlayer thePlayer) {

		SendMessageToAll(_playerChangedState + " " + ((int)OurPlayer.GetState()));
	}

	/// <summary>
	/// Sets the ready state of our player to true
	/// </summary>
	/// <param name="state">If set to <c>true</c> state.</param>
	public void SetReadyTo(bool state) {

		LogDebugMessage("SetReadyTo game room state " + CurState);

		if (CurState == RoomStates.WaitingForPlayersReady) {

			OurPlayer.ChangeStateTo(state == true ? UserStates.Ready : UserStates.Connected);
		}
	}

	/// <summary>
	/// Called when the user has changed their multiplayer state
	/// </summary>
	/// <param name="player">Player.</param>
	public void PlayerChangedReady(MPPlayer player) {

		bool everyoneReady = true;

		//check to see if everyone in the room is ready
		foreach (MPPlayer loopedPlayer in _playersInRoom)
			everyoneReady &= loopedPlayer.GetState() == UserStates.Ready;

		if (everyoneReady == true) {

			SendMessageToAll(_gameStartingConst);
			GameStarted();
		}
	}

	/// <summary>
	/// Makes our user leave the current room, telling our peers we are doing so
	/// </summary>
	public void LeaveRoom() {

		if (PlayGamesPlatform.Instance.RealTime.IsRoomConnected() == true) {

			OurPlayer.ChangeStateTo(UserStates.Disconnected);

			PlayGamesPlatform.Instance.RealTime.LeaveRoom();
		}
	}

	/// <summary>
	/// Called when we are told that another player has left the room.
	/// Removes the player from the player list
	/// </summary>
	/// <param name="thePlayer">The player.</param>
	void OtherPlayerLeftRoom(MPPlayer thePlayer) {

		LogDebugMessage("Other player left room");

		_playersInRoom.Remove(thePlayer);
		thePlayer.ChangeStateTo(UserStates.Disconnected);

		if (_playersInRoom.Count < _minOpponents)
			CurState = RoomStates.WaitingForPlayersConnected;
	}

	/// <summary>
	/// Called when we are told that another player has joined the room
	/// Adds to the players in room list
	/// </summary>
	/// <param name="thePlayer">The player who joined.</param>
	void OtherPlayerJoinedRoom(MPPlayer thePlayer) {

		_playersInRoom.Add(thePlayer);
		thePlayer.ChangeStateTo(UserStates.Connected);

		if (_playersInRoom.Count >= _minOpponents)
			CurState = RoomStates.WaitingForPlayersReady;
	}

	/// <summary>
	/// Called when all players are ready and the game has started
	/// </summary>
	void GameStarted() {

		OurPlayer.ChangeStateTo(UserStates.Playing);
		CurState = RoomStates.GameInProgress;
	}

	#endregion

	#region IRealTimeMultiplayerClient methods

	public void OnRoomSetupProgress(float progress) {

	}

	public void OnRoomConnected(bool success) {

		if (success == true) {

			JoinedRoomSucessfuly();
		}
		else
			LogDebugMessage("Room connect fail");
	}

	public void OnLeftRoom() {

		CurState = RoomStates.None;
		_playersInRoom.Clear();

		LogDebugMessage("We have left the room");
	}

	public void OnPeersConnected(string[] peers) {

		LogDebugMessage("Peers connected " + peers);
	}

	public void OnPeersDisconnected(string[] peers) {

		LogDebugMessage("Peers disconnected " + peers);

		foreach (string disconnectedPeer in peers) {

			MPPlayer thePlayer = _playersInRoom.Find(x => x.GetID() == disconnectedPeer);

			if (thePlayer != null)
				OtherPlayerLeftRoom(thePlayer);
		}
	}

	public void OnRealTimeMessageReceived(bool reliable, string senderId, byte[] data) {

		string rawData = data.ConvertToString();
		string[] dataClumps = rawData.Split(' ');

		string dataHeader = dataClumps[0];

		LogDebugMessage("New MSG: " + dataHeader + " from " + senderId);

		switch (dataHeader) {

		case _introductionConst: {

			MPPlayer theNewPlayer = new MPPlayer(PlayGamesPlatform.Instance.RealTime.GetConnectedParticipants().Find(x => x.ParticipantId == senderId));

			OtherPlayerJoinedRoom(theNewPlayer);
		}

			break;

		case _gameStartingConst: {

			if (CurState != RoomStates.GameInProgress) {

				GameStarted();

				LogDebugMessage("Game Starting!");
			}
		}

			break;

		case _playerChangedState: {

			MPPlayer senderPlayer = _playersInRoom.Find(x => x.GetID() == senderId);

			if (senderPlayer != null) {
			
				UserStates oldState = senderPlayer.GetState();

				senderPlayer.ChangeStateTo((UserStates)int.Parse(dataClumps[1]));

				//check for player leaving
				if (senderPlayer.GetState() == UserStates.Disconnected)
					OtherPlayerLeftRoom(senderPlayer);

				//check to see if they are setting themselves to ready
				else if (
					(oldState == UserStates.Ready && senderPlayer.GetState() != UserStates.Ready)	//changing from ready to not ready
					|| (oldState != UserStates.Ready && senderPlayer.GetState() == UserStates.Ready)	//changing from not ready to ready
					)
					PlayerChangedReady(senderPlayer);
			}

		}
			break;
		}
	}

	#endregion

	#region Utilities

	void SendMessageToAll(string message, bool reliable = true) {

		LogDebugMessage("Sending Message: " + message);
		PlayGamesPlatform.Instance.RealTime.SendMessageToAll(reliable, message.ConvertToBytes());
	}

	void LogDebugMessage(string message) {

		if (_roomDebugEnabled == true)
			Debug.Log("MPManager: " + message);
	}

	#endregion

	void OnDestroy() {

		if (PlayGamesPlatform.Instance.RealTime != null && PlayGamesPlatform.Instance.RealTime.IsRoomConnected() == true)
			PlayGamesPlatform.Instance.RealTime.LeaveRoom();
	}
}
