using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField]
    private Transform sounds;
    [SerializeField]
    private AudioSource check;
    [SerializeField]
    private AudioSource capture;
    [SerializeField]
    private AudioSource castle;
    [SerializeField]
    private AudioSource promote;
    [SerializeField]
    private AudioSource move;

    public void PlaySound(Move _move, bool _check)
    {
        if (_check)
        {
            check.Play();
        }
        else if (_move.Attack != null)
        {
            capture.Play();
        }
        else if (_move.Castle != null)
        {
            castle.Play();
        }
        else if (_move.Promote != null)
        {
            promote.Play();
        }
        else
        {
            move.Play();
        }
    }
}
