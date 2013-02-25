// 
//  Notice.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
// 
//  Copyright (c) 2012 Lars Formella
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//  

using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using XG.Core;
using XG.Server.Util;

using log4net;

namespace XG.Server.Irc
{
	class Notice : AParser
	{
		#region PARSING

		protected override void Parse(Core.Server aServer, string aRawData, string aMessage, string[] aCommands)
		{
			ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType + "(" + aServer.Name + ")");

			string tUserName = aCommands[0].Split('!')[0];

			Bot tBot = aServer.Bot(tUserName);
			if (tBot != null)
			{
				bool isParsed = false;
				Match tMatch;
				Match tMatch1;
				Match tMatch2 ;
				Match tMatch3;
				Match tMatch4;

				int valueInt;
				aMessage = RemoveSpecialIrcChars(aMessage);
				//double valueDouble;

				#region ALL SLOTS FULL / ADDING TO QUEUE

				if (true)
				{
					tMatch1 = Regex.Match(aMessage,
					                      "(" + Magicstring +
					                      " All Slots Full, |)Added you to the main queue (for pack ([0-9]+) \\(\".*\"\\) |).*in positi(o|0)n (?<queue_cur>[0-9]+)\\. To Remove you(r|)self at a later time .*",
					                      RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage,
					                      "Queueing you for pack [0-9]+ \\(.*\\) in slot (?<queue_cur>[0-9]+)/(?<queue_total>[0-9]+)\\. To remove you(r|)self from the queue, type: .*\\. To check your position in the queue, type: .*\\. Estimated time remaining in queue: (?<queue_d>[0-9]+) days, (?<queue_h>[0-9]+) hours, (?<queue_m>[0-9]+) minutes",
					                      RegexOptions.IgnoreCase);
					tMatch3 = Regex.Match(aMessage,
					                      "(" + Magicstring +
					                      " |)Es laufen bereits genug .bertragungen, Du bist jetzt in der Warteschlange f.r Datei [0-9]+ \\(.*\\) in Position (?<queue_cur>[0-9]+)\\. Wenn Du sp.ter Abbrechen willst schreibe .*",
					                      RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success || tMatch3.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2;
						tMatch = tMatch.Success ? tMatch : tMatch3;
						isParsed = true;
						if (tBot.State == Bot.States.Idle)
						{
							tBot.State = Bot.States.Waiting;
						}

						tBot.InfoSlotCurrent = 0;
						if (int.TryParse(tMatch.Groups["queue_cur"].ToString(), out valueInt))
						{
							tBot.QueuePosition = valueInt;
							tBot.InfoQueueCurrent = tBot.QueuePosition;
						}

						if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt))
						{
							tBot.InfoQueueTotal = valueInt;
						}
						else if (tBot.InfoQueueTotal < tBot.InfoQueueCurrent)
						{
							tBot.InfoQueueTotal = tBot.InfoQueueCurrent;
						}

						int time = 0;
						if (int.TryParse(tMatch.Groups["queue_m"].ToString(), out valueInt))
						{
							time += valueInt * 60;
						}
						if (int.TryParse(tMatch.Groups["queue_h"].ToString(), out valueInt))
						{
							time += valueInt * 60 * 60;
						}
						if (int.TryParse(tMatch.Groups["queue_d"].ToString(), out valueInt))
						{
							time += valueInt * 60 * 60 * 24;
						}
						tBot.QueueTime = time;
					}
				}

				#endregion

				#region REMOVE FROM QUEUE

				if (!isParsed)
				{
					tMatch = Regex.Match(aMessage, Magicstring + " Removed From Queue: .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.State == Bot.States.Waiting)
						{
							tBot.State = Bot.States.Idle;
						}
						FireCreateTimer(aServer, tBot, Settings.Instance.CommandWaitTime, false);
					}
				}

				#endregion

				#region INVALID PACKET NUMBER

				if (!isParsed)
				{
					tMatch1 = Regex.Match(aMessage, Magicstring + " Die Nummer der Datei ist ung.ltig", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage, Magicstring + " Invalid Pack Number, Try Again", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						isParsed = true;
						Packet tPack = tBot.OldestActivePacket();
						if (tPack != null)
						{
							// remove all packets with ids beeing greater than the current one because they MUST be missing, too
							var tPackets = from packet in tBot.Packets where packet.Id >= tPack.Id select packet;
							foreach (Packet pack in tPackets)
							{
								pack.Enabled = false;
								pack.Commit();
								tBot.RemovePacket(pack);
							}
						}
						log.Error("Parse() invalid packetnumber from " + tBot);
					}
				}

				#endregion

				#region PACK ALREADY REQUESTED

				if (!isParsed)
				{
					tMatch1 = Regex.Match(aMessage, Magicstring + " You already requested that pack(.*|)", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage, Magicstring + " Du hast diese Datei bereits angefordert(.*|)", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						isParsed = true;
						if (tBot.State == Bot.States.Idle)
						{
							tBot.State = Bot.States.Waiting;
						}
					}
				}

				#endregion

				#region ALREADY QUEUED / RECEIVING

				if (!isParsed)
				{
					tMatch1 = Regex.Match(aMessage, "Denied, You already have ([0-9]+) item(s|) queued, Try Again Later", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage, Magicstring + " All Slots Full, Denied, You already have that item queued\\.", RegexOptions.IgnoreCase);
					tMatch3 = Regex.Match(aMessage, "You are already receiving or are queued for the maximum number of packs .*", RegexOptions.IgnoreCase);
					tMatch4 = Regex.Match(aMessage, "Du hast max\\. ([0-9]+) transfer auf einmal, Du bist jetzt in der Warteschlange f.r Datei .*",
					                      RegexOptions.IgnoreCase);
					Match tMatch5 = Regex.Match(aMessage, "Es laufen bereits genug .bertragungen, abgewiesen, Du hast diese Datei bereits in der Warteschlange\\.",
					                            RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success || tMatch3.Success || tMatch4.Success || tMatch5.Success)
					{
						isParsed = true;
						if (tBot.State == Bot.States.Idle)
						{
							tBot.State = Bot.States.Waiting;
						}
						else if (tBot.State == Bot.States.Waiting)
						{
							// if there is no active packets lets remove us from the queue
							if (tBot.OldestActivePacket() == null)
							{
								FireUnRequestFromBot(aServer, tBot);
							}
						}
					}
				}

				#endregion

				#region DCC PENDING

				if (!isParsed)
				{
					tMatch1 = Regex.Match(aMessage,
					                      Magicstring +
					                      " You have a DCC pending, Set your client to receive the transfer\\. ((Type .*|Send XDCC CANCEL) to abort the transfer\\. |)\\((?<time>[0-9]+) seconds remaining until timeout\\)",
					                      RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage,
					                      Magicstring +
					                      " Du hast eine .bertragung schwebend, Du mu.t den Download jetzt annehmen\\. ((Schreibe .*|Sende XDCC CANCEL)            an den Bot um die .bertragung abzubrechen\\. |)\\((?<time>[0-9]+) Sekunden bis zum Abbruch\\)",
					                      RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2;
						isParsed = true;
						if (int.TryParse(tMatch.Groups["time"].ToString(), out valueInt))
						{
							if (valueInt == 30 && tBot.State != Bot.States.Active)
							{
								tBot.State = Bot.States.Idle;
							}
							FireCreateTimer(aServer, tBot, (valueInt + 2) * 1000, false);
						}
					}
				}

				#endregion

				#region ALL SLOTS AND QUEUE FULL

				if (!isParsed)
				{
					tMatch1 = Regex.Match(aMessage, Magicstring + " All Slots Full, Main queue of size (?<queue_total>[0-9]+) is Full, Try Again Later",
					                      RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage,
					                      Magicstring +
					                      " Es laufen bereits genug .bertragungen, abgewiesen, die Warteschlange ist voll, max\\. (?<queue_total>[0-9]+) Dateien, Versuche es sp.ter nochmal",
					                      RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2;
						isParsed = true;
						if (tBot.State == Bot.States.Waiting)
						{
							tBot.State = Bot.States.Idle;
						}
						tBot.InfoSlotCurrent = 0;
						tBot.InfoQueueCurrent = 0;
						if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt))
						{
							tBot.InfoQueueTotal = valueInt;
						}

						FireCreateTimer(aServer, tBot, Settings.Instance.BotWaitTime, false);
					}
				}

				#endregion

				#region TRANSFER LIMIT

				if (!isParsed)
				{
					tMatch = Regex.Match(aMessage, Magicstring + " You can only have ([0-9]+) transfer(s|) at a time,.*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.State == Bot.States.Idle)
						{
							tBot.State = Bot.States.Waiting;
						}
					}
				}

				#endregion

				#region OWNER REQUEST

				if (!isParsed)
				{
					tMatch = Regex.Match(aMessage, Magicstring + " The Owner Has Requested That No New Connections Are Made In The Next (?<time>[0-9]+) Minute(s|)",
					                     RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.State == Bot.States.Waiting)
						{
							tBot.State = Bot.States.Idle;
						}

						if (int.TryParse(tMatch.Groups["time"].ToString(), out valueInt))
						{
							FireCreateTimer(aServer, tBot, (valueInt * 60 + 1) * 1000, false);
						}
					}
				}

				#endregion

				#region XDCC DOWN

				if (!isParsed)
				{
					tMatch = Regex.Match(aMessage, "The XDCC is down, try again later.*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.State == Bot.States.Waiting)
						{
							tBot.State = Bot.States.Idle;
						}
						FireCreateTimer(aServer, tBot, Settings.Instance.BotWaitTime, false);
					}
				}

				#endregion

				#region XDCC DENIED

				if (!isParsed)
				{
					tMatch = Regex.Match(aMessage, Magicstring + " XDCC SEND denied, (?<info>.*)", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						string info = tMatch.Groups["info"].ToString().ToLower();
						if (info.StartsWith("you must be on a known channel to request a pack"))
						{
							FireSendData(aServer, "WHOIS " + tBot.Name);
						}
						else if (info.StartsWith("i don't send transfers to"))
						{
							foreach (Packet tPacket in tBot.Packets)
							{
								if (tPacket.Enabled)
								{
									tPacket.Enabled = false;
									tPacket.Commit();
								}
							}
						}
						else
						{
							if (tBot.State == Bot.States.Waiting)
							{
								tBot.State = Bot.States.Idle;
							}
							FireCreateTimer(aServer, tBot, Settings.Instance.CommandWaitTime, false);
							log.Error("Parse() XDCC denied from " + tBot + ": " + info);
						}
					}
				}

				#endregion

				#region XDCC SENDING

				if (!isParsed)
				{
					tMatch1 = Regex.Match(aMessage, Magicstring + " Sending You (Your Queued |)Pack .*", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage, Magicstring + " Sende dir jetzt die Datei .*", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						isParsed = true;
						if (tBot.State == Bot.States.Waiting)
						{
							tBot.State = Bot.States.Idle;
						}
					}
				}

				#endregion

				#region QUEUED

				if (!isParsed)
				{
					tMatch1 = Regex.Match(aMessage,
					                      "Queued ([0-9]+)h([0-9]+)m for .*, in position (?<queue_cur>[0-9]+) of (?<queue_total>[0-9]+). (?<queue_h>[0-9]+)h(?<queue_m>[0-9]+)m or .* remaining\\.",
					                      RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage,
					                      "In der Warteschlange seit  ([0-9]+)h([0-9]+)m f.r .*, in Position (?<queue_cur>[0-9]+) von (?<queue_total>[0-9]+). Ungef.hr (?<queue_h>[0-9]+)h(?<queue_m>[0-9]+)m oder .*",
					                      RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2;
						isParsed = true;
						if (tBot.State == Bot.States.Idle)
						{
							tBot.State = Bot.States.Waiting;
						}

						tBot.InfoSlotCurrent = 0;
						if (int.TryParse(tMatch.Groups["queue_cur"].ToString(), out valueInt))
						{
							tBot.QueuePosition = valueInt;
						}
						if (int.TryParse(tMatch.Groups["queue_total"].ToString(), out valueInt))
						{
							tBot.InfoQueueTotal = valueInt;
						}
						else if (tBot.InfoQueueTotal < tBot.QueuePosition)
						{
							tBot.InfoQueueTotal = tBot.QueuePosition;
						}

						int time = 0;
						if (int.TryParse(tMatch.Groups["queue_m"].ToString(), out valueInt))
						{
							time += valueInt * 60;
						}
						if (int.TryParse(tMatch.Groups["queue_h"].ToString(), out valueInt))
						{
							time += valueInt * 60 * 60;
						}
						tBot.QueueTime = time;
					}
				}

				#endregion

				#region CLOSING CONNECTION

				if (!isParsed)
				{
					// Closing Connection You Must JOIN MG-CHAT As Well To Download - Your Download Will Be Canceled Now
					tMatch1 = Regex.Match(aMessage, Magicstring + " (Closing Connection|Transfer Completed)(?<reason>.*)", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage, Magicstring + " (Schlie.e Verbindung)(?<reason>.*)", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success)
					{
						isParsed = true;
						if (tBot.State != Bot.States.Active)
						{
							tBot.State = Bot.States.Idle;
						}
						else
						{
							// kill that connection if the bot sends a close message , but our real bot 
							// connection is still alive and hangs for some crapy reason - maybe because 
							// some admins do some network fu to stop my downloads (happend to me)
							FireRemoveDownload(tBot);
						}
						FireCreateTimer(aServer, tBot, Settings.Instance.CommandWaitTime, false);

						tMatch3 = Regex.Match(aMessage, @".*\s+JOIN (?<channel>[^\s]+).*", RegexOptions.IgnoreCase);
						if (tMatch3.Success && Settings.Instance.AutoJoinOnInvite)
						{
							string channel = tMatch3.Groups["channel"].ToString();
							if (!channel.StartsWith("#"))
							{
								channel = "#" + channel;
							}
							// ok, lets do a silent auto join
							log.Info("Parse() auto joining " + channel);
							FireSendData(aServer, "JOIN " + channel);
						}
					}
				}

				#endregion

				#region YOU ARE NOT IN QUEUE

				if (!isParsed)
				{
					tMatch = Regex.Match(aMessage, "(You Don't Appear To Be In A Queue|Removed you from the queue for.*)", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
						if (tBot.State == Bot.States.Waiting)
						{
							tBot.State = Bot.States.Idle;
						}
						tBot.QueuePosition = 0;
						FireCreateTimer(aServer, tBot, Settings.Instance.CommandWaitTime, false);
					}
				}

				#endregion

				#region PUNISH / AUTO IGNORE

				if (!isParsed)
				{
					tMatch1 = Regex.Match(aMessage, "Punish-ignore activated for .* \\(.*\\) (?<time_m>[0-9]*) minutes", RegexOptions.IgnoreCase);
					tMatch2 = Regex.Match(aMessage,
					                      "Auto-ignore activated for .* lasting (?<time_m>[0-9]*)m(?<time_s>[0-9]*)s\\. Further messages will increase duration\\.",
					                      RegexOptions.IgnoreCase);
					tMatch3 = Regex.Match(aMessage, "Zur Strafe wirst du .* \\(.*\\) f.r (?<time_m>[0-9]*) Minuten ignoriert(.|)", RegexOptions.IgnoreCase);
					tMatch4 = Regex.Match(aMessage, "Auto-ignore activated for .* \\(.*\\)", RegexOptions.IgnoreCase);
					if (tMatch1.Success || tMatch2.Success || tMatch3.Success || tMatch4.Success)
					{
						tMatch = tMatch1.Success ? tMatch1 : tMatch2.Success ? tMatch2 : tMatch3.Success ? tMatch3 : tMatch4;
						isParsed = true;
						if (tBot.State == Bot.States.Waiting)
						{
							tBot.State = Bot.States.Idle;
						}

						if (int.TryParse(tMatch.Groups["time_m"].ToString(), out valueInt))
						{
							int time = valueInt * 60 + 1;
							if (int.TryParse(tMatch.Groups["time_s"].ToString(), out valueInt))
							{
								time += valueInt;
							}
							FireCreateTimer(aServer, tBot, time * 1000, true);
						}
					}
				}

				#endregion

				#region NOT NEEDED INFOS

				if (!isParsed)
				{
					tMatch = Regex.Match(aMessage, ".* bandwidth limit .*", RegexOptions.IgnoreCase);
					if (tMatch.Success)
					{
						isParsed = true;
					}
				}

				#endregion

				if (!isParsed)
				{
					FireParsingError("[DCC Notice] " + tBot + " : " + aMessage);
				}
				else
				{
					tBot.LastMessage = aMessage;
					log.Info("Parse() message from " + tBot + ": " + aMessage);
				}

				tBot.Commit();
			}
		}

		#endregion
	}
}
