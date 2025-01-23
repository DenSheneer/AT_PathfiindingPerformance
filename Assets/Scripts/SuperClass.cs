using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SuperClass : MonoBehaviour
{
    public static SuperClass Instance { get; private set; }


    [Header("Scene references")]
    [SerializeField] private TMP_InputField _inputX;
    [SerializeField] private TMP_InputField _inputY;
    [SerializeField] private TMP_InputField _pfCooldownMS;
    [SerializeField] private TMP_InputField _inputRandomSeed;
    [SerializeField] private DungeonGenerator _dungeon;

    private int randomSeed = 0;
    private System.Random randomClass;

    public int PathfindCooldownMS { get { return Int32.Parse(_pfCooldownMS.text); } }
    public int SizeX { get { return Int32.Parse(_inputX.text); } }
    public int SizeY { get { return Int32.Parse(_inputY.text); } }
    public int RandomSeed { get { return randomSeed; } }
    public System.Random Random { get { return randomClass; } }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        _dungeon.OnDungeonGenerateStart += updateUIStart;
    }

    private void updateUIStart()
    {
        randomSeed = Int32.Parse(_inputRandomSeed.text);
        randomSeed = randomSeed == 0 ? Guid.NewGuid().GetHashCode() : randomSeed;
        _inputRandomSeed.text = randomSeed.ToString();
        randomClass = new System.Random(randomSeed);
    }
}