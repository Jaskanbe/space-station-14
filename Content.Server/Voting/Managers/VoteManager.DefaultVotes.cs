﻿using System;
using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Random;

namespace Content.Server.Voting.Managers
{
    public sealed partial class VoteManager
    {
        public void CreateStandardVote(IPlayerSession? initiator, StandardVoteType voteType)
        {
            switch (voteType)
            {
                case StandardVoteType.Restart:
                    CreateRestartVote(initiator);
                    break;
                case StandardVoteType.Preset:
                    CreatePresetVote(initiator);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(voteType), voteType, null);
            }

            TimeoutStandardVote(voteType);
        }

        private void CreateRestartVote(IPlayerSession? initiator)
        {
            var alone = _playerManager.PlayerCount == 1 && initiator != null;
            var options = new VoteOptions
            {
                Title = Loc.GetString("ui-vote-restart-title"),
                Options =
                {
                    (Loc.GetString("ui-vote-restart-yes"), true),
                    (Loc.GetString("ui-vote-restart-no"), false)
                },
                Duration = alone
                    ? TimeSpan.FromSeconds(10)
                    : TimeSpan.FromSeconds(30),
                InitiatorTimeout = TimeSpan.FromMinutes(3)
            };

            if (alone)
                options.InitiatorTimeout = TimeSpan.FromSeconds(10);

            WirePresetVoteInitiator(options, initiator);

            var vote = CreateVote(options);

            vote.OnFinished += (_, _) =>
            {
                var votesYes = vote.VotesPerOption[true];
                var votesNo = vote.VotesPerOption[false];
                var total = votesYes + votesNo;

                var ratioRequired = _cfg.GetCVar(CCVars.VoteRestartRequiredRatio);
                if (votesYes / (float) total >= ratioRequired)
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-restart-succeeded"));
                    EntitySystem.Get<GameTicker>().RestartRound();
                }
                else
                {
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("ui-vote-restart-failed", ("ratio", ratioRequired)));
                }
            };

            if (initiator != null)
            {
                // Cast yes vote if created the vote yourself.
                vote.CastVote(initiator, 0);
            }

            foreach (var player in _playerManager.GetAllPlayers())
            {
                if (player != initiator && !_afkManager.IsAfk(player))
                {
                    // Everybody else defaults to a no vote.
                    vote.CastVote(player, 1);
                }
            }
        }

        private void CreatePresetVote(IPlayerSession? initiator)
        {
            var presets = new Dictionary<string, string>
            {
                ["traitor"] = "mode-traitor",
                ["extended"] = "mode-extended",
                ["sandbox"] = "mode-sandbox",
                ["suspicion"] = "mode-suspicion",
            };

            var alone = _playerManager.PlayerCount == 1 && initiator != null;
            var options = new VoteOptions
            {
                Title = Loc.GetString("ui-vote-gamemode-title"),
                Duration = alone
                    ? TimeSpan.FromSeconds(10)
                    : TimeSpan.FromSeconds(30)
            };

            if (alone)
                options.InitiatorTimeout = TimeSpan.FromSeconds(10);

            foreach (var (k, v) in presets)
            {
                options.Options.Add((Loc.GetString(v), k));
            }

            WirePresetVoteInitiator(options, initiator);

            var vote = CreateVote(options);

            vote.OnFinished += (_, args) =>
            {
                string picked;
                if (args.Winner == null)
                {
                    picked = (string) _random.Pick(args.Winners);
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("ui-vote-gamemode-tie", ("picked", Loc.GetString(presets[picked]))));
                }
                else
                {
                    picked = (string) args.Winner;
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("ui-vote-gamemode-win", ("winner", Loc.GetString(presets[picked]))));
                }

                EntitySystem.Get<GameTicker>().SetStartPreset(picked);
            };
        }

        private void TimeoutStandardVote(StandardVoteType type)
        {
            var timeout = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteSameTypeTimeout));
            _standardVoteTimeout[type] = _timing.RealTime + timeout;
            DirtyCanCallVoteAll();
        }
    }
}
