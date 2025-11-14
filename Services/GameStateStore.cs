// DartsAPI/Services/GameStateStore.cs
using System.Collections.Concurrent;
using DartsAPI.Dtos;
using DartsAPI.Models;

namespace DartsAPI.Services
{
    public static class GameStateStore
    {
        private static readonly ConcurrentDictionary<string, GameState> _states = new();
        private static readonly HashSet<int> NotPossibleThrown = new() { 179, 178, 176, 175, 173, 172, 169, 166, 163 };
        private static readonly HashSet<int> NotPossibleCheckouts = new() { 180, 177, 174, 171, 169, 168, 166, 165, 163, 162, 159 };

        public static void InitializeFromLobby(string lobbyGUID, List<string> players, GameSettings settings)
        {
            if (players is null || players.Count != 2)
                throw new InvalidOperationException("Exactly 2 players required.");

            var scores = new Dictionary<string, int>
            {
                [players[0]] = settings.StartScore,
                [players[1]] = settings.StartScore
            };

            var state = new GameState
            {
                LobbyGUID = lobbyGUID,
                Players = players,
                Settings = settings,
                Scores = scores,
                CurrentPlayerIndex = 0,
                NextLegStarterIndex = 0,
                LegWins = new Dictionary<string, int>
                {
                    [players[0]] = 0,
                    [players[1]] = 0
                },
                TotalLegsPlayed = 0,
                MatchEnded = false
            };

            _states[lobbyGUID] = state;
        }

        public static GameStateDto GetSnapshot(string lobbyGUID)
        {
            if (!_states.TryGetValue(lobbyGUID, out var s))
                throw new KeyNotFoundException("Game state not found.");

            return new GameStateDto
            {
                LobbyGUID = s.LobbyGUID,
                CurrentPlayer = s.Players[s.CurrentPlayerIndex],
                Scores = new Dictionary<string, int>(s.Scores),
                LegEnded = false,
                MatchEnded = s.MatchEnded
            };
        }

        public static ThrowAppliedDto ApplyThrow(string lobbyGUID, SubmitThrowDto dto, string authUsername)
        {
            if (!_states.TryGetValue(lobbyGUID, out var s))
                throw new InvalidOperationException("Game state not initialized for this lobby.");

            var currentPlayer = s.Players[s.CurrentPlayerIndex];

            if (!string.Equals(currentPlayer, dto.PlayerName, StringComparison.Ordinal))
                throw new InvalidOperationException("Not your throw.");

            if (!string.Equals(authUsername, dto.PlayerName, StringComparison.Ordinal))
                throw new InvalidOperationException("Authenticated user mismatch.");

            if (dto.InputScore < 0 || dto.InputScore > 180)
                throw new InvalidOperationException("Invalid score range.");

            if (NotPossibleThrown.Contains(dto.InputScore))
                throw new InvalidOperationException("Impossible single-visit score.");

            var currentScore = s.Scores[currentPlayer];
            var possibleScore = currentScore - dto.InputScore;

            if (possibleScore < 0 || possibleScore == 1)
                throw new InvalidOperationException("Bust.");

            var isCheckout = possibleScore == 0;
            if (isCheckout && NotPossibleCheckouts.Contains(currentScore))
                throw new InvalidOperationException("Impossible checkout from this score (double-out).");

            if (s.AppliedThrowIds.Contains(dto.ClientThrowId))
            {
                return new ThrowAppliedDto
                {
                    LobbyGUID = lobbyGUID,
                    PlayerName = dto.PlayerName,
                    Scored = dto.InputScore,
                    NewScore = s.Scores[dto.PlayerName],
                    LegEnded = false,
                    MatchEnded = s.MatchEnded,
                    DoubleTries = Math.Clamp(dto.DoubleTries, 0, 3),
                    UsedDarts = isCheckout ? Math.Clamp(dto.UsedDarts, 1, 3) : 3,
                    ClientThrowId = dto.ClientThrowId
                };
            }
            s.AppliedThrowIds.Add(dto.ClientThrowId);

            s.Scores[currentPlayer] = possibleScore;

            s.SaveThrow(new ThrowRecord
            {
                PlayerName = currentPlayer,
                Scored = dto.InputScore,
                DoubleTries = dto.DoubleTries,
                UsedDarts = dto.UsedDarts,
                CheckedOut = isCheckout,
                ScoreBefore = currentScore
            });

            bool legEnded = false;
            bool matchEnded = false;

            if (isCheckout)
            {
                legEnded = true;
                s.LegWins[currentPlayer]++;

                int winsNeeded = s.Settings.LegFormat == LegFormat.FirstTo
                    ? s.Settings.LegCount
                    : (int)Math.Ceiling(s.Settings.LegCount / 2.0);

                if (s.Settings.LegFormat == LegFormat.FirstTo)
                {
                    matchEnded = s.LegWins[currentPlayer] >= winsNeeded;
                }
                else
                {
                    s.TotalLegsPlayed++;
                    matchEnded = s.LegWins[currentPlayer] >= winsNeeded || s.TotalLegsPlayed >= s.Settings.LegCount;
                }

                if (!matchEnded)
                {
                    foreach (var p in s.Players)
                        s.Scores[p] = s.Settings.StartScore;

                    s.NextLegStarterIndex = 1 - s.NextLegStarterIndex;
                    s.CurrentPlayerIndex = s.NextLegStarterIndex;
                }
                else
                {
                    s.MatchEnded = true;
                }
            }
            else
            {
                s.CurrentPlayerIndex = 1 - s.CurrentPlayerIndex;
            }

            return new ThrowAppliedDto
            {
                LobbyGUID = lobbyGUID,
                PlayerName = dto.PlayerName,
                Scored = dto.InputScore,
                NewScore = s.Scores[dto.PlayerName],
                LegEnded = legEnded,
                MatchEnded = matchEnded,
                DoubleTries = dto.DoubleTries,
                UsedDarts = dto.UsedDarts,
                ClientThrowId = dto.ClientThrowId
            };
        }

        public static GameSummaryDto BuildSummary(string lobbyGUID)
        {
            if (!_states.TryGetValue(lobbyGUID, out var s))
                throw new KeyNotFoundException("Game state not found.");

            var summary = new GameSummaryDto
            {
                LobbyGUID = lobbyGUID,
                Winner = s.LegWins.OrderByDescending(kv => kv.Value).First().Key,
                EndReason = s.EndedByForfeit ? "Forfeit" : "Normal",
                ForfeitedBy = s.ForfeitedBy
            };

            foreach (var p in s.Players)
                summary.LegsWon[p] = s.LegWins.TryGetValue(p, out var w) ? w : 0;

            foreach (var p in s.Players)
            {
                var throws = s.Throws.Where(t => t.PlayerName == p).ToList();
                var totalPoints  = throws.Sum(t => t.Scored);
                var totalThrows  = throws.Sum(t=>t.UsedDarts);

                summary.TotalPoints[p] = totalPoints;
                summary.TotalThrows[p] = totalThrows;
                summary.Average[p] = totalThrows == 0 ? 0 : totalPoints / totalThrows * 3;

                var checkouts = throws.Count(t => t.CheckedOut);
                var attempts  = throws.Sum(t => t.DoubleTries);
                summary.Checkouts[p] = checkouts;
                summary.CheckoutAttempts[p] = attempts;
                summary.CheckoutPercent[p] = attempts == 0 ? 0 : (int)Math.Round(100.0 * checkouts / attempts);
            }

            return summary;
        }

        public static ThrowAppliedDto Forfeit(string lobbyGUID, string quittingPlayer)
        {
            if (!_states.TryGetValue(lobbyGUID, out var s))
                throw new InvalidOperationException("Game state not initialized.");
            if (s.MatchEnded)
                throw new InvalidOperationException("Match already ended.");
            if (!s.Players.Contains(quittingPlayer))
                throw new InvalidOperationException("Player not in this match.");

            var winner = s.Players.First(p => p != quittingPlayer);

            s.EndedByForfeit = true;
            s.ForfeitedBy = quittingPlayer;
            s.MatchEnded = true;

            return new ThrowAppliedDto
            {
                LobbyGUID = lobbyGUID,
                PlayerName = winner,
                Scored = 0,
                NewScore = s.Scores[winner],
                LegEnded = true,
                MatchEnded = true,
                DoubleTries = 0,
                UsedDarts = 0,
                ClientThrowId = Guid.NewGuid().ToString()
            };
        }

        public static bool Cleanup(string lobbyGUID)
        {
            return _states.TryRemove(lobbyGUID, out _);
        }

        public class GameState
        {
            public string LobbyGUID { get; set; } = string.Empty;
            public List<string> Players { get; set; } = new();
            public GameSettings Settings { get; set; } = default!;

            public Dictionary<string, int> Scores { get; set; } = new();
            public int CurrentPlayerIndex { get; set; }
            public int NextLegStarterIndex { get; set; }
            public Dictionary<string, int> LegWins { get; set; } = new();
            public int TotalLegsPlayed { get; set; } = 0;
            public bool MatchEnded { get; set; } = false;

            public bool EndedByForfeit { get; set; } = false;
            public string? ForfeitedBy { get; set; } = null;

            public List<ThrowRecord> Throws { get; } = new();
            public HashSet<string> AppliedThrowIds { get; } = new();

            public void SaveThrow(ThrowRecord rec) => Throws.Add(rec);
        }

        public class ThrowRecord
        {
            public string PlayerName { get; set; } = string.Empty;
            public int Scored { get; set; }
            public int UsedDarts { get; set; }
            public int DoubleTries { get; set; }
            public bool CheckedOut { get; set; }
            public int ScoreBefore { get; set; }
        }
    }
}
