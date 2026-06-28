namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    private bool multiplayerUIHidden;

    private string usernameInput = "Player";
    private bool allowCheatsInput;
    private string playerPulsingForceInput = "1.0";

    private void FirstTimeScreen()
    {
        var valid = true;

        DrawText("Please select an username to play multiplayer.");

        DrawText("Username:", 2);
        usernameInput = DrawSafeTextInput("username", CalculateInputLayout(6, 2, 1), usernameInput, 32);

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            DrawText("You must set an Username first.");
            valid = false;
        }

        if (!valid) return;
        if (!GUI.Button(CalculateButtonLayout(6), "Save settings")) return;

        firstTime = false;
        Main.SetConfigValue("internal_setup_ui", false);
        Main.SetConfigValue("username", usernameInput);
    }

    private void SettingsScreen()
    {
        DrawText("Username:", 2);
        usernameInput = DrawSafeTextInput("username", CalculateInputLayout(6, 2, 1), usernameInput, 32);

        DrawText("Allow Cheats:", 2);
        if (GUI.Button(CalculateButtonLayout(6, 2, 1), allowCheatsInput.ToStringYesOrNo()))
            allowCheatsInput = !allowCheatsInput;

        DrawText("Player Pulsing Force (0=off):", 2);
        playerPulsingForceInput = DrawSafeTextInput("player_pulsing_force", CalculateInputLayout(6, 2, 1), playerPulsingForceInput, 5);

        if (string.IsNullOrWhiteSpace(usernameInput))
        {
            DrawText("You must set an Username.");
            return;
        }

        if (!GUI.Button(CalculateButtonLayout(6), "Save")) return;

        if (!float.TryParse(playerPulsingForceInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var forceVal) || forceVal < 0)
        {
            forceVal = 1.0f; // fallback
        }

        Main.SetConfigValue("username", usernameInput);
        Main.SetConfigValue("allow_cheats", allowCheatsInput);
        Main.SetConfigValue("player_pulsing_force", forceVal);
        viewingSettings = false;
    }

    private void MainMenuScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;

        if (GUI.Button(CalculateButtonLayout(6), "Save Manager"))
        {
            viewingSaveManager = true;
            RefreshSaveList();
        }

        DrawJoinSection();
    }

    private void InGameScreen()
    {
        if (GUI.Button(CalculateButtonLayout(6), "Settings"))
            viewingSettings = true;

        DrawHostSection();
    }

    private void UnimplementedScreen()
    {
        DrawText("This screen hasn't been implemented yet.");
    }
}