public class PathfindResult
{
    int _dungeonSizeX;
    int _dungeonSizeY;
    int _nrOfRooms;
    EPathFindMode _pathfindMode;
    double _timeMiliseconds;
    int _solutionSize;
    int _randomSeed;

    public int DungeonSizeX { get { return _dungeonSizeX; } }
    public int DungeonSizeY { get { return _dungeonSizeY; } }
    public int NrOfRooms { get { return _nrOfRooms; } }
    public string PathfindMode { get { return _pathfindMode.ToString(); } }
    public double TimeMilliseconds { get { return _timeMiliseconds; } }
    public int Steps { get { return _solutionSize; } }
    public int RandomSeed { get { return _randomSeed; } }

    public PathfindResult(int dungeonSizeX, int dungeonSizeY, int nrOfRooms, EPathFindMode pathfindMode, double timeMiliseconds, int solutionSize, int randomSeed)
    {
        _dungeonSizeX = dungeonSizeX;
        _dungeonSizeY = dungeonSizeY;
        _nrOfRooms = nrOfRooms;
        _pathfindMode = pathfindMode;
        _timeMiliseconds = timeMiliseconds;
        _solutionSize = solutionSize;
        _randomSeed = randomSeed;
    }
}

public enum EPathFindMode
{
    DFS = 0,
    AsyncDFS = 1,
    MT_DFS = 2,
    Astar = 3
}