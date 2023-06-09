using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using static HSSR.Program;
using CAttributes;

namespace HSSR
{
	public class BotAdminCommands : BaseCommandModule
	{
		[Command("addauth"), CommandClass(Classes.CommandClasses.OwnerCommands), Description("Adds/Removes a user to the list of bot admins\n\nUsage:\n```addauth < ID / @mention >```"), RequireAuth()]
		public async Task AddAuth(CommandContext e, DiscordUser NewAdmin)
		{
			try
			{
				DiscordMessage resmsg = await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = !cInf.AuthUsers.Contains(NewAdmin.Id) ? $"{NewAdmin.Mention} has been authorized" : $"{NewAdmin.Mention} has been deauthorized" });
				if (!cInf.AuthUsers.Contains(NewAdmin.Id))
				{
					cInf.AuthUsers.Add(NewAdmin.Id);
				}
				else
				{
					cInf.AuthUsers.Remove(NewAdmin.Id);
				}
				File.WriteAllText("config/mconfig.json", Newtonsoft.Json.JsonConvert.SerializeObject(cInf));
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		[Command("status"), CommandClass(Classes.CommandClasses.OwnerCommands), Description("Sets bot status to given text. \"clear\" to delete status.\n\nUsage:\n```status <new status>```"), RequireAuth()]
		public async Task Status(CommandContext e, [RemainingText] string NewStatus)
		{
			try
			{
				if (NewStatus != "clear")
				{
					g1.Name = NewStatus;
					await discord.UpdateStatusAsync(g1);
				}
				else
				{
					await discord.UpdateStatusAsync();
				}
				DiscordMessageBuilder msgb = new DiscordMessageBuilder();
				msgb.WithEmbed(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Status updated to **{NewStatus}**" });
				msgb.WithReply(e.Message.Id);
				await e.Message.RespondAsync(msgb);
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

		
		[Command("globalexclude"), Description("Excludes a user from using the bot globally\n\nUsage:\n```globalexclude < ID / @mention >```"), RequireAuth, CommandClass(Classes.CommandClasses.OwnerCommands), IsExclude]
		public async Task GlobalExclude(CommandContext e, DiscordUser u)
		{
			try
			{
				if(!cInf.GlobalBlockedUsers.Contains(u.Id))
				{
					cInf.GlobalBlockedUsers.Add(u.Id);
					await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"User {u.Username}#{u.Discriminator} has been banned from using the bot globally"});
				}
				else
				{
					cInf.GlobalBlockedUsers.Remove(u.Id);
					await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"User {u.Username}#{u.Discriminator} has been banned from using the bot globally" });

				}
					File.WriteAllText("config/mconfig.json", Newtonsoft.Json.JsonConvert.SerializeObject(cInf));
					File.WriteAllText("config/mconfig.json", Newtonsoft.Json.JsonConvert.SerializeObject(cInf));
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}
	}
}
