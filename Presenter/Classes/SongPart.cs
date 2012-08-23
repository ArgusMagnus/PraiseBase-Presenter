﻿/*
 *   PraiseBase Presenter
 *   The open source lyrics and image projection software for churches
 *
 *   http://code.google.com/p/praisebasepresenter
 *
 *   This program is free software; you can redistribute it and/or
 *   modify it under the terms of the GNU General Public License
 *   as published by the Free Software Foundation; either version 2
 *   of the License, or (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program; if not, write to the Free Software
 *   Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 *
 *   Author:
 *      Nicolas Perrenoud <nicu_at_lavine.ch>
 *   Co-authors:
 *      ...
 *
 */

namespace Pbp
{
    /// <summary>
    /// A song part with a given name and one or more slides
    /// </summary>
    public class SongPart
    {
        /// <summary>
        /// Part constructor
        /// </summary>
        public SongPart()
            : this("Neuer Liedteil")
        {
        }

        /// <summary>
        /// Part constructor
        /// </summary>
        /// <param name="caption">The part's caption</param>
        public SongPart(string caption)
        {
            Slides = new SongSlideList();
            Caption = caption;
        }

        /// <summary>
        /// Song part name like chorus, bridge, part 1 ...
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// A list of containing slides. Each part has one slide at minimum
        /// </summary>
        public SongSlideList Slides { get; set; }

        public override int GetHashCode()
        {
            return Caption.GetHashCode() ^ Slides.GetHashCode();
        }
    }
}