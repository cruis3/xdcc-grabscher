// 
//  Server.cs
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace XG.Core
{
	[Serializable]
	public class Server : AObjects
	{
		#region VARIABLES

		public override bool Connected
		{
			get { return base.Connected; }
			set
			{
				if (!value)
				{
					foreach (AObject obj in All)
					{
						obj.Connected = false;
						obj.Commit();
					}
				}
				base.Connected = value;
			}
		}

		public new Servers Parent
		{
			get { return base.Parent as Servers; }
			set { base.Parent = value; }
		}

		int _port;

		public int Port
		{
			get { return _port; }
			set { SetProperty(ref _port, value, "Port"); }
		}

		SocketErrorCode _errorCode = SocketErrorCode.None;

		public SocketErrorCode ErrorCode
		{
			get { return _errorCode; }
			set { SetProperty(ref _errorCode, value, "ErrorCode"); }
		}

		#endregion
		
		#region CHILDREN

		public IEnumerable<Channel> Channels
		{
			get { return All.Cast<Channel>(); }
		}

		public Channel Channel(string aName)
		{
			if (!aName.StartsWith("#"))
			{
				aName = "#" + aName;
			}
			return base.Named(aName) as Channel;
		}

		public Bot Bot(string aName)
		{
			Bot tBot = null;
			foreach (Channel chan in Channels)
			{
				tBot = chan.Bot(aName);
				if (tBot != null)
				{
					break;
				}
			}
			return tBot;
		}

		public bool AddChannel(Channel aChannel)
		{
			return Add(aChannel);
		}

		public bool AddChannel(string aChannel)
		{
			aChannel = aChannel.Trim().ToLower();
			if (!aChannel.StartsWith("#"))
			{
				aChannel = "#" + aChannel;
			}
			if (Channel(aChannel) == null)
			{
				var tChannel = new Channel {Name = aChannel, Enabled = Enabled};
				return AddChannel(tChannel);
			}
			return false;
		}

		public bool RemoveChannel(Channel aChannel)
		{
			return Remove(aChannel);
		}

		public override bool DuplicateChildExists(AObject aObject)
		{
			return Channel((aObject as Channel).Name) != null;
		}

		#endregion
	}
}
