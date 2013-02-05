//
//  resize.js
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

var XGResize = (function()
{
	var width, height;

	/**
	 * @param innerLayout
	 */
	function resizeMain (innerLayout)
	{
		var searchLayout = $('#searchLayout');
		var objectLayout = $('#layoutObjects');

		/* left search tab */
		// set table
		$("#searchGrid")
			.height(searchLayout.height() - 28);
		// patch search input
		$("#searchText").width(searchLayout.width() - 8);

		/* main container */
		var subSize = 68;
		if ($.browser.mozilla)
		{
			subSize -= 2;
		}
		$("#layoutObjectsContainer").height(searchLayout.height() - subSize);
		// bots + packets table
		$("#botGrid, #packetGrid").width(objectLayout.width() - 1);

		/* other container */
		subSize = 48;
		if ($.browser.mozilla)
		{
			subSize += 1;
		}
		$("#externalGrid, #fileGrid")
			.width(objectLayout.width() - 1)
			.height(searchLayout.height() - subSize);

		/* dialog */
		var width1 = $(window).width() - 20;
		var height1 = $(window).height() - 20;
		// just resize if necessary
		if (width1 != width || height1 != height)
		{
			width = width1;
			height = height1;

			$("#dialogSnapshots").dialog("option", {
				width: width1,
				height: height1
			});
			$("#snapshot")
				.width(width1 - 240)
				.height(height1 - 40);
		}

		innerLayout.resizeAll();

		self.onResize.notify({}, null, self);
	}

	function resizeContainer ()
	{
		$("#botGrid").height($('#botLayout').height());
		$("#packetGrid").height($('#packetLayout').height());

		self.onResize.notify({}, null, self);
	}

	var self = {
		onResize: new Slick.Event(),

		start: function ()
		{
			$("body").layout({
				onresize: function () {
					resizeMain(innerLayout);
				}
			});
			var innerLayout = $("#layoutObjectsContainer").layout({
				resizeWithWindow: false,
				onresize: function () {
					resizeContainer();
				}
			});

			// make bot and packet grid height almost equal
			innerLayout.sizePane("north", $('#layoutObjects').height() / 2 - 80);

			// resize after all is visible - twice, because the first run wont change all values :|
			resizeMain(innerLayout);
			setTimeout(function() { resizeMain(innerLayout); }, 1000);
		}
	};
	return self;
}());
