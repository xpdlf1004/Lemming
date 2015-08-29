﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class GameController : MonoBehaviour
{
	[SerializeField]
	private GameObject
		mainHud;
	[SerializeField]
	private GameObject
		gameOverPanel;
	[SerializeField]
	private GameObject
		readyCounter;
	[SerializeField]
	private GameObject
		optionPanel;
	[SerializeField]
	private GameTimer
		timer;
	public HexagonMap map;
	private GameState currentGameState;
	private LemmingContainer lemmingContainer;

	public GameState CurrentGameState {
		get {
			return currentGameState;
		}
	}

	public enum GameState
	{
		Ready,
		Start,
		GameOver,
		Pause
	}

	public static GameController Instance {
		get;
		private set;
	}

	void Awake ()
	{
		Instance = this;
		currentGameState = GameState.Ready;
		Initialize ();
	}

	void Start ()
	{

	}

	[UnityEventListener]
	private void StartGame ()
	{
		ShowMainHud ();
		timer.ResetTime ();
		StartCoroutine (ShowReadyCounter (StartCounterCallback));
	}

	private void HideMainHud ()
	{
		mainHud.SetActive (false);
	}

	[UnityEventListener]
	private void OpenOptionPanel ()
	{
		PauseGame ();
		ShowOptionPanel ();
	}

	[UnityEventListener]
	private void CloseOptionMenu ()
	{
		ContinueGame ();
		HideOptionPanel ();
	}

	private void PauseGame ()
	{
		currentGameState = GameState.Pause;
		timer.StopTimer ();
	}

	private void ContinueGame ()
	{
		StartCoroutine (ShowReadyCounter (() => {
			timer.StartTimer ();
			currentGameState = GameState.Start;

			// FIXME: In puase state CheckingLemmingState couroutine is killed. I don't know why this occured.
			StopCoroutine ("CheckingLemmingState");
			StartCoroutine ("CheckingLemmingState");
		}));
	}

	private void ShowOptionPanel ()
	{
		optionPanel.SetActive (true);
	}

	private void HideOptionPanel ()
	{
		optionPanel.SetActive (false);
	}

	private IEnumerator ShowReadyCounter (Action counterCallback = null)
	{
		readyCounter.SetActive (true);
		readyCounter.GetComponentInChildren<Text> ().text = "3";
		yield return new WaitForSeconds (1f);
		readyCounter.GetComponentInChildren<Text> ().text = "2";
		yield return new WaitForSeconds (1f);
		readyCounter.GetComponentInChildren<Text> ().text = "1";
		yield return new WaitForSeconds (1f);
		readyCounter.SetActive (false);

		if (counterCallback != null)
			counterCallback ();
	}

	private void StartCounterCallback ()
	{
		currentGameState = GameState.Start;
		timer.StartTimer ();
		
		const float fixedTickTime = 10f;
		timer.RegisterTicker (fixedTickTime, IncreaseGameLevel);

		StartCoroutine ("CheckingLemmingState");
	}

	private void ShowMainHud ()
	{
		mainHud.SetActive (true);
	}

	private IEnumerator CheckingLemmingState ()
	{
		var lemmings = lemmingContainer.LemmingObjects.Select (go => go.GetComponent<Lemming> ());
		while (currentGameState == GameState.Start) {
			foreach (var lemming in lemmings) {
				switch (lemming.GetCurrentState ()) {
				case Lemming.State.Idle:
					if (!lemmingContainer.IsAnyLemmingFindingCliff ())
						lemming.ChangeAction (Lemming.Action.MoveToCliff);
					else
						lemming.ChangeAction (Lemming.Action.WaitForFindingCliff);
					break;
				case Lemming.State.MoveToCliff:
					break;
				case Lemming.State.FindAvailableCliff:
					break;
				case Lemming.State.BackToCenter:
					break;
				case Lemming.State.JumpToCliff:
					GameOver ();
					break;
				case Lemming.State.Die:
					break;
				case Lemming.State.WaitForFindingCliff:
					break;
				default:
					Debug.Assert (false, "Not reach here " + lemming.GetCurrentState ().ToString ());
					break;
				}
			}
			yield return new WaitForFixedUpdate ();
		}
	}

	[UnityEventListener]
	private void RestartGame ()
	{
		lemmingContainer.ResetLemmingPosition ();
		lemmingContainer.ResetLemmingState ();
		lemmingContainer.ResetTargetPositionIndexQueue ();
		lemmingContainer.ResetLemmingSpeed ();
		StartGame ();
	}

	private void GameOver ()
	{
		lemmingContainer.ChangeToGameOverState ();
		currentGameState = GameState.GameOver;
		StopCoroutine ("CheckingLemmingState");
		gameOverPanel.SetActive (true);
		HideMainHud ();
		timer.StopTimer ();
	}

	void Update ()
	{
	}

	private void IncreaseGameLevel ()
	{
		Debug.Log ("Lemming speed Increased..");
		const float increasingSpeedValue = 0.5f;
		lemmingContainer.IncreaseLemmingSpeed (increasingSpeedValue);
	}

	private void Initialize ()
	{
		InitializeLemmings ();
	}

	private void InitializeLemmings ()
	{
		lemmingContainer = new LemmingContainer ();
		lemmingContainer.SpawnLemmings ();
		lemmingContainer.ResetLemmingState ();
	}

	public void BroadcastToFindNewTargetToAllLemmings (HexagonMap.MapPosition targetPosition)
	{
		lemmingContainer.BroadcastToFindNewTargetToAllLemmings (targetPosition);
	}

	public Vector2 GetCenterPosition ()
	{
		return map.GetCenterPosition ();
	}

	public void TouchInputTrigger (GameObject trigger)
	{
		map.TouchInputTrigger (trigger);
	}
}
