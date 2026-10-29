using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ColorBlockCamera : MonoBehaviour
{
    [Header("<< Cinemachine >>")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private ColorBlockPlayer spectatorTarget;
    private bool isSpectating;

    private void Update()
    {
        if (!isSpectating)
            return;

        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SwitchSpectatorTarget();
            return;
        }

        if (spectatorTarget == null || spectatorTarget.Object == null)
        {
            ColorBlockPlayer previousTarget = spectatorTarget;
            spectatorTarget = null;

            SwitchSpectatorTarget(previousTarget);
        }
    }

    public void SetTarget(Transform target)
    {
        if (cinemachineCamera == null)
        {
            Debug.LogError("[ColorBlockCamera] Cinemachine Camera가 연결되지 않았습니다.");
            return;
        }

        if (target == null)
        {
            Debug.LogError("[ColorBlockCamera] Target이 없습니다.");
            return;
        }

        cinemachineCamera.Target.TrackingTarget = target;

        Debug.Log($"[ColorBlockCamera] Cinemachine Tracking Target 연결 완료 : {target.name}");
    }

    // 관전 시작
    public void StartSpectating(ColorBlockPlayer target)
    {
        if (target == null || target.Object == null || target.camPos == null)
            return;

        isSpectating = true;
        spectatorTarget = target;

        SetTarget(target.camPos);

        Debug.Log($"[ColorBlockCamera] 관전 시작 | Player={target.Object.InputAuthority}");
    }

    public void OnPlayerDespawned(ColorBlockPlayer player)
    {
        if (!isSpectating || spectatorTarget != player)
            return;

        spectatorTarget = null;

        SwitchSpectatorTarget(player);
    }

    private void SwitchSpectatorTarget(ColorBlockPlayer excludePlayer = null)
    {
        ColorBlockPlayer[] players =
            FindObjectsByType<ColorBlockPlayer>(
                FindObjectsSortMode.InstanceID);

        List<ColorBlockPlayer> candidates = new List<ColorBlockPlayer>();

        foreach (ColorBlockPlayer player in players)
        {
            if (player == null || player == excludePlayer)
                continue;

            if (player.Object == null || player.camPos == null)
                continue;

            candidates.Add(player);
        }

        if (candidates.Count == 0)       
           
            return;
        

        int nextIndex = 0;

        if (spectatorTarget != null)
        {
            int currentIndex = candidates.IndexOf(spectatorTarget);

            if (currentIndex >= 0)
                nextIndex = (currentIndex + 1) % candidates.Count;
        }

        StartSpectating(candidates[nextIndex]);
    }
}