using Classes;
using CAttributes;
using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using static HSSR.Program;
namespace HSSR
{
	[Group("config"), Description("Contains commands for the configuration within a server.\n\nUsage:\n```config <commandname>```"), IsExclude(), CommandClass(CommandClasses.ConfigCommands)]
	public class ConfigCommands : BaseCommandModule
	{
		[Command("show"), Description("Shows current configurations of the server\n\nUsage:\n```config show```"), CommandClass(CommandClasses.ConfigCommands)]
		public async Task ListCfg(CommandContext e, string format = "n")
		{
			try
			{
				DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Color = DiscordColor.Green, Title = $"Server configuration for server {e.Guild.Name}" };
				RegisteredServer s = servers.Find(x => x.Id == e.Guild.Id);
				
				if(format == "n")
				{
					string enModules = "";
					string lvlroles = "";

					foreach (var module in Enum.GetValues(typeof(Classes.Modules)))
					{
						if ((Modules)module != Classes.Modules.None && (Modules)module != Classes.Modules.All)
						{
							enModules += $"{module.ToString()}: {servers.Find(x => x.Id == e.Guild.Id).EnabledModules.HasFlag((Enum)module)}\n";
						}
					}

					foreach(LevelRole l in s.lvlroles)
					{
						if(l.RoleId != 0)
						{
							lvlroles += $"{l.Name} : {l.XpReq}xp (ID:{l.RoleId})\n";
						}
					}

					embed.AddField("General",
					$"```Id: {s.Id.ToString()}\nName: {e.Guild.Name}```");
					embed.AddField("Modules", $"```{enModules}```");
					if(!string.IsNullOrEmpty(lvlroles)) { embed.AddField("Level roles", $"```{lvlroles}```"); }
					embed.AddField("XP options", $"```MinXp: {s.MinXp}xp\nMaxXp: {s.MaxXp}xp\nXp cooldown: {s.CoolDown}```");
				}
				else if(format == "json")
				{
					embed.Description = $"```json\n{Newtonsoft.Json.JsonConvert.SerializeObject(s, Newtonsoft.Json.Formatting.Indented)}```";
				}
				await e.RespondAsync(embed);
			}
			catch(Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		[Command("minxp"), Description("Changes the minimum amount of xp you can receive.\n\nUsage:\n```config minxp <amount>```"), CommandClass(CommandClasses.ConfigCommands), RequireUserPermissions2(DSharpPlus.Permissions.ManageGuild)]
		public async Task SetMinXp(CommandContext e, int newxp)
		{
			try
			{
				RegisteredServer s = servers.Find(x => x.Id == e.Guild.Id);
				int oldamt = s.MinXp;
				if(newxp <= 0 || newxp >= s.MaxXp)
				{
					await e.Message.RespondAsync(new DiscordEmbedBuilder { Description = $"New MinXp has to be greater than 0 and smaller than MaxXp\n```Specified amount: {newxp}\nMaxXp: {s.MaxXp}```", Color = DiscordColor.Red});
					return;
				}
				s.MinXp = newxp;
				await e.RespondAsync(new DiscordEmbedBuilder { Description = $"MinXp has been changed!\n```Before: {oldamt}\nAfter: {newxp}```", Color = DiscordColor.Green});
				File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Newtonsoft.Json.Formatting.Indented));
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		[Command("maxxp"), Description("Changes the maximum amount of xp you can receive.\n\nUsage:\n```config maxxp <amount>```"), CommandClass(CommandClasses.ConfigCommands), RequireUserPermissions2(DSharpPlus.Permissions.ManageGuild)]
		public async Task SetMaxXp(CommandContext e, int newxp)
		{
			try
			{
				RegisteredServer s = servers.Find(x => x.Id == e.Guild.Id);
				int oldamt = s.MaxXp;
				if (newxp <= s.MinXp)
				{
					await e.Message.RespondAsync(new DiscordEmbedBuilder { Description = $"The new MaxXp Value has to be greater than MinXp!\n```Specified amount: {newxp}\nMinXp: {s.MinXp}```", Color = DiscordColor.Red });
					return;
				}
				s.MaxXp = newxp;
				await e.RespondAsync(new DiscordEmbedBuilder { Description = $"MaxXp has been changed!\n```Before: {oldamt}\nAfter: {newxp}```", Color = DiscordColor.Green });
				File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Newtonsoft.Json.Formatting.Indented));

			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		[Command("cooldown"), Description("Changes the xp-receiving cooldown\n\nUsage:\n```config cooldown <new cooldown time>```\nFormat:\n```hh:mm:ss```"), CommandClass(CommandClasses.ConfigCommands), RequireUserPermissions2(DSharpPlus.Permissions.ManageGuild)]
		public async Task SetCoolDown(CommandContext e, string d)
		{
			try
			{
				RegisteredServer s = servers.Find(x => x.Id == e.Guild.Id);
				TimeSpan dt;
				TimeSpan oldamt = s.CoolDown;
				if(!TimeSpan.TryParse(d, new System.Globalization.CultureInfo("de-DE"), out dt))
				{
					await e.RespondAsync(new DiscordEmbedBuilder { Description = "Invalid Format (hh:mm:ss)", Color = DiscordColor.Red });
					return;
				}

				if(dt.TotalMilliseconds < 0)
				{
					await e.RespondAsync(new DiscordEmbedBuilder { Description = "Cooldown time has to be greater than or equal to 0", Color = DiscordColor.Red });
					return;
				}

				s.CoolDown = dt;
				await e.RespondAsync(new DiscordEmbedBuilder { Description = $"Cooldown has been changed!\n```Before: {oldamt}\nAfter: {dt}```", Color = DiscordColor.Green });
				File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Newtonsoft.Json.Formatting.Indented));
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}
		[Command("togglemodule"), Description("Toggles a specified module on or off\n\nUsage:\n```config togglemodule <module name>```"), CommandClass(CommandClasses.ConfigCommands), RequireUserPermissions2(DSharpPlus.Permissions.ManageGuild)]
		public async Task ToggleModule(CommandContext e, string ModuleName = "help")
		{
			try
			{
				if (ModuleName == "help")
				{
					DiscordEmbedBuilder embed = new DiscordEmbedBuilder
					{
						Title = $"Modules in server {e.Guild.Name}",
						Color = DiscordColor.Green
					};
					foreach (var module in Enum.GetValues(typeof(Classes.Modules)))
					{
						if ((int)module != 0b11)
						{
							embed.Description += $"```{module.ToString()}: {servers.Find(x => x.Id == e.Guild.Id).EnabledModules.HasFlag((Enum)module)}```";
						}
					}
					await e.RespondAsync(embed: embed);
					return;
				}
				if (Enum.TryParse(ModuleName, true, out Classes.Modules mod))
				{
					DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Color = DiscordColor.Green };
					servers.Find(x => x.Id == e.Guild.Id).EnabledModules = mod ^ servers.Find(x => x.Id == e.Guild.Id).EnabledModules;
					embed.Description = $"Module **`{mod}`** has been toggled";
					await e.RespondAsync(embed: embed);
					File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Newtonsoft.Json.Formatting.Indented));
				}
				else
				{
					await e.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"Module {ModuleName} not found" });
				}
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}
	}
}