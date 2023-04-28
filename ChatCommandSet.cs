using Kitchen;
using Kitchen.NetworkSupport;
using KitchenInGameChat.Commands;
using Steamworks.Data;
using System.Collections.Generic;
using UnityEngine;

namespace KitchenInGameChat
{
    public sealed class ChatCommandSet : BaseCommandSet
    {
        public override string Name => "Chat Commands";

        public override void RegisterCommands()
        {
            AddCommand<InviteCommand>();
        }

        class InviteCommand : BaseCommand
        {
            public override string Action => "invite";

            public override CommandResult Perform(List<string> args)
            {
                string outputMessage = null;
                bool echo = false;
                bool success = true;

                if (!ulong.TryParse(args[0], out ulong id))
                {
                    outputMessage = "Error! Invalid argument, ID. Usage: \"/invite <SteamID>\"";
                    success = false;
                }
                else
                {
                    string idString = SanitiseID(args[0]);
                    switch (Session.GetNetworkPermissions())
                    {
                        case NetworkPermissions.Private:
                            outputMessage = $"Cannot send invite to {idString}! Lobby is set to PRIVATE.";
                            success = false;
                            break;
                        case NetworkPermissions.InviteOnly:
                        case NetworkPermissions.Open:
                            Lobby lobby = SteamPlatform.Steam.CurrentInviteLobby;
                            if (lobby.MemberCount < lobby.MaxMembers)
                            {
                                if (lobby.InviteFriend(id))
                                {
                                    outputMessage = $"Sent game invite to {idString}.";
                                }
                                else
                                {
                                    outputMessage = $"Failed to send game invite to {idString}.";
                                    success = false;
                                }
                            }
                            else
                            {
                                outputMessage = $"Cannot send invite to {idString}! Lobby is full ({lobby.MemberCount}/{lobby.MaxMembers})";
                                success = false;
                            }
                            break;
                        default:
                            outputMessage = $"Cannot send invite to {idString}! Network permissions unknown.";
                            success = false;
                            break;
                    }
                }

                return new CommandResult()
                {
                    Success = success,
                    OutputMessage = outputMessage,
                    Echo = echo
                };
            }

            private string SanitiseID(string input)
            {
                int pos = Mathf.Max(0, input.Length - 4);
                return $"{new string('*', pos)}{input.Substring(pos)}";
            }
        }


        //const bool DEBUG = false;
        //protected override CommandResult HandleCommand(CommandData command)
        //{
        //    if (DEBUG)
        //    {
        //        return new CommandResult()
        //        {
        //            Success = true,
        //            OutputMessage = $"Action: {command.Action}\nArgs: {string.Join(", ", command.Args)}",
        //            Echo = true
        //        };
        //    }


        //    string outputMessage = null;
        //    bool echo = false;
        //    bool success = true;

        //    if (!ulong.TryParse(command.Args[0], out ulong id))
        //    {
        //        outputMessage = "Error! Invalid argument, ID. Usage: \"/invite <SteamID>\"";
        //        success = false;
        //    }
        //    else
        //    {
        //        switch (Session.GetNetworkPermissions())
        //        {
        //            case NetworkPermissions.Private:
        //                outputMessage = $"Cannot send invite to {id}! Lobby is set to PRIVATE.";
        //                success = false;
        //                break;
        //            case NetworkPermissions.InviteOnly:
        //            case NetworkPermissions.Open:
        //                Lobby lobby = SteamPlatform.Steam.CurrentInviteLobby;
        //                if (lobby.MemberCount < lobby.MaxMembers)
        //                {
        //                    outputMessage = $"Sent game invite to {id}.";
        //                    lobby.InviteFriend(id);
        //                }
        //                else
        //                {
        //                    outputMessage = $"Cannot send invite to {id}! Lobby is full ({lobby.MemberCount}/{lobby.MaxMembers})";
        //                    success = false;
        //                }
        //                break;
        //            default:
        //                outputMessage = $"Cannot send invite to {id}! Network permissions unknown.";
        //                success = false;
        //                break;
        //        }
        //    }

        //    return new CommandResult()
        //    {
        //        Success = success,
        //        OutputMessage = outputMessage,
        //        Echo = echo
        //    };
        //}
    }
}
