using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool isOpponentHuman = true;
    public bool isWhiteSelected = true;
    public float playTimeAmount = 0;
    //public delegate void PlayerExited();
    //public static event PlayerExited OnExited;
    //public delegate void PlayerRestarted();
    //public static event PlayerRestarted OnRestarted;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
