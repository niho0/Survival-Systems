using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameEvent
{
    public string eventName;//事件名称

    public int weight = 10;//事件权重

    public string description;//描述

    //触发条件
    public List<string> requiredFlags = new List<string>();//需要的"钥匙"

    public List<string> blockingFlags = new List<string>();//阻止的"锁"

}

