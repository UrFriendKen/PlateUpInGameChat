﻿using Kitchen;
using KitchenInGameChat.Commands;
using KitchenLib.Utils;
using KitchenMods;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenInGameChat.MessageWindow
{
    public enum MessageWindowStyle
    {
        Normal
    }

    public struct NewWindowRequest
    {
        public string ModGUID;
        public string Name;
        public bool HideName;
        public float MessageTimeOut;
        public int MaxMessageCount;
        public MessageWindowStyle Style;
        public bool IsReadOnly;
        public bool DoNotDraw;
        public bool CanDrag;
        public BaseCommandSet CommandHandler;

        public int ID => MessageWindowController.GetWindowID(ModGUID, Name);

        public NewWindowRequest()
        {
        }
    }

    public struct StaticMessageRequest
    {
        public int TargetWindowID;
        public int InputSource;
        public string Owner;
        public string Text;
        public bool IsColorOverride;
        public Color ColorOverride;
        public float MessageTimeoutOverride;

        public bool IsPlayerMessage => InputSource != -1;

        public StaticMessageRequest(StaticMessageRequest copy)
        {
            TargetWindowID = copy.TargetWindowID;
            InputSource = copy.InputSource;
            Owner = copy.Owner;
            Text = copy.Text;
            IsColorOverride = copy.IsColorOverride;
            ColorOverride = copy.ColorOverride;
            MessageTimeoutOverride = copy.MessageTimeoutOverride;
        }

        public static StaticMessageRequest FromCMessageRequest(CMessageRequest cMessageRequest)
        {
            return new StaticMessageRequest()
            {
                TargetWindowID = cMessageRequest.WindowID,
                InputSource = cMessageRequest.InputSource,
                Owner = cMessageRequest.Owner.Value,
                Text = cMessageRequest.Text.Value,
                IsColorOverride = false,
                ColorOverride = default
            };
        }
    }

    public struct CMessageRequest : IComponentData, IModComponent
    {
        public int WindowID;
        public int InputSource;
        public FixedString128 Owner;
        public FixedString512 Text;
    }

    public struct CMessageWindow : IComponentData, IModComponent
    {
        public int ID;
        public FixedString512 Name;
        public bool HideName;
        public int LastMessageIndex;
        public float MessageTimeOut;
        public int MaxMessageCount;
        public MessageWindowStyle Style;
        public bool IsReadOnly;
        public bool DoNotDraw;
        public bool CanDrag;

        public static CMessageWindow FromNewWindowRequest(NewWindowRequest request)
        {
            return new CMessageWindow()
            {
                ID = request.ID,
                Name = request.Name,
                HideName = request.HideName,
                LastMessageIndex = 0,
                MessageTimeOut = request.MessageTimeOut,
                MaxMessageCount = request.MaxMessageCount,
                Style = request.Style,
                IsReadOnly = request.IsReadOnly,
                DoNotDraw = request.DoNotDraw,
                CanDrag = request.CanDrag
            };
        }
    }

    public struct CMessageWindowPreference : IComponentData, IModComponent
    {
        public int WindowID;
    }

    [InternalBufferCapacity(10)]
    public struct CMessage : IBufferElementData
    {
        public int WindowID;
        public int Index;
        public bool HasTimeOut;
        public float TimeRemaining;
        public int InputSource;
        public FixedString128 Owner;
        public FixedString512 Text;
        public bool IsColorOverride;
        public Color ColorOverride;

        public static CMessage Create(int windowId, int index, bool hasTimeOut, float timeRemaining, int inputSource, string owner, string message, bool isColorOverride = false, Color colorOverride = default)
        {
            return new CMessage()
            {
                WindowID = windowId,
                Index = index,
                HasTimeOut = hasTimeOut,
                TimeRemaining = timeRemaining,
                InputSource = inputSource,
                Owner = new FixedString128(owner),
                Text = new FixedString512(message),
                IsColorOverride = isColorOverride,
                ColorOverride = colorOverride
            };
        }

        public static CMessage FromWindowAndMessageRequest(CMessageWindow window, StaticMessageRequest messageRequest)
        {
            float timeout = messageRequest.MessageTimeoutOverride > 0f ? messageRequest.MessageTimeoutOverride : window.MessageTimeOut;
            return new CMessage()
            {
                WindowID = messageRequest.TargetWindowID,
                Index = window.LastMessageIndex,
                HasTimeOut = timeout > 0f,
                TimeRemaining = timeout,
                InputSource = messageRequest.InputSource,
                Owner = new FixedString128(messageRequest.Owner),
                Text = new FixedString512(messageRequest.Text),
                IsColorOverride = messageRequest.IsColorOverride,
                ColorOverride = messageRequest.ColorOverride
            };
        }

        public CMessage UpdateTimeRemaining(float timePassed)
        {
            TimeRemaining -= timePassed;
            return this;
        }
    }

    public class MessageWindowController : GameSystemBase
    {
        public static ViewType MessageWindowViewType => (ViewType)VariousUtils.GetID($"{Main.MOD_GUID}:MessageWindowView");

        private static Queue<StaticMessageRequest> _staticMessageRequests = new Queue<StaticMessageRequest>();
        private static Queue<NewWindowRequest> _newWindowRequests = new Queue<NewWindowRequest>();

        private static Dictionary<int, BaseCommandSet> _callbacks = new Dictionary<int, BaseCommandSet>();

        EntityContext ctx;
        EntityQuery _messageWindows;
        EntityQuery _messageRequests;

        protected override void Initialise()
        {
            ctx = new EntityContext(EntityManager);
            _messageWindows = GetEntityQuery(typeof(CMessageWindow));
            _messageRequests = GetEntityQuery(typeof(CMessageRequest));
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> windowEntities = _messageWindows.ToEntityArray(Allocator.Temp);
            using NativeArray<CMessageWindow> messageWindows = _messageWindows.ToComponentDataArray<CMessageWindow>(Allocator.Temp);
            using NativeArray<CMessageRequest> messageRequests = _messageRequests.ToComponentDataArray<CMessageRequest>(Allocator.Temp);
            Dictionary<int, (CMessageWindow, DynamicBuffer<CMessage>)> windowsAndBuffers = new Dictionary<int, (CMessageWindow, DynamicBuffer<CMessage>)>();
            for (int i = 0; i < windowEntities.Length; i++)
            {
                Entity windowEntity = windowEntities[i];
                CMessageWindow messageWindow = messageWindows[i];
                DynamicBuffer<CMessage> messages = ctx.GetBuffer<CMessage>(windowEntity);

                if (!windowsAndBuffers.ContainsKey(messageWindow.ID))
                    windowsAndBuffers.Add(messageWindow.ID, (messageWindow, messages));

                UpdateMessageTimeRemaining(messageWindow, ref messages);

                Set(windowEntity, messageWindow);
            }

            for (int i = 0; i < messageRequests.Length; i++)
            {
                CMessageRequest messageRequest = messageRequests[i];
                _staticMessageRequests.Enqueue(StaticMessageRequest.FromCMessageRequest(messageRequest));
            }
            EntityManager.DestroyEntity(_messageRequests);

            HandleStaticMessageRequests(windowsAndBuffers);
            HandleNewWindowRequests(windowsAndBuffers.Keys.ToHashSet());
        }

        protected void UpdateMessageTimeRemaining(CMessageWindow messageWindow, ref DynamicBuffer<CMessage> messages)
        {
            float dt = Time.DeltaTime;
            int counter = 0;
            for (int i = messages.Length - 1; i > -1; i--)
            {
                counter++;
                CMessage message = messages[i];
                if (message.HasTimeOut)
                {
                    messages[i] = message.UpdateTimeRemaining(dt);
                }
                if (message.HasTimeOut && message.TimeRemaining < 0f || counter > messageWindow.MaxMessageCount)
                {
                    messages.RemoveAt(i);
                }
            }
        }

        protected void HandleStaticMessageRequests(Dictionary<int, (CMessageWindow, DynamicBuffer<CMessage>)> windowsAndBuffers)
        {
            if (_staticMessageRequests.Count > 0)
            {
                StaticMessageRequest messageRequest = _staticMessageRequests.Dequeue();
                if (!windowsAndBuffers.TryGetValue(messageRequest.TargetWindowID, out (CMessageWindow, DynamicBuffer<CMessage>) windowAndBuffer))
                {
                    Main.LogError($"Failed to send text! Window with ID {messageRequest.TargetWindowID} does not exist.");
                    return;
                }

                bool echo = true;
                StaticMessageRequest? outputMessage = null;
                string modifiedText = null;
                if (_callbacks.TryGetValue(messageRequest.TargetWindowID, out BaseCommandSet commandHandler))
                    echo = commandHandler.Run(new StaticMessageRequest(messageRequest), out modifiedText, out outputMessage);

                if (!modifiedText.IsNullOrEmpty())
                    messageRequest.Text = modifiedText;

                CMessageWindow messageWindow = windowAndBuffer.Item1;
                DynamicBuffer<CMessage> messageBuffer = windowAndBuffer.Item2;
                if (echo)
                {
                    messageBuffer.Add(CMessage.FromWindowAndMessageRequest(messageWindow, messageRequest));
                }
                if (commandHandler != default && outputMessage != null)
                {
                    messageBuffer.Add(CMessage.FromWindowAndMessageRequest(messageWindow, outputMessage.Value));
                }
            }
        }

        protected void HandleNewWindowRequests(HashSet<int> existingWindowIds)
        {
            if (_newWindowRequests.Count > 0)
            {
                NewWindowRequest newWindowRequest = _newWindowRequests.Dequeue();
                if (existingWindowIds.Contains(newWindowRequest.ID))
                {
                    Main.LogError($"Window with ID {newWindowRequest.Name} ({newWindowRequest.ID}) already exists! Use a different window name.");
                }

                if (newWindowRequest.CommandHandler != null && !_callbacks.ContainsKey(newWindowRequest.ID))
                {
                    newWindowRequest.CommandHandler.RegisterCommands();
                    _callbacks.Add(newWindowRequest.ID, newWindowRequest.CommandHandler);
                }

                Entity newWindow = EntityManager.CreateEntity();
                Set(newWindow, new CDoNotPersist());
                Set(newWindow, new CPersistThroughSceneChanges());
                Set(newWindow, CMessageWindow.FromNewWindowRequest(newWindowRequest));
                EntityManager.AddBuffer<CMessage>(newWindow);
                Set(newWindow, new CRequiresView()
                {
                    Type = MessageWindowViewType,
                    PhysicsDriven = false,
                    ViewMode = ViewMode.Screen
                });
            }
        }

        private static int PrivateCreateMessageWindow(string modGUID, string name, bool hideName, bool canDrag, float messageTimeOutSeconds, int maxMessageCount, MessageWindowStyle style, bool isReadOnly, bool doNotDraw, BaseCommandSet commandSet)
        {
            if (modGUID.IsNullOrEmpty())
            {
                Main.LogError("CreateMessageWindow: modGUID cannot be null or empty!");
                return -1;
            }

            if (name.IsNullOrEmpty())
            {
                Main.LogError("CreateMessageWindow: name cannot be null or empty!");
                return -1;
            }

            NewWindowRequest request = new NewWindowRequest()
            {
                ModGUID = modGUID,
                Name = name,
                HideName = hideName,
                MessageTimeOut = messageTimeOutSeconds,
                MaxMessageCount = maxMessageCount,
                Style = style,
                IsReadOnly = isReadOnly,
                DoNotDraw = doNotDraw,
                CanDrag = canDrag,
                CommandHandler = commandSet
            };

            if (Session.CurrentGameNetworkMode == GameNetworkMode.Host)
            {
                _newWindowRequests.Enqueue(request);
            }

            return request.ID;
        }

        public static int CreateMessageWindow(string modGUID, string name, bool hideName = false, bool canDrag = true, float messageTimeOutSeconds = 0f, int maxMessageCount = 10, MessageWindowStyle style = MessageWindowStyle.Normal, bool isReadOnly = false, bool doNotDraw = false, BaseCommandSet commandSet = null)
        {
            return PrivateCreateMessageWindow(modGUID, name, hideName, canDrag, messageTimeOutSeconds, maxMessageCount, style, isReadOnly, doNotDraw, commandSet);
        }

        public static int GetWindowID(string modGUID, string windowName)
        {
            return VariousUtils.GetID($"{modGUID}:{windowName}");
        }

        public static void SendMessage(int targetWindowID, string displayName, string text, Color? colorOverride = null, float messageTimeoutOverride = 0f)
        {
            StaticMessageRequest request = new StaticMessageRequest()
            {
                TargetWindowID = targetWindowID,
                InputSource = -1,
                Owner = displayName,
                Text = text,
                IsColorOverride = colorOverride.HasValue,
                ColorOverride = colorOverride.HasValue ? colorOverride.Value : default,
                MessageTimeoutOverride = messageTimeoutOverride
            };
            _staticMessageRequests.Enqueue(request);
        }
    }
}
