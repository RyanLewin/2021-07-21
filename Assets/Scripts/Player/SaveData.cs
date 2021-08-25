using System.Collections;
using System.Collections.Generic;
using System;
using PropertyListenerTool;

[Serializable]
public class SaveData
{
    public string playerName = "";
    public int points = 1000;
    public string playerUsername = "";
    public string playerPassword = "";

    public float volumeSFX = 1f;
    public bool isMusicMuted = false;
    public float volumeMusic = .1f;
    public bool isSFXMuted = false;
}

