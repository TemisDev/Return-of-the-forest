using System.Collections;
using System.Reflection;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Bosque.Tests
{
    // Queued Input System events, never physical-keyboard or human acceptance evidence.
    public sealed class ArroyoInputPlayTests
    {
        private Keyboard keyboard;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
        private bool changedMode;
        [UnityTest] public IEnumerator QueuedKeysFollowFocusAndCursorGuards()
        {
            EditorSceneManager.OpenScene("Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity");
            yield return new EnterPlayMode();
            yield return null;
            var session = Object.FindFirstObjectByType<ArroyoSession>();
            var hud = Object.FindFirstObjectByType<ArroyoPrototypeHud>();
            var actor = session.Player.GetComponent<PlayerInteractor>();
            previousMode = InputSystem.settings.updateMode;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            changedMode = true;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            var helpField = typeof(ArroyoPrototypeHud).GetField("helpVisible", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(helpField, Is.Not.Null);
            bool focused = Application.isFocused;
            KeyDown(Key.F1);
            Assert.That(keyboard.f1Key.wasPressedThisFrame, Is.True, "La prueba necesita un evento real de Input System procesado.");
            Assert.That(Keyboard.current, Is.SameAs(keyboard));
            hud.SendMessage("Update");
            Assert.That((bool)helpField.GetValue(hud), Is.EqualTo(focused));
            KeyDown();
            KeyDown(Key.F1); hud.SendMessage("Update");
            Assert.That((bool)helpField.GetValue(hud), Is.False, "Segundo F1 cierra ayuda si hay foco.");
            session.Progress.TryRemove("arroyo.log");
            KeyDown(); KeyDown(Key.R);
            Assert.That(keyboard.rKey.wasPressedThisFrame, Is.True);
            hud.SendMessage("Update");
            Assert.That(session.Progress.RemovedCount, Is.EqualTo(focused ? 0 : 1));
            session.Restart();
            session.Player.Teleport(new Vector3(-.6f, 0, 10), Quaternion.identity);
            Physics.SyncTransforms();
            KeyDown(); KeyDown(Key.E);
            Assert.That(keyboard.eKey.wasPressedThisFrame, Is.True);
            actor.SendMessage("Update");
            bool acceptsInput = focused && Cursor.lockState == CursorLockMode.Locked;
            Assert.That(session.Progress.RemovedCount, Is.EqualTo(acceptsInput ? 1 : 0));
            Debug.Log("[InputV4] queued-events; focus=" + focused + "; cursorLocked=" +
                (Cursor.lockState == CursorLockMode.Locked) + "; help/reset=" + focused + "; interaction=" + acceptsInput);
            KeyDown();
        }
        private void KeyDown(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (changedMode)
            {
                InputSystem.settings.updateMode = previousMode;
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            }
            if (Application.isPlaying) yield return new ExitPlayMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
    }
}
