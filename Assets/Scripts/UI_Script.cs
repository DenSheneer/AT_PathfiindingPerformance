using MyBox;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Script : MonoBehaviour
{
    [SerializeField] private DungeonGenerator _dungeon;

    [Header("UI Object references")]
    [SerializeField] private Toggle _normalReapeatToggle;
    [SerializeField] private Toggle _asyncRepeatToggle;
    [SerializeField] private Toggle _MTReapeatToggle;
    [SerializeField] private Toggle _aStarRepeatToggle;

    [SerializeField] private Button _generateButton;
    [SerializeField] private Button _resetButton;

    [SerializeField] private TMP_Text _runsText;

    private PathfindAgent[] _agents;
    private EPathFindMode _currentMode;
    private int _runs;
    private Toggle[] _toggles;

    private void Start()
    {
        _dungeon.OnDungeonGenerated += () =>
        {
            setPathfindingButtonsInteractivity(true);
            setGenerateButtonActive(false);
        };

        _toggles = new Toggle[]
        {
            _normalReapeatToggle,
            _asyncRepeatToggle,
            _MTReapeatToggle,
            _aStarRepeatToggle
        };

        _agents = FindObjectsByType<PathfindAgent>(FindObjectsSortMode.None);
        subscribeToAgents(_agents);

        setPathfindingButtonsOff(true);
        setPathfindingButtonsInteractivity(false);

        _normalReapeatToggle.onValueChanged.AddListener((isActive) => { setPathfindingButtonsOff(isActive, _normalReapeatToggle); setAgentsPathfindMode(isActive, EPathFindMode.DFS); });
        _asyncRepeatToggle.onValueChanged.AddListener((isActive) => { setPathfindingButtonsOff(isActive, _asyncRepeatToggle); setAgentsPathfindMode(isActive, EPathFindMode.AsyncDFS); });
        _MTReapeatToggle.onValueChanged.AddListener((isActive) => { setPathfindingButtonsOff(isActive, _MTReapeatToggle); setAgentsPathfindMode(isActive, EPathFindMode.MT_DFS); });
        _aStarRepeatToggle.onValueChanged.AddListener((isActive) => { setPathfindingButtonsOff(isActive, _aStarRepeatToggle); setAgentsPathfindMode(isActive, EPathFindMode.Astar); });
    }

    private void subscribeToAgents(PathfindAgent[] agents)
    {
        foreach (PathfindAgent agent in agents)
        {
            agent.OnPathFound += updateRunsText;
        }
    }

    private void updateRunsText(List<Room> list, EPathFindMode mode)
    {
        if (_currentMode == mode)
            _runs++;
        else
        {
            _runs = 1;
            _currentMode = mode;
        }
        _runsText.text = _runs.ToString();
    }

    private void setGenerateButtonActive(bool isActive)
    {
        _generateButton.interactable = isActive;
    }

    private void setPathfindingButtonsInteractivity(bool isActive)
    {
        foreach (Toggle toggle in _toggles)
        {
            toggle.interactable = isActive;
        }
    }
    private void setPathfindingButtonsOff(bool isActive, Toggle exception = null)
    {
        if (!isActive) { return; }

        foreach (Toggle toggle in _toggles)
        {
            if (toggle == exception) { continue; }
            else
                toggle.isOn = false;
        }
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
