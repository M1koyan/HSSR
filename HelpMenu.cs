using System;
using Classes;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.CommandsNext.Converters;
using static HSSR.Program;
using CAttributes;
namespace HSSR
{
	public class CustomHelpFormatter : BaseHelpFormatter
	{
		protected DiscordEmbedBuilder _embed;
		// protected StringBuilder _strBuilder;

		public CustomHelpFormatter(CommandContext ctx) : base(ctx)
		{
			_embed = new DiscordEmbedBuilder();
			// _strBuilder = new StringBuilder();

			// Help formatters do support dependency injection.
			// Any required services can be specified by declaring constructor parameters. 

			// Other required initialization here ...
		}

		public override BaseHelpFormatter WithCommand(Command command)
		{
			// _strBuilder.AppendLine($"{command.Name} - {command.Description}");
			_embed.Title = "Help";
			_embed.Color = DiscordColor.Green;
			if (string.IsNullOrEmpty(command.Description))
			{
				_embed.AddField(command.Name, "No description");
			}
			else
			{
				_embed.AddField(command.Name, command.Description);
			}
			if (command.Aliases.Count != 0)
			{
				string alstring = "";
				foreach (var alias in command.Aliases)
				{
					alstring += $"{alias} ";
				}
				_embed.AddField("Aliases", alstring);
			}
			var p = command.ExecutionChecks.ToList();
			string permstr = "";
			string permstr2 = "";

			if(command.ExecutionChecks.ToList().FindIndex(x => x.GetType() == typeof(CAttributes.RequireBotPermissions2Attribute)) != -1)
			{
				permstr = $"**Your additional permissions:** ```{((CAttributes.RequireBotPermissions2Attribute)command.ExecutionChecks.ToList().Find(x => x.GetType() == typeof(CAttributes.RequireBotPermissions2Attribute))).Permissions}```\n";
			}

			if (command.ExecutionChecks.ToList().FindIndex(x => x.GetType() == typeof(CAttributes.RequireUserPermissions2Attribute)) != -1)
			{
				permstr = $"**My additional permissions:** ```{((CAttributes.RequireUserPermissions2Attribute)command.ExecutionChecks.ToList().Find(x => x.GetType() == typeof(CAttributes.RequireUserPermissions2Attribute))).Permissions}```\n";
			}

			if (permstr != "" || permstr2 != "")
			{
				_embed.AddField("Permissions", permstr + permstr2);
			}
			_embed.WithFooter("<> are mandatory arguments, [] are optional arguments");
			return this;
		}

		public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
		{
			_embed.Title = "Help";
			_embed.Description = $"More information with `{cInf.Prefixes[0]}help <commandname>`";
			_embed.Color = DiscordColor.Green;

			foreach (var cclass in Enum.GetValues(typeof(Classes.CommandClasses)))
			{
				string cmdinmod = "|";
				List<string> e = new List<string>();
				foreach(Command cmd in cmds)
				{
					if(cmd.CustomAttributes.ToList().FindIndex(x => x.GetType() == typeof(CommandClassAttribute)) == -1)
					{
						continue;
					}
					if(((CommandClassAttribute)cmd.CustomAttributes.ToList().Find(x => x.GetType() == typeof(CommandClassAttribute))).Classname.HasFlag((Enum)cclass))
					{
						e.Add(cmd.Name);
					}
				}
				e = (from entry in e orderby (short)entry[0] ascending select entry).ToList<string>();

				foreach(string i in e)
				{
					cmdinmod += $" `{i}` |";
				}

				if(cmdinmod == "|")
				{
					continue;
				}

				_embed.AddField(((Enum)cclass).ToName(), cmdinmod, true);
			}
			
			_embed.AddField("Useful links", $"[Discord]({cInf.DiscordInvite}) | [GitHub]({cInf.GitHub}) | [Bot Invite](https://discord.com/oauth2/authorize?client_id={discord.CurrentUser.Id}&scope=bot&permissions=805317632)", false);
			_embed.WithFooter($"Version {cInf.Version}");
			return this;
		}
		public override CommandHelpMessage Build()
		{
			return new CommandHelpMessage(embed: _embed);
			// return new CommandHelpMessage(content: _strBuilder.ToString());
		}
	}
}
