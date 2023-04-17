using System.Text.RegularExpressions;
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
using Classes;

namespace Sylt51bot
{
    class Program
    {
		public static CommandsNextExtension commands;
		public static DiscordClient discord;
		public static DiscordActivity g1 = new DiscordActivity("");
		public static ulong LastHb = 0; // Last heartbeat message
		public static SetupInfo cInf; // The setup info
        
		public static CommandsNextConfiguration cNcfg; // The commanddsnext config
		public static DiscordConfiguration dCfg; // The discord config
		public static List<RegisteredServer> servers; // The list registered of servers
		static void Main(string[] args)
        {
            try
            {
                if (File.Exists("config/mconfig.json"))
                {
                    cInf = Newtonsoft.Json.JsonConvert.DeserializeObject<SetupInfo>(File.ReadAllText("config/mconfig.json"));
					cInf.Version = new SetupInfo().Version;
                    if(File.Exists("config/RegServers.json"))
                    {
                        servers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<RegisteredServer>>(File.ReadAllText("config/RegServers.json"));
						foreach(RegisteredServer s in servers)
						{
							if(s.channelxpexclude == null)
							{
								s.channelxpexclude = new List<ulong>();
							}
							if(s.lvlroles == null)
							{
								s.lvlroles = new List<LevelRole> { new LevelRole { RoleId = 0, XpReq = 0, Name = "No Role"} };
							}
							if(s.xplist == null)
							{
								s.xplist = new Dictionary<ulong, int>();
							}
							if(s.timedoutedusers == null)
							{
								s.timedoutedusers = new Dictionary<ulong, DateTime>();
							}
						}

					}
                    else
                    {
                        servers = new List<RegisteredServer>();
                    }
					File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine("Missing setup info");
                    Environment.Exit(0);
                }
                cNcfg = new CommandsNextConfiguration
                {
                    StringPrefixes = cInf.Prefixes,
                    CaseSensitive = false,
                    EnableDefaultHelp = true
                };
                dCfg = new DiscordConfiguration
                {
                    Token = cInf.Token,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
                };
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Environment.Exit(0);
            }
            MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            try
            {
                discord = new DiscordClient(dCfg);
                commands = discord.UseCommandsNext(cNcfg);

                commands.CommandErrored += CmdErrorHandler;
                commands.SetHelpFormatter<CustomHelpFormatter>();
				commands.RegisterCommands<LevelCommands>();
				commands.RegisterCommands<BotAdminCommands>();
				commands.RegisterCommands<GenCommands>();
				commands.RegisterCommands<ConfigCommands>();
				commands.RegisterCommands<ModeratorCommands>();
                
				discord.MessageCreated += async (client, e) =>
                {
					if(servers.FindIndex(x => x.Id == e.Guild.Id) == -1)
					{
						servers.Add(new RegisteredServer { Id = e.Guild.Id} );
						File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
					}
                    if(e.Message.Author.Id == 159985870458322944 && e.Message.Content.StartsWith("GG Hero named ") && servers.Find(x => x.Id == e.Guild.Id).EnabledModules.HasFlag(Modules.Mee6Migration))
					{
						Match match = System.Text.RegularExpressions.Regex.Match(e.Message.Content, @"GG Hero named <@(?<uID>[\d]+)>, your powers have increased to \*\*level (?<lvl>[\d]+)\*\* !");
						ulong uID = ulong.Parse(match.Groups["uID"].Value);
						int lvl = int.Parse(match.Groups["lvl"].Value);
						Command c = commands.FindCommand("xpedit", out string arg);
						
						await commands.ExecuteCommandAsync(commands.CreateContext(e.Message, cInf.Prefixes[0], c, $"{uID} {ConvertLevelToXp(lvl)}"));
					}
					LevelSystem.DoTheTimer(e);
                };

				discord.GuildCreated += async (client, e) =>
				{
					if (servers.FindIndex(x => x.Id == e.Guild.Id) == -1)
					{

                        RegisteredServer newJoinedServer = new RegisteredServer
                        {
                            Id = e.Guild.Id
                        };
                        servers.Add(newJoinedServer);
                        File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
                    }
                    await Task.Delay(1);
				};

				
                await discord.ConnectAsync();
                await SendHeartbeatAsync().ConfigureAwait(false);
				await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                try
				{
					Console.WriteLine("CONNECTION TERMINATED\nAttempting automatic restart...");
					File.WriteAllText("Error.log", ex.ToString());
					Main(new string[]{});
				}
				catch
				{
					Console.WriteLine("Automatic restart failed.");
				}
            }
        }
        static async  Task CmdErrorHandler(CommandsNextExtension _m, CommandErrorEventArgs e)
        {
            try
			{
				var failedChecks = ((DSharpPlus.CommandsNext.Exceptions.ChecksFailedException)e.Exception).FailedChecks;
				DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = "Command could not be executed :c" };
				bool canSend = false;
				if(e.Context.Channel.PermissionsFor(await e.Context.Guild.GetMemberAsync(discord.CurrentUser.Id)).HasPermission(Permissions.SendMessages))
				{
					canSend = true;
				}
				foreach (var failedCheck in failedChecks)
				{
					if (failedCheck is CAttributes.RequireBotPermissions2Attribute)
					{
						var botperm = (CAttributes.RequireBotPermissions2Attribute)failedCheck;
						embed.AddField("Your needed permissions", $"```{botperm.Permissions.ToPermissionString()}```");
						if (botperm.Permissions.HasFlag(Permissions.SendMessages))
						{
							canSend = false;
						}
					}
					if (failedCheck is CAttributes.RequireUserPermissions2Attribute)
					{
						var botperm = (CAttributes.RequireUserPermissions2Attribute)failedCheck;
						embed.AddField("My needed permission", $"```{botperm.Permissions.ToPermissionString()}```");
					}
					if (failedCheck is RequireGuildAttribute)
					{
						RequireGuildAttribute guild = (RequireGuildAttribute)failedCheck;
						embed.AddField("Server only", "This command cannot be used in DMs");
					}
					if(failedCheck is CAttributes.ModuleAttribute)
					{
						CAttributes.ModuleAttribute mod = (CAttributes.ModuleAttribute)failedCheck;
						embed.AddField("The following module has to be activated:", $"```{mod.module}```");
					}
					if(failedCheck is CAttributes.IsExcludeAttribute)
					{
						embed.AddField("Locked", "You have been blocked from using the bot locally or globally");
					}
					embed.AddField("Error:", $"```{e.Exception.ToString()}```");
				}
				if (canSend == true)
				{
					await e.Context.Message.RespondAsync(embed);
				}
				else
				{
					await e.Context.Guild.Owner.SendMessageAsync("I can't send messages in your server but I'm lacking perms to work so have this list in DMs instead", embed);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
				Console.WriteLine(e.Exception.ToString());
			}
        }
		public static int ConvertLevelToXp(int lvl)
		{
			int xp = 100 * lvl + 25 * lvl * (lvl - 1) + (5 * (lvl - 1) * lvl * (2 * lvl - 1)) / 6;
			return xp;
		}
		public static async Task AlertException(CommandContext e, Exception ex)
		{
			await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = "An error occurred" });
			Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
			await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
		}

		public static async Task AlertException(MessageCreateEventArgs e, Exception ex)
		{
			await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = "An error occurred" });
			Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
			await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
		}

		public static async Task AlertException(MessageReactionAddEventArgs e, Exception ex)
		{
			await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = "An error occurred" });
			Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
			await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
		}

		public static async Task AlertException(Exception ex)
		{
			Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
			await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
		}
		public static async Task SendHeartbeatAsync()
		{
			while (true)
			{
				try
				{
					await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
					DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Description = $"Heartbeat received!\n{discord.Ping.ToString()}ms" };
					int ping = discord.Ping;
					embed.WithFooter($"Today at [{System.DateTime.UtcNow.ToShortTimeString()}]");
					if (ping < 200)
					{
						embed.Color = DiscordColor.Green;
					}
					else if (ping < 500)
					{
						embed.Color = DiscordColor.Orange;
					}
					else
					{
						embed.Color = DiscordColor.Red;
					}
					DiscordMessage msghb = null;
					msghb = await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), embed);


					await discord.UpdateStatusAsync(g1);
					Console.WriteLine($"{System.DateTime.UtcNow.ToShortTimeString()} Ping: {discord.Ping}ms ");
					if (LastHb != 0)
					{
						try
						{
							DiscordChannel hbch = await discord.GetChannelAsync(cInf.ErrorHbChannel);
							DiscordMessage hbmsg = await hbch.GetMessageAsync(LastHb);
							await hbmsg.DeleteAsync();
						}
						catch { }
					}
					LastHb = msghb.Id;
					foreach (RegisteredServer e in servers)
					{
						try
						{
							foreach (KeyValuePair<ulong, DateTime> kvp in e.timedoutedusers)
							{
								if (DateTime.Now - kvp.Value >= e.CoolDown)
								{
									servers.Find(x => x.Id == e.Id).timedoutedusers.Remove(kvp.Key);
								}
							}
						}
						catch { }
					}
                    File.WriteAllText("config/RegServers.json", Newtonsoft.Json.JsonConvert.SerializeObject(servers, Formatting.Indented));
				}
				catch (Exception ex)
				{
					await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), $"Failed to heartbeat\n\n{ex.ToString()}");
				}
				await Task.Delay(TimeSpan.FromMinutes(10));
			}
		}
    }
	
}

namespace CAttributes
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
	public class CommandClassAttribute : System.Attribute
	{
		public Classes.CommandClasses Classname { get; set; }
		public CommandClassAttribute(Classes.CommandClasses e)
		{
			Classname = e;
		}
	}
	
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
	public class ModuleAttribute : CheckBaseAttribute
	{
		public Modules module { get; set; }
		public ModuleAttribute(Modules e)
		{
			module = e;
		}
		public override Task<bool> ExecuteCheckAsync(CommandContext e, bool help)
		{
			return Task.FromResult(Sylt51bot.Program.servers.Find(x => x.Id == e.Guild.Id).EnabledModules.HasFlag(module));
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class RequireAuthAttribute : CheckBaseAttribute
	{
		public override Task<bool> ExecuteCheckAsync(CommandContext e, bool help)
		{
			return Task.FromResult(Sylt51bot.Program.cInf.AuthUsers.Contains(e.User.Id));
		}
	}

	[AttributeUsage( AttributeTargets.All, AllowMultiple = false, Inherited = false)]
	public sealed class RequireUserPermissions2Attribute : CheckBaseAttribute
    {
        /// <summary>
        /// Gets the permissions required by this attribute.
        /// </summary>
        public Permissions Permissions { get; }

        /// <summary>
        /// Gets this check's behaviour in DMs. True means the check will always pass in DMs, whereas false means that it will always fail.
        /// </summary>
        public bool IgnoreDms { get; } = true;

        /// <summary>
        /// Defines that usage of this command is restricted to members with specified permissions.
        /// </summary>
        /// <param name="permissions">Permissions required to execute this command.</param>
        /// <param name="ignoreDms">Sets this check's behaviour in DMs. True means the check will always pass in DMs, whereas false means that it will always fail.</param>
        public RequireUserPermissions2Attribute(Permissions permissions, bool ignoreDms = true)
        {
            this.Permissions = permissions;
            this.IgnoreDms = ignoreDms;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
			if(ctx.Command.Name == "help")
			{
				return Task.FromResult(true);
			}
            if (ctx.Guild == null)
                return Task.FromResult(this.IgnoreDms);

            var usr = ctx.Member;
            if (usr == null)
                return Task.FromResult(false);

            if (usr.Id == ctx.Guild.OwnerId)
                return Task.FromResult(true);

            var pusr = ctx.Channel.PermissionsFor(usr);

            if ((pusr & Permissions.Administrator) != 0)
                return Task.FromResult(true);

            return (pusr & this.Permissions) == this.Permissions ? Task.FromResult(true) : Task.FromResult(false);
        }
    }

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class RequireBotPermissions2Attribute : CheckBaseAttribute
    {
        /// <summary>
        /// Gets the permissions required by this attribute.
        /// </summary>
        public Permissions Permissions { get; }

        /// <summary>
        /// Gets this check's behaviour in DMs. True means the check will always pass in DMs, whereas false means that it will always fail.
        /// </summary>
        public bool IgnoreDms { get; } = true;

        /// <summary>
        /// Defines that usage of this command is only possible when the bot is granted a specific permission.
        /// </summary>
        /// <param name="permissions">Permissions required to execute this command.</param>
        /// <param name="ignoreDms">Sets this check's behaviour in DMs. True means the check will always pass in DMs, whereas false means that it will always fail.</param>
        public RequireBotPermissions2Attribute(Permissions permissions, bool ignoreDms = true)
        {
            this.Permissions = permissions;
            this.IgnoreDms = ignoreDms;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
			if(ctx.Command.Name == "help")
			{
				return true;
			}
            if (ctx.Guild == null)
                return this.IgnoreDms;

            var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(false);
            if (bot == null)
                return false;

            if (bot.Id == ctx.Guild.OwnerId)
                return true;

            var pbot = ctx.Channel.PermissionsFor(bot);

            if ((pbot & Permissions.Administrator) != 0)
                return true;

            return (pbot & this.Permissions) == this.Permissions;
        }
    }

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
	public class IsExcludeAttribute : CheckBaseAttribute
	{

		public override Task<bool> ExecuteCheckAsync(CommandContext e, bool help)
		{
			return Task.FromResult(!Sylt51bot.Program.servers.Find(x => x.Id == e.Guild.Id).ServerBlockedUsers.Contains(e.Message.Author.Id) && !Sylt51bot.Program.cInf.GlobalBlockedUsers.Contains(e.Message.Author.Id));
		}
	}
}

namespace Classes
{
	public class SetupInfo
	{
        // Main Info
		public string Token { get; set; }
		public ulong ErrorHbChannel { get; set; }
		public List<string> Prefixes { get; set; }
        // Links
		public string DiscordInvite { get; set; } = "";
		public string GitHub { get; set; } = "";
        public List<ulong> AuthUsers { get; set; } = new List<ulong>();
		public List<ulong> GlobalBlockedUsers { get; set; } = new List<ulong>();
		public string Version = "1.0.1";
	}

	public class RegisteredServer
	{
		public ulong Id { get; set; }
        public Dictionary<ulong, int> xplist { get; set; } = new Dictionary<ulong, int>();
        public Dictionary<ulong, DateTime> timedoutedusers { get; set; } = new Dictionary<ulong, DateTime>();
        public List<LevelRole> lvlroles { get; set; } = new List<LevelRole>();
        public List<ulong> channelxpexclude { get; set; } = new List<ulong>();
		public int MinXp { get; set; } = 10;
		public int MaxXp { get; set; } = 20;
		public TimeSpan CoolDown { get; set; } = TimeSpan.FromMinutes(2);
		public Modules EnabledModules { get; set; } = Modules.None;
		public List<ulong> ServerBlockedUsers { get; set; } = new List<ulong>();
	}

	public class LevelRole
	{
		public string Name { get; set; }
		public ulong RoleId { get; set; }
		public int XpReq { get; set; }
	}

	[Flags]
	public enum Modules
	{
        None = 0x00,
		Levelling = 0b0001,
		Mee6Migration = 0b0010,
		AssignLevelRoles = 0b0011,
        All = 0xFF
	}

	[Flags]
	public enum CommandClasses
	{
		[System.ComponentModel.Description("Config Commands")]
		ConfigCommands = 1,

		[System.ComponentModel.Description("Level Commands")]
		LevelCommands = 2,


		[System.ComponentModel.Description("Other Commands")]
		OtherCommands = 4,

		[System.ComponentModel.Description("Moderator Commands")]
		ModCommands = 8,

		[System.ComponentModel.Description("Bot Owner Commands")]
		OwnerCommands = 16
	}

	public static class EnumExtensions
	{

		// This extension method is broken out so you can use a similar pattern with 
		// other MetaData elements in the future. This is your base method for each.
		public static T GetAttribute<T>(this Enum value) where T : Attribute
		{
			var type = value.GetType();
			var memberInfo = type.GetMember(value.ToString());
			var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);
			return attributes.Length > 0
			  ? (T)attributes[0]
			  : null;
		}

		// This method creates a specific call to the above method, requesting the
		// Description MetaData attribute.
		public static string ToName(this Enum value)
		{
			var attribute = value.GetAttribute<System.ComponentModel.DescriptionAttribute>();
			return attribute == null ? value.ToString() : attribute.Description;
		}

	}
}