using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BestoNetSamples.Singleton;
using BestoNetSamples.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BestoNetSamples
{
    public class NotificationManager : SingletonBehaviour<NotificationManager>
    {
        private class NotificationInfo
        {
            public string Message { get; set; }
            public float Duration { get; set; }
            public Action OnComplete { get; set; }
        }

        private readonly Queue<NotificationInfo> _notificationQueue = new();
        private bool _isShowingNotification;
        private Label _notificationLabel;
        private Coroutine _currentNotificationCoroutine;

        public void Initialize(Label notificationLabel)
        {
            _notificationLabel = notificationLabel;
            if (_isShowingNotification && _notificationQueue.Count > 0)
            {
                ShowNextNotification();
            }
        }

        public void ShowNotification(string message, float duration = 3f, Action onComplete = null)
        {
            _notificationQueue.Enqueue(new NotificationInfo
            {
                Message = message,
                Duration = duration,
                OnComplete = onComplete
            });
            if (!_isShowingNotification)
            {
                ShowNextNotification();
            }
        }

        private void ShowNextNotification()
        {
            if (_notificationQueue.Count == 0 || _notificationLabel == null)
            {
                _isShowingNotification = false;
                return;
            }
            _isShowingNotification = true;
            NotificationInfo notification = _notificationQueue.Dequeue();
            if (_currentNotificationCoroutine != null)
            {
                StopCoroutine(_currentNotificationCoroutine);
            }
            _currentNotificationCoroutine = StartCoroutine(ShowNotificationCoroutine(notification));
        }

        private IEnumerator ShowNotificationCoroutine(NotificationInfo notification)
        {
            _notificationLabel.text = notification.Message;
            _notificationLabel.AddToClassList("visible");
            yield return WaitInstructionCache.Seconds(notification.Duration);
            _notificationLabel.RemoveFromClassList("visible");
            yield return WaitInstructionCache.Seconds(0.3f);
            notification.OnComplete?.Invoke();
            ShowNextNotification();
        }

        public void ClearNotifications()
        {
            if (_currentNotificationCoroutine != null)
            {
                StopCoroutine(_currentNotificationCoroutine);
                _currentNotificationCoroutine = null;
            }
            // Execute any pending completion callbacks
            while (_notificationQueue.Count > 0)
            {
                NotificationInfo notification = _notificationQueue.Dequeue();
                notification.OnComplete?.Invoke();
            }
            _isShowingNotification = false;
            _notificationLabel?.RemoveFromClassList("visible");
        }

        protected override void OnDestroy()
        {
            ClearNotifications();
            base.OnDestroy();
        }

        public float GetRemainingNotificationsTime()
        {
            return _notificationQueue.Sum(notification => notification.Duration + 0.3f);
        }
    }
}