using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    private Transform sounds;
    private AudioSource check;
    private AudioSource capture;
    private AudioSource castle;
    private AudioSource promote;
    private AudioSource move;

    private void Awake()
    {
        sounds = GameObject.Find("AudioController").transform;
        check = sounds.Find("AudioSourceCheck").GetComponent<AudioSource>();
        capture = sounds.Find("AudioSourceCapture").GetComponent<AudioSource>();
        castle = sounds.Find("AudioSourceCastle").GetComponent<AudioSource>();
        promote = sounds.Find("AudioSourcePromote").GetComponent<AudioSource>();
        move = sounds.Find("AudioSourceMove").GetComponent<AudioSource>();
    }

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
