using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;
using static HSSR.Program;
using Classes;
using CAttributes;
using System.Linq;
namespace HSSR
{
	public static class LevelSystem
	{

		public static async void DoTheTimer(MessageCreateEventArgs e)
		{
			try
			{
				if (e.Message.Author.IsBot == true || e.Channel.IsPrivate == true || servers.FindIndex(x => x.Id == e.Guild.Id) == -1 || !servers.Find(x => x.Id == e.Guild.Id).EnabledModules.HasFlag(Modules.Levelling) || servers.Find(x => x.Id == e.Guild.Id).channelxpexclude.Contains(e.Guild.Id))
				{
					return;
				}

				RegisteredServer s = servers.Find(x => x.Id == e.Guild.Id);

				if (!s.timedoutedusers.ContainsKey(e.Message.Author.Id))
				{
					s.timedoutedusers.Add(e.Message.Author.Id, DateTime.Now);
					AddXp(e);
				}
				
				if(DateTime.Now - s.timedoutedusers[e.Message.Author.Id] >= s.CoolDown)
				{
					s.timedoutedusers[e.Message.Author.Id] = DateTime.Now;
					AddXp(e);
				}

				int userslevel = 0;
				int j = 0;
				bool hasChanged = false;
				try
				{
					foreach (LevelRole i in s.lvlroles)
					{
						if(i.RoleId == 0)
						{
							continue;
						}

						if (i.XpReq <= s.xplist[e.Author.Id] && i.RoleId != 0)
						{
							userslevel++;
							j++;
							if (!(await e.Guild.GetMemberAsync(e.Author.Id)).Roles.Any(x => x.Id == i.RoleId))
							{
								await (await e.Guild.GetMemberAsync(e.Author.Id)).GrantRoleAsync(e.Guild.GetRole(i.RoleId));
								hasChanged = true;
							}

						}
						else
						{
							j++;
							if ((await e.Guild.GetMemberAsync(e.Author.Id)).Roles.Any(x => x.Id == i.RoleId))
							{
								await (await e.Guild.GetMemberAsync(e.Author.Id)).RevokeRoleAsync(e.Guild.GetRole(s.lvlroles.Find(x => x.RoleId == i.RoleId).RoleId));
							}
						}
					}
					if (hasChanged == true)
					{
						await discord.SendMessageAsync(e.Channel, new DiscordEmbedBuilder { Description = $"**{e.Author.Mention}** reached level **`{userslevel}`**!", Color = DiscordColor.Green });
					}
					File.WriteAllText("config/RegServers.json", JsonConvert.SerializeObject(servers, Formatting.Indented));
				}
				catch (DSharpPlus.Exceptions.UnauthorizedException)
				{
					await e.Channel.SendMessageAsync("I don't have permission to edit roles!");
				}
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}
		public static void AddXp(MessageCreateEventArgs e, int amount = -1)
		{
			if(servers.FindIndex(x=> x.Id == e.Guild.Id) == -1)
			{
				servers.Add(new RegisteredServer { Id = e.Guild.Id });
			}

			RegisteredServer s = servers.Find(x => x.Id == e.Guild.Id);

			if (amount == -1)
			{
				amount = new Random().Next(s.MinXp, s.MaxXp + 1);
			}

			if (s.xplist.ContainsKey(e.Message.Author.Id))
			{
				s.xplist[e.Message.Author.Id] += amount;
			}
			else
			{
				s.xplist.Add(e.Message.Author.Id, amount);
			}

			servers[servers.FindIndex(x => x.Id == e.Guild.Id)] = s;
			File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
		}
	}

	public class LevelCommands : BaseCommandModule
	{
		[Command("lvlroles"), CommandClass(CommandClasses.LevelCommands), RequireGuild(), Description("Shows the level roles of this server and the required xp\n\nUsage:\n```lvlroles```"), RequireBotPermissions2(Permissions.SendMessages)]
		public async Task LvlRoles(CommandContext e)
		{
			try
			{
				DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Color = DiscordColor.Green, Title = $"Level roles for {e.Guild.Name}", Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = e.Guild.IconUrl } };
				if (servers.FindIndex(x => x.Id == e.Guild.Id) == -1)
				{
					servers.Add(new RegisteredServer { Id = e.Guild.Id });
					File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
				}

				List<LevelRole> roles = servers.Find(x => x.Id == e.Guild.Id).lvlroles;
				int i = 0;
				string embedstring = "";
				if (roles.Count() == 1)
				{
					embedstring = "There's no level roles in this server!";
					await discord.SendMessageAsync(await discord.GetChannelAsync(e.Message.Channel.Id), embed);
					return;
				}
				else
				{
					foreach (LevelRole kvp in roles)
					{
						if (kvp.XpReq != 0)
						{
							embedstring += $"**`[{i + 1}]`** | <@&{kvp.RoleId}> (**{kvp.XpReq}**xp)\n";
							i++;
						}
					}
				}
				embed.AddField("Chat to gain xp!", embedstring, true);
				await discord.SendMessageAsync(await discord.GetChannelAsync(e.Message.Channel.Id), embed);
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		[Command("top"), CommandClass(CommandClasses.LevelCommands), RequireGuild(), Description("Shows the servers xp leaderboard\n\nUsage:\n```top [page, defaults to 1]```"), Aliases("lb")]
		public async Task Leaderboard(CommandContext e, int page = 1)
		{
			try
			{
				if (servers.FindIndex(x => x.Id == e.Guild.Id) == -1)
				{
					servers.Add(new RegisteredServer { Id = e.Guild.Id });
					File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
				}
				RegisteredServer s = servers.Find(x => x.Id == e.Guild.Id);
				DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"Page {page}/{Math.Ceiling((double)s.xplist.Count / 5)}" }, Color = DiscordColor.Green, Title = "Server XP leaderboard", Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = e.Guild.IconUrl } };
				var sortedleederboard = from entry in s.xplist orderby entry.Value descending select entry;

				string embedstring = "";
				int i = 0;
				foreach (KeyValuePair<ulong, int> kvp in sortedleederboard)
				{
					if (i >= (page - 1) * 5)
					{
						int userslevel = 0;
						foreach (var j in s.lvlroles)
						{
							if (j.XpReq <= s.xplist[kvp.Key] && j.XpReq != 0)
							{
								userslevel++;
							}
						}
						string role = "";
						if (userslevel != 0)
						{
							role = $"<@&{s.lvlroles[userslevel].RoleId.ToString()}>";
						}
						else
						{
							role = "No Role";
						}
						try
						{
							DiscordUser user = await discord.GetUserAsync(kvp.Key);
							if (kvp.Key != e.Message.Author.Id)
							{
								embedstring += $"**```#{i + 1} | {user.Username}``` {kvp.Value}xp | [{role}]**\n\n";
							}
							else
							{
								embedstring += $"**```< #{i + 1} | {user.Username} >```{kvp.Value}xp | [{role}]**\n\n";
							}
						}
						catch
						{
							embedstring += $"**```#{i + 1} | Unknown User - ID:{kvp.Key}``` {kvp.Value}xp | [{role}]**\n\n";
						}
						i++;
						if (i == ((page - 1) * 5) + 5)
						{
							break;
						}
					}
					else
					{
						i++;
					}
				}
				embed.Description = embedstring;

				await discord.SendMessageAsync(e.Channel, embed);
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		[Command("rank"), CommandClass(CommandClasses.LevelCommands), RequireGuild(), Description("Shows yours or another users level\n\nUsage:\n```rank [ ID / @mention ]```"), Aliases("lvl", "level"), RequireBotPermissions2(Permissions.SendMessages)]
		public async Task Rank(CommandContext e, DiscordUser user = null)
		{
			try
			{
				if (user == null)
				{
					user = e.Message.Author;
				}
				if (servers.FindIndex(x => x.Id == e.Guild.Id) == -1)
				{
					servers.Add(new RegisteredServer { Id = e.Guild.Id });
					File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
				}

				RegisteredServer s = servers.Find(x => x.Id == e.Guild.Id);
				if (s.xplist.ContainsKey(user.Id))
				{
					int userslevel = 0;
					foreach (var i in s.lvlroles)
					{
						if (i.XpReq <= s.xplist[user.Id] && i.XpReq != 0)
						{
							userslevel++;
						}
					}
					var sortedleederboard = from entry in s.xplist orderby entry.Value descending select entry;
					DiscordEmbedBuilder embed = new DiscordEmbedBuilder
					{
						Title = "Server rank card",
						Description = $"**```{user.Username}#{user.Discriminator}  | Level {userslevel} | Rank #{sortedleederboard.ToList().FindIndex(x => x.Key == user.Id) + 1}```**\n",
						Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = user.AvatarUrl }
					};
					if (userslevel == 0)
					{
						embed.Color = DiscordColor.Black;
					}
					else
					{
						embed.Color = e.Guild.GetRole(s.lvlroles[userslevel].RoleId).Color;
						embed.Description += $"**[<@&{s.lvlroles[userslevel].RoleId}>]\n**";
					}
					string progstring = "";
					embed.AddField("Total", $"**```{s.xplist[user.Id]}xp```**", true);
					if (userslevel < s.lvlroles.Count() - 1)
					{
						// 🟦
						for (int i = 0; i < 10; i++)
						{
							if (s.xplist[user.Id] - s.lvlroles[userslevel].XpReq >= ((s.lvlroles[userslevel + 1].XpReq - s.lvlroles[userslevel].XpReq) / 10) * i)
							{
								progstring += "🟦";
							}
							else
							{
								progstring += "⬜";
							}
						}
						embed.AddField("Progress", $"**```{s.xplist[user.Id] - s.lvlroles[userslevel].XpReq}xp / {s.lvlroles[userslevel + 1].XpReq - s.lvlroles[userslevel].XpReq}xp```**\n" + progstring, true);
						embed.AddField("Next Level", $"**[Level {s.lvlroles.IndexOf(s.lvlroles[userslevel + 1])}]** | **<@&{s.lvlroles[userslevel + 1].RoleId}> | {s.lvlroles[userslevel + 1].XpReq} total xp needed**", false);
					}
					else
					{
						embed.Fields[0].Value = "Max level reached!";
						// 🟦
						for (int i = 0; i < 10; i++)
						{
							progstring += "🟦";
						}
						embed.AddField("Progress", progstring, true);
					}
					await discord.SendMessageAsync(await discord.GetChannelAsync(e.Message.Channel.Id), embed);
				}
				else
				{
					await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Description = $"**{user.Username}** hasn't collected any XP yet!", Color = DiscordColor.Green });
				}
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}
		[Command("convertleveltoxp"), CommandClass(CommandClasses.LevelCommands), RequireGuild()]
		public async Task ConvertLevelToXpCmd(CommandContext e, int lvl)
		{
			await e.RespondAsync(Program.ConvertLevelToXp(lvl).ToString());
		}
		[Command("lvledit"), CommandClass(CommandClasses.LevelCommands), RequireGuild(), Description("Edits the required amount of xp to obtain a specified role.\nIf the amount is zero, or omitted, the role will be removed.\n\nUsage:\n```lvladd < ID / @mention > [score]```"), RequireUserPermissions2(Permissions.ManageGuild), RequireBotPermissions2(Permissions.ManageRoles & Permissions.SendMessages)]
		public async Task LvlAdd(CommandContext e, DiscordRole role, int score = 0)
		{
			try
			{

				if (e.Guild.Roles.Values.ToList().FindIndex(x => x.Id == role.Id) == -1)
				{
					await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"Role **{role.Name}** doesn't exist in this server!" });
					return;
				}

				if (servers.FindIndex(x => x.Id == e.Guild.Id) == -1)
				{
					servers.Add(new RegisteredServer { Id = e.Guild.Id });
					File.WriteAllText("config/RegServers.json", JsonConvert.SerializeObject(servers, Formatting.Indented));
				}

				RegisteredServer s = servers[servers.FindIndex(x => x.Id == e.Guild.Id)];

				if (s.lvlroles.FindIndex(x => x.RoleId == role.Id) != -1)
				{
					LevelRole therole = s.lvlroles.Find(x => x.RoleId == role.Id);
					if (score <= 0)
					{
						s.lvlroles.Remove(s.lvlroles[s.lvlroles.FindIndex(x => x.RoleId == role.Id)]);
						await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Description = $"Role {role.Name} deleted", Color = DiscordColor.Green });
					}
					else
					{
						s.lvlroles.Find(x => x.RoleId == role.Id);
						s.lvlroles[s.lvlroles.FindIndex(x => x.RoleId == role.Id)].XpReq = score;
						var sortedleederboard = from entry in s.lvlroles orderby entry.XpReq ascending select entry;
						var list = sortedleederboard.ToList();
						s.lvlroles = list;

						await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Description = $"Role {role.Name} edited", Color = DiscordColor.Green });
					}
				}
				else
				{
					s.lvlroles.Add(new LevelRole { Name = role.Name, XpReq = score, RoleId = role.Id });
					var sortedleederboard = from entry in s.lvlroles orderby entry.XpReq ascending select entry;
					var list = sortedleederboard.ToList();
					s.lvlroles = list;
					await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Role **{role.Name}** added to **{e.Guild.Name}**'s level roles!" });
				}

				servers[servers.FindIndex(x => x.Id == e.Guild.Id)] = s;

				File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		[Command("xpedit"), CommandClass(CommandClasses.LevelCommands), RequireGuild(), Description("Edits the **total** amount of xp a user has to the provided value.\n\nUsage:\n```addxp < ID / @mention > [xp]```"), RequireUserPermissions2(Permissions.ManageGuild), RequireBotPermissions2(Permissions.SendMessages)]
		public async Task AddXpUser(CommandContext e, DiscordUser user, int xp = 0)
		{
			try
			{
				if (xp >= 0)
				{
					if (servers.FindIndex(x => x.Id == e.Guild.Id) == -1)
					{
						servers.Add(new RegisteredServer { Id = e.Guild.Id });
						File.WriteAllText("config/RegServers.json", JsonConvert.SerializeObject(servers, Formatting.Indented));
					}
					if (await e.Guild.GetMemberAsync(user.Id) != null)
					{
						RegisteredServer s = servers[servers.FindIndex(x => x.Id == e.Guild.Id)];
						if (s.xplist.ContainsKey(user.Id))
						{
							if (xp == 0)
							{
								await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"**{user.Username}#{user.Discriminator}**'s xp was set to 0!\n**```Before: {s.xplist[user.Id]}\nAfter: 0```**" });
								s.xplist.Remove(user.Id);
							}
							else
							{
								await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"**{user.Username}#{user.Discriminator}**'s xp was edited!\n**```Before: {s.xplist[user.Id]}xp\nAfter: {xp}xp```**" });
								s.xplist[user.Id] = xp;
							}
						}
						else
						{
							s.xplist.Add(user.Id, xp);
							await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"**{xp}**xp was added to **{user.Username}#{user.Discriminator}**! **```Before: 0\nAfter: {xp}xp```**" });
						}
						servers[servers.FindIndex(x => x.Id == e.Guild.Id)] = s;
					}
					else
					{
						await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"User not found!" });
					}
					File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
				}
				else
				{
					await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"You can't set negative xp, silly!" });
				}
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		[Command("channeledit"), CommandClass(CommandClasses.LevelCommands), Description("Activates/Deactivates xp gaining in the specified channel \n\nUsage:\n```channeledit < ID / #mention >```"), RequireGuild(), RequireUserPermissions2(Permissions.ManageGuild), RequireBotPermissions2(Permissions.SendMessages)]
		public async Task ChannelEdit(CommandContext e, DiscordChannel channel) 
		{
			try
			{
				if (servers.FindIndex(x => x.Id == e.Guild.Id) == -1)
				{
					servers.Add(new RegisteredServer { Id = e.Guild.Id });
					File.WriteAllText("config/RegServers.json", JsonConvert.SerializeObject(servers, Formatting.Indented));
				}
				RegisteredServer s = servers[servers.FindIndex(x => x.Id == e.Guild.Id)];
				if (e.Guild.GetChannel(channel.Id) != null)
				{
					if (s.channelxpexclude.Contains(channel.Id))
					{
						s.channelxpexclude.Remove(channel.Id);
						await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Channel {channel.Mention} is no longer excluded from gaining xp!" });
						if (s.channelxpexclude.Count == 0)
						{
							servers[servers.FindIndex(x => x.Id == e.Guild.Id)].channelxpexclude.Clear();
						}
					}
					else
					{
						s.channelxpexclude.Add(channel.Id);
						await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Channel {channel.Mention} is now excluded from gaining xp!" });
					}
					servers[servers.FindIndex(x => x.Id == e.Guild.Id)] = s;
					File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
				}
				else
				{
					await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"Channel not found!" });
				}
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		[Command("xpreset"), CommandClass(CommandClasses.LevelCommands), RequireGuild(), RequireAuth, Hidden()]
		public async Task ResetXp(CommandContext e, ulong serverid = 0)
		{
			try
			{
				if (serverid == 0)
				{
					servers[servers.FindIndex(x => x.Id == e.Guild.Id)].xplist = new Dictionary<ulong, int>();
				}
				else
				{
					servers[servers.FindIndex(x => x.Id == serverid)].xplist = new Dictionary<ulong, int>();
				}
				File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
				await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Reseted xp of the whole server{serverid}!" });
			}
			catch (Exception ex)
			{
				await AlertException(e, ex); // add addxp command for specific user!
			}
		}
	}
}