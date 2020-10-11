using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;
using scp035.API;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CISpy
{
	partial class EventHandlers
	{
		internal static void MakeSpy(Player player, bool isVulnerable = false, bool full = true)
		{
			if (!CISpy.instance.Config.SpawnWithGrenade && full)
			{
				for (int i = player.Inventory.items.Count - 1; i >= 0; i--)
				{
					if (player.Inventory.items[i].id == ItemType.GrenadeFrag)
					{
						player.Inventory.items.RemoveAt(i);
					}
				}
			}
			spies.Add(player, isVulnerable);
			player.Broadcast(10, "<size=32><color=red>[</color><color=green>Chaos</color><color=red>] </color>Eres un <b><color=green>Espia</color></b> en las filas de los <color=#003EFF>MTF</color></size>\n<size=25>Tu objetivo es asesinar a los <color=#FFE800>Científicos</color> y <color=#003EFF>MTF</color> sigilosamente y <color=#FF8000>Class-D</color> si es que lo deseas\n<i>[<color=#CE1111>Ñ</color>] O [<color=#CE1111>~</color>] para mas informacion.</i></size>");
			player.ReferenceHub.characterClassManager.TargetConsolePrint(player.ReferenceHub.scp079PlayerScript.connectionToClient, "Eres un espia de la insugencia del caos.\n\nAyuda a los Chaos a ganara esta ronda, mata a tantos MTF y cientificos como puedas pero recuerda, si un MTF o cientifico te ve matando a otro seras revelado", "yellow");
		}

		private Player TryGet035()
		{
			return Scp035Data.GetScp035();
		}

		private void RevealSpies()
		{
			foreach (KeyValuePair<Player, bool> spy in spies)
			{
				Inventory.SyncListItemInfo items = new Inventory.SyncListItemInfo();
				foreach (var item in spy.Key.Inventory.items) items.Add(item);
				Vector3 pos = spy.Key.Position;
				Vector3 rot = spy.Key.Rotation;
				int health = (int)spy.Key.Health;
				uint ammo1 = spy.Key.Ammo[(int)AmmoType.Nato556];
				uint ammo2 = spy.Key.Ammo[(int)AmmoType.Nato762];
				uint ammo3 = spy.Key.Ammo[(int)AmmoType.Nato9];

				spy.Key.SetRole(RoleType.ChaosInsurgency);

				Timing.CallDelayed(0.3f, () =>
				{
					spy.Key.Position = pos;
					spy.Key.Rotation = rot;
					spy.Key.Inventory.items.Clear();
					foreach (var item in items) spy.Key.Inventory.AddNewItem(item.id);
					spy.Key.Health = health;
					spy.Key.Ammo[(int)AmmoType.Nato556] = ammo1;
					spy.Key.Ammo[(int)AmmoType.Nato762] = ammo2;
					spy.Key.Ammo[(int)AmmoType.Nato9] = ammo3;
				});

				spy.Key.Broadcast(10, "Tus compañeros <color=\"green\">Chaos Insurgency</color> murieron.\nLos MTF saben que eres un traidor!");
			}
			spies.Clear();
		}

		private void GrantFF(Player player)
		{
			player.IsFriendlyFireEnabled = true;
			ffPlayers.Add(player);
		}

		private void RemoveFF(Player player)
		{
			player.IsFriendlyFireEnabled = false;
			ffPlayers.Remove(player);
		}

		private int CountRoles(RoleType role, List<Player> pList)
		{
			int count = 0;
			foreach (Player pl in pList) if (pl.Role == role) count++;
			return count;
		}

		private int CountRoles(Team team, List<Player> pList)
		{
			int count = 0;
			foreach (Player pl in pList) if (pl.Team == team) count++;
			return count;
		}

		private void CheckSpies(Player exclude = null)
		{
			Player scp035 = null;

			try
			{
				scp035 = TryGet035();
			}
			catch (Exception x)
			{
				//Log.Error($"SCP-035 not installed, skipping method call... {x}");
			}

			int playerid = -1;
			if (exclude != null) playerid = exclude.Id;
			List<Player> pList = Player.List.Where(x =>
			x.Id != playerid &&
			x.Id != scp035?.Id &&
			!spies.ContainsKey(x)).ToList();

			bool CiAlive = CountRoles(Team.CHI, pList) > 0;
			bool ScpAlive = CountRoles(Team.SCP, pList) > 0 + (scp035 != null ? 1 : 0);
			bool DClassAlive = CountRoles(Team.CDP, pList) > 0;
			bool ScientistsAlive = CountRoles(Team.RSC, pList) > 0;
			bool MTFAlive = CountRoles(Team.MTF, pList) > 0;

			if
			(
				((CiAlive || (CiAlive && ScpAlive) || (CiAlive && DClassAlive)) && !ScientistsAlive && !MTFAlive) ||
				((ScpAlive || DClassAlive) && !ScientistsAlive && !MTFAlive) ||
				((ScientistsAlive || MTFAlive || (ScientistsAlive && MTFAlive)) && !CiAlive && !ScpAlive && !DClassAlive)
			)
			{
				RevealSpies();
			}
		}
	}
}
