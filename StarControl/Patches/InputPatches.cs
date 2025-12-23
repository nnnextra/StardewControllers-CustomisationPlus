using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace StarControl.Patches;

internal static class InputPatches
{
    public static Buttons? ToolUseButton { get; set; }
    public static TimeSpan RightStickSuppressionDuration { get; set; } = TimeSpan.FromSeconds(0.5);
    public static bool ForceHideCursor { get; set; }

    private static double rightStickSuppressUntilMs;
    private static bool rightStickCursorAwaitingMove;
    private static double rightStickCursorAwaitMoveAfterMs;

    private static readonly FieldInfo GameInputField = AccessTools.Field(
        typeof(Game1),
        nameof(Game1.input)
    );
    private static readonly MethodInfo GetGamePadStateMethod = AccessTools.Method(
        typeof(InputState),
        nameof(InputState.GetGamePadState)
    );
    private static readonly MethodInfo GetRemappedGamePadStateMethod = AccessTools.Method(
        typeof(InputPatches),
        nameof(GetRemappedGamePadState)
    );
    private static readonly MethodInfo GetRemappedOldPadStateMethod = AccessTools.Method(
        typeof(InputPatches),
        nameof(GetRemappedOldPadState)
    );
    private static readonly FieldInfo OldPadStateField = AccessTools.Field(
        typeof(Game1),
        nameof(Game1.oldPadState)
    );

    public static IEnumerable<CodeInstruction> GenericGamePadStateTranspiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator gen,
        MethodBase original
    )
    {
        return new CodeMatcher(instructions, gen)
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldsfld, GameInputField),
                new CodeMatch(OpCodes.Callvirt, GetGamePadStateMethod)
            )
            .Repeat(
                matcher =>
                    matcher
                        .SetAndAdvance(OpCodes.Call, GetRemappedGamePadStateMethod)
                        .RemoveInstructions(1),
                _ =>
                    throw new InvalidOperationException(
                        "Couldn't find call to Game1.input.GetGamePadState() in the method body"
                    )
            )
            .InstructionEnumeration();
    }

    public static IEnumerable<CodeInstruction> GenericOldPadStateTranspiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator gen,
        MethodBase original
    )
    {
        var stateLocal = gen.DeclareLocal(typeof(GamePadState));
        return new CodeMatcher(instructions, gen)
            .MatchStartForward(new CodeMatch(OpCodes.Ldsflda, OldPadStateField))
            .Repeat(
                matcher =>
                    matcher
                        .SetAndAdvance(OpCodes.Call, GetRemappedOldPadStateMethod)
                        .Insert(
                            new CodeInstruction(OpCodes.Stloc_S, stateLocal.LocalIndex),
                            new CodeInstruction(OpCodes.Ldloca_S, stateLocal.LocalIndex)
                        ),
                _ =>
                    throw new InvalidOperationException(
                        "Couldn't find call to Game1.oldPadState in the method body"
                    )
            )
            .InstructionEnumeration();
    }

    private static GamePadState GetRemappedGamePadState()
    {
        var gamepadState = Game1.input.GetGamePadState();
        // We are going to be suppressing our own input in the RemappingController to prevent
        // vanilla function, e.g. B button is mapped to a tool and therefore suppressed to avoid
        // bringing up the menu. This means we need to bypass that suppression in order to determine
        // if the button is actually being pressed, which requires going to the "raw" state
        // unmodified by SMAPI, similar to the hack used for trigger buttons in MenuToggle.
        //
        // Note however that we don't want to actually use this raw state as the _result_, since it
        // may lose other nuances, for example other unrelated buttons being suppressed. Only want
        // to pull remapped buttons from the raw state into the real un-remapped state.
        var rawState =
            Game1.playerOneIndex >= PlayerIndex.One
                ? GamePad.GetState(Game1.playerOneIndex)
                : new();
        RemapGamePadState(ref gamepadState, rawState);
        return gamepadState;
    }

    public static void SuppressRightStickFor(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }
        var nowMs = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
        rightStickSuppressUntilMs = Math.Max(
            rightStickSuppressUntilMs,
            nowMs + duration.TotalMilliseconds
        );
        rightStickCursorAwaitingMove = true;
        rightStickCursorAwaitMoveAfterMs = Math.Max(
            rightStickCursorAwaitMoveAfterMs,
            rightStickSuppressUntilMs
        );
    }

    public static void AwaitRightStickMoveForCursor()
    {
        var nowMs = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
        rightStickCursorAwaitingMove = true;
        rightStickCursorAwaitMoveAfterMs = Math.Max(rightStickCursorAwaitMoveAfterMs, nowMs);
    }

    public static void GetGamePadState_Postfix(ref GamePadState __result)
    {
        var nowMs = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
        if (
            rightStickCursorAwaitingMove
            && nowMs >= rightStickCursorAwaitMoveAfterMs
            && __result.ThumbSticks.Right.Length() > 0.1f
        )
        {
            rightStickCursorAwaitingMove = false;
        }
        if (!IsRightStickSuppressed())
        {
            return;
        }
        var sticks = __result.ThumbSticks;
        if (sticks.Right == Vector2.Zero)
        {
            return;
        }
        __result = new GamePadState(
            new GamePadThumbSticks(sticks.Left, Vector2.Zero),
            __result.Triggers,
            __result.Buttons,
            __result.DPad
        );
    }

    public static void ShouldDrawMouseCursor_Postfix(ref bool __result)
    {
        if (ForceHideCursor || rightStickCursorAwaitingMove || IsRightStickSuppressed())
        {
            __result = false;
        }
    }

    public static bool DrawMouseCursor_Prefix()
    {
        return !ForceHideCursor && !rightStickCursorAwaitingMove && !IsRightStickSuppressed();
    }

    private static bool IsRightStickSuppressed()
    {
        var nowMs = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
        return nowMs > 0 && nowMs < rightStickSuppressUntilMs;
    }

    private static GamePadState GetRemappedOldPadState()
    {
        var gamepadState = Game1.oldPadState;
        RemapGamePadState(ref gamepadState);
        return gamepadState;
    }

    private static void RemapGamePadState(
        ref GamePadState gamepadState,
        GamePadState? rawState = null
    )
    {
        if (ToolUseButton is null)
        {
            return;
        }
        var downButtons = gamepadState.Buttons._buttons;
        var remapState = rawState ?? gamepadState;
        if (remapState.IsButtonDown(ToolUseButton.Value))
        {
            downButtons |= Buttons.X;
        }
        gamepadState.Buttons = new(downButtons);
    }
}
