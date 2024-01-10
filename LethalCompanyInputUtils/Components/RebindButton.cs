﻿using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalCompanyInputUtils.Components;

public class RebindButton : MonoBehaviour
{
    public TextMeshProUGUI? bindLabel;
    private RemappableKey? _key;
    private bool _isBaseGame;
    
    private static MethodInfo? _setChangesNotAppliedMethodInfo;

    public void SetKey(RemappableKey key, bool isBaseGame)
    {
        _key = key;
        _isBaseGame = isBaseGame;
        
        UpdateState();
    }

    public void UpdateState()
    {
        if (bindLabel is null)
            return;

        if (_key is null)
            return;

        var bindingIndex = GetRebindingIndex();
        var action = _key.currentInput.action;
        
        var bindPath = InputControlPath.ToHumanReadableString(action.bindings[bindingIndex].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
        bindLabel.SetText(bindPath);
    }

    private int GetRebindingIndex()
    {
        if (_key is null)
            return -1;

        var action = _key.currentInput.action;

        return _key.rebindingIndex < 0 ? action.GetBindingIndexForControl(action.controls[0]) : _key.rebindingIndex;
    }

    public void StartRebinding()
    {
        if (_key is null)
            return;

        var rebindIndex = GetRebindingIndex();
        
        _key.currentInput.action.Disable();

        if (_key.gamepadOnly)
        {
            RebindGamepad(_key.currentInput, rebindIndex);
        }
        else
        {
            RebindKbm(_key.currentInput, rebindIndex);
        }
    }

    private void FinishRebinding()
    {
        if (_key is null)
            return;
        
        _key.currentInput.action.Enable();
        UpdateState();
    }

    private void RebindKbm(InputActionReference inputActionRef, int rebindIndex)
    {
        inputActionRef.action.PerformInteractiveRebinding(rebindIndex)
            .OnMatchWaitForAnother(0.1f)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsHavingToMatchPath("<Mouse>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation => OnRebindComplete(operation, this))
            .Start();
    }

    private void RebindGamepad(InputActionReference inputActionRef, int rebindIndex)
    {
        inputActionRef.action.PerformInteractiveRebinding(rebindIndex)
            .OnMatchWaitForAnother(0.1f)
            .WithControlsHavingToMatchPath("<Gamepad>")
            .OnComplete(operation => OnRebindComplete(operation, this))
            .Start();
    }
    
    private static void OnRebindComplete(InputActionRebindingExtensions.RebindingOperation operation, RebindButton instance)
    {
        if (!operation.completed)
            return;

        if (instance._isBaseGame)
        {
            IngamePlayerSettings.Instance.unsavedSettings.keyBindings =
                IngamePlayerSettings.Instance.playerInput.actions.SaveBindingOverridesAsJson();

            _setChangesNotAppliedMethodInfo ??= AccessTools.Method(typeof(IngamePlayerSettings), "SetChangesNotAppliedTextVisible");
            _setChangesNotAppliedMethodInfo.Invoke(IngamePlayerSettings.Instance, [true]);
        }
        
        instance.FinishRebinding();
    }
}