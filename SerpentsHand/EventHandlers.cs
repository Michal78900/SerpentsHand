﻿using System.Collections.Generic;
using System.Linq;
using MEC;
using UnityEngine;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using System.Text;

namespace SerpentsHand
{
    public partial class EventHandlers
    {
        public static List<int> shPlayers = new List<int>();
        private List<int> shPocketPlayers = new List<int>();

        public static int teamRespawnCount = 0;
        public static int serpentsRespawnCount = 0;

        public static bool isSpawnable;

        bool test = false;

        private static System.Random rand = new System.Random();

        private static Vector3 shSpawnPos = new Vector3(0, 1002, 8);

        public void OnRoundStart()
        {
            test = false;
            shPlayers.Clear();
            shPocketPlayers.Clear();
            teamRespawnCount = 0;
            serpentsRespawnCount = 0;
        }

        public void CalculateChance()
        {
            if (rand.Next(1, 101) <= SerpentsHand.instance.Config.SpawnChance && Player.List.Count() > 0 && teamRespawnCount >= SerpentsHand.instance.Config.RespawnDelay && serpentsRespawnCount < SerpentsHand.instance.Config.MaxSpawns)
                isSpawnable = true;

            else isSpawnable = false;
        }

        public void OnTeamRespawn(RespawningTeamEventArgs ev)
        {
            if (isSpawnable)
            {
                if (ev.NextKnownTeam == Respawning.SpawnableTeamType.NineTailedFox)
                {
                    // Prevent announcement
                    ev.NextKnownTeam = Respawning.SpawnableTeamType.ChaosInsurgency;
                }

                List<Player> SHPlayers = new List<Player>();
                List<Player> CIPlayers = new List<Player>(ev.Players);
                ev.Players.Clear();

                for (int i = 0; i < SerpentsHand.instance.Config.MaxSquad && CIPlayers.Count > 0; i++)
                {
                    Player player = CIPlayers[rand.Next(CIPlayers.Count)];
                    SHPlayers.Add(player);
                    CIPlayers.Remove(player);
                }
                Timing.CallDelayed(0.1f, () =>
                {
                    if (!isSpawnable)
                        SHPlayers.Clear();

                    if (isSpawnable)
                    {
                        SpawnSquad(SHPlayers);
                        serpentsRespawnCount++;
                    }
                });
            }
            else if (ev.NextKnownTeam == Respawning.SpawnableTeamType.ChaosInsurgency)
            {
                string ann = SerpentsHand.instance.Config.CiEntryAnnouncement;
                if (ann != string.Empty)
                {
                    Cassie.GlitchyMessage(ann, 0.05f, 0.05f);
                }
            }
            teamRespawnCount++;
        }

        public void OnPocketDimensionEnter(EnteringPocketDimensionEventArgs ev)
        {
            if (shPlayers.Contains(ev.Player.Id))
            {
                shPocketPlayers.Add(ev.Player.Id);
            }
        }

        public void OnPocketDimensionDie(FailingEscapePocketDimensionEventArgs ev)
        {
            if (shPlayers.Contains(ev.Player.Id))
            {
                if (!SerpentsHand.instance.Config.FriendlyFire)
                {
                    ev.IsAllowed = false;
                }
                if (SerpentsHand.instance.Config.TeleportTo106)
                {
                    TeleportTo106(ev.Player);
                }
                shPocketPlayers.Remove(ev.Player.Id);
            }
        }

        public void OnPocketDimensionExit(EscapingPocketDimensionEventArgs ev)
        {
            if (shPlayers.Contains(ev.Player.Id))
            {
                ev.IsAllowed = false;
                if (SerpentsHand.instance.Config.TeleportTo106)
                {
                    TeleportTo106(ev.Player);
                }
                shPocketPlayers.Remove(ev.Player.Id);
            }
        }

        public void OnPlayerHurt(HurtingEventArgs ev)
        {
            Player scp035 = null;

            if (SerpentsHand.isScp035)
            {
                scp035 = TryGet035();
            }

            if (((shPlayers.Contains(ev.Target.Id) && (ev.Attacker.Team == Team.SCP || ev.HitInformations.GetDamageType() == DamageTypes.Pocket)) ||
                (shPlayers.Contains(ev.Attacker.Id) && (ev.Target.Team == Team.SCP || (scp035 != null && ev.Target == scp035))) ||
                (shPlayers.Contains(ev.Target.Id) && shPlayers.Contains(ev.Attacker.Id) && ev.Target != ev.Attacker)) && !SerpentsHand.instance.Config.FriendlyFire)
            {
                ev.Amount = 0f;
            }
        }

        public void OnPlayerDying(DyingEventArgs ev)
        {
            /* if (shPlayers.Contains(ev.Target.Id))
             {
                 shPlayers.Remove(ev.Target.Id);
             }

             if (ev.Target.Role == RoleType.Scp106 && !SerpentsHand.instance.Config.FriendlyFire)
             {
                 foreach (Player player in Player.List.Where(x => shPocketPlayers.Contains(x.Id)))
                 {
                     player.ReferenceHub.playerStats.HurtPlayer(new PlayerStats.HitInfo(50000, "WORLD", ev.HitInformation.GetDamageType(), player.Id), player.GameObject);
                 }
             }*/
        }

        public void OnPlayerDeath(DiedEventArgs ev)
        {
            if (shPlayers.Contains(ev.Target.Id))
            {
                ev.Target.CustomInfo = string.Empty;
                ev.Target.ReferenceHub.nicknameSync.ShownPlayerInfo |= PlayerInfoArea.Role;
                shPlayers.Remove(ev.Target.Id);
            }

            if (ev.Target.Role == RoleType.Scp106 && !SerpentsHand.instance.Config.FriendlyFire)
            {
                foreach (Player player in Player.List.Where(x => shPocketPlayers.Contains(x.Id)))
                {
                    player.ReferenceHub.playerStats.HurtPlayer(new PlayerStats.HitInfo(50000, "WORLD", ev.HitInformations.GetDamageType(), player.Id), player.GameObject);
                }
            }

            /* for (int i = shPlayers.Count - 1; i >= 0; i--)
             {
                 if (Player.Get(shPlayers[i]).Role == RoleType.Spectator)
                 {
                     shPlayers.RemoveAt(i);
                 }
             }*/
        }

        public void OnCheckRoundEnd(EndingRoundEventArgs ev)
        {
            Player scp035 = null;

            if (SerpentsHand.isScp035)
            {
                scp035 = TryGet035();
            }

            bool MTFAlive = CountRoles(Team.MTF) > 0;
            bool CiAlive = CountRoles(Team.CHI) > 0;
            bool ScpAlive = CountRoles(Team.SCP) + (scp035 != null && scp035.Role != RoleType.Spectator ? 1 : 0) > 0;
            bool DClassAlive = CountRoles(Team.CDP) > 0;
            bool ScientistsAlive = CountRoles(Team.RSC) > 0;
            bool SHAlive = shPlayers.Count > 0;

            if (SHAlive && ((CiAlive && !SerpentsHand.instance.Config.ScpsWinWithChaos) || DClassAlive || MTFAlive || ScientistsAlive))
            {
                ev.IsAllowed = false;
                test = true;
            }
            else if (SHAlive && ScpAlive && !MTFAlive && !DClassAlive && !ScientistsAlive)
            {
                if (!SerpentsHand.instance.Config.ScpsWinWithChaos)
                {
                    if (!CiAlive)
                    {
                        ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.Anomalies;
                        ev.IsAllowed = true;
                        ev.IsRoundEnded = true;

                        if (SerpentsHand.instance.Config.EndRoundFriendlyFire) GrantFF();
                    }
                }
                else
                {
                    ev.LeadingTeam = Exiled.API.Enums.LeadingTeam.Anomalies;
                    ev.IsAllowed = true;
                    ev.IsRoundEnded = true;

                    if (SerpentsHand.instance.Config.EndRoundFriendlyFire) GrantFF();
                }
            }
            else if (SHAlive && !ScpAlive && !MTFAlive && !DClassAlive && !ScientistsAlive)
            {
                if (SerpentsHand.instance.Config.EndRoundFriendlyFire) GrantFF();
            }
            else
            {
                test = false;
            }
        }

        public void OnSetRole(ChangingRoleEventArgs ev)
        {
            if (shPlayers.Contains(ev.Player.Id))
            {
                if (GetTeam(ev.NewRole) != Team.TUT)
                {
                    shPlayers.Remove(ev.Player.Id);
                    ev.Player.CustomInfo = string.Empty;
                    ev.Player.ReferenceHub.nicknameSync.ShownPlayerInfo |= PlayerInfoArea.Nickname;
                    ev.Player.ReferenceHub.nicknameSync.ShownPlayerInfo |= PlayerInfoArea.Role;
                }
                else
                {
                    ev.Player.CustomInfo = $"<color=#00FF58>{ev.Player.Nickname}\n{SerpentsHand.instance.Config.RoleName}</color>";
                    ev.Player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Nickname;
                    ev.Player.ReferenceHub.nicknameSync.ShownPlayerInfo &= ~PlayerInfoArea.Role;
                }
            }
        }

        public void OnShoot(ShootingEventArgs ev)
        {
            Player target = Player.Get(ev.Target);
            if (target != null && target.Role == RoleType.Scp096 && shPlayers.Contains(ev.Shooter.Id))
            {
                ev.IsAllowed = false;
            }
        }

        public void OnDisconnect(LeftEventArgs ev)
        {
            if (shPlayers.Contains(ev.Player.Id))
            {
                shPlayers.Remove(ev.Player.Id);
                ev.Player.CustomInfo = string.Empty;
                ev.Player.ReferenceHub.nicknameSync.ShownPlayerInfo |= PlayerInfoArea.Nickname;
                ev.Player.ReferenceHub.nicknameSync.ShownPlayerInfo |= PlayerInfoArea.Role;
            }
        }

        public void OnContain106(ContainingEventArgs ev)
        {
            if (shPlayers.Contains(ev.Player.Id) && !SerpentsHand.instance.Config.FriendlyFire)
            {
                ev.IsAllowed = false;
            }
        }

        public void OnRACommand(SendingRemoteAdminCommandEventArgs ev)
        {
            string cmd = ev.Name.ToLower();
            if (cmd == "spawnsh")
            {
                ev.IsAllowed = false;

                if (ev.Arguments.Count > 0 && ev.Arguments[0].Length > 0)
                {
                    Player cPlayer = Player.Get(ev.Arguments[0]);
                    if (cPlayer != null)
                    {
                        SpawnPlayer(cPlayer);
                        ev.Sender.RemoteAdminMessage($"Spawned {cPlayer.Nickname} as Serpents Hand.", true);
                        return;
                    }
                    else
                    {
                        ev.Sender.RemoteAdminMessage("Invalid player.", false);
                        return;
                    }
                }
                else
                {
                    ev.Sender.RemoteAdminMessage("SPAWNSH [Player Name / Player ID]", false);
                }
            }
            else if (cmd == "spawnshsquad")
            {
                ev.IsAllowed = false;

                if (ev.Arguments.Count > 0)
                {
                    if (int.TryParse(ev.Arguments[0], out int a))
                    {
                        CreateSquad(a);
                    }
                    else
                    {
                        ev.Sender.RemoteAdminMessage("Error: invalid size.", false);
                        return;
                    }
                }
                else
                {
                    CreateSquad(5);
                }
                Cassie.Message(SerpentsHand.instance.Config.EntryAnnouncement, true, true);
                ev.Sender.RemoteAdminMessage("Spawned squad.", true);
            }
        }

        public void OnGeneratorInsert(InsertingGeneratorTabletEventArgs ev)
        {
            if (shPlayers.Contains(ev.Player.Id) && !SerpentsHand.instance.Config.FriendlyFire)
            {
                ev.IsAllowed = false;
            }
        }

        public void OnFemurEnter(EnteringFemurBreakerEventArgs ev)
        {
            if (shPlayers.Contains(ev.Player.Id) && !SerpentsHand.instance.Config.FriendlyFire)
            {
                ev.IsAllowed = false;
            }
        }

        public void a(SendingConsoleCommandEventArgs ev)
        {
            if (ev.Name.ToLower() == "sh")
            {
                string msg = "1. " + (shPlayers.Count > 0) + "\n2. " + test;
                foreach (int player in shPlayers) msg += "- " + Player.Get(player).Nickname + "\n";
                ev.ReturnMessage = msg;
            }
        }

        public static int MaxSpawns = SerpentsHand.instance.Config.MaxSpawns;
    }
}