using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

// Shared packet-friendly card payload.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CardInfo
{
    public Card.FruitType type;
    public int count;
}
