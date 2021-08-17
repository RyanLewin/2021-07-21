using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillSwitch : MonoBehaviour
{
    [SerializeField] private int teamNumber;
    public int GetTeamNumber { get { return teamNumber; } }
}
