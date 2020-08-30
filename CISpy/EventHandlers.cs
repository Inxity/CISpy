using Exiled.API.Features;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.Events.EventArgs;

namespace CISpy
{
	public partial class EventHandlers
	{
		public static Dictionary<Player, bool> spies = new Dictionary<Player, bool> ();
		public List<Player> ffPlayers = new List<Player>();

		public bool isDisplayFriendly = false;
		//private bool isDisplaySpy = false;

		private Random rand = new Random();

		public void OnRoundStart()
		{
			spies.Clear();
			ffPlayers.Clear();
			if (rand.Next(1, 101) <= CISpy.instance.Config.GuardSpawnChance)
			{
				Player player = Player.List.FirstOrDefault(x => x.Role == RoleType.FacilityGuard);
				if (player != null)
				{
					MakeSpy(player);
				}
			}
		}

		public void OnTeamRespawn(RespawningTeamEventArgs ev)
		{
			if (ev.NextKnownTeam == Respawning.SpawnableTeamType.NineTailedFox && rand.Next(1, 101) <= CISpy.instance.Config.SpawnChance && ev.Players.Count >= CISpy.instance.Config.MinimumSquadSize)
			{
				List<Player> respawn = new List<Player>(ev.Players);
				Timing.CallDelayed(0.1f, () =>
				{
					List<Player> roleList = respawn.Where(x => CISpy.instance.Config.SpyRoles.Contains((int)x.Role)).ToList();
					if (roleList.Count > 0)
					{
						Player player = roleList[rand.Next(roleList.Count)];
						if (player != null)
						{
							MakeSpy(player);
						}
					}
				});
			}
		}

		public void OnSetClass(ChangingRoleEventArgs ev)
		{
			if (spies.ContainsKey(ev.Player))
			{
				Timing.CallDelayed(0.1f, () => spies.Remove(ev.Player));
			}
		}

		public void OnPlayerDie(DiedEventArgs ev)
		{
			if (spies.ContainsKey(ev.Target))
			{
				spies.Remove(ev.Target);
			}

			CheckSpies(ev.Target);
		}

		public void OnPlayerLeave(LeftEventArgs ev)
		{
			CheckSpies(ev.Player);
		}

		public void OnPlayerHurt(HurtingEventArgs ev)
		{
			if (ffPlayers.Contains(ev.Attacker))
			{
				RemoveFF(ev.Attacker);
			}

			Player scp035 = null;

			try
			{
				scp035 = TryGet035();
			} 
			catch (Exception x)
			{
				Log.Error($"SCP-035 not installed, skipping method call... {x}");
			}

			if (spies.ContainsKey(ev.Target) && !spies.ContainsKey(ev.Attacker) && ev.Target.Id != ev.Attacker.Id && (ev.Attacker.Team == Team.CHI || ev.Attacker.Team == Team.CDP))
			{
				if (!isDisplayFriendly)
				{
					ev.Attacker.Broadcast(3, "Estas disparandole al<b><color=\"green\">espia de los chaos insurgency</color></b>");
					isDisplayFriendly = true;
				}
				Timing.CallDelayed(3f, () =>
				{
					isDisplayFriendly = false;
				});
				ev.Amount = 0;
			}
			else if (!spies.ContainsKey(ev.Target) && spies.ContainsKey(ev.Attacker) && (ev.Target.Team == Team.CHI || ev.Target.Team == Team.CDP) && ev.Target.Id != scp035?.Id)
			{
				ev.Amount = 0;
			}
			/*else if (spies.ContainsKey(ev.Attacker) && spies.ContainsKey(ev.Player))
			{
				if (!isDisplaySpy)
				{
					ev.Attacker.Broadcast(3, "You are shooting another <b><color=\"green\">CISpy!</color></b>", false);
					isDisplaySpy = true;
				}
				Timing.CallDelayed(3f, () =>
				{
					isDisplaySpy = false;
				});
				ev.Amount = 0;
			}*/ 
		}

		public void OnShoot(ShootingEventArgs ev)
		{
			if (ev.Target == null) return;
			Player target = Player.Get(ev.Target);
			if (target == null) return;

			Player scp035 = null;

			try
			{
				scp035 = TryGet035();
			}
			catch (Exception x)
			{
				Log.Error($"SCP-035 not installed, skipping method call... {x}");
			}

			if (spies.ContainsKey(ev.Shooter) && !spies.ContainsKey(target) && (target.Team == Team.RSC || target.Team == Team.MTF) && target.Id != scp035?.Id)
			{
				if (!spies[ev.Shooter])
				{
					spies[ev.Shooter] = true;
					ev.Shooter.Broadcast(10, $"Has atacado a un {(target.Team == Team.MTF ? "<color=#00b0fc>MTF" : "<color=#fcff8d>Cientifico")}</color>, ten cuidado de que no te haya visto los <color=#00b0fc>MTF</color> y <color=#fcff8d>cientificos</color> o podrian matarte");
				}
				GrantFF(ev.Shooter);
			}
			else if (spies.ContainsKey(target) && !spies.ContainsKey(ev.Shooter) && (ev.Shooter.Team == Team.MTF || ev.Shooter.Team == Team.RSC))
			{
				if (spies[target])
				{
					GrantFF(ev.Shooter);
				}
			}
		}
	}
}
