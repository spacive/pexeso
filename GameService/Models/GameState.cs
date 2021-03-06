﻿using System;
using System.Linq;
using GameService.Library.Models;
using GameService.Library.Utils;

namespace GameService.Models
{
    public class GameState
    {
        public Player FirstPlayer { get; set; }

        public Player SecondPlayer { get; set; }

        public GameParams.GameSizes GameSize { get; set; }

        public int[] Map { get; }

        public int? RevealedCardIndex { get; set; }

        public int FirstPlayerScore { get; set; }

        public int SecondPlayerScore { get; set; }

        public DateTime GameBegin { get; set; }

        public DateTime GameEnd { get; set; }

        public GameState()
        {
        }

        public GameState(Player firstPlayer, Player secondPlayer, GameParams.GameSizes gameSize)
        {
            FirstPlayer = firstPlayer;
            SecondPlayer = secondPlayer;
            GameSize = gameSize;

            // generate random map
            Library.Utils.Utils.GetGameSize(gameSize, out var rows, out var cols);
            var rand = new Random();
            var elements = Enumerable.Range(1, rows * cols / 2).ToList();
            Map = elements.Concat(elements).OrderBy(x => rand.Next()).ToArray();

            GameBegin = DateTime.Now;

        }

        public void IncrementScore(Player player)
        {
            if (player.Equals(FirstPlayer))
            {
                FirstPlayerScore++;
            }
            else if (player.Equals(SecondPlayer))
            {
                SecondPlayerScore++;
            }
        }

        public bool IsGameEnd()
        {
            return (FirstPlayerScore + SecondPlayerScore) * 2 == Map.Length;
        }
    }
}
