using UnityEngine;
using System.Collections;

public class UIMultiplayer : MonoBehaviour {

	public MPManager GameRoom;

	public GameObject ReadyButton;

	public UILabel StateLabel;

	public UILabel PlayersLabel;

	public void ToggleReady() {

		if (GameRoom.CurState == MPManager.RoomStates.WaitingForPlayersReady) {

			bool state = GameRoom.OurPlayer.GetState() == UserStates.Ready;

			state = !state;

			GameRoom.SetReadyTo(state);

			ReadyButton.GetComponentInChildren<UILabel>().text = state ? "Set Not Ready" : "Set Ready";
		}
	}

	public void LookForGame() {

		if (GameRoom.CurState == MPManager.RoomStates.None)
			GameRoom.LookForQuickGame(1, 1);
	}

	public void LeaveRoom() {

		if (GameRoom.CurState != MPManager.RoomStates.None && GameRoom.CurState != MPManager.RoomStates.LookingForGame)
			GameRoom.LeaveRoom();
	}

	void Update() {

		/*
		if (GameRoom.CurState == MPManager.RoomStates.WaitingForPlayersConnected)
			StateLabel.text = "Players Connected";
		else if (GameRoom.CurState == MPManager.RoomStates.WaitingForPlayersReady)
			StateLabel.text = "Waiting for Ready";
			*/
		StateLabel.text = GameRoom.CurState.ToString();

		PlayersLabel.text = "";

		if (GameRoom._playersInRoom != null) {

			foreach (MPPlayer loopedPlayer in GameRoom._playersInRoom) {

				if (loopedPlayer == null)
					Debug.Log("looped player null");
				else
					PlayersLabel.text += "\n" + loopedPlayer.GetID() + " " + loopedPlayer.GetState();	
			}

			PlayersLabel.UpdateNGUIText();
		}
	}
}
