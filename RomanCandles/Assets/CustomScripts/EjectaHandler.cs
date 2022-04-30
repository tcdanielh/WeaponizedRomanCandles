using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class EjectaHandler : ScriptableObject
{
    [SerializeField] HashSet<FireworkSim> fireworks = new HashSet<FireworkSim>();
    public void Reset()
    {
        fireworks = new HashSet<FireworkSim>();
    }

    public ScreenWriter.Ejecta[] getEjectas()
    {
        List<ScreenWriter.Ejecta> e = new List<ScreenWriter.Ejecta>();
        foreach (FireworkSim f in fireworks)
        {
            e.AddRange(f.es);
        }
        return e.ToArray();
    }

    public void addFirework(FireworkSim f)
    {
        fireworks.Add(f);
    }

    public void removeFirework(FireworkSim f)
    {
        fireworks.Remove(f);
    }
}
