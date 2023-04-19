using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using CAttributes;
using static HSSR.Program;
namespace HSSR
{
	public class ModeratorCommands : BaseCommandModule
	{
		[Command("exclude"), Description("Excludes a user locally from using the bot\n\nUsage:\n```localexclude < ID / @mention >```"), RequireUserPermissions2(DSharpPlus.Permissions.ManageGuild), IsExclude, CommandClass(Classes.CommandClasses.ModCommands)]
		public async Task LocalExclude(CommandContext e, DiscordUser u)
		{
			try
			{
				Classes.RegisteredServer s = servers.Find(x => x.Id == e.Guild.Id);
				if(!s.ServerBlockedUsers.Contains(u.Id))
				{
					servers.Find(x => x.Id == e.Guild.Id).ServerBlockedUsers.Add(u.Id);
					await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"User {u.Username}#{u.Discriminator} has been banned from using the bot on this server." });
				}
				else
				{
					servers.Find(x => x.Id == e.Guild.Id).ServerBlockedUsers.Remove(u.Id);
					await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"User {u.Username}#{u.Discriminator} has been unbanned from using the bot on this server" });
				}
				System.IO.File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers));
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}
	}
}