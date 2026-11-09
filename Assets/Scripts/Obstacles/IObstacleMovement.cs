public interface IObstacleMovement
{
    void ProcessMovement(ObstacleBase host);
}

/// <summary>
/// Movers that remember a home spot (on a platform, or in the world) and need to pick a new one
/// after the obstacle falls off and lands somewhere else.
/// </summary>
public interface IObstacleAnchor
{
    void Reanchor();
}
