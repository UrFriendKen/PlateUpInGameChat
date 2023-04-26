using Controllers;
using Kitchen;
using Kitchen.NetworkSupport;
using KitchenLib.Utils;
using MessagePack;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenInGameChat
{
    public class MessageWindowView : ResponsiveObjectView<MessageWindowView.ViewData, MessageWindowView.ResponseData>
    {
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

                return InputSource != other.InputSource ||
                    Owner != other.Owner ||
                    Text != other.Text;
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
                        DrawWindow = !window.DoNotDraw
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
        
        public const float MINIMUM_WINDOW_WIDTH_PERCENT = 0.05f;
        public const float MINIMUM_WINDOW_HEIGHT_PERCENT = 0.0325f;
        public const float DRAG_HANDLE_HEIGHT_PERCENT = 0.075f;

        public Color BackgroundColor = Color.gray;
        public Color NonPlayerColor = Color.yellow;
        public Color SelfPlayerColor = Color.red;
        public Color OtherPlayerColor = Color.white;
        public float InactiveBackgroundOpacity = 0.2f;
        public float ActiveBackgroundOpacity = 0.8f;
        public float ActiveBackgroundOpacityTime = 5f;
        public float OpacityFadeTime = 0.5f;
        public bool DoFade = true;
        public float HandleBrightnessOffset = 0.5f;
        public float TextFieldWidthPercent = 0.8f;
        public KeyCode SendMessageKeyCode = KeyCode.Return;
        public bool DoNotDrawOverride = false;
        public int FontSize = 12;
        public float WindowWidthPercent = 0.2f;
        public float WindowHeightPercent = 0.15f;

        int _id = 0;
        public int ID => _id;
        List<Message> _messages = new List<Message>();
        Queue<(string, string)> _outgoingMessages = new Queue<(string, string)>();

        string _title = "";
        bool _hideTitle = false;
        bool _isReadOnly = false;
        bool _drawWindow = false;
        MessageWindowStyle _style = MessageWindowStyle.Normal;
        float _opacityProgress = 0f;
        bool _resetOpacityProgress = false;
        
        protected override void UpdateData(ViewData data)
        {
            _id = data.WindowID;
            _title = data.Title;
            _messages = data.Messages;
            _style = data.Style;
            _hideTitle = data.HideTitle;
            _isReadOnly = data.IsReadOnly;
            _drawWindow = data.DrawWindow;
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

        Rect _windowRect;
        Rect _handleRect;
        float _scrollViewHeight;

        GUIStyle _windowStyle;
        GUIStyle _messageStyle;
        GUIStyle _textFieldStyle;
        float _textFieldWidth;
        GUIStyle _verticalScrollbarStyle;
        Vector2 _historyScrollPostion = Vector2.zero;
        string _textFieldContent = string.Empty;

        Color _backgroundColorWithAlpha;
        readonly Texture2D _handleTexture = new Texture2D(1, 1);

        bool _isDragging = false;
        bool _isResizing = false;
        Vector2 _mousePositionBeforeResize = Vector2.zero;
        Vector2 _windowSizePercentBeforeResize = Vector2.zero;

        void OnGUI()
        {
            if (_id != 0 && _drawWindow && !DoNotDrawOverride)
            {
                RecalculateDisplayVariables();
                RecalculateColorsAndTextures();
                GUI.backgroundColor = _backgroundColorWithAlpha;
                _windowRect = GUILayout.Window(_id, _windowRect, DrawWindow, !_hideTitle? _title : string.Empty, _windowStyle);
            }
        }

        void RecalculateDisplayVariables()
        {
            float windowWidth = WindowWidthPercent * Screen.width;
            float windowHeight = WindowHeightPercent * Screen.height;
            if (_windowRect == default)
                _windowRect = new Rect(0, Screen.height - windowHeight, windowWidth, windowHeight);
            else
                _windowRect = new Rect(_windowRect.x, _windowRect.y, windowWidth, windowHeight);

            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Screen.width - windowWidth);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Screen.height - windowHeight);

            _windowStyle = new GUIStyle(GUI.skin.window);   // To set style based on _style enum value

            _messageStyle = GUI.skin.label; // To be updated for dynamic fontsize
            _messageStyle.wordWrap = true;

            _textFieldStyle = GUI.skin.textField; // To be updated for dynamic fontsize
            _textFieldWidth = TextFieldWidthPercent * windowWidth;

            _verticalScrollbarStyle = GUI.skin.verticalScrollbar;
            _verticalScrollbarStyle.alignment = TextAnchor.LowerCenter;

            float handleHeight = DRAG_HANDLE_HEIGHT_PERCENT * windowHeight;   // Change DRAG_HANDLE_HEIGHT to a percent if using this
            _handleRect = new Rect(0, windowHeight - handleHeight, windowWidth, handleHeight);

            float textFieldHeight = _textFieldStyle.CalcHeight(new GUIContent(_textFieldContent), windowWidth - _windowStyle.padding.horizontal);

            _scrollViewHeight = windowHeight - _windowStyle.padding.vertical - (_isReadOnly? 0f : INTER_ELEMENT_SPACE + textFieldHeight) - handleHeight;
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
                    _opacityProgress = OpacityFadeTime + ActiveBackgroundOpacityTime;
            }
            else
                _opacityProgress -= DeltaTime;
            _opacityProgress = Mathf.Clamp(_opacityProgress, 0f, OpacityFadeTime + ActiveBackgroundOpacityTime);
            _resetOpacityProgress = false;
        }

        private void DrawWindow(int windowId)
        {
            Color defaultContentColor = GUI.contentColor;
            _historyScrollPostion = GUILayout.BeginScrollView(_historyScrollPostion, false, true, null, _verticalScrollbarStyle, GUILayout.Height(_scrollViewHeight));
            for (int i = _messages.Count - 1; i > -1; i--)
            {
                Message message = _messages[i];
                GUI.contentColor = message.IsColorOverride? message.ColorOverride : GetTextColor(message);
                GUILayout.Label($"[{message.Owner}] {message.Text}");
            }
            GUILayout.EndScrollView();
            GUI.contentColor = defaultContentColor;

            if (!_isReadOnly)
            {
                GUILayout.Space(INTER_ELEMENT_SPACE);

                GUILayout.BeginHorizontal();
                GUI.SetNextControlName(TEXT_FIELD_NAME);
                _textFieldContent = GUILayout.TextField(_textFieldContent, GUILayout.Width(_textFieldWidth));
                if (GUILayout.Button("Send"))
                {
                    SendMessage();
                    GUI.FocusControl(TEXT_FIELD_NAME);
                }
                GUILayout.EndHorizontal();
                if (GUI.GetNameOfFocusedControl() == TEXT_FIELD_NAME)
                {
                    if (SendMessageKeyCode != KeyCode.None && Event.current.keyCode == SendMessageKeyCode)
                        SendMessage();
                    _resetOpacityProgress = true;
                }
            }

            GUI.DrawTexture(_handleRect, _handleTexture);
            bool isresizingOrDragging = false;
            if (_handleRect.Contains(Event.current.mousePosition))
            {
                _resetOpacityProgress = true;
                if (Input.GetMouseButtonDown(0))
                {
                    if (false)//_handleRect.Contains(Input.mousePosition) && Input.GetMouseButtonDown(0))   // Change from handle rect to a resize handle rect
                        _isResizing = true;
                    else
                        _isDragging = true;
                    isresizingOrDragging = true;
                }
            }
            
            if (!isresizingOrDragging)
            {
                _isResizing = false;
                _isDragging = false;
            }
            GUI.DragWindow(_handleRect);
        }

        private void SendMessage()
        {
            if (_textFieldContent.IsNullOrEmpty())
                return;
            _outgoingMessages.Enqueue((SteamPlatform.Steam.LocalUsername, _textFieldContent));
            _textFieldContent = string.Empty;
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
