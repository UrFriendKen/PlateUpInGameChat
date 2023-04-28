using Controllers;
using Kitchen;
using Kitchen.NetworkSupport;
using KitchenInGameChat.Utils;
using KitchenLib.Utils;
using MessagePack;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenInGameChat.MessageWindow
{
    public class MessageWindowView : ResponsiveObjectView<MessageWindowView.ViewData, MessageWindowView.ResponseData>
    {
        private static HashSet<int> _inputBlockingWindows = new HashSet<int>();

        public static bool ShouldBlockInput => _inputBlockingWindows.Count > 0;

        internal static bool ShouldBlockInputForPlayer(int playerId)
        {
            return ShouldBlockInput && PlayerUtils.GetLocalPlayerControllerType(playerId) == ControllerType.Keyboard;
        }

        protected void AddInputBlock()
        {
            if (_id == -1 || _inputBlockingWindows.Contains(_id))
                return;
            _inputBlockingWindows.Add(_id);
        }

        protected void RemoveInputBlock()
        {
            if (_inputBlockingWindows.Contains(_id))
                _inputBlockingWindows.Remove(_id);
        }



        [MessagePackObject(false)]
        public struct Message
        {
            [Key(0)] public int InputSource;
            [Key(1)] public string Owner;
            [Key(2)] public string Text;
            [Key(3)] public bool IsColorOverride;
            [Key(4)] public Color ColorOverride;

            public override bool Equals(object obj)
            {
                if (!(obj is Message other))
                    return false;
                return GetHashCode().Equals(other.GetHashCode());
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 31 + InputSource;
                hash = hash * 31 + Owner.GetHashCode();
                hash = hash * 31 + Text.GetHashCode();
                return hash;
            }

            public static bool operator ==(Message lhs, Message rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Message lhs, Message rhs) => !(lhs == rhs);

            public static Message FromCMessage(CMessage cMessage)
            {
                return new Message()
                {
                    InputSource = cMessage.InputSource,
                    Owner = cMessage.Owner.Value,
                    Text = cMessage.Text.Value,
                    IsColorOverride = cMessage.IsColorOverride,
                    ColorOverride = cMessage.ColorOverride
                };
            }
        }

        public class UpdateView : ResponsiveViewSystemBase<ViewData, ResponseData>
        {
            EntityQuery Query;
            protected override void Initialise()
            {
                Query = GetEntityQuery(typeof(CLinkedView), typeof(CMessageWindow));
            }

            protected override void OnUpdate()
            {
                using NativeArray<Entity> entities = Query.ToEntityArray(Allocator.Temp);
                using NativeArray<CLinkedView> linkedViews = Query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using NativeArray<CMessageWindow> messageWindows = Query.ToComponentDataArray<CMessageWindow>(Allocator.Temp);

                for (int i = 0; i < linkedViews.Length; i++)
                {
                    Entity entity = entities[i];
                    CLinkedView view = linkedViews[i];
                    CMessageWindow window = messageWindows[i];

                    DynamicBuffer<CMessage> cMessages = EntityManager.GetBuffer<CMessage>(entity);

                    List<Message> messages = new List<Message>();
                    foreach (var message in cMessages)
                    {
                        messages.Add(Message.FromCMessage(message));
                    }

                    SendUpdate(view.Identifier, new ViewData()
                    {
                        Title = window.Name.Value,
                        WindowID = window.ID,
                        Messages = messages,
                        Style = window.Style,
                        HideTitle = window.HideName,
                        IsReadOnly = window.IsReadOnly,
                        DrawWindow = !window.DoNotDraw,
                        CanDrag = window.CanDrag
                    });

                    if (ApplyUpdates(view.Identifier, HandleResponse, only_final_update: false))
                    {
                    }
                }
            }

            private void HandleResponse(ResponseData responseData)
            {
                Entity entity = EntityManager.CreateEntity();
                Set(entity, new CMessageRequest()
                {
                    WindowID = responseData.WindowID,
                    InputSource = responseData.Message.InputSource,
                    Owner = responseData.Message.Owner,
                    Text = responseData.Message.Text
                });
            }
        }

        [MessagePackObject(false)]
        public class ViewData : ISpecificViewData, IViewData.ICheckForChanges<ViewData>
        {
            [Key(0)] public int WindowID;
            [Key(1)] public string Title;
            [Key(2)] public List<Message> Messages;
            [Key(3)] public MessageWindowStyle Style;
            [Key(4)] public bool HideTitle;
            [Key(5)] public bool IsReadOnly;
            [Key(6)] public bool DrawWindow;
            [Key(7)] public bool CanDrag;

            public IUpdatableObject GetRelevantSubview(IObjectView view) => view.GetSubView<MessageWindowView>();

            public bool IsChangedFrom(ViewData check)
            {
                if (Messages.Count != check.Messages.Count)
                    return true;
                for (int i = 0; i < Messages.Count; i++)
                {
                    if (Messages[i] != check.Messages[i])
                        return true;
                }
                return false;
            }
        }

        [MessagePackObject(false)]
        public class ResponseData : IResponseData
        {
            [Key(0)] public int WindowID;
            [Key(1)] public Message Message;
        }

        public Color BackgroundColor = Color.gray;
        public Color NonPlayerColor = Color.yellow;
        public Color SelfPlayerColor = Color.red;
        public Color OtherPlayerColor = Color.white;
        public float InactiveBackgroundOpacity = 0.2f;
        public float ActiveBackgroundOpacity = 0.8f;
        public float ActiveBackgroundOpacityTime = 5f;
        public float OpacityFadeTime = 0.5f;
        public bool DoFade = true;
        public bool HideScrollbarWhenFaded = true;
        public bool HideHistoryWhenFaded = false;
        public float HandleBrightnessOffset = 0.5f;
        public float TextFieldWidthPercent = 0.8f;
        public KeyCode SendMessageKeyCode = KeyCode.None;
        public KeyCode AltFocusTextFieldKeyCode = KeyCode.None;
        public KeyCode DefocusTextFieldKeyCode = KeyCode.None;
        public bool IsSendKeyFocusesTextField = true;
        public float TextFieldDefocusBySendKeyDelay = 0.5f;
        public bool DoNotDrawOverride = false;
        public int FontSize = 12;
        public float WindowWidthPercent = 0.2f;
        public float WindowHeightPercent = 0.15f;
        public bool TopDragHandleEnabled = true;
        public float DragHandleHeightPercent = 0.075f;
        public bool PreventDragOverride = false;
        public bool BlockInputCaptureWhenFocused = false;

        int _id = -1;
        public int ID => _id;
        List<Message> _messages = new List<Message>();
        Queue<(string, string)> _outgoingMessages = new Queue<(string, string)>();

        string _title = "";
        bool _hideTitle = false;
        bool _isReadOnly = false;
        bool _drawWindow = false;
        bool _canDrag = true;
        MessageWindowStyle _style = MessageWindowStyle.Normal;

        protected override void UpdateData(ViewData data)
        {
            _id = data.WindowID;
            _title = data.Title;
            _messages = data.Messages;
            _style = data.Style;
            _hideTitle = data.HideTitle;
            _isReadOnly = data.IsReadOnly;
            _drawWindow = data.DrawWindow;
            _canDrag = data.CanDrag;

            _resetOpacityProgress = true;
        }

        public override bool HasStateUpdate(out IResponseData state)
        {
            state = default;
            if (!(_outgoingMessages.Count > 0))
                return false;

            (string owner, string text) = _outgoingMessages.Dequeue();

            state = new ResponseData()
            {
                WindowID = _id,
                Message = new Message()
                {
                    InputSource = InputSourceIdentifier.Identifier.Value,
                    Owner = owner,
                    Text = text
                }
            };
            return true;
        }

        void Update()
        {
            UpdateOpacityProgress();
        }

        const string TEXT_FIELD_NAME = "textField";
        const int INTER_ELEMENT_SPACE = 5;

        public bool IsInit { get; protected set; } = false;

        Rect _windowRect;
        Rect _topHandleRect;
        Rect _handleRect;
        float _scrollViewHeight;

        GUIStyle _windowStyle;
        GUIStyle _messageStyle;
        GUIStyle _textFieldStyle;
        GUIStyle _sendButtonStyle;
        float _textFieldWidth;
        GUIStyle _verticalScrollbarStyle;
        Vector2 _historyScrollPostion = Vector2.zero;
        string _textFieldContent = string.Empty;
        float _opacityProgress = 0f;
        bool _resetOpacityProgress = false;
        bool _showHistory => !(HideHistoryWhenFaded && _opacityProgress <= 0f);
        bool _showVerticalScrollbar => !(HideScrollbarWhenFaded && _opacityProgress <= 0f);
        string _uniqueTextFieldName => $"{_title}:{TEXT_FIELD_NAME}";

        Color _backgroundColorWithAlpha;
        readonly Texture2D _handleTexture = new Texture2D(1, 1);

        public bool IsDragging { get; protected set; } = false;
        private Vector2? _moveWindowTarget = null;
        public Vector2 WindowPos => new Vector2(_windowRect.x, _windowRect.y);

        float _textFieldWasDefocusedDelay = 0f;
        bool _sendMessageKeyWasPressed = false;
        bool _altFocusKeyWasPressed = false;

        void OnGUI()
        {
            if (_id != 0 && _drawWindow && !DoNotDrawOverride)
            {
                RecalculateDisplayVariables();
                RecalculateColorsAndTextures();
                GUI.backgroundColor = _backgroundColorWithAlpha;
                _windowRect = GUILayout.Window(_id, _windowRect, DrawWindow, !_hideTitle ? _title : string.Empty, _windowStyle);
                if (GUI.GetNameOfFocusedControl().IsNullOrEmpty())
                {
                    bool shouldFocus = false;
                    if (AltFocusTextFieldKeyCode != KeyCode.None && Input.GetKeyDown(AltFocusTextFieldKeyCode))
                    {
                        _altFocusKeyWasPressed = true;
                        shouldFocus = true;
                    }
                    if (SendMessageKeyCode != KeyCode.None && IsSendKeyFocusesTextField && Input.GetKeyDown(SendMessageKeyCode))
                    {
                        _sendMessageKeyWasPressed = true;
                        shouldFocus = true;
                    }

                    if (shouldFocus && !(_textFieldWasDefocusedDelay > 0f))
                    {
                        GUI.FocusControl(_uniqueTextFieldName);
                    }
                }
                else if (Input.GetKey(AltFocusTextFieldKeyCode) == false)
                    _altFocusKeyWasPressed = false;

                CheckBlockInput();
            }
            else
            {
                RemoveInputBlock();
            }
            if (_textFieldWasDefocusedDelay > 0f)
                _textFieldWasDefocusedDelay -= DeltaTime;
        }

        void CheckBlockInput()
        {
            if (BlockInputCaptureWhenFocused && GUI.GetNameOfFocusedControl() == _uniqueTextFieldName)
            {
                AddInputBlock();
                return;
            }
            RemoveInputBlock();
        }

        void RecalculateDisplayVariables()
        {
            float windowWidth = WindowWidthPercent * Screen.width;
            float windowHeight = WindowHeightPercent * Screen.height;

            if (!IsInit)
            {
                _windowRect = new Rect(0, Screen.height - windowHeight, windowWidth, windowHeight);
                IsInit = true;
            }

            _windowRect = new Rect(_windowRect.x, _windowRect.y, windowWidth, windowHeight);
            if (_moveWindowTarget.HasValue)
            {
                _windowRect.x = _moveWindowTarget.Value.x;
                _windowRect.y = _moveWindowTarget.Value.y;
                _moveWindowTarget = null;
            }

            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Screen.width - windowWidth);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Screen.height - windowHeight);

            _windowStyle = new GUIStyle(GUI.skin.window);   // To set style based on _style enum value

            _messageStyle = new GUIStyle(GUI.skin.label);
            _messageStyle.wordWrap = true;
            _messageStyle.fontSize = FontSize;

            _textFieldStyle = new GUIStyle(GUI.skin.textField);
            _textFieldStyle.wordWrap = true;
            _textFieldStyle.fontSize = FontSize;

            _sendButtonStyle = new GUIStyle(GUI.skin.button);
            _sendButtonStyle.fontSize = FontSize;

            if (_showVerticalScrollbar)
                _verticalScrollbarStyle = new GUIStyle(GUI.skin.verticalScrollbar);
            else
                _verticalScrollbarStyle = GUIStyle.none;

            float topHandleHeight = _windowStyle.border.top;
            _topHandleRect = new Rect(0, 0, windowWidth, topHandleHeight);

            float handleHeight = DragHandleHeightPercent * windowHeight;
            _handleRect = new Rect(0, windowHeight - handleHeight, windowWidth, handleHeight);

            _textFieldWidth = TextFieldWidthPercent * windowWidth;
            float textFieldHeight = _textFieldStyle.CalcHeight(new GUIContent(_textFieldContent), _textFieldWidth);

            _scrollViewHeight = windowHeight - _windowStyle.padding.vertical - (_isReadOnly ? 0f : INTER_ELEMENT_SPACE + textFieldHeight) - (!PreventDragOverride && _canDrag ? handleHeight : 0f);
        }

        void RecalculateColorsAndTextures()
        {
            float currentOpacity = Mathf.Lerp(InactiveBackgroundOpacity, ActiveBackgroundOpacity, Mathf.Clamp(_opacityProgress, 0f, OpacityFadeTime) / OpacityFadeTime);
            _backgroundColorWithAlpha = GetBackgroundColorWithAlpha(BackgroundColor, currentOpacity);
            _handleTexture.SetPixel(0, 0, GetHandleColorWithAlpha(BackgroundColor, currentOpacity));
            _handleTexture.Apply();
        }

        void UpdateOpacityProgress()
        {
            if (!DoFade)
            {
                _opacityProgress = OpacityFadeTime + ActiveBackgroundOpacityTime;
            }
            else if (_resetOpacityProgress)
            {
                _opacityProgress += DeltaTime;
                if (_opacityProgress > OpacityFadeTime)
                {
                    _opacityProgress = OpacityFadeTime + ActiveBackgroundOpacityTime;
                    _resetOpacityProgress = false;
                }
            }
            else
                _opacityProgress -= DeltaTime;
            _opacityProgress = Mathf.Clamp(_opacityProgress, 0f, OpacityFadeTime + ActiveBackgroundOpacityTime);
        }

        private void DrawWindow(int windowId)
        {
            Color defaultContentColor = GUI.contentColor;
            _historyScrollPostion = GUILayout.BeginScrollView(_historyScrollPostion, false, true, null, _verticalScrollbarStyle, GUILayout.Height(_scrollViewHeight));
            if (_showHistory)
            {
                for (int i = _messages.Count - 1; i > -1; i--)
                {
                    Message message = _messages[i];
                    GUI.contentColor = message.IsColorOverride ? message.ColorOverride : GetTextColor(message);
                    GUILayout.Label($"[{message.Owner}] {message.Text}", _messageStyle);
                }
            }
            GUILayout.EndScrollView();
            GUI.contentColor = defaultContentColor;

            if (!_isReadOnly)
            {
                GUILayout.Space(INTER_ELEMENT_SPACE);

                GUILayout.BeginHorizontal();
                GUI.SetNextControlName(_uniqueTextFieldName);
                string textFieldTemp = GUILayout.TextField(_textFieldContent, _textFieldStyle, GUILayout.Width(_textFieldWidth));
                if (!_sendMessageKeyWasPressed && !_altFocusKeyWasPressed)
                {
                    _textFieldContent = textFieldTemp;
                }

                if (GUILayout.Button("Send", _sendButtonStyle))
                {
                    SendMessage();
                    GUI.FocusControl(_uniqueTextFieldName);
                }
                GUILayout.EndHorizontal();

                if (GUI.GetNameOfFocusedControl() == _uniqueTextFieldName)
                {
                    if (SendMessageKeyCode != KeyCode.None)
                    {
                        if (EventIsKeyPressed(SendMessageKeyCode, down: true))
                        {
                            if (!_sendMessageKeyWasPressed && !SendMessage())
                            {
                                _textFieldWasDefocusedDelay = TextFieldDefocusBySendKeyDelay;
                                GUI.FocusControl(null);
                            }
                            _sendMessageKeyWasPressed = true;
                        }
                        else if (EventIsKeyPressed(SendMessageKeyCode, down: false))
                        {
                            _sendMessageKeyWasPressed = false;
                        }
                    }
                    if (SendMessageKeyCode != KeyCode.None &&
                        SendMessageKeyCode != DefocusTextFieldKeyCode &&
                        EventIsKeyPressed(DefocusTextFieldKeyCode, down: true))
                    {
                        _textFieldContent = string.Empty;
                        GUI.FocusControl(null);
                    }
                    _resetOpacityProgress = true;
                }
                else
                {
                    _sendMessageKeyWasPressed = false;
                }
            }

            if (!PreventDragOverride && _canDrag)
            {
                GUI.DrawTexture(_handleRect, _handleTexture);
                bool isInteractingWithHandle = false;
                if (_handleRect.Contains(Event.current.mousePosition) || (TopDragHandleEnabled && _topHandleRect.Contains(Event.current.mousePosition)))
                {
                    _resetOpacityProgress = true;
                    if (Input.GetMouseButton(0) == true)
                    {
                        IsDragging = true;
                        isInteractingWithHandle = true;
                    }
                }

                if (!isInteractingWithHandle)
                {
                    IsDragging = false;
                }
                if (TopDragHandleEnabled)
                    GUI.DragWindow(_topHandleRect);
                GUI.DragWindow(_handleRect);
            }
        }

        private bool EventIsKeyPressed(KeyCode keyCode, bool down = true)
        {
            if (keyCode == KeyCode.None)
                return false;

            if (Event.current.keyCode == keyCode)
            {
                if (down)
                {
                    return Event.current.type == UnityEngine.EventType.Used;
                }
                else
                {
                    return Event.current.type == UnityEngine.EventType.KeyUp;
                }
            }
            return false;
        }

        public void MoveWindow(Vector2 screenPositionInPixels)
        {
            _moveWindowTarget = screenPositionInPixels;
        }

        private bool SendMessage()
        {
            if (_textFieldContent.IsNullOrEmpty())
                return false;
            _outgoingMessages.Enqueue((SteamPlatform.Steam.LocalUsername, _textFieldContent));
            _textFieldContent = string.Empty;
            return true;
        }

        public void SendMessage(string owner, string text)
        {
            _outgoingMessages.Enqueue((owner, text));
        }

        private Color GetBackgroundColorWithAlpha(Color baseColor, float opacity)
        {
            return new Color(baseColor.r, baseColor.g, baseColor.b, opacity);
        }

        private Color GetHandleColorWithAlpha(Color baseColor, float opacity)
        {
            baseColor = baseColor * HandleBrightnessOffset;
            return new Color(baseColor.r, baseColor.g, baseColor.b, opacity);
        }

        private Color GetTextColor(Message message)
        {
            if (message.InputSource == -1)
                return NonPlayerColor;
            if (message.InputSource == InputSourceIdentifier.Identifier)
                return SelfPlayerColor;
            return OtherPlayerColor;
        }
    }
}
