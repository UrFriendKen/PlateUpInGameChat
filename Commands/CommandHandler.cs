using Controllers;
using KitchenInGameChat.MessageWindow;
using KitchenLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KitchenInGameChat.Commands
{
    public abstract class BaseCommandSet
    {
        protected struct CommandData
        {
            public string Action;
            public List<string> Args;

            public int TargetWindowID;
            public string Owner;
            public string Text;
            public bool IsPlayer;
            public bool IsHost;
        }

        protected struct CommandResult
        {
            public bool Success;
            public bool Echo;

            public string OutputMessage;
            public float MessageTimeout = 0f;

            public CommandResult()
            {
            }
        }

        protected abstract class BaseCommand
        {
            public abstract string Action { get; }
            public virtual CommandResult Perform(CommandData data)
            {
                return Handle(data);
            }
            protected abstract CommandResult Handle(CommandData data);
        }

        public enum Status
        {
            Idle,
            Success,
            PassThrough,
            Escaped,
            FailedToTokenize,
            FailedToHandle
        }

        public abstract string Name { get; }
        protected virtual char CommandAndEscapeChar => '/';
        protected virtual bool EchoIfFailedToTokenize => true;
        protected virtual bool SendErrorIfFailedToTokenize => true;

        protected const string WHITESPACE_PATTERN = @"\s+";
        public Status LastCommandStatus { get; private set; } = Status.Idle;

        private readonly Dictionary<string, BaseCommand> Commands = new Dictionary<string, BaseCommand>();

        protected virtual bool ParseInput(StaticMessageRequest input, out CommandData commandData, out string errorMessage)
        {
            string text = input.Text;

            commandData = default;
            errorMessage = null;
            string[] parts = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string action = parts[0].Substring(1);
            if (action.IsNullOrEmpty())
            {
                return false;
            }

            List<string> arguments = new List<string>();
            bool inQuotes = false;
            for (int i = 1; i < parts.Length; i++)
            {
                string arg = parts[i];

                if (arg.StartsWith("\""))
                {
                    inQuotes = true;
                    for (int j = i + 1; j < parts.Length; j++)
                    {
                        arg += " " + parts[j];
                        i = j;
                        if (arg.EndsWith("\""))
                        {
                            inQuotes = false;
                            arg = arg.Trim('"');
                            break;
                        }
                    }
                }

                arguments.Add(arg);
            }

            if (inQuotes)
            {
                return false;
            }

            commandData.Action = action;
            commandData.Args = arguments;

            commandData.TargetWindowID = input.TargetWindowID;
            commandData.Owner = input.Owner;
            commandData.Text = input.Text;
            commandData.IsPlayer = input.IsPlayerMessage;
            commandData.IsHost = input.InputSource == InputSourceIdentifier.Identifier.Value;

            return true;
        }

        // If return true, echo input (or escapedInput if not null or empty)
        public bool Run(StaticMessageRequest inputMessage, out string modifiedMessageText, out StaticMessageRequest? runStatusOutput)
        {
            modifiedMessageText = null;
            runStatusOutput = null;

            string text = inputMessage.Text;
            if (!text.StartsWith($"{CommandAndEscapeChar}"))
            {
                LastCommandStatus = Status.PassThrough;
                return true;
            }

            if (text.StartsWith($"{CommandAndEscapeChar}{CommandAndEscapeChar}"))
            {
                text = text.Substring(1);
                LastCommandStatus = Status.Escaped;
                return true;
            }

            if (!ParseInput(inputMessage, out CommandData commandData, out string errorMessage))
            {
                if (!errorMessage.IsNullOrEmpty())
                {
                    LogError(errorMessage);
                    runStatusOutput = CreateMessageRequest(errorMessage);
                }
                LastCommandStatus = Status.FailedToTokenize;
                return EchoIfFailedToTokenize;
            }

            CommandResult result;
            if (!Commands.TryGetValue(commandData.Action, out BaseCommand command))
            {
                result = new CommandResult()
                {
                    Success = false,
                    OutputMessage = $"Command \"{commandData.Action}\" not found!",
                    Echo = true
                };
            }
            else
            {
                result = command.Perform(commandData);
            }

            if (result.Success)
            {
                LastCommandStatus = Status.Success;
            }
            else
            {
                LastCommandStatus = Status.FailedToHandle;
            }

            if (!result.OutputMessage.IsNullOrEmpty())
            {
                runStatusOutput = CreateMessageRequest(result.OutputMessage, result.MessageTimeout);
            }
            return result.Echo;
        }

        private StaticMessageRequest CreateMessageRequest(string message, float timeout = 0f)
        {
            return new StaticMessageRequest()
            {
                Owner = Name,
                InputSource = -1,
                Text = message,
                MessageTimeoutOverride = timeout
            };
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"*[In-Game Chat] {Name ?? "BaseCommandSet"} - {message}");
        }
        private void LogError(string message)
        {
            Debug.LogError($"*[In-Game Chat] {Name ?? "BaseCommandSet"} - {message}");
        }

        protected bool AddCommand<T>() where T : BaseCommand, new()
        {
            T command = new T();
            string action = command.Action.ToLowerInvariant().Trim();
            if (action.Any(Char.IsWhiteSpace))
            {
                LogWarning($"Action, {action}, in {typeof(T)} is invalid. Action cannot contain any whitespace.");
                return false;
            }
            if (Commands.ContainsKey(action))
            {
                LogWarning($"Command {typeof(T)} already registered.");
                return false;
            }
            Commands.Add(action, command);
            return true;
        }

        public abstract void RegisterCommands();
    }
}
