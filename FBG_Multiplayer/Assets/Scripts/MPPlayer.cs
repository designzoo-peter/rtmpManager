using UnityEngine;
using System.Collections;

using GooglePlayGames.BasicApi.Multiplayer;

public enum UserStates
{
	Connected = 0,
	Ready = 1,
	Playing = 2,
	Finished = 3,
	Disconnected = 4,
}

public class MPPlayer {

	Participant _ourData;

	/// <summary>
	/// The state of the user. MPPlayers can only be created after joining a room, so they default to connected
	/// </summary>
	UserStates _userState;

	#region Events and Delegates

	public delegate void PlayerStateChangDel(MPPlayer thePlayer);
	/// <summary>
	/// Occurs when user state changes
	/// </summary>
	public event PlayerStateChangDel PlayerStateChangedEvent;

	#endregion

	public MPPlayer(Participant participantData) {

		if (participantData == null)
			Debug.Log("Creating participant is null");

		_ourData = participantData;
		_userState = UserStates.Connected;
	}

	public void ChangeStateTo(UserStates newState) {

		UserStates oldState = _userState;
		_userState = newState;

		if (_userState != oldState && PlayerStateChangedEvent != null)
			PlayerStateChangedEvent(this);
	}

	public string GetID() {

		return _ourData.ParticipantId;
	}

	public UserStates GetState() {

		return _userState;
	}
}
