﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using F23.StringSimilarity;
using Microsoft.Extensions.Configuration;
using TioSharp;

namespace RunIt.Modules
{
	[Name("Coding"), Summary("Code and compile your program.")]
	public class CodingModule : ModuleBase<SocketCommandContext>
	{
		private static readonly TioApi Compiler = new TioApi();
		private readonly IConfigurationRoot _config;

		// Common identifiers, also used in highlight.js and thus discord code blocks
		private readonly Dictionary<string, string> _quickMap = new Dictionary<string, string>  {
			{
				"asm",
				"assembly"},
			{
				"c#",
				"cs"},
			{
				"c++",
				"cpp"},
			{
				"csharp",
				"cs"},
			{
				"f#",
				"fs"},
			{
				"fsharp",
				"fs"},
			{
				"js",
				"javascript"},
			{
				"nimrod",
				"nim"},
			{
				"py",
				"python"},
			{
				"q#",
				"qs"},
			{
				"rs",
				"rust"},
			{
				"sh",
				"bash"}
		};

		public CodingModule(IConfigurationRoot config)
		{
			_config = config;
		}

		[Command("run"), Summary(@"Run the specified code.
__Usage__
|>run [--stats]
\`\`\`<language>
<code>
\`\`\`
[`input <input>`...]
[`compiler-flags <flag>`...]
[`command-line-options <option>`...]
[`args <arg>`...]")]
		public async Task RunCode([Remainder][Summary("The message to parse.")] string message)
		{
			string[] args = message.Split("```");

			string language = await new StringReader(args[1]).ReadLineAsync();
			string code = string.Join(Environment.NewLine, args[1].Split(Environment.NewLine.ToCharArray())
				.Skip(1)
				.ToArray());

			List<string> inputs = new List<string>();
			List<string> compilerFlags = new List<string>();
			List<string> commandLineOptions = new List<string>();
			List<string> arguments = new List<string>();

			foreach (string line in args[2].Split(Environment.NewLine.ToCharArray()).Where(s => !string.IsNullOrWhiteSpace(s)))
			{
				if (line.StartsWith("input "))
				{
					inputs.Add(string.Join(' ', line.Split(' ')[1..]).Trim('`'));
				}
				else if (line.StartsWith("compiler-flags "))
				{
					compilerFlags.AddRange(line[15..].Trim('`').Split(' '));
				}
				else if (line.StartsWith("command-line-options "))
				{
					commandLineOptions.AddRange(line[21..].Trim('`').Split(' '));
				}
				else if (line.StartsWith("args "))
				{
					arguments.AddRange(line[10..].Trim('`').Split(' '));
				}
			}

			// Match language to available languages
			if (_quickMap.ContainsKey(language!))
			{
				language = _quickMap[language];
			}
			if (_config.GetSection(language).Exists())
			{
				language = _config[language];
			}
			else if (Compiler.Languages.All(l => l != language))
			{
				var l = new JaroWinkler();
				// Get first 10 matches with >89% similarity
				string matches = string.Join('\n',
					Compiler.Languages.Where(c => l.Similarity(language, c) > .89).Take(10));
				Console.WriteLine(args[0]);
				string matchesReply = $"`{language}` not available.\n\n";
				if (!string.IsNullOrWhiteSpace(matches))
				{
					matchesReply += $"**Did you mean:**\n{matches}";
				}

				await ReplyAsync(matchesReply);
				return;
			}

			// Create and send response
			byte[] requestData = Compiler.CreateRequestData(language, code, inputs.ToArray(), compilerFlags.ToArray(), arguments.ToArray());
			string response = await Compiler.SendAsync(requestData);

			string result;
			//check if --stats flag is passed
			if (args[0].Contains("--stats"))
			{
				result = $"```\n{response}\n```";
			}
			else
			{
				// Parse Response
				string[] lines = response.Split(Environment.NewLine.ToCharArray());
				string output = string.Join(Environment.NewLine, lines
					.SkipLast(5)) + '\n' + lines.TakeLast(1).ElementAt(0);
				result = $"```{language}\n{output}\n```";
			}

			await ReplyAsync(result);
		}
	}
}