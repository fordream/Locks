using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Locks.iOS.Screens
{
	public class Level : Sunfish.Screen
	{
		private Sunfish.Views.Popup PausedPopup { get; set; }

		private Sunfish.Views.Popup SolvedPopup { get; set; }

		private int WorldNumber { get; set; }

		private int LevelNumber { get; set; }

		private Models.Level Model { get; set; }

		private int Moves { get; set; }

		private Sunfish.Views.Label TurnsLabel { get; set; }

		private Sunfish.Views.Label LockedCountLabel { get; set; }

		private Dictionary<string, Views.Lock> LockViewsDictionary { get; set; }

		private Views.SettingsPopup SettingsPopup;

		private Views.Stars StarsView { get; set; }

		private int SpaceBetweenLocks;

		private int CurrentlyAnimatingLockColumnShift = -1;

		private int LastLockColumnToShift = -1;

		private const double LockShiftDelayMilliseconds = 373d;

		public Level (Sunfish.SunfishGame currentGame, int worldNumber, int levelNumber) :
			base (currentGame, "WorldBackground_" + (worldNumber + 1).ToString ())
		{
			WorldNumber = worldNumber;
			LevelNumber = levelNumber;
			Model = Rules.Level.ReadLevel (WorldNumber, LevelNumber);
			LockViewsDictionary = new Dictionary<string, Views.Lock> ();
			// Compute the space between the locks, which is used during the animation when the user solves the level
			Texture2D pipeHorizontal = LocksGame.ActiveScreen.LoadTexture ("PipeHorizontal1");
			SpaceBetweenLocks = pipeHorizontal.Width;
		}

		public override void PopulateScreenViews ()
		{
			CreateAndPopulateTopBar ();
			CreateLocks (TopBar.Height);
			CreatePausedPopup ();
			CreateSolvedPopup ();
			CreateAndShowTutorialPopupIfNecessary ();
		}

		private void CreateAndPopulateTopBar ()
		{

			Sunfish.Views.Sprite pauseButton = new Sunfish.Views.Sprite (LoadTexture ("PauseButton"));
			pauseButton.EnableTapGesture (HandlePauseButtonTapped);

			Sunfish.Views.Sprite settingsButton = new Sunfish.Views.Sprite (LoadTexture ("SettingsButton"));
			settingsButton.EnableTapGesture (HandleSettingsButtonTapped);

			TurnsLabel = new Sunfish.Views.Label ("0", LocksGame.GetTopBarFont (), Color.Black);
			UpdateTurnsLabel ();

			LockedCountLabel = new Sunfish.Views.Label ("0", LocksGame.GetTopBarFont (), Color.Black);
			UpdateLockCountLabel ();

			Sunfish.Views.Label levelLabel = new Sunfish.Views.Label ("World " + WorldNumber.ToString () + " Level " + LevelNumber.ToString (), LocksGame.GetTopBarFont (), Color.Black);

			SettingsPopup = new Views.SettingsPopup ();
			AddChildView (SettingsPopup);

			CreateTopBar ();
			TopBar.AddChild (pauseButton, PixelsWithDensity (10), PixelsWithDensity (10));
			TopBar.AddChild (settingsButton, PixelsWithDensity (10), PixelsWithDensity (10));
			TopBar.AddChild (TurnsLabel, PixelsWithDensity (80), PixelsWithDensity (20));
			TopBar.AddChild (LockedCountLabel, PixelsWithDensity (70), PixelsWithDensity (20));
			TopBar.AddChild (levelLabel, PixelsWithDensity (70), PixelsWithDensity (20));

		}

		private void CreateLocks (int yOffset)
		{
		
			Sunfish.Views.Container lockContainer = CreateLevelGridContainer ();
			int rowCount = Model.LockGrid.RowCount;
			int colCount = Model.LockGrid.ColCount;
			for (int row = 0; row < rowCount; row++) {
				for (int col = 0; col < colCount; col++) {
					Models.Lock lockModel = Model.LockGrid.Locks [row, col];
					Views.Lock lockView = new Views.Lock (lockModel, WorldNumber, row == 0, row == (rowCount - 1), col == 0, col == (colCount - 1));
					lockView.OnLockButtonPush = HandleLockButtonPush;
					lockContainer.AddChild (lockView, (col == 0) ? 0 : PixelsWithDensity (18), PixelsWithDensity (18));
					LockViewsDictionary.Add (lockModel.GetRowColString (), lockView);
				}
			}

			// Center the locks in a play area
			if (Model.LockGrid.RowCount < 3) {
				yOffset += PixelsWithDensity (50);
			}
			Sunfish.Views.Container locksPlayArea = new Sunfish.Views.Container (LocksGame.ScreenHeight, LocksGame.ScreenWidth, new Vector2 (PixelsWithDensity (9), yOffset), Sunfish.Constants.ViewContainerLayout.StackCentered);
			locksPlayArea.AddChild (lockContainer);

			AddChildView (locksPlayArea);

		}

		private void CreatePausedPopup ()
		{

			PausedPopup = AddPopup (LoadTexture ("PopupBackground"), Sunfish.Constants.ViewContainerLayout.StackCentered);
			Sunfish.Views.Sprite resumeButton = new Sunfish.Views.Sprite (LoadTexture ("ResumeGameButton"), Sunfish.Constants.ViewLayer.Modal);
			Sunfish.Views.Sprite restartButton = new Sunfish.Views.Sprite (LoadTexture ("RestartGameButton"), Sunfish.Constants.ViewLayer.Modal);
			Sunfish.Views.Sprite quitButton = new Sunfish.Views.Sprite (LoadTexture ("QuitGameButton"), Sunfish.Constants.ViewLayer.Modal);

			resumeButton.EnableTapGesture (HandleResumeButtonTapped);
			restartButton.EnableTapGesture (HandleRestartButtonTapped);
			quitButton.EnableTapGesture (HandleQuitButtonFromPausedPopupTapped);

			PausedPopup.AddChild (resumeButton, 0, PixelsWithDensity (80));
			PausedPopup.AddChild (restartButton, 0, PixelsWithDensity (30));
			PausedPopup.AddChild (quitButton, 0, PixelsWithDensity (30));

		}

		private void CreateSolvedPopup ()
		{

			StarsView = Views.Stars.Create (0, Sunfish.Constants.ViewLayer.Modal);

			Sunfish.Views.Sprite nextLevelButton = null;
			if (WorldNumber != Core.Constants.WorldCount - 1 || LevelNumber != Core.Constants.WorldLevelCount - 1) {
				nextLevelButton = new Sunfish.Views.Sprite (LoadTexture ("NextLevelButton"), Sunfish.Constants.ViewLayer.Modal);
				nextLevelButton.EnableTapGesture (HandleNextLevelButtonTapped);
			}

			Sunfish.Views.Sprite retryButton = new Sunfish.Views.Sprite (LoadTexture ("RetryButton"), Sunfish.Constants.ViewLayer.Modal);
			retryButton.EnableTapGesture (HandleRetryButtonTapped);

			Sunfish.Views.Sprite quitButton = new Sunfish.Views.Sprite (LoadTexture ("QuitGameButton"), Sunfish.Constants.ViewLayer.Modal);
			quitButton.EnableTapGesture (HandleQuitButtonFromSolvedPopupTapped);

			SolvedPopup = AddPopup (LoadTexture ("PopupBackground"), Sunfish.Constants.ViewContainerLayout.StackCentered);
			SolvedPopup.OnShown = HandleSolvedPopupShown;
			SolvedPopup.AddChild (StarsView, 0, PixelsWithDensity (60));
			if (nextLevelButton != null) {
				SolvedPopup.AddChild (nextLevelButton, 0, PixelsWithDensity (30));
			}
			SolvedPopup.AddChild (retryButton, 0, PixelsWithDensity (30));
			SolvedPopup.AddChild (quitButton, 0, PixelsWithDensity (30));

		}

		private void CreateAndShowTutorialPopupIfNecessary ()
		{
//			if (LevelNumber == 0 && WorldNumber == 0 && LocksGame.GameProgress.GetSolvedLevel (LevelNumber, WorldNumber) == null) {
//				Views.TutorialPopup tutorialPopup = new Views.TutorialPopup ();
//				AddChildView (tutorialPopup);
//				tutorialPopup.Show ();
//			}
		}

		private Sunfish.Views.Container CreateLevelGridContainer ()
		{
			int lockWidthWithMargin = Views.Lock.LoadLockBackground (1).Width + PixelsWithDensity (18);
			int width = Model.LockGrid.ColCount * lockWidthWithMargin;
			return new Sunfish.Views.Container (width, LocksGame.ScreenWidth, Sunfish.Constants.ViewContainerLayout.FloatLeft);
		}

		private void HandleLockButtonPush (Models.LockButtonPushResult pushResult)
		{

			Moves++;
			UpdateTurnsLabel ();

			if (pushResult.LinkedButton != null) {
				Views.Lock lockView = null;
				if (LockViewsDictionary.TryGetValue (pushResult.LinkedButton.ContainingLock.GetRowColString (), out lockView)) {
					lockView.SwitchAndPulsateLockButton (pushResult.LinkedButton.ContainingLockIndex);
					lockView.OnDialRotateComplete = HandleLockDialRotateComplete;
					lockView.RotateDial (pushResult.GetLinkedButtonContainingLockPositionDelta ());
				}
			}

		}

		private void HandleLockDialRotateComplete (Views.Lock lockWhoseDialRotated)
		{

			UpdateLockCountLabel ();
			lockWhoseDialRotated.OnDialRotateComplete = null;

			if (Model.LockGrid.IsSolved ()) {

				// Save the game progress
				Models.SolvedLevel solvedLevel = LocksGame.GameProgress.GetSolvedLevel (WorldNumber, LevelNumber);
				if (solvedLevel == null || Moves < solvedLevel.Moves) {
					int stars = Model.LockGrid.GetStars (Moves);
					solvedLevel = new Models.SolvedLevel (WorldNumber, LevelNumber, Moves, stars);
					LocksGame.GameProgress.AddSolvedLevel (solvedLevel);
					Rules.GameProgress.SaveGameProgress (LocksGame.GameProgress);
				}

				// Start an animation indicating that the level is solved
				StartSolvedLockAnimation ();

			}

		}

		private void StartSolvedLockAnimation()
		{
			// Calculate the last column to shift during the animation
			if (Model.LockGrid.ColCount % 2 == 0) { // Even number of columns?
				LastLockColumnToShift = (Model.LockGrid.ColCount / 2) - 1;
			} else {
				LastLockColumnToShift = ((Model.LockGrid.ColCount - 1) / 2) - 1;
			}

			StartNextLockShiftOrShowSolvedPopup (null);
		}

		private void StartNextLockShiftOrShowSolvedPopup(Sunfish.Views.Effects.Effect effectThatIsComplete)
		{
			CurrentlyAnimatingLockColumnShift++;
			if (CurrentlyAnimatingLockColumnShift > LastLockColumnToShift) { // Done shifting columns?
				StartLockRowShiftAndShowSolvedPopupWhenDone ();
			} else {
				StartLockColumnShift (CurrentlyAnimatingLockColumnShift);
			}
		}

		private void StartLockColumnShift(int leftEndCol)
		{
			/* Step 1: Figure out how many x units to shift the columns by */

			// Default x shift
			int xShift = SpaceBetweenLocks;
			// If there is an even number of columns, then the last shift should only be by half the SpaceBetweenLocks
			if (Model.LockGrid.ColCount % 2 == 0) { // Even number of columns
				// Is this the last column shift?
				if ((leftEndCol + 1) * 2 == Model.LockGrid.ColCount) {
					xShift = (int) (SpaceBetweenLocks * 0.5f); // When even
				}
			}

			/* Step 2: Start a TranslateBy effect on each of the appropriate locks (according to leftEndCol) */

			// Shift the locks on the left side of the grid to the right
			for (int row = 0; row < Model.LockGrid.RowCount; row++) {
				for (int col = 0; col <= leftEndCol; col++) {
					Sunfish.Views.Effects.TranslateBy shiftLock = new Sunfish.Views.Effects.TranslateBy (new Vector2(xShift,0), LockShiftDelayMilliseconds);
					if (row == 0 && col == 0) {
						shiftLock.OnComplete = StartNextLockShiftOrShowSolvedPopup;
					}
					Views.Lock currentLockView = null;
					string lockViewKey = Models.Lock.GetRowColString (row, col);
					LockViewsDictionary.TryGetValue (lockViewKey, out currentLockView);
					currentLockView.StartEffect (shiftLock);
				}
			}

			// Shift the locks on the right side of the grid to the left
			int lastRightCol = Model.LockGrid.ColCount - 1 - leftEndCol;
			for (int row = 0; row < Model.LockGrid.RowCount; row++) {
				for (int col = Model.LockGrid.ColCount - 1; col >= lastRightCol; col--) {
					Sunfish.Views.Effects.TranslateBy shiftLock = new Sunfish.Views.Effects.TranslateBy (new Vector2(-xShift,0), LockShiftDelayMilliseconds);
					Views.Lock currentLockView = null;
					string lockViewKey = Models.Lock.GetRowColString (row, col);
					LockViewsDictionary.TryGetValue (lockViewKey, out currentLockView);
					currentLockView.StartEffect (shiftLock);
				}
			}

			PlaySoundEffect ("Shift1");

		}

		private void StartLockRowShiftAndShowSolvedPopupWhenDone()
		{
			if (Model.LockGrid.RowCount > 1) { // Should the rows be shifted?

				// Shift the top row
				for (int col = 0; col < Model.LockGrid.ColCount; col++) {
					Sunfish.Views.Effects.TranslateBy shiftLock = new Sunfish.Views.Effects.TranslateBy (new Vector2 (0, SpaceBetweenLocks), LockShiftDelayMilliseconds);
					if (col == 0) {
						shiftLock.OnComplete = PrepareStarsAndShowSolvedPopup;
					}
					Views.Lock currentLockView = null;
					string lockViewKey = Models.Lock.GetRowColString (0, col);
					LockViewsDictionary.TryGetValue (lockViewKey, out currentLockView);
					currentLockView.StartEffect (shiftLock);
				}

				// Shift the bottom row if necessary
				if (Model.LockGrid.RowCount == 3) {
					for (int col = 0; col < Model.LockGrid.ColCount; col++) {
						Sunfish.Views.Effects.TranslateBy shiftLock = new Sunfish.Views.Effects.TranslateBy (new Vector2 (0, -SpaceBetweenLocks), LockShiftDelayMilliseconds);
						Views.Lock currentLockView = null;
						string lockViewKey = Models.Lock.GetRowColString (2, col);
						LockViewsDictionary.TryGetValue (lockViewKey, out currentLockView);
						currentLockView.StartEffect (shiftLock);
					}
				}

				PlaySoundEffect ("Shift1");


			} else {
				PrepareStarsAndShowSolvedPopup (null);
			}
		}

		private void PrepareStarsAndShowSolvedPopup(Sunfish.Views.Effects.Effect effect)
		{
			LocksGame.ActiveScreen.PlaySoundEffect ("Unlocked");
			int stars = Model.LockGrid.GetStars (Moves);
			StarsView.SetStars (stars);
			SolvedPopup.Show ();
		}

		private void UpdateTurnsLabel ()
		{
			if (Moves == 1) {
				TurnsLabel.SetText (Moves.ToString () + " Turn");
			} else {
				TurnsLabel.SetText (Moves.ToString () + " Turns");
			}
		}

		private void UpdateLockCountLabel ()
		{
			string lockCountText = Model.LockGrid.CountUnlocked ().ToString () + " of " + (Model.LockGrid.ColCount * Model.LockGrid.RowCount).ToString () + " Unlocked";
			LockedCountLabel.SetText (lockCountText);
		}

		private void HandleSolvedPopupShown (Sunfish.Views.Popup popupThatIsNowShown)
		{
			LocksGame.ActiveScreen.PlaySoundEffect ("LevelSuccess");
		}

		private void HandlePauseButtonTapped (Sunfish.Views.View pauseButton)
		{
			PauseGame ();
		}

		private void HandleSettingsButtonTapped (Sunfish.Views.View settingsButton)
		{
			SettingsPopup.Show ();
		}

		private void HandleResumeButtonTapped (Sunfish.Views.View pauseButton)
		{
			ResumeGame ();
		}

		private void HandleRestartButtonTapped (Sunfish.Views.View pauseButton)
		{
			PausedPopup.Hide ();
			RetryLevel ();
		}

		private void HandleQuitButtonFromPausedPopupTapped (Sunfish.Views.View pauseButton)
		{
			PausedPopup.Hide ();
			QuitGame ();
		}

		private void HandleQuitButtonFromSolvedPopupTapped (Sunfish.Views.View pauseButton)
		{
			SolvedPopup.Hide ();
			QuitGame ();
		}

		private void HandleRetryButtonTapped (Sunfish.Views.View retryButton)
		{
			SolvedPopup.Hide ();
			RetryLevel ();
		}

		private void HandleNextLevelButtonTapped (Sunfish.Views.View nextLevelButton)
		{
			if (LevelNumber == Core.Constants.WorldLevelCount - 1) {
				CurrentGame.SetActiveScreen (new Screens.Level (CurrentGame, WorldNumber + 1, 0));
			} else {
				CurrentGame.SetActiveScreen (new Screens.Level (CurrentGame, WorldNumber, LevelNumber + 1));
			}
			SolvedPopup.Hide ();
		}

		private void PauseGame ()
		{
			PausedPopup.Show ();
		}

		private void ResumeGame ()
		{
			PausedPopup.Hide ();
		}

		private void RetryLevel ()
		{
			CurrentGame.SetActiveScreen (new Screens.Level (CurrentGame, WorldNumber, LevelNumber));
		}

		private void QuitGame ()
		{
			CurrentGame.SetActiveScreen (new Screens.LevelChooser (CurrentGame));
		}
	}
}

