using UnityEngine;
using UnityEngine.UI;

public class UI_Script : MonoBehaviour
{
    [SerializeField] private Toggle _normalReapeatToggle;
    [SerializeField] private Toggle _asyncRepeatToggle;
    [SerializeField] private Toggle _MTReapeatToggle;
    [SerializeField] private Toggle _aStarReapeatToggle;

    [SerializeField] private MyAgent[] _agents;

    private void Start()
    {
        _agents = FindObjectsByType<MyAgent>(FindObjectsSortMode.None);

        _normalReapeatToggle.isOn = false;
        _asyncRepeatToggle.isOn = false;
        _MTReapeatToggle.isOn = false;
        _aStarReapeatToggle.isOn = false;

        _normalReapeatToggle.onValueChanged.AddListener((isActive) => { setAgentsPathfindMode(isActive, EPathFindMode.DFS); });
        _asyncRepeatToggle.onValueChanged.AddListener((isActive) => { setAgentsPathfindMode(isActive, EPathFindMode.AsyncDFS); });
        _MTReapeatToggle.onValueChanged.AddListener((isActive) => { setAgentsPathfindMode(isActive, EPathFindMode.MT_DFS); });
        _aStarReapeatToggle.onValueChanged.AddListener((isActive) => { setAgentsPathfindMode(isActive, EPathFindMode.Astar); });
    }

    private void setAgentsPathfindMode(bool isActive, EPathFindMode mode)
    {
        foreach (var agent in _agents)
        {
            if (isActive)
                agent.RepeatPathFind(mode);
            else
                agent.StopRepeatPathfind();
        }
    }
}
