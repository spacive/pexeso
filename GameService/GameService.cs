﻿using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using GameService.Data;
using GameService.Library;

namespace GameService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    class GameService : IGameService
    {
        private const int PlayerNameMinLength = 5;
        private const int PlayerPasswordMinLength = 5;

        // holds players available for game
        private static readonly PlayerStore PlayersOnline = new PlayerStore();
    
        private IGameClient Client => OperationContext.Current.GetCallbackChannel<IGameClient>();
 
        public Player RegisterPlayer(string name, string password)
        {
            // check if name and password are valid
            if (name.Length < PlayerNameMinLength || password.Length < PlayerPasswordMinLength)
            {
                return null;
            }

            using (var ctx = new GameContext())
            {
                // check if player's name is unique
                var count = ctx.Players.Count(p => p.Name.Equals(name));
                if (count != 0)
                {
                    return null;
                }

                var newPlayer = ctx.Players.Add(new Player(name, password));
                ctx.SaveChanges();

                return newPlayer;
            }
        }

        public Player ConnectPlayer(string name, string password)
        {
            var ctx = new GameContext();

            try
            {
                var player = ctx.Players.Single(p => p.Name.Equals(name) && p.Password.Equals(password));
                PlayersOnline.AddPlayer(Client, player);

                // notify other players
                PlayersOnline.GetActivePlayers().ForEach(p =>
                {
                    if (p.Id != player.Id)
                    {
                        PlayersOnline.GetGameClient(p).NotifyPlayerConnected(player);
                    }
                });

                return player;
            }
            catch
            {
                return null;
            }
            finally
            {
                ctx.Dispose();
            }      
        }

        public List<Player> GetAvailablePlayers()
        {
            return PlayersOnline.GetActivePlayers().Where(p => p.InGame == false).ToList();
        }

        public void InvitePlayer(Player player, GameParams.GameSizes gameSize)
        {
            var invitingPlayer = PlayersOnline.GetGamePlayer(Client);
            var client = PlayersOnline.GetGameClient(player);
            client?.InvitedBy(invitingPlayer, gameSize);
        }

        public void AcceptInvitation(Player invitingPlayer, GameParams.GameSizes gameSize)
        {
            var invitingClient = PlayersOnline.GetGameClient(invitingPlayer);
            var acceptingPlayer = PlayersOnline.GetGamePlayer(Client);

            // update players state - hide these in UI
            PlayersOnline.GetGamePlayer(invitingClient).InGame = true;
            acceptingPlayer.InGame = true;

            NotifyPlayerUpdate(invitingPlayer);
            NotifyPlayerUpdate(acceptingPlayer);

            var gameInitParams = new GameParams(invitingPlayer, acceptingPlayer, gameSize);           

            // callback
            invitingClient.StartGame(gameInitParams);
            Client.StartGame(gameInitParams);
        }

        public void RefuseInvitation(Player player)
        {
            var refusingPlayer = PlayersOnline.GetGamePlayer(Client);
            var client = PlayersOnline.GetGameClient(player);
            client?.InvitationRefused(refusingPlayer);
        }

        public void DisconnectPlayer(Player player)
        {
            // try is here because we don't know in which store the player is
            // player doesn't have to be in either
            // TODO: do it the better way
            try
            {
                // notify other players
                PlayersOnline.GetActivePlayers().ForEach(p =>
                {
                    if (p.Id != player.Id)
                    {
                        PlayersOnline.GetGameClient(p).NotifyPlayerDisconnected(player);
                    }
                });

                PlayersOnline.RemovePlayer(player);
            }
            catch
            {
                // ignored
            }
        }

        private void NotifyPlayerUpdate(Player updatedPlayer)
        {
            PlayersOnline.GetActivePlayers().ForEach(player =>
            {
                PlayersOnline.GetGameClient(player).NotifyPlayerUpdate(updatedPlayer);
            });
        }
    }
}
