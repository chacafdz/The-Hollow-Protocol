using System.Collections.Generic;
using UnityEngine;

// Se agrega solo al Player cuando recoge una llave o lee una pista.
public class Inventory : MonoBehaviour
{
    readonly HashSet<string> keys = new HashSet<string>();
    public int cluesFound;

    public void AddKey(string id) { keys.Add(id); }

    // Una llave vacia significa "sin llave": la puerta se abre libremente
    public bool HasKey(string id) { return string.IsNullOrEmpty(id) || keys.Contains(id); }

    public static Inventory Of(PlayerController player)
    {
        var inv = player.GetComponent<Inventory>();
        if (inv == null) inv = player.gameObject.AddComponent<Inventory>();
        return inv;
    }
}
