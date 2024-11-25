using UnityEngine;
using UnityEngine.UIElements;

namespace BestoNetSamples.UI
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        
        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            Button hostButton = root.Q<Button>("host-button");
            Button joinButton = root.Q<Button>("join-button");
            Label notification = root.Q<Label>("notification");
            NotificationManager.Instance.Initialize(notification);
            hostButton.clicked += () => GameStateManager.Instance.StartHosting();
            joinButton.clicked += () => GameStateManager.Instance.JoinGame();
            GameStateManager.Instance.OnNotification += HandleNotification;
        }

        private static void HandleNotification(string message)
        {
            NotificationManager.Instance.ShowNotification(message);
        }
        
        private void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnNotification -= HandleNotification;
            }
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ClearNotifications();
            }
        }
    }
}