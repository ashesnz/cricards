using Godot;

public partial class GameController : Node2D
{
    public enum GameState { PLAYER_TURN, ENEMY_TURN, GAME_OVER, GAME_WON }

    public bool is_running = true;
    public GameState current_state = GameState.PLAYER_TURN;

    public void TogglePauseAndResume()
    {
        is_running = !is_running;
        if (is_running) Pause(); else Resume();
    }

    public void Pause()
    {
        is_running = false;
    }

    public void Resume()
    {
        is_running = true;
    }

    public void Transition(GameState next_state)
    {
        current_state = next_state;
        switch (current_state)
        {
            case GameState.PLAYER_TURN:
                break;
            case GameState.ENEMY_TURN:
                break;
            case GameState.GAME_OVER:
                break;
            case GameState.GAME_WON:
                break;
        }
    }
}

