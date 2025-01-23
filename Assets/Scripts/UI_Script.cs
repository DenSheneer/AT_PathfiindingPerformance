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
    [SerializeField] private Toggle _aStarReapeatToggle;

    [SerializeField] private Button _generateButton;
    [SerializeField] private Button _resetButton;

    [SerializeField] private TMP_Text _runsText;

    private PathfindAgent[] _agents;
    private EPathFindMode _currentMode;
    private int _runs;

    private void Awake()
    {
        _dungeon.OnDungeonGenerated += () =>
        {
            setPathfindingButtonsActive(true);
            setGenerateButtonActive(false);
        };

        _agents = FindObjectsByType<PathfindAgent>(FindObjectsSortMode.None);
        subscribeToAgents(_agents);

        _normalReapeatToggle.isOn = false;
        _asyncRepeatToggle.isOn = false;
        _MTReapeatToggle.isOn = false;
        _aStarReapeatToggle.isOn = false;

        setPathfindingButtonsActive(false);

        _normalReapeatToggle.onValueChanged.AddListener((isActive) => { setAgentsPathfindMode(isActive, EPathFindMode.DFS); });
        _asyncRepeatToggle.onValueChanged.AddListener((isActive) => { setAgentsPathfindMode(isActive, EPathFindMode.AsyncDFS); });
        _MTReapeatToggle.onValueChanged.AddListener((isActive) => { setAgentsPathfindMode(isActive, EPathFindMode.MT_DFS); });
        _aStarReapeatToggle.onValueChanged.AddListener((isActive) => { setAgentsPathfindMode(isActive, EPathFindMode.Astar); });
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

    private void setPathfindingButtonsActive(bool isActive)
    {
        _normalReapeatToggle.interactable = isActive;
        _asyncRepeatToggle.interactable = isActive;
        _MTReapeatToggle.interactable = isActive;
        _aStarReapeatToggle.interactable = isActive;
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
