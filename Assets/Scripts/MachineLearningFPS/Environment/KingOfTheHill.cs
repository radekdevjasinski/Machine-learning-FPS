using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using MachineLearningFPS.Character;

public class KingOfTheHillZone : MonoBehaviour
{
    private List<MLController> _botsInZone = new List<MLController>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var bot = other.GetComponent<MLController>();
            if (bot != null && !_botsInZone.Contains(bot))
            {
                _botsInZone.Add(bot);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var bot = other.GetComponent<MLController>();
            if (bot != null && _botsInZone.Contains(bot))
            {
                _botsInZone.Remove(bot);
            }
        }
    }

    private void FixedUpdate()
    {
        if (_botsInZone.Count == 1)
        {
            _botsInZone[0].ApplyKingOfTheHillReward(1);
        }
    }

    public void ResetZone()
    {
        _botsInZone.Clear();
    }
}