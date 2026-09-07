using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MachineLearningFPS.Environment;
using MachineLearningFPS.MachineLearning;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MachineLearningFPS.Tests.PlayMode
{
    public class ArenaControllerSpawnTests
    {
        private const string AgentPrefabPath = "Assets/Prefabs/Character/MLPlayer.prefab";

        private GameObject _agentInstance;
        private GameObject _arenaObject;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_agentInstance != null) Object.Destroy(_agentInstance);
            if (_arenaObject != null) Object.Destroy(_arenaObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResetArena_PlacesAgentAtPredefinedSpawnPoint()
        {
            GameObject agentPrefab = null;
#if UNITY_EDITOR
            agentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AgentPrefabPath);
#endif
            Assert.IsNotNull(agentPrefab, $"Agent prefab not found at {AgentPrefabPath}.");

            _agentInstance = Object.Instantiate(agentPrefab);
            yield return null;

            MLController agent = _agentInstance.GetComponent<MLController>();
            Assert.IsNotNull(agent, "Spawned prefab has no MLController component.");

            Vector3 expectedSpawnPosition = new Vector3(12f, 1f, -7f);
            ArenaController arenaController = BuildArenaWithSingleSpawnPoint(expectedSpawnPosition);

            arenaController.ResetArena(new List<MLController> { agent });

            Assert.AreEqual(expectedSpawnPosition, agent.transform.position);

            CharacterController characterController = agent.GetComponent<CharacterController>();
            Assert.IsNotNull(characterController);
            Assert.IsTrue(characterController.enabled);
        }

        private ArenaController BuildArenaWithSingleSpawnPoint(Vector3 spawnPosition)
        {
            _arenaObject = new GameObject("TestArena");
            _arenaObject.SetActive(false);

            ArenaController arenaController = _arenaObject.AddComponent<ArenaController>();

            GameObject spawnPointsParent = new GameObject("SpawnPoints");
            spawnPointsParent.transform.SetParent(_arenaObject.transform);

            GameObject spawnPoint = new GameObject("SpawnPoint0");
            spawnPoint.transform.SetParent(spawnPointsParent.transform);
            spawnPoint.transform.position = spawnPosition;

            SetPrivateField(arenaController, "_spawnPointsParent", spawnPointsParent.transform);
            SetPrivateField(arenaController, "_applyStartingRotationRandomization", false);
            SetPrivateField(arenaController, "_agentsSpawnOffsetEnabled", false);

            _arenaObject.SetActive(true);

            return arenaController;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }
    }
}
