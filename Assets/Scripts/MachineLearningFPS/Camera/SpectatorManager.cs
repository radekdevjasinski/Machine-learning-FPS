using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

using MachineLearningFPS.Character;

namespace MachineLearningFPS.Camera
{

    public class SpectatorManager : MonoBehaviour
    {
        [Header("Components")]
        public UnityEngine.Camera mainCamera;
        public FreeCam freeCam;

        [Header("Per-Player Grade")]
        public Volume worldVolume;
        public Light sun;
        public VolumeProfile playerOneProfile;
        public VolumeProfile playerTwoProfile;
        public VolumeProfile spectatorProfile;
        public Color playerOneSunColor = new Color(1f, 0.6f, 0.3f);
        public Color playerTwoSunColor = new Color(0.55f, 0.7f, 1f);
        public Color spectatorSunColor = Color.white;

        public List<Transform> targetsHeads = new List<Transform>();

        [Header("Input Actions")]
        public InputActionReference freeCamToggleAction;
        public InputActionReference nextBotAction;
        public InputActionReference prevBotAction;

        private InputAction toggleColorSchemeAction;

        private enum ColorGrade { PlayerOne, PlayerTwo, Spectator }

        private int currentTargetIndex = -1;
        private bool isFreeCam = true;
        private ColorGrade debugGrade = ColorGrade.PlayerOne;

        public static event Action<Transform> OnActiveTargetChanged;

        private void NotifyActiveTargetChanged()
        {
            OnActiveTargetChanged?.Invoke(GetCurrentTarget());
        }

        void OnEnable()
        {
            freeCamToggleAction.action.Enable();
            nextBotAction.action.Enable();
            prevBotAction.action.Enable();

            toggleColorSchemeAction = nextBotAction.action.actionMap.FindAction("ToggleColorScheme");
            toggleColorSchemeAction?.Enable();

            freeCamToggleAction.action.performed += _ => EnableFreeCam();
            nextBotAction.action.performed += _ => NextTarget();
            prevBotAction.action.performed += _ => PrevTarget();
            if (toggleColorSchemeAction != null) toggleColorSchemeAction.performed += _ => ToggleColorScheme();
            PerspectiveController.OnCharacterAppear += AddTarget;
            PerspectiveController.OnCharacterDisappear += RemoveTarget;
        }

        private void OnDisable()
        {
            freeCamToggleAction.action.Disable();
            nextBotAction.action.Disable();
            prevBotAction.action.Disable();
            toggleColorSchemeAction?.Disable();

            freeCamToggleAction.action.performed -= _ => EnableFreeCam();
            nextBotAction.action.performed -= _ => NextTarget();
            prevBotAction.action.performed -= _ => PrevTarget();
            if (toggleColorSchemeAction != null) toggleColorSchemeAction.performed -= _ => ToggleColorScheme();
            PerspectiveController.OnCharacterAppear -= AddTarget;
            PerspectiveController.OnCharacterDisappear -= RemoveTarget;
        }

        void AddTarget(Transform head)
        {
            if (!targetsHeads.Contains(head))
            {
                targetsHeads.Add(head);
            }
        }

        void RemoveTarget(Transform head)
        {
            int removedIndex = targetsHeads.IndexOf(head);

            if (removedIndex == -1) return;

            if (!isFreeCam && currentTargetIndex == removedIndex)
            {
                currentTargetIndex = -1;
                EnableFreeCam();
            }

            targetsHeads.RemoveAt(removedIndex);

            if (removedIndex < currentTargetIndex)
            {
                currentTargetIndex--;
            }

            if (currentTargetIndex >= targetsHeads.Count)
            {
                currentTargetIndex = targetsHeads.Count - 1;
            }
        }

        void Start()
        {
            EnableFreeCam();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            PerspectiveController[] existingTargets = FindObjectsByType<PerspectiveController>();
            foreach (var target in existingTargets)
            {
                AddTarget(target?.Head);
            }
        }

        private void EnableFreeCam()
        {
            isFreeCam = true;
            mainCamera.transform.SetParent(null);
            freeCam.enabled = true;
            freeCam.SetPosition(new Vector3(0, 10, 0), Quaternion.Euler(75, 0, 0));

            if (currentTargetIndex >= 0 && currentTargetIndex < targetsHeads.Count)
            {
                DeactivateTarget(targetsHeads[currentTargetIndex]);
            }

            ApplySpectatorGrade();

            NotifyActiveTargetChanged();
        }

        private void NextTarget()
        {
            if (targetsHeads.Count == 0) return;

            if (!isFreeCam) DeactivateTarget(targetsHeads[currentTargetIndex]);

            isFreeCam = false;

            currentTargetIndex = (currentTargetIndex + 1) % targetsHeads.Count;

            ActivateTarget(targetsHeads[currentTargetIndex]);
            NotifyActiveTargetChanged();
        }

        private void PrevTarget()
        {
            if (targetsHeads.Count == 0) return;

            if (!isFreeCam) DeactivateTarget(targetsHeads[currentTargetIndex]);

            isFreeCam = false;
            currentTargetIndex = (currentTargetIndex - 1 + targetsHeads.Count) % targetsHeads.Count;

            ActivateTarget(targetsHeads[currentTargetIndex]);
            NotifyActiveTargetChanged();
        }

        private void ActivateTarget(Transform head)
        {
            freeCam.enabled = false;

            mainCamera.transform.SetParent(head);
            mainCamera.transform.localPosition = Vector3.zero;
            mainCamera.transform.localRotation = Quaternion.identity;

            ApplyGradeForTarget(head);

            PerspectiveController perspective = head.GetComponentInParent<PerspectiveController>();
            if (perspective != null) perspective.SetHideView();

            PlayerController player = head.GetComponentInParent<PlayerController>();
            if (player != null) player.enabled = true;

            UnityEngine.Camera UICamera = head.GetComponentInChildren<UnityEngine.Camera>();
            if (UICamera != null && UICamera != mainCamera)
            {
                UICamera.enabled = true;
            }
        }

        private void DeactivateTarget(Transform head)
        {
            if (head == null) return;

            PerspectiveController perspective = head.GetComponentInParent<PerspectiveController>();
            if (perspective != null) perspective.SetDefaultView();

            PlayerController player = head.GetComponentInParent<PlayerController>();
            if (player != null) player.enabled = false;

            UnityEngine.Camera UICamera = head.GetComponentInChildren<UnityEngine.Camera>();
            if (UICamera != null && UICamera != mainCamera)
            {
                UICamera.enabled = false;
            }
        }
        // Grade must follow the target's actual team (TeamID, same source CharacterColor uses for
        // robot material), not its position in targetsHeads — that list's order comes from spawn/
        // registration order and has no guaranteed relationship to team, which let a blue-team robot
        // occasionally get the orange grade and vice versa.
        private void ApplyGradeForTarget(Transform head)
        {
            MLController controller = head.GetComponentInParent<MLController>();
            int teamId = controller != null ? controller.TeamID : 0;

            // playerOneProfile/playerOneSunColor = orange (team 1), playerTwoProfile/playerTwoSunColor = blue (team 0).
            ApplyPlayerGrade(teamId == 0);
        }

        private void ApplyPlayerGrade(bool isPlayerTwo)
        {
            SetGrade(isPlayerTwo ? ColorGrade.PlayerTwo : ColorGrade.PlayerOne);
        }

        private void ApplySpectatorGrade()
        {
            SetGrade(ColorGrade.Spectator);
        }

        private void SetGrade(ColorGrade grade)
        {
            debugGrade = grade;

            VolumeProfile profile = playerOneProfile;
            Color sunColor = playerOneSunColor;
            bool fogEnabled = true;

            switch (grade)
            {
                case ColorGrade.PlayerTwo:
                    profile = playerTwoProfile;
                    sunColor = playerTwoSunColor;
                    break;
                case ColorGrade.Spectator:
                    profile = spectatorProfile;
                    sunColor = spectatorSunColor;
                    fogEnabled = false;
                    break;
            }

            if (worldVolume != null)
            {
                worldVolume.profile = profile;
            }

            if (sun != null)
            {
                sun.color = sunColor;
            }

            // Fog aids close-range FPS immersion in a bot's POV but occludes the arena overview in free/spectator cam.
            RenderSettings.fog = fogEnabled;
        }

        /// Debug-only: cycles through all three grades regardless of the active view (key 9).
        public void ToggleColorScheme()
        {
            ColorGrade nextGrade = (ColorGrade)(((int)debugGrade + 1) % 3);
            SetGrade(nextGrade);
        }

        public Transform GetCurrentTarget()
        {
            if (isFreeCam || currentTargetIndex < 0 || currentTargetIndex >= targetsHeads.Count)
            {
                return null;
            }
            return targetsHeads[currentTargetIndex];
        }
    }
}